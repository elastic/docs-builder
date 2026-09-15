// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Mapping;

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// Twitter Card metadata harvested from a crawled page's <c>&lt;meta name="twitter:*"&gt;</c> tags.
/// Nested under <see cref="SiteDocument.Twitter"/> (JSON <c>twitter.*</c>).
/// </summary>
public record TwitterCardData
{
	[Keyword(Index = false)]
	[JsonPropertyName("image")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Image { get; set; }

	[Keyword]
	[JsonPropertyName("card")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Card { get; set; }
}
