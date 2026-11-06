// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Export;

/// <summary>
/// One version tree of one API that the explorer renders and the search index exports.
/// <paramref name="ApiKey"/> is the key in <c>/api/doc/{key}/</c>, <paramref name="ProductId"/> the <c>products.yml</c> id it binds to,
/// and <paramref name="Moniker"/> the tree: <c>main</c> or a major such as <c>8</c>.
/// </summary>
public sealed record IndexedTree(string ApiKey, string ProductId, string Moniker, OpenApiDocument Document);
