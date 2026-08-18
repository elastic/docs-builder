// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Infrastructure;

namespace Elastic.ApiExplorer.Landing;

public class ApiCatalogViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	public required IReadOnlyList<ApiCatalogEntry> Entries { get; init; }
}
