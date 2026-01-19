// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Configuration;

namespace Elastic.Documentation.Api.Infrastructure.Adapters.AskAi;

public class KibanaOptions(IConfiguration configuration)
{
	public string Url { get; } = configuration["DOCUMENTATION_KIBANA_URL"]
		?? throw new InvalidOperationException("DOCUMENTATION_KIBANA_URL not configured");
	public string ApiKey { get; } = configuration["DOCUMENTATION_KIBANA_APIKEY"]
		?? throw new InvalidOperationException("DOCUMENTATION_KIBANA_APIKEY not configured");
}
