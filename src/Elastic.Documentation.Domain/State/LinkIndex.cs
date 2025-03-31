// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elastic.Markdown.Links.CrossLinks;

public record LinkIndexRegistry
{
	[JsonPropertyName("repositories")] public required Dictionary<string, Dictionary<string, LinkIndexEntry>> Repositories { get; init; }

	public static LinkIndexRegistry Deserialize(string json) =>
		JsonSerializer.Deserialize(json, SourceGenerationContext.Default.LinkIndexRegistry)!;

	public static string Serialize(LinkIndexRegistry indexRegistry) =>
		JsonSerializer.Serialize(indexRegistry, SourceGenerationContext.Default.LinkIndexRegistry);
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
}

