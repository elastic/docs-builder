// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Text.Json;
using Elastic.Documentation.Api.AskAi;
using Elastic.Documentation.Search;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api;

public static class MappingsExtension
{
	public static void MapElasticDocsApiEndpoints(this IEndpointRouteBuilder group)
	{
		_ = group.MapGet("/", () => Results.Empty);
		_ = group.MapPost("/", () => Results.Empty);
		MapAskAiEndpoint(group);
		MapNavigationSearch(group);
		MapFullSearch(group);
		MapRelatedPages(group);
		MapChanges(group);
	}

	private static void MapAskAiEndpoint(IEndpointRouteBuilder group)
	{
		var askAiGroup = group.MapGroup("/ask-ai");
		_ = askAiGroup.MapPost("/stream", async (HttpContext context, AskAiRequest askAiRequest, IAskAiService askAiService, IStreamTransformer streamTransformer, ILogger<Program> logger, Cancel ctx) =>
		{
			context.Response.ContentType = "text/event-stream";
			context.Response.Headers.CacheControl = "no-cache";
			context.Response.Headers.Connection = "keep-alive";

			var askAiActivitySource = new ActivitySource(TelemetryConstants.AskAiSourceName);
			logger.LogInformation("Starting AskAI chat with {AgentProvider} and {AgentId}", streamTransformer.AgentProvider, streamTransformer.AgentId);
			var activity = askAiActivitySource.StartActivity($"chat {streamTransformer.AgentProvider}", ActivityKind.Client);
			_ = activity?.SetTag("gen_ai.operation.name", "chat");
			_ = activity?.SetTag("gen_ai.provider.name", streamTransformer.AgentProvider);
			_ = activity?.SetTag("gen_ai.agent.id", streamTransformer.AgentId);
			if (askAiRequest.ConversationId is not null)
				_ = activity?.SetTag("gen_ai.conversation.id", askAiRequest.ConversationId.ToString());

			var inputMessages = new[]
			{
				new InputMessage("user", [new MessagePart("text", askAiRequest.Message)])
			};
			var inputMessagesJson = JsonSerializer.Serialize(inputMessages, ApiJsonContext.Default.InputMessageArray);
			_ = activity?.SetTag("gen_ai.input.messages", inputMessagesJson);
			var sanitizedMessage = askAiRequest.Message?.Replace("\r", "").Replace("\n", "");
			logger.LogInformation("AskAI input message: <{ask_ai.input.message}>", sanitizedMessage);
			logger.LogInformation("Streaming AskAI response");

			var response = await askAiService.AskAi(askAiRequest, ctx);

			var conversationId = response.GeneratedConversationId ?? askAiRequest.ConversationId;
			if (conversationId is not null)
				_ = activity?.SetTag("gen_ai.conversation.id", conversationId.ToString());

			var transformedStream = await streamTransformer.TransformAsync(
				response.Stream,
				response.GeneratedConversationId,
				activity,
				ctx);
			await transformedStream.CopyToAsync(context.Response.Body, ctx);
		});

		// UUID validation is automatic via Guid type deserialization (returns 400 if invalid)
		_ = askAiGroup.MapPost("/message-feedback", async (HttpContext context, AskAiMessageFeedbackRequest request, IAskAiMessageFeedbackService feedbackService, ILogger<Program> logger, Cancel ctx) =>
		{
			// Extract euid cookie for user tracking
			_ = context.Request.Cookies.TryGetValue("euid", out var euid);

			var feedbackActivitySource = new ActivitySource(TelemetryConstants.AskAiFeedbackSourceName);
			using var activity = feedbackActivitySource.StartActivity("record message-feedback", ActivityKind.Internal);
			_ = activity?.SetTag("gen_ai.conversation.id", request.ConversationId);
			_ = activity?.SetTag("ask_ai.message.id", request.MessageId);
			_ = activity?.SetTag("ask_ai.feedback.reaction", request.Reaction.ToString().ToLowerInvariant());

			logger.LogInformation(
				"Recording message feedback for message {MessageId} in conversation {ConversationId}: {Reaction}",
				request.MessageId,
				request.ConversationId,
				request.Reaction);

			var record = new AskAiMessageFeedbackRecord(
				request.MessageId,
				request.ConversationId,
				request.Reaction,
				euid
			);

			await feedbackService.RecordFeedbackAsync(record, ctx);
			return Results.NoContent();
		}).DisableAntiforgery();
	}

	private static void MapNavigationSearch(IEndpointRouteBuilder group)
	{
		var searchGroup = group.MapGroup("/navigation-search");
		_ = searchGroup.MapGet("/",
			async (
				[FromQuery(Name = "q")] string query,
				[FromQuery(Name = "page")] int? pageNumber,
				[FromQuery(Name = "type")] string? typeFilter,
				INavigationSearchService navigationSearchService,
				Cancel ctx
			) =>
			{
				var request = new NavigationSearchRequest
				{
					Query = query,
					PageNumber = pageNumber ?? 1,
					TypeFilter = typeFilter
				};
				var response = await navigationSearchService.NavigationSearchAsync(request, ctx);
				return Results.Ok(response);
			});
	}

	private static void MapFullSearch(IEndpointRouteBuilder group)
	{
		var searchGroup = group.MapGroup("/search");
		_ = searchGroup.MapGet("/",
			async (
				[FromQuery(Name = "q")] string query,
				[FromQuery(Name = "page")] int? pageNumber,
				[FromQuery(Name = "size")] int? pageSize,
				[FromQuery(Name = "type")] string[]? typeFilter,
				[FromQuery(Name = "section")] string[]? sectionFilter,
				[FromQuery(Name = "deployment")] string[]? deploymentFilter,
				[FromQuery(Name = "product")] string[]? productFilter,
				[FromQuery(Name = "version")] string? versionFilter,
				[FromQuery(Name = "sort")] string? sortBy,
				IFullSearchService searchService,
				Cancel ctx
			) =>
			{
				var request = new FullSearchRequest
				{
					Query = query,
					PageNumber = pageNumber ?? 1,
					PageSize = pageSize ?? 20,
					TypeFilter = typeFilter,
					SectionFilter = sectionFilter,
					DeploymentFilter = deploymentFilter,
					ProductFilter = productFilter,
					VersionFilter = versionFilter,
					SortBy = sortBy ?? "relevance"
				};
				var response = await searchService.SearchAsync(request, ctx);
				return Results.Ok(response);
			});
	}

	private static void MapRelatedPages(IEndpointRouteBuilder group) =>
		group.MapGet("/related-pages",
			async (
				[FromQuery(Name = "path")] string path,
				IRelatedPagesService relatedPagesService,
				Cancel ctx
			) =>
			{
				if (string.IsNullOrWhiteSpace(path))
					return Results.BadRequest("The path query parameter is required.");
				if (path.Length > RelatedPagesQuery.MaximumPathLength)
					return Results.BadRequest($"The path query parameter must not exceed {RelatedPagesQuery.MaximumPathLength} characters.");

				var response = await relatedPagesService.GetRelatedPagesAsync(path, ctx);
				return Results.Ok(response);
			});

	private static void MapChanges(IEndpointRouteBuilder group) =>
		group.MapGet("/changes",
			async (
				[FromQuery(Name = "since")] DateTimeOffset since,
				[FromQuery(Name = "cursor")] string? cursor,
				[FromQuery(Name = "size")] int? pageSize,
				IChangesService changesService,
				Cancel ctx
			) =>
			{
				var request = new ChangesRequest
				{
					Since = since,
					PageSize = pageSize ?? ChangesDefaults.PageSize,
					Cursor = cursor
				};
				var response = await changesService.GetChangesAsync(request, ctx);
				return Results.Ok(response);
			});

}
