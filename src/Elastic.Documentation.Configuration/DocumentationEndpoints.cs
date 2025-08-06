// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Configuration;

public record DocumentationEndpoints
{
	public required ElasticsearchEndpoint Elasticsearch { get; init; }
}

public record ElasticsearchEndpoint
{
	public static ElasticsearchEndpoint Default { get; } = new ElasticsearchEndpoint { Uri = new Uri("https://localhost:9200") };

	public required Uri Uri { get; init; }
	public string? Username { get; init; }
	public string? Password { get; init; }
	public string? ApiKey { get; init; }
}
