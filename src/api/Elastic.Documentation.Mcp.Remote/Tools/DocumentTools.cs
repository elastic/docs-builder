// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel;
using Elastic.Documentation.Assembler.Mcp;
using Elastic.Documentation.Mcp.Remote.Gateways;
using Elastic.Documentation.Mcp.Remote.Responses;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Elastic.Documentation.Mcp.Remote.Tools;

/// <summary>
/// MCP tools for document-specific operations.
/// </summary>
[McpServerToolType]
public class DocumentTools(IDocumentGateway documentGateway, ILogger<DocumentTools> logger)
{
	/// <summary>
	/// Gets a document by its URL.
	/// </summary>
	[McpServerTool(ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false), McpToolName("get_{scope}document_by_url"), Description("Retrieves a specific {docs} page by its URL. "
		+ "Use when the user provides a documentation URL, references a known page, "
		+ "or you need the full content and metadata of a specific doc. "
		+ "Returns title, AI summaries, headings, navigation context, and the full body only when includeBody is true. "
		+ "Pass includeBody=true to read the page content; otherwise body is null.")]
	public Task<string> GetDocumentByUrl(
		[Description("The URL of the document. Accepts a full URL (e.g. 'https://www.elastic.co/docs/deploy-manage/api-keys') or a path (e.g. '/docs/deploy-manage/api-keys'). Query strings, fragments, and trailing slashes are ignored.")] string url,
		[Description("Include the full page body (default: false, so body is null). Set true whenever you need to read the page content.")] bool includeBody =
			false,
		CancellationToken cancellationToken = default
	) =>
		McpToolRunner.RunAsync(logger, "get_{scope}document_by_url", new Dictionary<string, object?>
		{
			["url"] = url,
			["includeBody"] = includeBody
		}, async () =>
		{
			var result = await documentGateway.GetByUrlAsync(url, cancellationToken);
			if (result == null)
				return DocumentNotFound(url);

			var response = new DocumentResponse
			{
				Url = result.Url,
				Title = result.Title,
				Type = result.Type,
				Description = result.Description,
				NavigationSection = result.NavigationSection,
				AiShortSummary = result.AiShortSummary,
				AiRagOptimizedSummary = result.AiRagOptimizedSummary,
				AiQuestions = result.AiQuestions,
				AiUseCases = result.AiUseCases,
				LastUpdated = result.LastUpdated,
				SourceUrl = result.SourceUrl,
				Parents = result.Parents.Select(p => new ParentDto { Title = p.Title, Url = p.Url }).ToList(),
				Headings = result.Headings.ToList(),
				Product = result.Product != null ? new ProductDto { Id = result.Product.Id, Repository = result.Product.Repository } : null,
				RelatedProducts = result.RelatedProducts?.Select(p => new ProductDto { Id = p.Id, Repository = p.Repository }).ToList(),
				Api = result.Api is { } api
					? new ApiOperationDto
					{
						Method = api.Method,
						Path = api.Path,
						Endpoints = api.Endpoints,
						OperationId = api.OperationId,
						Deprecated = api.Deprecated
					}
					: null,
				Body = includeBody ? result.Body : null,
				BodyLength = result.Body?.Length ?? 0,
				BodyNote = ResponseBody.Note(result.Body, includeBody)
			};
			return ToolSucceeded.Serialize(response, McpJsonContext.Default.DocumentResponse);
		});

	/// <summary>
	/// Analyzes the structure of a document.
	/// </summary>
	[McpServerTool(ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false), McpToolName("analyze_{scope}document_structure"), Description("Analyzes the structure of a {docs} page. "
		+ "Use when evaluating page quality, checking heading hierarchy, or assessing AI enrichment status. "
		+ "Returns heading count, link count, parent pages, and whether AI summaries are present.")]
	public Task<string> AnalyzeDocumentStructure(
		[Description("The URL of the document to analyze. Accepts a full URL (e.g. 'https://www.elastic.co/docs/deploy-manage/api-keys') or a path (e.g. '/docs/deploy-manage/api-keys'). Query strings, fragments, and trailing slashes are ignored.")] string url,
		CancellationToken cancellationToken = default
	) =>
		McpToolRunner.RunAsync(logger, "analyze_{scope}document_structure", new Dictionary<string, object?> { ["url"] = url }, async () =>
		{
			var result = await documentGateway.GetStructureAsync(url, cancellationToken);
			if (result == null)
				return DocumentNotFound(url);

			var response = new DocumentStructureResponse
			{
				Url = result.Url,
				Title = result.Title,
				HeadingCount = result.HeadingCount,
				LinkCount = result.LinkCount,
				ParentCount = result.ParentCount,
				BodyLength = result.BodyLength,
				Headings = result.Headings.ToList(),
				Parents = result.Parents.Select(p => new ParentDto { Title = p.Title, Url = p.Url }).ToList(),
				AiEnrichment = new AiEnrichmentStatusDto
				{
					HasSummary = result.HasAiSummary,
					HasQuestions = result.HasAiQuestions,
					HasUseCases = result.HasAiUseCases
				}
			};
			return ToolSucceeded.Serialize(response, McpJsonContext.Default.DocumentStructureResponse);
		});

	private static ToolRejected DocumentNotFound(string url) => new("document_not_found", $"Document not found for URL: {url}");
}
