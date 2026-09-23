// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Inference;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Contract;
using Elastic.Documentation.Versions;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Export;

/// <summary>
/// Exports versioned OpenAPI specifications from the version index and converts them to
/// <see cref="DocumentationDocument"/> instances.
/// </summary>
public partial class OpenApiDocumentExporter(
	VersionsConfiguration versionsConfiguration,
	IDocumentInferrerService? documentInferrer = null,
	VersionIndexClient? versionIndexClient = null,
	IOpenApiSpecificationReader? openApiReader = null
)
{
	private static readonly ResolvedApiConfiguration[] Sources =
	[
		Source("elasticsearch", "elasticsearch.json", "elastic/elasticsearch-specification"),
		Source("kibana", "kibana.yaml", "elastic/kibana")
	];

	private readonly VersionIndexClient _versionIndexClient = versionIndexClient ?? new VersionIndexClient();
	private readonly IOpenApiSpecificationReader _openApiReader = openApiReader ?? OpenApiReader.Instance;

	[GeneratedRegex(@"Added in (\d+\.\d+\.\d+)", RegexOptions.IgnoreCase)]
	private static partial Regex AddedInVersionRegex();

	/// <summary>
	/// Fetches every version of the Elasticsearch and Kibana OpenAPI specifications listed in the
	/// version index and converts them to search documents.
	/// </summary>
	/// <param name="collector">Receives version-index and spec-fetch diagnostics</param>
	/// <param name="limitPerSource">Optional limit of documents to return per source (Elasticsearch and Kibana)</param>
	/// <param name="ctx">Cancellation token</param>
	public async IAsyncEnumerable<DocumentationDocument> ExportDocuments(
		IDiagnosticsCollector collector,
		int? limitPerSource = null,
		[EnumeratorCancellation] Cancel ctx = default
	)
	{
		foreach (var source in Sources)
		{
			var count = 0;
			await foreach (var doc in ExportProductVersions(collector, source, ctx).ConfigureAwait(false))
			{
				yield return doc;
				count++;
				if (limitPerSource.HasValue && count >= limitPerSource.Value)
					break;
			}
		}
	}

	private async IAsyncEnumerable<DocumentationDocument> ExportProductVersions(
		IDiagnosticsCollector collector,
		ResolvedApiConfiguration source,
		[EnumeratorCancellation] Cancel ctx
	)
	{
		var versions = await _versionIndexClient.ResolveVersionsAsync(
			GitCheckoutInformation.Unavailable,
			source.ProductKey,
			source,
			collector,
			ctx
		).ConfigureAwait(false);

		foreach (var version in versions)
		{
			var stream = await _versionIndexClient.FetchSpecStreamAsync(source.ProductKey, version, collector, ctx).ConfigureAwait(false);
			if (stream is null)
				continue;

			var document = await _openApiReader.ReadAsync(stream, source.SpecFileName).ConfigureAwait(false);
			if (document is null)
				continue;

			foreach (var doc in ConvertToDocuments(document, source.ProductKey, version.Moniker))
				yield return doc;
		}
	}

	private static ResolvedApiConfiguration Source(string productKey, string specFileName, string repository) =>
		new()
		{
			ProductKey = productKey,
			Product = new Product { Id = productKey, DisplayName = productKey },
			SpecFileName = specFileName,
			Repository = repository
		};

	/// <summary>
	/// Converts an OpenAPI document to DocumentationDocument instances.
	/// Internal (rather than private) so tests can exercise it against an in-memory spec.
	/// </summary>
	internal IEnumerable<DocumentationDocument> ConvertToDocuments(OpenApiDocument openApiDocument, string product, string moniker = "main")
	{
		var productUrl = ApiUrlBuilder.ProductRoot("/docs", ApiUrlBuilder.ProductSuffix(product, moniker));
		var versionLabel = ApiVersionSwitcher.Label(moniker);
		var productLabel = ProductApiLabel(product);
		var inference = documentInferrer?.InferForOpenApi(product);

		foreach (var path in openApiDocument.Paths)
		{
			if (path.Value.Operations == null)
				continue;

			foreach (var operation in path.Value.Operations)
			{
				var operationId = operation.Value.OperationId ?? GenerateOperationId(operation.Key, path.Key);

				// Check x-state extension for version filtering
				if (!ShouldIncludeOperation(operation.Value, moniker))
					continue;

				var operationMoniker = ApiUrlBuilder.OperationMoniker(operationId, path.Key);
				var url = $"{productUrl}/operation/{operationMoniker}";

				// Trim: spec summaries occasionally carry stray leading/trailing whitespace or a
				// trailing newline, which would otherwise flow verbatim into the indexed title.
				var summary = operation.Value.Summary?.Trim();
				var title = string.IsNullOrEmpty(summary) ? operationId : summary;
				var method = operation.Key.ToString().ToUpperInvariant();
				var searchTitle = BuildSearchTitle(title, productLabel, operationId, $"{method} {path.Key}");
				var description = ApiMarkdown.StripHtml(operation.Value.Description);

				// Build body content from operation details
				var bodyBuilder = new StringBuilder();
				_ = bodyBuilder.AppendLine($"# {title}");
				_ = bodyBuilder.AppendLine();

				if (!string.IsNullOrEmpty(description))
				{
					_ = bodyBuilder.AppendLine(description);
					_ = bodyBuilder.AppendLine();
				}

				_ = bodyBuilder.AppendLine($"**Method:** {method}");
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

				// Extract tags as headings
				var headings = operation.Value.Tags?.Select(t => t.Name).Where(n => !string.IsNullOrEmpty(n)).OfType<string>().ToArray()
					?? [];

				// Extract ApplicableTo from x-state
				var applies = ExtractApplicableTo(operation.Value);

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
					ApiVersion = versionLabel,
					Parents =
					[
						new ParentDocument { Title = "API", Path = "/docs/api" },
						new ParentDocument { Title = productLabel, Path = productUrl },
						new ParentDocument { Title = versionLabel, Path = productUrl }
					],
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
	/// Determines if an operation should be included based on its x-state extension.
	/// </summary>
	private bool ShouldIncludeOperation(OpenApiOperation operation, string moniker)
	{
		// "Added in" is measured against the current Stack release; a frozen major's spec already is that version's operation set.
		if (moniker != "main")
			return true;

		// Try to get x-state extension
		if (operation.Extensions == null || !operation.Extensions.TryGetValue("x-state", out var stateExtension))
			return true; // No x-state, safe to include

		// Get the state string value from JsonNodeExtension
		if (stateExtension is not JsonNodeExtension jsonNodeExtension)
			return true; // Not a JSON node, safe to include

		var stateValue = jsonNodeExtension.Node.GetValue<string>();
		if (string.IsNullOrEmpty(stateValue))
			return true; // Empty state, safe to include

		// Parse version from "Added in X.Y.Z"
		var match = AddedInVersionRegex().Match(stateValue);
		if (!match.Success)
			return true; // No version found, safe to include

		var versionString = match.Groups[1].Value;
		if (!SemVersion.TryParse(versionString, out var addedInVersion))
			return true; // Could not parse version, safe to include

		// All API products currently version against Stack
		var versioningSystem = versionsConfiguration.GetVersioningSystem(VersioningSystemId.Stack);
		var currentVersion = versioningSystem.Current;

		// Include if added version is <= current version
		return addedInVersion <= currentVersion;
	}

	/// <summary>
	/// Display label for the product crumb, e.g. <c>elasticsearch</c> → <c>Elasticsearch API</c>.
	/// </summary>
	internal static string ProductApiLabel(string product) =>
		$"{CultureInfo.InvariantCulture.TextInfo.ToTitleCase(product.Replace('-', ' '))} API";

	/// <summary>
	/// Search tokens users type for an operation: the product label, the raw operation id
	/// (e.g. <c>_bulk</c>, <c>indices.get</c>), and the HTTP method plus path
	/// (e.g. <c>PUT /_bulk</c>). Keep ids and paths verbatim — no case or punctuation
	/// rewriting — because that is what people paste. The product label lives here rather
	/// than in <c>Title</c>, so result rows do not repeat "Elasticsearch API" on every hit.
	/// </summary>
	internal static string BuildSearchTitle(string title, string productLabel, string operationId, string methodAndPath)
	{
		var searchTitle = $"{title} - {productLabel} - {operationId} - {methodAndPath}";
		return operationId.Contains('.', StringComparison.Ordinal) ? $"{searchTitle} - {operationId.Replace('.', ' ')}" : searchTitle;
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
		var applicability = new Applicability { Lifecycle = lifecycle, Version = version };

		// Create AppliesCollection
		var appliesCollection = new AppliesCollection([applicability]);

		// Return ApplicableTo with Stack set
		return new ApplicableTo { Stack = appliesCollection };
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
}
