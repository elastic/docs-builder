// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Api.Core.AskAi;

namespace Elastic.Documentation.Api.Lambda;

public static class AskAiEndpoint
{
	public static void MapAskAiEndpoint(this IEndpointRouteBuilder app)
	{
		var askAiGroup = app.MapGroup("/ask-ai");
		_ = askAiGroup.MapPost("/stream", async (AskAiRequest askAiRequest, AskAiUsecase askAiUsecase, Cancel ctx) =>
		{
			var stream = await askAiUsecase.AskAi(askAiRequest, ctx);
			return Results.Stream(stream, "text/event-stream");
		});
	}
}
