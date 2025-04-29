// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Documentation.Serialization;

namespace Elastic.Documentation.Links;

public record LinkIndex
{
	[JsonPropertyName("repositories")] public required Dictionary<string, Dictionary<string, LinkIndexEntry>> Repositories { get; init; }

	public static LinkIndex Deserialize(Stream json) =>
		JsonSerializer.Deserialize(json, SourceGenerationContext.Default.LinkIndex)!;

	public static LinkIndex Deserialize(string json) =>
		JsonSerializer.Deserialize(json, SourceGenerationContext.Default.LinkIndex)!;

	public static string Serialize(LinkIndex index) =>
		JsonSerializer.Serialize(index, SourceGenerationContext.Default.LinkIndex);
}

public record LinkIndexEntry
{
	[JsonPropertyName("repository")]
	public required string Repository { get; init; }

	[JsonPropertyName("path")]
	public required string Path { get; init; }

	[JsonPropertyName("branch")]
	public required string Branch { get; init; }

	[JsonPropertyName("etag")]
	public required string ETag { get; init; }

	// TODO can be made required after all doc_sets have published again
	[JsonPropertyName("ref")]
	public string GitReference { get; init; } = "unknown";

	// TODO can be made required after all doc_sets have published again
	[JsonPropertyName("updated_at")]
	public DateTime UpdatedAt { get; init; } = DateTime.MinValue;
}

