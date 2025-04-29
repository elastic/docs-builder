// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Documentation.Serialization;

namespace Elastic.Documentation.Links;

public record LinkReferenceRegistry
{
	/// Map of branch to <see cref="LinkRegistryEntry"/>
	[JsonPropertyName("repositories")]
	public required Dictionary<string, Dictionary<string, LinkRegistryEntry>> Repositories { get; init; }

	public static LinkReferenceRegistry Deserialize(Stream json) =>
		JsonSerializer.Deserialize(json, SourceGenerationContext.Default.LinkReferenceRegistry)!;

	public static LinkReferenceRegistry Deserialize(string json) =>
		JsonSerializer.Deserialize(json, SourceGenerationContext.Default.LinkReferenceRegistry)!;

	public static string Serialize(LinkReferenceRegistry referenceRegistry) =>
		JsonSerializer.Serialize(referenceRegistry, SourceGenerationContext.Default.LinkReferenceRegistry);
}

public record LinkRegistryEntry
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

