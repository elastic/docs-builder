// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Mapping;

namespace Elastic.Documentation.Search.Contract;

public record ApiPropertyEntry
{
	[Keyword]
	[JsonPropertyName("name")]
	public required string Name { get; init; }

	[Keyword]
	[JsonPropertyName("path")]
	public required string Path { get; init; }

	/// <summary>The <see cref="ApiPropertyPath.Words"/> of <see cref="Path"/> joined by spaces: the standard analyzer keeps <c>min_score</c> whole and gives each word a position for a phrase match. The field is search_as_you_type, so its index prefixes serve the last word's prefix and a partly typed segment is not subject to the phrase prefix expansion limit.</summary>
	[Text]
	[JsonPropertyName("path_words")]
	public string PathWords { get; init; } = "";

	[Keyword]
	[JsonPropertyName("type")]
	public required string Type { get; init; }

	[Keyword]
	[JsonPropertyName("location")]
	public required string Location { get; init; }

	[Keyword]
	[JsonPropertyName("anchor")]
	public required string Anchor { get; init; }

	[Text]
	[JsonPropertyName("description")]
	public required string Description { get; init; }

	/// <summary><see cref="ApiPropertyPath.ParentKey"/> of the path of the row this one is listed under, so a typed path matches it; empty for a top-level field or a parameter.</summary>
	[Keyword]
	[JsonPropertyName("parent")]
	public string Parent { get; init; } = "";
}
