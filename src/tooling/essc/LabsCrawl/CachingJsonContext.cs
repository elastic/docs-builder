// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.SiteSearch.Cli.LabsCrawl;

[JsonSourceGenerationOptions(
	WriteIndented = false,
	PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(SourceDocument))]
internal sealed partial class CachingJsonContext : JsonSerializerContext;

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
