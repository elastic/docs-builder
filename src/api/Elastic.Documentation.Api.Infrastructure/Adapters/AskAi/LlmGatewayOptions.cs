// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Api.Infrastructure.Aws;

namespace Elastic.Documentation.Api.Infrastructure.Adapters.AskAi;

public class LlmGatewayOptions
{
	public LlmGatewayOptions(IParameterProvider parameterProvider)
	{
		ServiceAccount = parameterProvider.GetParam("llm-gateway-service-account").GetAwaiter().GetResult();
		FunctionUrl = parameterProvider.GetParam("llm-gateway-function-url").GetAwaiter().GetResult();
		var uri = new Uri(FunctionUrl);
		TargetAudience = $"{uri.Scheme}://{uri.Host}";
	}

	public string ServiceAccount { get; }
	public string FunctionUrl { get; }
	public string TargetAudience { get; }
}
