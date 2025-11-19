// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json.Nodes;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Search;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Elastic.ApiExplorer.Elasticsearch;

/// <summary>
/// Exports OpenAPI specifications from CloudFront URLs and converts them to DocumentationDocument instances.
/// </summary>
public partial class OpenApiDocumentExporter(VersionsConfiguration versionsConfiguration)
{
	private static readonly HttpClient HttpClient = new();

	private const string ElasticsearchOpenApiUrl = "https://d31bhlox0wglh.cloudfront.net/elasticsearch-openapi-docs.json";
	private const string KibanaOpenApiUrl = "https://d31bhlox0wglh.cloudfront.net/kibana-openapi.json";

	[GeneratedRegex(@"Added in (\d+\.\d+\.\d+)", RegexOptions.IgnoreCase)]
	private static partial Regex AddedInVersionRegex();

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

				var title = operation.Value.Summary ?? operationId;
				var description = operation.Value.Description;

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
					{
						_ = bodyBuilder.AppendLine($"- **{param.Name}** ({param.In}): {param.Description}");
					}
					_ = bodyBuilder.AppendLine();
				}

				var body = bodyBuilder.ToString();

				// Extract tags as headings
				var headings = operation.Value.Tags?
					.Select(t => t.Name)
					.Where(n => !string.IsNullOrEmpty(n))
					.OfType<string>()
					.ToArray() ?? [];

				yield return new DocumentationDocument
				{
					Url = url,
					Title = title,
					Description = description,
					Body = body,
					StrippedBody = body, // Already plain text, no markdown to strip
					UrlSegmentCount = url.Split('/', StringSplitOptions.RemoveEmptyEntries).Length,
					Headings = headings,
					Links = [],
					Parents =
					[
						new ParentDocument { Title = "API Reference", Url = "/docs/api" },
						new ParentDocument { Title = product, Url = $"/docs/api/doc/{product}" }
					]
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
		var versioningSystemId = product == "elasticsearch"
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
}
