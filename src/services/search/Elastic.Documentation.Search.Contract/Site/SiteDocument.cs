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
	[JsonPropertyName("locale")]
	public string Locale { get; set; } = "en";

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

	[Object]
	[JsonPropertyName("og")]
	public OpenGraphData Og { get; set; } = new();

	[Object]
	[JsonPropertyName("twitter")]
	public TwitterCardData Twitter { get; set; } = new();

	[Object]
	[JsonPropertyName("http")]
	public HttpMetadata Http { get; set; } = new();
}
