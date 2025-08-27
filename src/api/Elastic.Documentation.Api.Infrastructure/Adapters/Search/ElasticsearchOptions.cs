// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Api.Infrastructure.Aws;

namespace Elastic.Documentation.Api.Infrastructure.Adapters.Search;

public class ElasticsearchOptions(IParameterProvider parameterProvider)
{
	public string Url { get; } = parameterProvider.GetParam("docs-elasticsearch-url").GetAwaiter().GetResult();
	public string ApiKey { get; } = parameterProvider.GetParam("docs-elasticsearch-apikey").GetAwaiter().GetResult();
	public string IndexName { get; } = parameterProvider.GetParam("docs-elasticsearch-index").GetAwaiter().GetResult() ?? "documentation-latest";
}
