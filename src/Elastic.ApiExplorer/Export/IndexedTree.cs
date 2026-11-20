// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Infrastructure;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Export;

/// <summary>
/// One version tree of one API that the explorer renders and the search index exports.
/// <paramref name="Routes"/> are the API's routes, shared by all its trees, <paramref name="ProductId"/> the
/// <c>products.yml</c> id it binds to, and <paramref name="Moniker"/> the tree: <c>main</c> or a major such as <c>8</c>.
/// </summary>
public sealed record IndexedTree(ApiVersionRoutes Routes, string ProductId, string Moniker, OpenApiDocument Document)
{
	/// <summary>The key in <c>/api/{key}/</c>.</summary>
	public string ApiKey => Routes.ApiKey;

	/// <summary>The URL suffix the search index links this tree at: one per version, the versionless one for the default.</summary>
	public string Suffix => Routes.Suffix(Moniker);
}
