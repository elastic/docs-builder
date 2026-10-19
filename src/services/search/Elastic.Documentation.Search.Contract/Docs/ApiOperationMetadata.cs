// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Mapping;

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// Attributes of an OpenAPI operation page. Nested under <see cref="DocumentationDocument.Api"/>
/// (JSON <c>api.*</c>) and null on Markdown pages.
/// </summary>
public record ApiOperationMetadata
{
	/// <summary>
	/// Lowercased endpoint paths without the leading <c>/</c>, with and without leading and trailing
	/// <c>{param}</c> segments, e.g. <c>{index}/_search</c> and <c>_search</c>. Plain keyword so an
	/// exact term query on what the user typed matches; <c>keyword_normalizer</c> would strip <c>/ { }</c>.
	/// </summary>
	[Keyword]
	[JsonPropertyName("endpoints")]
	public required string[] Endpoints { get; init; }

	/// <summary>HTTP method of the operation, e.g. <c>POST</c>.</summary>
	[Keyword]
	[JsonPropertyName("method")]
	public required string Method { get; init; }

	/// <summary>Spec path of the operation, verbatim, e.g. <c>/{index}/_search</c>.</summary>
	[Keyword]
	[JsonPropertyName("path")]
	public required string Path { get; init; }

	/// <summary>OpenAPI operation id, e.g. <c>search</c>.</summary>
	[Keyword]
	[JsonPropertyName("operation_id")]
	public required string OperationId { get; init; }
}
