// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Mapping;

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// One version tree of an API that renders an operation page with this document's content, e.g. <c>main</c> at
/// <c>/docs/api/doc/elasticsearch</c> or <c>8</c> at <c>/docs/api/doc/elasticsearch/v8</c>. Search scopes by
/// <see cref="Tree"/> and swaps <see cref="Root"/> into hit URLs, so the URL rule stays with the explorer.
/// </summary>
public record ApiTreeEntry
{
	/// <summary>Version moniker of the tree: <c>main</c> or a major such as <c>9</c>.</summary>
	[Keyword]
	[JsonPropertyName("tree")]
	public required string Tree { get; init; }

	/// <summary>URL of the tree's landing page, without a trailing slash.</summary>
	[Keyword(Index = false)]
	[JsonPropertyName("root")]
	public required string Root { get; init; }
}
