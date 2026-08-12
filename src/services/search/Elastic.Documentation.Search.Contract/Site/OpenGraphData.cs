// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Mapping;

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// Open Graph metadata harvested from a crawled page's <c>&lt;meta property="og:*"&gt;</c> tags.
/// Nested under <see cref="SiteDocument.Og"/> (JSON <c>og.*</c>).
/// </summary>
public record OpenGraphData
{
	[Text]
	[JsonPropertyName("title")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Title { get; set; }

	[Text]
	[JsonPropertyName("description")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Description { get; set; }

	[Keyword(Index = false)]
	[JsonPropertyName("image")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Image { get; set; }
}
