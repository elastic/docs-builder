// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Mapping;

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// Document type for non-documentation pages on elastic.co (marketing, blog, product pages, etc.).
/// Sourced from ContentStack via the <c>essc</c> sync command.
/// </summary>
public record SiteDocument : SearchDocumentBase, ICrawlDocument
{
	[JsonIgnore]
	public override string Type { get; } = "site";

	[Keyword]
	[JsonPropertyName("language")]
	public string Language { get; set; } = "en";

	[Keyword]
	[JsonPropertyName("author")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Author { get; set; }

	[JsonPropertyName("published_date")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public DateTimeOffset? PublishedDate { get; set; }

	[JsonPropertyName("modified_date")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public DateTimeOffset? ModifiedDate { get; set; }

	[Text]
	[JsonPropertyName("og_title")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? OgTitle { get; set; }

	[Text]
	[JsonPropertyName("og_description")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? OgDescription { get; set; }

	[Keyword(Index = false)]
	[JsonPropertyName("og_image")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? OgImage { get; set; }

	[Keyword(Index = false)]
	[JsonPropertyName("twitter_image")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? TwitterImage { get; set; }

	[Keyword]
	[JsonPropertyName("twitter_card")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? TwitterCard { get; set; }

	[Keyword]
	[JsonPropertyName("enrichment_key")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? EnrichmentKey { get; set; }

	[Keyword(Index = false)]
	[JsonPropertyName("http_etag")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? HttpEtag { get; set; }

	[JsonPropertyName("http_last_modified")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public DateTimeOffset? HttpLastModified { get; set; }
}
