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
	[JsonPropertyName("path")]
	public string? Path { get; set; }

	[JsonPropertyName("hash")]
	public string? Hash { get; set; }

	[JsonPropertyName("last_updated")]
	public DateTimeOffset? LastUpdated { get; set; }

	[JsonPropertyName("http")]
	public HttpSourceDocument? Http { get; set; }
}

internal sealed class HttpSourceDocument
{
	[JsonPropertyName("etag")]
	public string? Etag { get; set; }

	[JsonPropertyName("last_modified")]
	public DateTimeOffset? LastModified { get; set; }
}
