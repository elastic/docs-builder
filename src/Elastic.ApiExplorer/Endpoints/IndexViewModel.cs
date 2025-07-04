// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Landing;
using Elastic.Documentation;

namespace Elastic.ApiExplorer.Endpoints;

public class IndexViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	public required ApiEndpoint ApiEndpoint { get; init; }
}
