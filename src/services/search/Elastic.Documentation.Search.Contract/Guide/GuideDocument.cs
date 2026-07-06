// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Mapping;

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// Document type for legacy <c>/guide</c> documentation pages on elastic.co.
/// Narrower than <see cref="DocumentationDocument"/> (no applies_to/products); narrower
/// language matrix than <see cref="SiteDocument"/>.
/// </summary>
public record GuideDocument : SearchDocumentBase, ICrawlDocument
{
	[JsonIgnore]
	public override string Type { get; } = "guide";

	[Keyword]
	[JsonPropertyName("product")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Product { get; set; }

	[Keyword]
	[JsonPropertyName("version")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Version { get; set; }

	[Keyword(Index = false)]
	[JsonPropertyName("http_etag")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? HttpEtag { get; set; }

	[JsonPropertyName("http_last_modified")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public DateTimeOffset? HttpLastModified { get; set; }
}
