// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel;
using System.Text.Json;
using Elastic.Documentation.Api.Core.Search;
using Elastic.Documentation.Mcp.Remote.Responses;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Elastic.Documentation.Mcp.Remote.Tools;

/// <summary>
/// MCP tools for semantic search operations on Elastic documentation.
/// </summary>
[McpServerToolType]
public class SearchTools(IFullSearchGateway fullSearchGateway, ILogger<SearchTools> logger)
{
	/// <summary>
	/// Performs semantic search across all Elastic documentation.
	/// </summary>
	[McpServerTool, Description(
		"Searches all published Elastic documentation by meaning. " +
		"Use when the user asks about Elastic product features, needs to find existing docs pages, " +
		"verify published content, or research what documentation exists on a topic. " +
		"Returns relevant documents with AI summaries, relevance scores, and navigation context.")]
	public async Task<string> SemanticSearch(
		[Description("The search query - can be a question or keywords")] string query,
		[Description("Page number (1-based, default: 1)")] int pageNumber = 1,
		[Description("Number of results per page (default: 10, max: 50)")] int pageSize = 10,
		[Description("Filter by product ID (e.g., 'elasticsearch', 'kibana')")] string? productFilter = null,
		[Description("Filter by navigation section (e.g., 'reference', 'getting-started')")] string? sectionFilter = null,
		CancellationToken cancellationToken = default)
	{
		try
		{
			pageSize = Math.Clamp(pageSize, 1, 50);
			pageNumber = Math.Max(1, pageNumber);

			var request = new FullSearchRequest
			{
				Query = query,
				PageNumber = pageNumber,
				PageSize = pageSize,
				ProductFilter = productFilter != null ? [productFilter] : null,
				SectionFilter = sectionFilter != null ? [sectionFilter] : null,
				IncludeHighlighting = false
			};

			var result = await fullSearchGateway.SearchAsync(request, cancellationToken);

			var response = new SemanticSearchResponse
			{
				Query = query,
				TotalHits = result.TotalHits,
				IsSemanticQuery = result.IsSemanticQuery,
				Results = result.Results.Select(r => new SearchResultDto
				{
					Url = r.Url,
					Title = r.Title,
					Description = r.Description,
					Score = r.Score,
					AiShortSummary = r.AiShortSummary,
					NavigationSection = r.NavigationSection,
					Product = r.Product?.DisplayName,
					LastUpdated = r.LastUpdated
				}).ToList()
			};

			return JsonSerializer.Serialize(response, McpJsonContext.Default.SemanticSearchResponse);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
		{
			logger.LogError(ex, "SemanticSearch failed for query '{Query}'", query);
			return JsonSerializer.Serialize(new ErrorResponse(ex.Message), McpJsonContext.Default.ErrorResponse);
		}
	}

	/// <summary>
	/// Finds documents related to a given topic or document URL.
	/// </summary>
	[McpServerTool, Description(
		"Finds Elastic documentation pages related to a given topic. " +
		"Use when exploring what documentation exists around a subject, building context for writing, " +
		"or discovering related content the user should be aware of.")]
	public async Task<string> FindRelatedDocs(
		[Description("Topic or search terms to find related documents for")] string topic,
		[Description("Maximum number of related documents to return (default: 10)")] int limit = 10,
		[Description("Filter by product ID (e.g., 'elasticsearch', 'kibana')")] string? productFilter = null,
		CancellationToken cancellationToken = default)
	{
		try
		{
			limit = Math.Clamp(limit, 1, 20);

			var request = new FullSearchRequest
			{
				Query = topic,
				PageNumber = 1,
				PageSize = limit,
				ProductFilter = productFilter != null ? [productFilter] : null,
				IncludeHighlighting = false
			};

			var result = await fullSearchGateway.SearchAsync(request, cancellationToken);

			var response = new RelatedDocsResponse
			{
				Topic = topic,
				Count = result.Results.Count,
				RelatedDocs = result.Results.Select(r => new RelatedDocDto
				{
					Url = r.Url,
					Title = r.Title,
					Description = r.Description,
					Score = r.Score,
					AiShortSummary = r.AiShortSummary,
					Product = r.Product?.DisplayName
				}).ToList()
			};

			return JsonSerializer.Serialize(response, McpJsonContext.Default.RelatedDocsResponse);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
		{
			logger.LogError(ex, "FindRelatedDocs failed for topic '{Topic}'", topic);
			return JsonSerializer.Serialize(new ErrorResponse(ex.Message), McpJsonContext.Default.ErrorResponse);
		}
	}
}
