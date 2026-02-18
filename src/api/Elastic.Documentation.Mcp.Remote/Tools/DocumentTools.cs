// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel;
using System.Text.Json;
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
	[McpServerTool, Description("Gets a specific documentation page by its URL. Returns full document content including AI summaries and metadata.")]
	public async Task<string> GetDocumentByUrl(
		[Description("The URL of the document (e.g., '/docs/elasticsearch/reference/index')")] string url,
		[Description("Include full body content (default: false, set true for detailed analysis)")] bool includeBody = false,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var result = await documentGateway.GetByUrlAsync(url, cancellationToken);

			if (result == null)
			{
				return JsonSerializer.Serialize(
					new ErrorResponse($"Document not found for URL: {url}"),
					McpJsonContext.Default.ErrorResponse);
			}

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
				Parents = result.Parents.Select(p => new ParentDto
				{
					Title = p.Title,
					Url = p.Url
				}).ToList(),
				Headings = result.Headings.ToList(),
				Product = result.Product != null ? new ProductDto
				{
					Id = result.Product.Id,
					Repository = result.Product.Repository
				} : null,
				RelatedProducts = result.RelatedProducts?.Select(p => new ProductDto
				{
					Id = p.Id,
					Repository = p.Repository
				}).ToList(),
				Body = includeBody ? result.Body : null,
				BodyLength = result.Body?.Length ?? 0
			};

			return JsonSerializer.Serialize(response, McpJsonContext.Default.DocumentResponse);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
		{
			logger.LogError(ex, "GetDocumentByUrl failed for URL '{Url}'", url);
			return JsonSerializer.Serialize(new ErrorResponse(ex.Message), McpJsonContext.Default.ErrorResponse);
		}
	}

	/// <summary>
	/// Analyzes the structure of a document.
	/// </summary>
	[McpServerTool, Description("Analyzes the structure of a documentation page. Returns heading count, links, parents, and AI enrichment status.")]
	public async Task<string> AnalyzeDocumentStructure(
		[Description("The URL of the document to analyze")] string url,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var result = await documentGateway.GetStructureAsync(url, cancellationToken);

			if (result == null)
			{
				return JsonSerializer.Serialize(
					new ErrorResponse($"Document not found for URL: {url}"),
					McpJsonContext.Default.ErrorResponse);
			}

			var response = new DocumentStructureResponse
			{
				Url = result.Url,
				Title = result.Title,
				HeadingCount = result.HeadingCount,
				LinkCount = result.LinkCount,
				ParentCount = result.ParentCount,
				BodyLength = result.BodyLength,
				Headings = result.Headings.ToList(),
				Parents = result.Parents.Select(p => new ParentDto
				{
					Title = p.Title,
					Url = p.Url
				}).ToList(),
				AiEnrichment = new AiEnrichmentStatusDto
				{
					HasSummary = result.HasAiSummary,
					HasQuestions = result.HasAiQuestions,
					HasUseCases = result.HasAiUseCases
				}
			};

			return JsonSerializer.Serialize(response, McpJsonContext.Default.DocumentStructureResponse);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
		{
			logger.LogError(ex, "AnalyzeDocumentStructure failed for URL '{Url}'", url);
			return JsonSerializer.Serialize(new ErrorResponse(ex.Message), McpJsonContext.Default.ErrorResponse);
		}
	}
}
