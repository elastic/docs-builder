// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Configuration;

namespace Elastic.Documentation.Api.Infrastructure.Adapters.Search;

public class ElasticsearchOptions(IConfiguration configuration)
{
	// Read from environment variables (set by Terraform from SSM at deploy time)
	public string Url { get; } = configuration["DOCUMENTATION_ELASTIC_URL"]
		?? throw new InvalidOperationException("DOCUMENTATION_ELASTIC_URL not configured");
	public string ApiKey { get; } = configuration["DOCUMENTATION_ELASTIC_APIKEY"]
		?? throw new InvalidOperationException("DOCUMENTATION_ELASTIC_APIKEY not configured");
	public string IndexName { get; } = configuration["DOCUMENTATION_ELASTIC_INDEX"]
		?? "documentation-latest";
}
