// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Logging;

namespace Api.Core.AskAi;

public class AskAiUsecase(IAskAiGateway<Stream> askAiGateway, ILogger<AskAiUsecase> logger)
{
	public async Task<Stream> AskAi(AskAiRequest askAiRequest, Cancel ctx)
	{
		logger.LogDebug("Processing AskAiRequest: {Request}", askAiRequest);
		return await askAiGateway.AskAi(askAiRequest, ctx);
	}
}
