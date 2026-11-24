// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Text.Json;
using Elastic.Documentation.Api.AskAi;
using Elastic.Documentation.Api.PageFeedback;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.ApiReference;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api;

public static class MappingsExtension
{
	// An ActivitySource stays registered with the runtime until disposed, so create each one once.
	private static readonly ActivitySource AskAiActivitySource = new(TelemetryConstants.AskAiSourceName);
	private static readonly ActivitySource AskAiFeedbackActivitySource = new(TelemetryConstants.AskAiFeedbackSourceName);

	public static void MapElasticDocsApiEndpoints(this IEndpointRouteBuilder group)
	{
		// Empty answers for a probe of the group root; not part of the API's contract.
		_ = group.MapGet("/", () => Results.Empty).ExcludeFromDescription();
		_ = group.MapPost("/", () => Results.Empty).ExcludeFromDescription();
		MapAskAiEndpoint(group);
		MapNavigationSearch(group);
		MapFullSearch(group);
		MapChanges(group);
		MapPageFeedback(group);
		MapApiOperations(group);
	}

	private static void MapAskAiEndpoint(IEndpointRouteBuilder group)
	{
		var askAiGroup = group.MapGroup("/ask-ai");
		_ = askAiGroup
			.MapPost("/stream", async (
				HttpContext context,
				AskAiRequest askAiRequest,
				IAskAiService askAiService,
				IStreamTransformer streamTransformer,
				ILogger<Program> logger,
				Cancel ctx
			) =>
			{
				context.Response.ContentType = "text/event-stream";
				context.Response.Headers.CacheControl = "no-cache";
				context.Response.Headers.Connection = "keep-alive";

				logger.LogInformation(
					"Starting AskAI chat with {AgentProvider} and {AgentId}",
					streamTransformer.AgentProvider,
					streamTransformer.AgentId
				);
				var activity = AskAiActivitySource.StartActivity($"chat {streamTransformer.AgentProvider}", ActivityKind.Client);
				_ = activity?.SetTag("gen_ai.operation.name", "chat");
				_ = activity?.SetTag("gen_ai.provider.name", streamTransformer.AgentProvider);
				_ = activity?.SetTag("gen_ai.agent.id", streamTransformer.AgentId);
				if (askAiRequest.ConversationId is not null)
					_ = activity?.SetTag("gen_ai.conversation.id", askAiRequest.ConversationId.ToString());

				var inputMessages = new[] { new InputMessage("user", [new MessagePart("text", askAiRequest.Message)]) };
				var inputMessagesJson = JsonSerializer.Serialize(inputMessages, ApiJsonContext.Default.InputMessageArray);
				_ = activity?.SetTag("gen_ai.input.messages", inputMessagesJson);
				logger.LogInformation("AskAI input message: <{ask_ai.input.message}>", SanitizeForLog(askAiRequest.Message));
				logger.LogInformation("Streaming AskAI response");

				Stream transformedStream;
				try
				{
					var response = await askAiService.AskAi(askAiRequest, ctx);

					var conversationId = response.GeneratedConversationId ?? askAiRequest.ConversationId;
					if (conversationId is not null)
						_ = activity?.SetTag("gen_ai.conversation.id", conversationId.ToString());

					transformedStream = await streamTransformer.TransformAsync(
						response.Stream,
						response.GeneratedConversationId,
						activity,
						ctx
					);
				}
				catch (Exception ex)
				{
					// The transformer ends the activity once it owns the stream; until then this handler must.
					_ = activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
					activity?.Dispose();
					throw;
				}
				await transformedStream.CopyToAsync(context.Response.Body, ctx);
			})
			.WithTags(AskAiTag)
			.WithName("ask-ai-stream")
			.WithSummary("Stream an Ask AI answer as server-sent events")
			.Produces<string>(StatusCodes.Status200OK, "text/event-stream")
			.ProducesErrors(StatusCodes.Status400BadRequest);

		// UUID validation is automatic via Guid type deserialization (returns 400 if invalid)
		_ = askAiGroup
			.MapPost("/message-feedback", async (
				HttpContext context,
				AskAiMessageFeedbackRequest request,
				IAskAiMessageFeedbackService feedbackService,
				ILogger<Program> logger,
				Cancel ctx
			) =>
			{
				var euid = TryGetEuid(context);

				using var activity = AskAiFeedbackActivitySource.StartActivity("record message-feedback", ActivityKind.Internal);
				_ = activity?.SetTag("gen_ai.conversation.id", request.ConversationId);
				_ = activity?.SetTag("ask_ai.message.id", request.MessageId);
				_ = activity?.SetTag("ask_ai.feedback.reaction", request.Reaction.ToString().ToLowerInvariant());

				logger.LogInformation(
					"Recording message feedback for message {MessageId} in conversation {ConversationId}: {Reaction}",
					request.MessageId,
					request.ConversationId,
					request.Reaction
				);

				var record = new AskAiMessageFeedbackRecord(request.MessageId, request.ConversationId, request.Reaction, euid);

				if (!await feedbackService.RecordFeedbackAsync(record, ctx))
					return FeedbackUnavailable();
				return Results.NoContent();
			})
			.DisableAntiforgery()
			.WithTags(AskAiTag)
			.WithName("ask-ai-message-feedback")
			.WithSummary("Record a reaction to an Ask AI answer")
			.Produces(StatusCodes.Status204NoContent)
			.ProducesErrors(StatusCodes.Status400BadRequest, StatusCodes.Status503ServiceUnavailable);
	}

	private static void MapNavigationSearch(IEndpointRouteBuilder group)
	{
		var searchGroup = group.MapGroup("/navigation-search");
		_ = searchGroup
			.MapGet("/", async (
				[FromQuery(Name = "q")] string query,
				[FromQuery(Name = "page")] int? pageNumber,
				[FromQuery(Name = "type")] string? typeFilter,
				[FromQuery(Name = "api_key")] string? apiKey,
				[FromQuery(Name = "api_tree")] string? apiTree,
				INavigationSearchService navigationSearchService,
				Cancel ctx
			) =>
			{
				if (!ApiPageParameters.TryParse(apiKey, apiTree, out var apiPage))
					return Invalid("api_key must be a lowercase API key, and api_tree 'main' or a major version with api_key.");

				var request = new NavigationSearchRequest
				{
					Query = query,
					PageNumber = pageNumber ?? 1,
					TypeFilter = typeFilter,
					ApiPage = apiPage
				};
				var response = await navigationSearchService.NavigationSearchAsync(request, ctx);
				return Results.Ok(response);
			})
			.WithTags(SearchTag)
			.WithName("navigation-search")
			.WithSummary("Search page titles and the navigation")
			.Produces<NavigationSearchResponse>()
			.ProducesErrors(StatusCodes.Status400BadRequest, StatusCodes.Status503ServiceUnavailable);
	}

	private static void MapFullSearch(IEndpointRouteBuilder group)
	{
		var searchGroup = group.MapGroup("/search");
		_ = searchGroup
			.MapGet("/", async (
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
			})
			.WithTags(SearchTag)
			.WithName("search")
			.WithSummary("Search the documentation")
			.Produces<FullSearchResponse>()
			.ProducesErrors(StatusCodes.Status400BadRequest, StatusCodes.Status503ServiceUnavailable);
	}

	private static void MapChanges(IEndpointRouteBuilder group) =>
		group
			.MapGet("/changes", async (
				[FromQuery(Name = "since")] DateTimeOffset since,
				[FromQuery(Name = "cursor")] string? cursor,
				[FromQuery(Name = "size")] int? pageSize,
				IChangesService changesService,
				Cancel ctx
			) =>
			{
				// A cursor this endpoint did not return would otherwise restart the feed from its first page.
				if (cursor is not null && !ChangesService.IsValidCursor(cursor))
					return Invalid("cursor is not a nextCursor that /changes returned.");
				var request = new ChangesRequest { Since = since, PageSize = pageSize ?? ChangesDefaults.PageSize, Cursor = cursor };
				var response = await changesService.GetChangesAsync(request, ctx);
				return Results.Ok(response);
			})
			.WithTags(ChangesTag)
			.WithName("list-changes")
			.WithSummary("List the pages that changed since a date")
			.Produces<ChangesResponse>()
			.ProducesErrors(StatusCodes.Status400BadRequest, StatusCodes.Status503ServiceUnavailable);

	// The APIs API Explorer publishes and, under each, its operations. An operation is always addressed inside its
	// API: many operation ids and routes exist in several APIs (elasticsearch and elasticsearch-serverless share
	// most), so a lookup across APIs could only guess. The MCP tools answer the same lookups.
	private static void MapApiOperations(IEndpointRouteBuilder group)
	{
		var apis = group.MapGroup("/apis").WithTags(ApiReferenceTag);
		_ = apis
			.MapGet(
				"/",
				async (ApiReferenceService apiReference, Cancel ctx) => ToResult(await apiReference.ListAsync(null, null, null, ctx))
			)
			.WithName("list-apis")
			.WithSummary("List every API")
			.Produces<ApiListResponse>()
			.ProducesErrors();
		_ = apis
			.MapGet(
				"/{api}",
				async (string api, ApiReferenceService apiReference, Cancel ctx) => ToResult(await apiReference.GetApiAsync(api, ctx))
			)
			.WithName("get-api")
			.WithSummary("Read one API")
			.Produces<ApiSummary>()
			.ProducesErrors(StatusCodes.Status404NotFound);
		_ = apis
			.MapGet(
				"/{api}/groups",
				async (
					string api,
					[FromQuery(Name = "version")] string? version,
					[FromQuery(Name = "include_deprecated")] bool? includeDeprecated,
					ApiReferenceService apiReference,
					Cancel ctx
				) => ToResult(await apiReference.ListGroupsAsync(api, version, includeDeprecated ?? false, ctx))
			)
			.WithName("list-api-groups")
			.WithSummary("Count one API's operations by group")
			.Produces<ApiOperationListResponse>()
			.ProducesErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound);
		_ = apis
			.MapGet(
				"/{api}/operations",
				async (
					string api,
					[FromQuery(Name = "version")] string? version,
					[FromQuery(Name = "group")] string? apiGroupName,
					[FromQuery(Name = "q")] string? query,
					[FromQuery(Name = "endpoint")] string? endpoint,
					[FromQuery(Name = "include_deprecated")] bool? includeDeprecated,
					[FromQuery(Name = "view")] string? view,
					[FromQuery(Name = "include_body")] bool? includeBody,
					[FromQuery(Name = "page")] int? pageNumber,
					[FromQuery(Name = "size")] int? pageSize,
					ApiReferenceService apiReference,
					Cancel ctx
				) => ToResult(
					await apiReference.QueryAsync(
						new ApiOperationQuery
						{
							Api = api,
							Version = version,
							Group = apiGroupName,
							Q = query,
							Endpoint = endpoint,
							IncludeDeprecated = includeDeprecated ?? false,
							View = view,
							IncludeBody = includeBody ?? false,
							Page = pageNumber ?? 1,
							Size = pageSize ?? 10
						},
						ctx
					)
				)
			)
			.WithName("list-api-operations")
			.WithSummary("Search, look up by endpoint, or list one API's operations")
			.Produces<ApiOperationCollectionResponse>()
			.ProducesErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status503ServiceUnavailable);
		_ = apis
			.MapGet(
				"/{api}/operations/{operationId}",
				async (
					string api,
					string operationId,
					[FromQuery(Name = "version")] string? version,
					[FromQuery(Name = "include_body")] bool? includeBody,
					ApiReferenceService apiReference,
					Cancel ctx
				) => ToResult(await apiReference.GetOperationAsync(api, operationId, version, includeBody ?? false, ctx))
			)
			.WithName("get-api-operation")
			.WithSummary("Read one operation of an API")
			.Produces<ApiOperationResponse>()
			.ProducesErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound);
	}

	private static IResult ToResult(ApiReferenceReply reply) => reply switch
	{
		ApiOperationSearchResponse r => Results.Ok(r),
		ApiOperationCollectionResponse r => Results.Ok(r),
		ApiSummary r => Results.Ok(r),
		ApiOperationResponse r => Results.Ok(r),
		ApiListResponse r => Results.Ok(r),
		ApiOperationListResponse r => Results.Ok(r),
		// The service names types in snake case for MCP; problem type fragments use kebab case.
		ApiReferenceRejection r =>
			ApiProblems.Problem(
				r.IsNotFound ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest,
				r.ErrorType.Replace('_', '-'),
				r.Message
			)
	};

	private const string SearchTag = "Search";
	private const string AskAiTag = "Ask AI";
	private const string ChangesTag = "Changes";
	private const string FeedbackTag = "Page feedback";
	private const string ApiReferenceTag = "API reference";

	private static IResult Invalid(string message) =>
		ApiProblems.Problem(StatusCodes.Status400BadRequest, ApiProblems.InvalidArguments, message);

	private static IResult FeedbackUnavailable() =>
		ApiProblems.Problem(
			StatusCodes.Status503ServiceUnavailable,
			ApiProblems.FeedbackUnavailable,
			"The feedback could not be stored. Try again later."
		);

	/// <summary>Declares the error responses of an endpoint as problem details; any endpoint can fail with 500.</summary>
	private static RouteHandlerBuilder ProducesErrors(this RouteHandlerBuilder builder, params int[] statusCodes)
	{
		foreach (var statusCode in statusCodes.Append(StatusCodes.Status500InternalServerError))
			_ = builder.ProducesProblem(statusCode);
		return builder;
	}

	private static void MapPageFeedback(IEndpointRouteBuilder group) =>
		_ = group
			.MapPut("/page-feedback/{feedbackId:guid}", async (
				Guid feedbackId,
				PageFeedbackRequest request,
				IPageFeedbackService feedbackService,
				ILogger<Program> logger,
				Cancel ctx
			) =>
			{
				if (!IsValidPageFeedback(request))
					return Invalid("The page feedback is invalid. Check pageUrl, pageTitle, reaction, reasons and comment.");

				var record = PageFeedbackRecord.From(feedbackId, request);
				var pageUrlForLog = SanitizeForLog(request.PageUrl);
				if (!await feedbackService.UpsertFeedbackAsync(record, ctx))
				{
					logger.LogWarning("Failed to record page feedback {FeedbackId} for {PageUrl}", feedbackId, pageUrlForLog);
					return FeedbackUnavailable();
				}

				logger.LogDebug("Recorded page feedback {FeedbackId} for {PageUrl}", feedbackId, pageUrlForLog);
				return Results.NoContent();
			})
			.DisableAntiforgery()
			.WithTags(FeedbackTag)
			.WithName("put-page-feedback")
			.WithSummary("Record or update feedback on a page")
			.Produces(StatusCodes.Status204NoContent)
			.ProducesErrors(StatusCodes.Status400BadRequest, StatusCodes.Status503ServiceUnavailable);

	private static bool IsValidPageFeedback(PageFeedbackRequest request) =>
		!string.IsNullOrWhiteSpace(request.PageUrl)
			&& request.PageUrl.Length <= 2048
			&& request.PageUrl.StartsWith('/')
			&& !request.PageUrl.StartsWith("//", StringComparison.Ordinal)
			&& Uri.TryCreate(request.PageUrl, UriKind.Relative, out _)
			&& !string.IsNullOrWhiteSpace(request.PageTitle)
			&& request.PageTitle.Length <= 500
			&& request.Reaction is PageFeedbackReaction.ThumbsUp or PageFeedbackReaction.ThumbsDown
			&& (request.Comment is null || request.Comment.Length <= 2000)
			&& IsValidFeedbackDetails(request);

	private static bool IsValidFeedbackDetails(PageFeedbackRequest request)
	{
		if (request.Reasons is null or [])
			return request.ReasonSetVersion is null && string.IsNullOrWhiteSpace(request.Comment);

		return request.ReasonSetVersion is > 0
			&& request.Reasons.All(r => Enum.IsDefined(r) && IsReasonValidForReaction(request.Reaction, r));
	}

	private static bool IsReasonValidForReaction(PageFeedbackReaction reaction, PageFeedbackReason reason) => reaction switch
	{
		PageFeedbackReaction.ThumbsUp =>
			reason is PageFeedbackReason.Accurate or PageFeedbackReason.SolvedProblem or PageFeedbackReason.EasyToUnderstand or PageFeedbackReason.HelpfulExamples or PageFeedbackReason.EasyToFind or PageFeedbackReason.AnotherReason,
		PageFeedbackReaction.ThumbsDown =>
			reason is PageFeedbackReason.Inaccurate or PageFeedbackReason.MissingInformation or PageFeedbackReason.HardToUnderstand or PageFeedbackReason.CodeSampleErrors or PageFeedbackReason.OutOfDate or PageFeedbackReason.AnotherReason,
		_ => false
	};

	private static string? TryGetEuid(HttpContext context)
	{
		_ = context.Request.Cookies.TryGetValue("euid", out var euid);
		return euid;
	}

	private static string? SanitizeForLog(string? value) =>
		value?.Replace("\r", "", StringComparison.Ordinal).Replace("\n", "", StringComparison.Ordinal);
}
