// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.Documentation;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Inference;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Contract;
using Elastic.Documentation.Versions;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Export;

/// <summary>
/// Converts OpenAPI operations into search documents from the version-index catalog.
/// </summary>
public partial class OpenApiDocumentExporter(
	VersionsConfiguration versionsConfiguration,
	IDocumentInferrerService? documentInferrer = null,
	VersionIndexClient? versionIndexClient = null,
	IOpenApiSpecificationReader? openApiReader = null,
	IDiagnosticsCollector? collector = null)
{
	private readonly IOpenApiSpecificationReader _openApiReader = openApiReader ?? OpenApiReader.Instance;
	private readonly IDiagnosticsCollector _collector = collector ?? new DiagnosticsCollector([]);

	[GeneratedRegex(@"Added in (\d+\.\d+\.\d+)", RegexOptions.IgnoreCase)]
	private static partial Regex AddedInVersionRegex();

	[GeneratedRegex(@"<span class=""operation-verb (\w+)"">(\w+)</span>\s*<span class=""operation-path"">([^<]+)</span>", RegexOptions.IgnoreCase)]
	private static partial Regex OperationVerbPathRegex();

	/// <summary>
	/// Resolves every version of each configured API from the version index and converts
	/// operations to search documents. Used when assembler-api-explorer is enabled.
	/// </summary>
	public async IAsyncEnumerable<DocumentationDocument> ExportDocuments(
		IReadOnlyList<OpenApiExportSource> sources,
		[EnumeratorCancellation] Cancel ctx = default)
	{
		VersionIndexClient? ownedClient = null;
		var client = versionIndexClient ?? (ownedClient = new VersionIndexClient());
		try
		{
			foreach (var source in sources)
			{
				await foreach (var doc in ExportSource(client, source, ctx).ConfigureAwait(false))
					yield return doc;
			}
		}
		finally
		{
			ownedClient?.Dispose();
		}
	}

	private async IAsyncEnumerable<DocumentationDocument> ExportSource(
		VersionIndexClient client,
		OpenApiExportSource source,
		[EnumeratorCancellation] Cancel ctx)
	{
		var versionless = source.ApiConfig.Product.VersioningSystem?.IsVersionless == true;
		var versions = await client.ResolveVersionsAsync(source.Git, source.ApiKey, source.ApiConfig, _collector, ctx)
			.ConfigureAwait(false);
		var versionsToExport = versionless
			? versions.Where(v => v.Moniker == "main")
			: versions;

		foreach (var version in versionsToExport)
		{
			var document = await ReadVersionDocument(client, source, version, ctx).ConfigureAwait(false);
			if (document is null)
				continue;

			foreach (var doc in ConvertToDocuments(document, CreateConvertContext(source, version)))
				yield return doc;
		}
	}

	private async Task<OpenApiDocument?> ReadVersionDocument(
		VersionIndexClient client,
		OpenApiExportSource source,
		ResolvedApiVersion version,
		Cancel ctx)
	{
		if (version.IsLocal)
			return await _openApiReader.ReadAsync(version.LocalFile!).ConfigureAwait(false);

		var stream = await client.FetchSpecStreamAsync(source.ApiKey, version, _collector, ctx).ConfigureAwait(false);
		if (stream is null)
			return null;

		return await _openApiReader.ReadAsync(stream, source.ApiConfig.SpecFileName).ConfigureAwait(false);
	}

	private OpenApiConvertContext CreateConvertContext(OpenApiExportSource source, ResolvedApiVersion version)
	{
		var current = source.ApiConfig.Product.VersioningSystem?.Current
			?? versionsConfiguration.GetVersioningSystem(VersioningSystemId.Stack).Current;
		var ceiling = version.Moniker == "main"
			? current
			: ParseFilterCeiling(version.Version, current);
		return new OpenApiConvertContext(
			source.ApiKey,
			version.Moniker,
			ceiling,
			source.ApiConfig.Product.DisplayName,
			source.ApiConfig.Product.Id);
	}

	internal static SemVersion ParseFilterCeiling(string version, SemVersion fallback)
	{
		if (SemVersion.TryParse(version, out var parsed))
			return parsed;
		if (SemVersion.TryParse(version + ".0", out parsed))
			return parsed;
		return fallback;
	}

	/// <summary>
	/// Converts an OpenAPI document to DocumentationDocument instances.
	/// Internal (rather than private) so tests can exercise it against an in-memory spec.
	/// </summary>
	internal IEnumerable<DocumentationDocument> ConvertToDocuments(OpenApiDocument openApiDocument, OpenApiConvertContext convert)
	{
		var productUrl = ApiUrlBuilder.ProductRoot("/docs", ApiUrlBuilder.ProductSuffix(convert.ApiKey, convert.VersionMoniker));
		var productLabel = convert.VersionMoniker == "main"
			? $"{convert.DisplayName} API"
			: $"{convert.DisplayName} {convert.VersionMoniker}.x API";

		foreach (var path in openApiDocument.Paths)
		{
			if (path.Value.Operations == null)
				continue;

			foreach (var operation in path.Value.Operations)
			{
				var operationId = operation.Value.OperationId ?? GenerateOperationId(operation.Key, path.Key);

				if (!ShouldIncludeOperation(operation.Value, convert.FilterCeiling))
					continue;

				var operationMoniker = ApiUrlBuilder.OperationMoniker(operationId, path.Key);
				var url = $"{productUrl}/operation/{operationMoniker}";

				var summary = operation.Value.Summary?.Trim();
				var title = $"{(string.IsNullOrEmpty(summary) ? operationId : summary)} - {productLabel}";
				var searchTitle = $"{title} - {operationId}";
				var description = TransformOperationListToMarkdown(operation.Value.Description);

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

				if (operation.Value.Parameters?.Count > 0)
				{
					_ = bodyBuilder.AppendLine("## Parameters");
					foreach (var param in operation.Value.Parameters)
						_ = bodyBuilder.AppendLine($"- **{param.Name}** ({param.In}): {param.Description}");
					_ = bodyBuilder.AppendLine();
				}

				var body = bodyBuilder.ToString();

				var headings = operation.Value.Tags?
					.Select(t => t.Name)
					.Where(n => !string.IsNullOrEmpty(n))
					.OfType<string>()
					.ToArray() ?? [];

				var applies = ExtractApplicableTo(operation.Value);
				var inference = documentInferrer?.InferForOpenApi(convert.ProductId);

				yield return new DocumentationDocument
				{
					ContentType = "api",
					Path = url,
					Title = title,
					SearchTitle = searchTitle,
					Description = description,
					Body = body,
					Headings = headings,
					Links = [],
					Applies = applies?.ToAppliesTo(),
					Parents =
					[
						new ParentDocument { Title = "API Reference", Path = "/docs/api" },
						new ParentDocument { Title = convert.DisplayName, Path = productUrl }
					],
					Product = inference?.Product?.Id,
					RelatedProducts = inference?.RelatedProducts.Count > 0
						? inference.RelatedProducts.Select(p => new IndexedProduct
						{
							Id = p.Id,
							Repository = p.Repository ?? inference.Repository
						}).ToArray()
						: null
				};
			}
		}
	}

	private static bool ShouldIncludeOperation(OpenApiOperation operation, SemVersion filterCeiling)
	{
		if (operation.Extensions == null || !operation.Extensions.TryGetValue("x-state", out var stateExtension))
			return true;

		if (stateExtension is not JsonNodeExtension jsonNodeExtension)
			return true;

		var stateValue = jsonNodeExtension.Node.GetValue<string>();
		if (string.IsNullOrEmpty(stateValue))
			return true;

		var match = AddedInVersionRegex().Match(stateValue);
		if (!match.Success)
			return true;

		var versionString = match.Groups[1].Value;
		if (!SemVersion.TryParse(versionString, out var addedInVersion))
			return true;

		return addedInVersion <= filterCeiling;
	}

	/// <summary>
	/// Generates an operation ID from method and path when one is not provided.
	/// </summary>
	private static string GenerateOperationId(HttpMethod method, string path)
	{
		var cleanPath = path.TrimStart('/').Replace('/', '-').Replace('{', '-').Replace('}', '-');
		return $"{method.ToString().ToLowerInvariant()}-{cleanPath}";
	}

	/// <summary>
	/// Extracts ApplicableTo information from an operation's x-state extension.
	/// </summary>
	private static ApplicableTo? ExtractApplicableTo(OpenApiOperation operation)
	{
		// Try to get x-state extension
		if (operation.Extensions == null || !operation.Extensions.TryGetValue("x-state", out var stateExtension))
			return null;

		// Get the state string value from JsonNodeExtension
		if (stateExtension is not JsonNodeExtension jsonNodeExtension)
			return null;

		var stateValue = jsonNodeExtension.Node.GetValue<string>();
		if (string.IsNullOrEmpty(stateValue))
			return null;

		// Parse lifecycle from state string (e.g., "Generally available; Added in 9.3.0")
		var lifecycle = ParseLifecycle(stateValue);

		// Parse version from "Added in X.Y.Z"
		var version = ParseVersion(stateValue);

		// Create Applicability instance
		var applicability = new Applicability
		{
			Lifecycle = lifecycle,
			Version = version
		};

		// Create AppliesCollection
		var appliesCollection = new AppliesCollection([applicability]);

		// Return ApplicableTo with Stack set
		return new ApplicableTo
		{
			Stack = appliesCollection
		};
	}

	/// <summary>
	/// Parses the product lifecycle from the x-state string.
	/// </summary>
	private static ProductLifecycle ParseLifecycle(string stateValue)
	{
		var lower = stateValue.ToLowerInvariant();

		if (lower.Contains("generally available"))
			return ProductLifecycle.GenerallyAvailable;
		if (lower.Contains("beta"))
			return ProductLifecycle.Beta;
		if (lower.Contains("experimental"))
			return ProductLifecycle.Experimental;
		if (lower.Contains("tech") && lower.Contains("preview"))
			return ProductLifecycle.TechnicalPreview;
		if (lower.Contains("deprecated"))
			return ProductLifecycle.Deprecated;
		if (lower.Contains("removed"))
			return ProductLifecycle.Removed;

		// Default to GA if we can't parse
		return ProductLifecycle.GenerallyAvailable;
	}

	/// <summary>
	/// Parses the version from "Added in X.Y.Z" pattern in the x-state string.
	/// </summary>
	private static VersionSpec? ParseVersion(string stateValue)
	{
		var match = AddedInVersionRegex().Match(stateValue);
		if (!match.Success)
			return null;

		var versionString = match.Groups[1].Value;
		return VersionSpec.TryParse(versionString, out var version) ? version : null;
	}

	/// <summary>
	/// Transforms HTML operation lists in descriptions to markdown format.
	/// Detects "**All methods and paths for this operation:**" followed by HTML divs/spans
	/// and converts them to a markdown list appended at the end.
	/// </summary>
	private static string TransformOperationListToMarkdown(string? description)
	{
		if (string.IsNullOrEmpty(description))
			return description ?? string.Empty;

		// Check if description starts with the operations list header
		if (!description.Contains("**All methods and paths for this operation:**"))
			return description;

		// Extract all operation verb and path pairs
		var matches = OperationVerbPathRegex().Matches(description);
		if (matches.Count == 0)
			return description;

		// Find where the HTML content starts and ends
		var htmlStartIndex = description.IndexOf("<div>", StringComparison.Ordinal);
		var lastMatchEnd = matches[^1].Index + matches[^1].Length;

		// Find the last closing div after the last match
		var htmlEndIndex = description.IndexOf("</div>", lastMatchEnd, StringComparison.Ordinal);
		if (htmlEndIndex == -1 || htmlStartIndex == -1)
			return description;

		// Build the clean description without HTML
		var beforeHtml = description[..htmlStartIndex].Trim();
		var afterHtml = description[(htmlEndIndex + 6)..].Trim();

		// Build markdown list
		var markdownList = new StringBuilder();
		_ = markdownList.AppendLine();
		_ = markdownList.AppendLine();

		foreach (Match match in matches)
		{
			var verb = match.Groups[2].Value.ToUpperInvariant();
			var path = match.Groups[3].Value;
			_ = markdownList.AppendLine($"- **{verb}** `{path}`");
		}

		// Combine: clean description (before + after HTML) + markdown list at the end
		var result = new StringBuilder();
		_ = result.Append(beforeHtml);
		if (!string.IsNullOrWhiteSpace(afterHtml))
		{
			_ = result.AppendLine();
			_ = result.AppendLine();
			_ = result.Append(afterHtml);
		}

		// Append markdown list at the end
		_ = result.Append(markdownList);

		return result.ToString().Trim();
	}
}
