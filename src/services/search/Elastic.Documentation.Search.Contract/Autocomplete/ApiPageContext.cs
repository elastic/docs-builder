// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// The API page a reader searches from: key <c>elasticsearch</c> anywhere under <c>/docs/api/doc/elasticsearch</c>, and
/// tree <c>8</c> only inside <c>/docs/api/doc/elasticsearch/v8</c>. The key lifts that API's hits; the tree also scopes them.
/// </summary>
public record ApiPageContext(string Key, string? Tree);
