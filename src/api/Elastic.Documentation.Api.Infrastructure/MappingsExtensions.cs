// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Api.Core.AskAi;
using Elastic.Documentation.Api.Core.Search;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Elastic.Documentation.Api.Infrastructure;

public static class MappingsExtension
{
	public static void MapElasticDocsApiEndpoints(this IEndpointRouteBuilder group)
	{
		_ = group.MapGet("/", () => Results.Empty);
		_ = group.MapPost("/", () => Results.Empty);
		MapAskAiEndpoint(group);
		MapSearchEndpoint(group);
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
	}

	private static void MapSearchEndpoint(IEndpointRouteBuilder group)
	{
		var searchGroup = group.MapGroup("/search");
		_ = searchGroup.MapGet("/",
			async (
				[FromQuery(Name = "q")] string query,
				[FromQuery(Name = "page")] int? pageNumber,
				SearchUsecase searchUsecase,
				Cancel ctx
			) =>
			{
				var searchRequest = new SearchRequest
				{
					Query = query,
					PageNumber = pageNumber ?? 1
				};
				var searchResponse = await searchUsecase.Search(searchRequest, ctx);
				return Results.Ok(searchResponse);
			});
	}
}
