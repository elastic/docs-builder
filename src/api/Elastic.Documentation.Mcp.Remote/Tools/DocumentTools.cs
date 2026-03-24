// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using Elastic.Documentation.Assembler.Mcp;
using Elastic.Documentation.Mcp.Remote.Gateways;
using Elastic.Documentation.Mcp.Remote.Responses;
using Elastic.Documentation.Mcp.Remote.Telemetry;
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
	[McpServerTool, McpToolName("get_{scope}document_by_url"), Description(
		"Retrieves a specific {docs} page by its URL. " +
		"Use when the user provides a documentation URL, references a known page, " +
		"or you need the full content and metadata of a specific doc. " +
		"Returns title, AI summaries, headings, navigation context, and optionally the full body.")]
	public async Task<string> GetDocumentByUrl(
		[Description("The URL of the document. Accepts a full URL (e.g. 'https://www.elastic.co/docs/deploy-manage/api-keys') or a path (e.g. '/docs/deploy-manage/api-keys'). Query strings, fragments, and trailing slashes are ignored.")] string url,
		[Description("Include full body content (default: false, set true for detailed analysis)")] bool includeBody = false,
		CancellationToken cancellationToken = default)
	{
		var toolName = McpToolTelemetry.ResolveToolName("get_{scope}document_by_url");
		using var activity = McpToolTelemetry.StartActivity(toolName);
		var payload = McpToolTelemetry.SetPayloadMetadata(activity, new Dictionary<string, object?>
		{
			["url"] = url,
			["includeBody"] = includeBody
		});
		McpToolTelemetry.LogStart(logger, toolName, payload);
		var duration = Stopwatch.StartNew();
		var outcome = "failure";

		try
		{
			var result = await documentGateway.GetByUrlAsync(url, cancellationToken);

			if (result == null)
			{
				McpToolTelemetry.MarkFailure(activity, "document_not_found", "Document not found for the requested URL");
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

			McpToolTelemetry.MarkSuccess(activity);
			outcome = "success";
			return JsonSerializer.Serialize(response, McpJsonContext.Default.DocumentResponse);
		}
		catch (OperationCanceledException)
		{
			McpToolTelemetry.MarkCancelled(activity);
			outcome = "cancelled";
			throw;
		}
		catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
		{
			McpToolTelemetry.MarkFailure(activity, ex);
			logger.LogError(ex, "GetDocumentByUrl failed for URL '{Url}'", url);
			return JsonSerializer.Serialize(new ErrorResponse(ex.Message), McpJsonContext.Default.ErrorResponse);
		}
		finally
		{
			duration.Stop();
			McpToolTelemetry.LogCompletion(logger, toolName, duration.ElapsedMilliseconds, outcome);
		}
	}

	/// <summary>
	/// Analyzes the structure of a document.
	/// </summary>
	[McpServerTool, McpToolName("analyze_{scope}document_structure"), Description(
		"Analyzes the structure of a {docs} page. " +
		"Use when evaluating page quality, checking heading hierarchy, or assessing AI enrichment status. " +
		"Returns heading count, link count, parent pages, and whether AI summaries are present.")]
	public async Task<string> AnalyzeDocumentStructure(
		[Description("The URL of the document to analyze. Accepts a full URL (e.g. 'https://www.elastic.co/docs/deploy-manage/api-keys') or a path (e.g. '/docs/deploy-manage/api-keys'). Query strings, fragments, and trailing slashes are ignored.")] string url,
		CancellationToken cancellationToken = default)
	{
		var toolName = McpToolTelemetry.ResolveToolName("analyze_{scope}document_structure");
		using var activity = McpToolTelemetry.StartActivity(toolName);
		var payload = McpToolTelemetry.SetPayloadMetadata(activity, new Dictionary<string, object?>
		{
			["url"] = url
		});
		McpToolTelemetry.LogStart(logger, toolName, payload);
		var duration = Stopwatch.StartNew();
		var outcome = "failure";

		try
		{
			var result = await documentGateway.GetStructureAsync(url, cancellationToken);

			if (result == null)
			{
				McpToolTelemetry.MarkFailure(activity, "document_not_found", "Document not found for the requested URL");
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

			McpToolTelemetry.MarkSuccess(activity);
			outcome = "success";
			return JsonSerializer.Serialize(response, McpJsonContext.Default.DocumentStructureResponse);
		}
		catch (OperationCanceledException)
		{
			McpToolTelemetry.MarkCancelled(activity);
			outcome = "cancelled";
			throw;
		}
		catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
		{
			McpToolTelemetry.MarkFailure(activity, ex);
			logger.LogError(ex, "AnalyzeDocumentStructure failed for URL '{Url}'", url);
			return JsonSerializer.Serialize(new ErrorResponse(ex.Message), McpJsonContext.Default.ErrorResponse);
		}
		finally
		{
			duration.Stop();
			McpToolTelemetry.LogCompletion(logger, toolName, duration.ElapsedMilliseconds, outcome);
		}
	}
}
