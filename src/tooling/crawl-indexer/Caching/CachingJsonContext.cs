// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace CrawlIndexer.Caching;

/// <summary>
/// Source generation context for JSON serialization in the caching module.
/// </summary>
[JsonSourceGenerationOptions(
	WriteIndented = false,
	PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(OpenPitResponse))]
[JsonSerializable(typeof(SearchResponse))]
internal sealed partial class CachingJsonContext : JsonSerializerContext;

internal sealed class OpenPitResponse
{
	[JsonPropertyName("id")]
	public string? Id { get; set; }
}

internal sealed class SearchResponse
{
	[JsonPropertyName("hits")]
	public HitsContainer? Hits { get; set; }
}

internal sealed class HitsContainer
{
	[JsonPropertyName("hits")]
	public List<Hit>? Hits { get; set; }
}

internal sealed class Hit
{
	[JsonPropertyName("_source")]
	public SourceDocument? Source { get; set; }

	[JsonPropertyName("sort")]
	public object[]? Sort { get; set; }
}

internal sealed class SourceDocument
{
	[JsonPropertyName("url")]
	public string? Url { get; set; }

	[JsonPropertyName("hash")]
	public string? Hash { get; set; }

	[JsonPropertyName("last_updated")]
	public DateTimeOffset? LastUpdated { get; set; }

	[JsonPropertyName("http_etag")]
	public string? HttpEtag { get; set; }

	[JsonPropertyName("http_last_modified")]
	public DateTimeOffset? HttpLastModified { get; set; }

	[JsonPropertyName("enrichment_key")]
	public string? EnrichmentKey { get; set; }

	[JsonPropertyName("enrichment_prompt_hash")]
	public string? EnrichmentPromptHash { get; set; }
}
