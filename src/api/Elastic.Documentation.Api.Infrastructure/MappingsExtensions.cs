// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Api.Core.AskAi;
using Elastic.Documentation.Api.Core.Search;
using Elastic.Documentation.Api.Core.Telemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Elastic.Documentation.Api.Infrastructure;

public static class MappingsExtension
{
	public static void MapElasticDocsApiEndpoints(this IEndpointRouteBuilder group, bool mapOtlpEndpoints = true)
	{

		_ = group.MapGet("/", () => Results.Empty);
		_ = group.MapPost("/", () => Results.Empty);
		MapAskAiEndpoint(group);
		MapNavigationSearch(group);
		MapFullSearch(group);
		if (mapOtlpEndpoints)
			MapOtlpProxyEndpoint(group);
	}

	private static void MapAskAiEndpoint(IEndpointRouteBuilder group)
	{
		var askAiGroup = group.MapGroup("/ask-ai");
		_ = askAiGroup.MapPost("/stream", async (HttpContext context, AskAiRequest askAiRequest, AskAiUsecase askAiUsecase, Cancel ctx) =>
		{
			context.Response.ContentType = "text/event-stream";
			context.Response.Headers.CacheControl = "no-cache";
			context.Response.Headers.Connection = "keep-alive";

			var stream = await askAiUsecase.AskAi(askAiRequest, ctx);
			await stream.CopyToAsync(context.Response.Body, ctx);
		});

		// UUID validation is automatic via Guid type deserialization (returns 400 if invalid)
		_ = askAiGroup.MapPost("/message-feedback", async (HttpContext context, AskAiMessageFeedbackRequest request, AskAiMessageFeedbackUsecase feedbackUsecase, Cancel ctx) =>
		{
			// Extract euid cookie for user tracking
			_ = context.Request.Cookies.TryGetValue("euid", out var euid);

			await feedbackUsecase.SubmitFeedback(request, euid, ctx);
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
				FindPageUsecase findPageUsecase,
				Cancel ctx
			) =>
			{
				var request = new FindPageApiRequest
				{
					Query = query,
					PageNumber = pageNumber ?? 1,
					TypeFilter = typeFilter
				};
				var response = await findPageUsecase.FindPageAsync(request, ctx);
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
				FullSearchUsecase searchUsecase,
				Cancel ctx
			) =>
			{
				var request = new FullSearchApiRequest
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
				var response = await searchUsecase.SearchAsync(request, ctx);
				return Results.Ok(response);
			});
	}

	private static void MapOtlpProxyEndpoint(IEndpointRouteBuilder group)
	{
		// Use /o/* to avoid adblocker detection (common blocklists target /otlp, /telemetry, etc.)
		var otlpGroup = group.MapGroup("/o");

		MapOtlpSignalEndpoint(otlpGroup, "/t", OtlpSignalType.Traces);
		MapOtlpSignalEndpoint(otlpGroup, "/l", OtlpSignalType.Logs);
		MapOtlpSignalEndpoint(otlpGroup, "/m", OtlpSignalType.Metrics);
	}

	private static void MapOtlpSignalEndpoint(
		IEndpointRouteBuilder group,
		string path,
		OtlpSignalType signalType) =>
		group.MapPost(path,
			async (HttpContext context, OtlpProxyUsecase proxyUsecase, Cancel ctx) =>
			{
				var contentType = context.Request.ContentType ?? "application/json";
				var result = await proxyUsecase.ProxyOtlp(
					signalType,
					context.Request.Body,
					contentType,
					ctx);
				return result.IsSuccess
					? Results.NoContent()
					: Results.StatusCode(result.StatusCode);
			})
			.DisableAntiforgery();
}
