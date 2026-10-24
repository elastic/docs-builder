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
}
