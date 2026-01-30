// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Elastic.Documentation;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Inference;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Search;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Elastic.ApiExplorer.Elasticsearch;

/// <summary>
/// Exports OpenAPI specifications from CloudFront URLs and converts them to DocumentationDocument instances.
/// </summary>
public partial class OpenApiDocumentExporter(
	VersionsConfiguration versionsConfiguration,
	IDocumentInferrerService? documentInferrer = null)
{
	private static readonly HttpClient HttpClient = new();

	private const string ElasticsearchOpenApiUrl = "https://d31bhlox0wglh.cloudfront.net/elasticsearch-openapi-docs.json";
	private const string KibanaOpenApiUrl = "https://d31bhlox0wglh.cloudfront.net/kibana-openapi.json";

	[GeneratedRegex(@"Added in (\d+\.\d+\.\d+)", RegexOptions.IgnoreCase)]
	private static partial Regex AddedInVersionRegex();

	[GeneratedRegex(@"<span class=""operation-verb (\w+)"">(\w+)</span>\s*<span class=""operation-path"">([^<]+)</span>", RegexOptions.IgnoreCase)]
	private static partial Regex OperationVerbPathRegex();

	/// <summary>
	/// Fetches and processes both Elasticsearch and Kibana OpenAPI specifications.
	/// </summary>
	/// <param name="limitPerSource">Optional limit of documents to return per source (Elasticsearch and Kibana)</param>
	/// <param name="ctx">Cancellation token</param>
	/// <returns>Enumerable of DocumentationDocument instances for all endpoints</returns>
	public async IAsyncEnumerable<DocumentationDocument> ExportDocuments(int? limitPerSource = null, [EnumeratorCancellation] Cancel ctx = default)
	{
		// Process Elasticsearch API
		var elasticsearchCount = 0;
		await foreach (var doc in ExportFromUrl(ElasticsearchOpenApiUrl, "elasticsearch", ctx))
		{
			yield return doc;
			elasticsearchCount++;
			if (limitPerSource.HasValue && elasticsearchCount >= limitPerSource.Value)
				break;
		}

		// Process Kibana API
		var kibanaCount = 0;
		await foreach (var doc in ExportFromUrl(KibanaOpenApiUrl, "kibana", ctx))
		{
			yield return doc;
			kibanaCount++;
			if (limitPerSource.HasValue && kibanaCount >= limitPerSource.Value)
				break;
		}
	}

	/// <summary>
	/// Fetches OpenAPI spec from a URL and converts it to DocumentationDocument instances.
	/// </summary>
	private async IAsyncEnumerable<DocumentationDocument> ExportFromUrl(
		string url,
		string product,
		[EnumeratorCancellation] Cancel ctx)
	{
		var openApiDocument = await FetchOpenApiDocument(url, ctx);
		if (openApiDocument == null)
			yield break;

		foreach (var doc in ConvertToDocuments(openApiDocument, product))
			yield return doc;
	}

	/// <summary>
	/// Fetches and parses an OpenAPI document from a URL.
	/// </summary>
	private static async Task<OpenApiDocument?> FetchOpenApiDocument(string url, Cancel ctx)
	{
		try
		{
			var response = await HttpClient.GetAsync(url, ctx);
			_ = response.EnsureSuccessStatusCode();

			await using var stream = await response.Content.ReadAsStreamAsync(ctx);
			var settings = new OpenApiReaderSettings { LeaveStreamOpen = false };
			var openApiDocument = await OpenApiDocument.LoadAsync(stream, settings: settings, cancellationToken: ctx);

			return openApiDocument.Document;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Failed to fetch OpenAPI document from {url}: {ex.Message}");
			return null;
		}
	}

	/// <summary>
	/// Converts an OpenAPI document to DocumentationDocument instances.
	/// </summary>
	private IEnumerable<DocumentationDocument> ConvertToDocuments(OpenApiDocument openApiDocument, string product)
	{
		foreach (var path in openApiDocument.Paths)
		{
			if (path.Value.Operations == null)
				continue;

			foreach (var operation in path.Value.Operations)
			{
				var operationId = operation.Value.OperationId ?? GenerateOperationId(operation.Key, path.Key);

				// Check x-state extension for version filtering
				if (!ShouldIncludeOperation(operation.Value, product))
					continue;

				var url = $"/docs/api/doc/{product}/operation/operation-{operationId.ToLowerInvariant()}";

				var productName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(product);
				// inject product name into title to ensure differentiation and better scoring
				var title = $"{operation.Value.Summary ?? operationId} - {productName} API ";
				var description = TransformOperationListToMarkdown(operation.Value.Description);

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

				// Extract tags as headings
				var headings = operation.Value.Tags?
					.Select(t => t.Name)
					.Where(n => !string.IsNullOrEmpty(n))
					.OfType<string>()
					.ToArray() ?? [];

				// Extract ApplicableTo from x-state
				var applies = ExtractApplicableTo(operation.Value);

				// Infer product and repository metadata
				var inference = documentInferrer?.InferForOpenApi(product);

				yield return new DocumentationDocument
				{
					Type = "api",
					Url = url,
					Title = title,
					SearchTitle = title,
					Description = description,
					Body = body,
					StrippedBody = body,
					Headings = headings,
					Links = [],
					Applies = applies,
					Parents =
					[
						new ParentDocument { Title = "API Reference", Url = "/docs/api" },
						new ParentDocument { Title = product, Url = $"/docs/api/doc/{product}" }
					],
					Product = inference?.Product is not null
						? new IndexedProduct { Id = inference.Product.Id, Repository = inference.Repository }
						: null,
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

	/// <summary>
	/// Determines if an operation should be included based on its x-state extension.
	/// </summary>
	private bool ShouldIncludeOperation(OpenApiOperation operation, string product)
	{
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

		// Get current version for the product
		var versioningSystemId = product.Equals("elasticsearch", StringComparison.OrdinalIgnoreCase)
			? VersioningSystemId.Stack
			: VersioningSystemId.Stack; // Both use Stack for now

		var versioningSystem = versionsConfiguration.GetVersioningSystem(versioningSystemId);
		var currentVersion = versioningSystem.Current;

		// Include if added version is <= current version
		return addedInVersion <= currentVersion;
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
