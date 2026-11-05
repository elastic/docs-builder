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
	[Nested]
	[JsonPropertyName("properties")]
	public ApiPropertyEntry[] Properties { get; init; } = [];

	/// <summary>Every route the operation page lists, with its methods, in page order.</summary>
	[Object]
	[JsonPropertyName("routes")]
	public ApiRouteEntry[] Routes { get; init; } = [];

	/// <summary>
	/// Lowercased endpoint paths without the leading <c>/</c>, with and without leading and trailing
	/// parameters, each parameter stored as <c>*</c>, e.g. <c>*/_search</c> and <c>_search</c>.
	/// Plain keyword so the typed term matches exactly.
	/// </summary>
	[Keyword]
	[JsonPropertyName("endpoints")]
	public required string[] Endpoints { get; init; }

	/// <summary>
	/// The keys of <see cref="Endpoints"/> without the underscore starting each segment, e.g. <c>search</c> for
	/// <c>_search</c>, so a typed <c>/search</c> finds the system route.
	/// </summary>
	[Keyword]
	[JsonPropertyName("endpoints_bare")]
	public string[] EndpointsBare { get; init; } = [];

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

	/// <summary>
	/// Plain lowercase words of the operation id and its literal path segments, e.g. <c>cluster health</c>.
	/// A typed <c>cluster</c> or <c>esql</c> names an operation by these words, which otherwise live only
	/// concatenated into <c>search_title</c>.
	/// </summary>
	[Text]
	[JsonPropertyName("words")]
	public string[] Words { get; init; } = [];

	/// <summary>Title of the operation's group as the explorer shows it, e.g. <c>ES|QL</c>. Empty without a group.</summary>
	[Text]
	[JsonPropertyName("group")]
	public string Group { get; init; } = "";

	/// <summary>
	/// Number of words in the operation id alone, e.g. 2 for <c>cluster.health</c>. Mapped as
	/// <c>rank_feature</c> with negative score impact. Fewer id words is a more specific operation,
	/// so ties break toward <c>esql-query</c> over <c>esql-async-query-stop</c>.
	/// </summary>
	[JsonPropertyName("id_words")]
	public int IdWords { get; init; }

	/// <summary>Whether the spec marks the operation deprecated.</summary>
	[Boolean]
	[JsonPropertyName("deprecated")]
	public bool Deprecated { get; init; }
}
