// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Api.Core.AskAi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Elastic.Documentation.Api.Infrastructure;

public static class MappingsExtension
{
	public static void MapElasticDocsApiEndpoints(this IEndpointRouteBuilder group) =>
		MapAskAiEndpoint(group);

	private static void MapAskAiEndpoint(IEndpointRouteBuilder group)
	{
		var askAiGroup = group.MapGroup("/ask-ai");
		_ = askAiGroup.MapPost("/stream", async (AskAiRequest askAiRequest, AskAiUsecase askAiUsecase, Cancel ctx) =>
		{
			var stream = await askAiUsecase.AskAi(askAiRequest, ctx);
			return Results.Stream(stream, "text/event-stream");
		});
	}
}
