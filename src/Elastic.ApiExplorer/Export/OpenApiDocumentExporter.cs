// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Inference;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Contract;
using Elastic.Documentation.Versions;
using Microsoft.AspNetCore.Html;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Elastic.ApiExplorer.Export;

/// <summary>
/// Converts the OpenAPI specifications the API explorer renders into DocumentationDocument instances.
/// </summary>
public partial class OpenApiDocumentExporter(VersionsConfiguration versionsConfiguration, IDocumentInferrerService? documentInferrer = null)
{
	[GeneratedRegex(@"\*\*[^*\n]*methods? and paths? for this operation:\*\*", RegexOptions.IgnoreCase)]
	private static partial Regex OperationListIntroRegex();

	[GeneratedRegex("""<span class="operation-path">(?<path>[^<]*)</span>""", RegexOptions.IgnoreCase)]
	private static partial Regex OperationPathSpanRegex();

	/// <summary>
	/// Converts every version tree of every API the generators render, one document per operation and distinct rendering
	/// (<see cref="ApiTreeCollapse"/>). When two generators declare the same API key, the first one owns all its trees.
	/// </summary>
	public async IAsyncEnumerable<DocumentationDocument> ExportDocuments(
		IEnumerable<OpenApiGenerator> generators,
		[EnumeratorCancellation] Cancel ctx = default
	)
	{
		var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var generator in generators)
		{
			var trees = await generator.ResolveIndexedTrees(ctx).ConfigureAwait(false);
			foreach (var api in trees.GroupBy(static t => t.ApiKey, StringComparer.OrdinalIgnoreCase))
			{
				if (!seenKeys.Add(api.Key))
					continue;

				var defaultTree = ApiVersions.DefaultTree(api.Select(static t => t.Moniker))!;
				foreach (var doc in ApiTreeCollapse.Collapse(api.SelectMany(ConvertToDocuments), defaultTree))
					yield return doc;
			}
		}
	}

	/// <summary>
	/// Converts one version tree of an API to one document per operation, with URLs into that tree. The API key builds
	/// URLs and labels; the product id drives product metadata.
	/// </summary>
	public IEnumerable<DocumentationDocument> ConvertToDocuments(IndexedTree tree)
	{
		var (product, productId, moniker, openApiDocument) = tree;
		var productUrl = ApiUrlBuilder.ProductRoot("/docs", ApiUrlBuilder.ProductSuffix(product, moniker));
		// All API products currently version against Stack
		var currentStack = versionsConfiguration.GetVersioningSystem(VersioningSystemId.Stack).Current;
		var tagMetadata = OpenApiExtensionReader.ParseTagMetadata(openApiDocument);

		var plainDescriptions = new Dictionary<string, string>();
		foreach (var path in openApiDocument.Paths)
		{
			if (path.Value.Operations == null)
				continue;

			foreach (var operation in path.Value.Operations)
			{
				var operationId = operation.Value.OperationId ?? GenerateOperationId(operation.Key, path.Key);

				// Skip APIs introduced in a Stack version that hasn't shipped yet
				var applies = XStateApplicability.From(operation.Value.Extensions);
				if (XStateApplicability.IsUnreleased(applies, currentStack))
					continue;

				var operationMoniker = ApiUrlBuilder.OperationMoniker(operationId, path.Key);
				var url = $"{productUrl}/operation/{operationMoniker}";

				var productLabel = ProductApiLabel(product);
				// Trim: spec summaries occasionally carry stray leading/trailing whitespace or a
				// trailing newline, which would otherwise flow verbatim into the indexed title.
				var summary = operation.Value.Summary?.Trim();
				var title = string.IsNullOrEmpty(summary) ? operationId : summary;
				var method = operation.Key.ToString().ToUpperInvariant();
				var description = IndexedHitText(summary, operation.Value.Description, operationId);

				// Build body content from operation details
				var bodyBuilder = new StringBuilder();
				_ = bodyBuilder.AppendLine($"# {title}");
				_ = bodyBuilder.AppendLine();

				if (!string.IsNullOrEmpty(description))
				{
					_ = bodyBuilder.AppendLine(description);
					_ = bodyBuilder.AppendLine();
				}

				_ = bodyBuilder.AppendLine($"**Method:** {operation.Key.ToString().ToUpperInvariant()}");
				_ = bodyBuilder.AppendLine($"**Path:** {path.Key}");
				_ = bodyBuilder.AppendLine();

				// Add parameters if any
				if (operation.Value.Parameters?.Count > 0)
				{
					_ = bodyBuilder.AppendLine("## Parameters");
					foreach (var param in operation.Value.Parameters)
						_ = bodyBuilder.AppendLine($"- **{param.Name}** ({param.In}): {param.Description}");
					_ = bodyBuilder.AppendLine();
				}

				var body = bodyBuilder.ToString();
				var apiOperation = new ApiOperation(operation.Key, operation.Value, path.Key, path.Value, product);
				var builder = new ApiPropertyTreeBuilder(
					openApiDocument,
					new PropertyDisplayOptions
					{
						// Listing identity keys on the markdown and the index stores plain text, so nothing here needs the HTML.
						RenderMarkdown = _ => HtmlString.Empty,
						ApiRootUrl = productUrl,
						VersionsConfiguration = versionsConfiguration
					}
				);
				var endpoint = OperationEndpoint.Build(apiOperation, operation.Value.Description, []);
				var trees = OperationPageModel.BuildPropertyTrees(
					apiOperation,
					new SchemaAnalyzer(openApiDocument),
					new ParameterRowSource(builder, null, endpoint)
				);

				var headings = operation.Value.Tags?.Select(OpenApiExtensionReader.TagName).OfType<string>().ToArray() ?? [];

				// Infer product and repository metadata
				var inference = documentInferrer?.InferForOpenApi(productId);

				var groupParent = GroupParent(headings, tagMetadata, productUrl);
				ParentDocument[] parents =
				[
					new ParentDocument { Title = "API", Path = "/docs/api" },
					new ParentDocument { Title = productLabel, Path = productUrl }
				];

				var endpointKeys = ApiEndpointPath.IndexKeys([path.Key, .. AlternatePaths(operation.Value.Description)]);
				var api = new ApiOperationMetadata
				{
					Key = product,
					Trees = [new ApiTreeEntry { Tree = moniker, Root = productUrl }],
					Properties = trees.IndexEntries(plainDescriptions),
					Routes = endpoint
						.Rows
						.Select(
							row => new ApiRouteEntry
							{
								Path = row.Route,
								Methods = [row.Method.ToUpperInvariant(), .. row.AlsoMethods.Select(m => m.ToUpperInvariant())]
							}
						)
						.ToArray(),
					Endpoints = endpointKeys,
					EndpointsBare = ApiEndpointPath.BareKeys(endpointKeys),
					Method = method,
					Path = path.Key,
					OperationId = operationId,
					Deprecated = operation.Value.Deprecated,
					Words = ApiSearchWords.Of(operationId, path.Key),
					Group = groupParent?.Title ?? "",
					IdWords = ApiSearchWords.OfOperationId(operationId).Length
				};

				yield return new DocumentationDocument
				{
					ContentType = "api",
					Path = url,
					Title = title,
					SearchTitle = BuildSearchTitle(title, productLabel, groupParent?.Title, api),
					Description = description,
					Body = body,
					Headings = headings,
					Links = [],
					Applies = applies?.ToAppliesTo(),
					Parents = groupParent is null ? parents : [.. parents, groupParent],
					Api = api,
					Product = inference?.Product?.Id,
					RelatedProducts = inference?.RelatedProducts.Count > 0
						? inference
							.RelatedProducts
							.Select(p => new IndexedProduct { Id = p.Id, Repository = p.Repository ?? inference.Repository })
							.ToArray()
						: null
				};
			}
		}
	}

	/// <summary>
	/// The breadcrumb entry for the operation's first tag, matching the group page the operation page links to;
	/// null when untagged.
	/// </summary>
	private static ParentDocument? GroupParent(
		string[] tags,
		IReadOnlyDictionary<string, OpenApiTagMetadata> tagMetadata,
		string productUrl
	) =>
		tags is [var tag, ..]
			? new ParentDocument
			{
				Title = OpenApiExtensionReader.TagMetadataFor(tag, tagMetadata).DisplayName,
				Path = $"{productUrl}/group/{ApiUrlBuilder.TagMoniker(tag)}"
			}
			: null;

	/// <summary>
	/// Display label for the product crumb, e.g. <c>elasticsearch</c> → <c>Elasticsearch API</c>.
	/// </summary>
	internal static string ProductApiLabel(string product) =>
		$"{CultureInfo.InvariantCulture.TextInfo.ToTitleCase(product.Replace('-', ' '))} API";

	/// <summary>
	/// Search tokens users type for an operation: the title, the product label, the raw operation id
	/// (e.g. <c>_bulk</c>, <c>indices.get</c>), the HTTP method plus path (e.g. <c>PUT /_bulk</c>),
	/// the group, and the plain words of the id and path (<see cref="ApiSearchWords"/>). The raw id
	/// and path stay verbatim because that is what people paste; the words serve people who type
	/// <c>ml flush</c>. The product label lives here rather than in <c>Title</c>, so result rows do
	/// not repeat "Elasticsearch API" on every hit.
	/// </summary>
	internal static string BuildSearchTitle(string title, string productLabel, string? group, ApiOperationMetadata api)
	{
		var searchTitle = $"{title} - {productLabel} - {api.OperationId} - {api.Method} {api.Path}";
		if (!string.IsNullOrWhiteSpace(group))
			searchTitle += $" - {group}";
		var words = ApiSearchWords.Of(api.OperationId, api.Path);
		return words.Length == 0 ? searchTitle : $"{searchTitle} - {string.Join(' ', words)}";
	}

	/// <summary>
	/// Search snippet for an operation. Prefer the spec prose, then the summary, then the
	/// operation id so a hit is never blank. The generated "method and path" list is omitted
	/// because it is the same sentence on every operation.
	/// </summary>
	private static string IndexedHitText(string? summary, string? rawDescription, string operationId)
	{
		var prose = ApiMarkdown.StripHtml(RemoveOperationListIntro(rawDescription));
		if (!string.IsNullOrWhiteSpace(prose))
			return prose;
		if (!string.IsNullOrWhiteSpace(summary))
			return summary;
		return operationId;
	}

	private static string RemoveOperationListIntro(string? description)
	{
		if (string.IsNullOrEmpty(description))
			return string.Empty;

		var match = OperationListIntroRegex().Match(description);
		if (!match.Success)
			return description;

		var end = EndOfOperationList(description, match.Index + match.Length);
		var before = description[..match.Index].Trim();
		var after = end < description.Length ? description[end..].Trim() : string.Empty;
		if (before.Length == 0)
			return after;
		if (after.Length == 0)
			return before;
		return $"{before}\n\n{after}";
	}

	/// <summary>
	/// Paths listed in the description's "All methods and paths for this operation" block, verbatim.
	/// Locates the block exactly as <see cref="RemoveOperationListIntro"/> does.
	/// </summary>
	internal static IEnumerable<string> AlternatePaths(string? description)
	{
		if (string.IsNullOrEmpty(description))
			return [];

		var match = OperationListIntroRegex().Match(description);
		if (!match.Success)
			return [];

		var start = match.Index + match.Length;
		var block = description[start..EndOfOperationList(description, start)];
		return OperationPathSpanRegex().Matches(block).Select(m => m.Groups["path"].Value.Trim());
	}

	private static int EndOfOperationList(string description, int afterHeader)
	{
		var htmlStart = description.IndexOf("<div", afterHeader, StringComparison.OrdinalIgnoreCase);
		if (htmlStart < 0)
			return afterHeader;

		var cursor = htmlStart;
		while (true)
		{
			var close = description.IndexOf("</div>", cursor, StringComparison.OrdinalIgnoreCase);
			if (close < 0)
				return afterHeader;

			var afterClose = close + "</div>".Length;
			var next = afterClose;
			while (next < description.Length && char.IsWhiteSpace(description[next]))
				next++;

			if (next >= description.Length || !description.AsSpan(next).StartsWith("<div", StringComparison.OrdinalIgnoreCase))
				return afterClose;

			cursor = afterClose;
		}
	}

	/// <summary>
	/// Generates an operation ID from method and path when one is not provided.
	/// </summary>
	private static string GenerateOperationId(HttpMethod method, string path)
	{
		var cleanPath = path.TrimStart('/').Replace('/', '-').Replace('{', '-').Replace('}', '-');
		return $"{method.ToString().ToLowerInvariant()}-{cleanPath}";
	}
}
