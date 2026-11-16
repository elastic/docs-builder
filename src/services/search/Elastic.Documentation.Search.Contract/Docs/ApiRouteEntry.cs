// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Mapping;

namespace Elastic.Documentation.Search.Contract;

/// <summary>One route of an operation with every method it accepts. Stored for display in search results, not searched.</summary>
public record ApiRouteEntry
{
	/// <summary>Spec path, verbatim, e.g. <c>/{index}/_search</c>.</summary>
	[Keyword(Index = false)]
	[JsonPropertyName("path")]
	public required string Path { get; init; }

	/// <summary>Upper-case HTTP methods, the one the page leads with first, e.g. <c>POST</c>, <c>GET</c>.</summary>
	[Keyword(Index = false)]
	[JsonPropertyName("methods")]
	public required string[] Methods { get; init; }
}
