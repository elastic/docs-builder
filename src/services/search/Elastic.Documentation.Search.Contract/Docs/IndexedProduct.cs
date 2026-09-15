// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Mapping;

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// JSON-serializable product reference embedded on <see cref="DocumentationDocument.Product"/> /
/// <see cref="DocumentationDocument.RelatedProducts"/>. Only id + repository are stored;
/// display names are resolved at read time by the consumer.
/// </summary>
public record IndexedProduct
{
	[Keyword(Normalizer = "keyword_normalizer")]
	[JsonPropertyName("id")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Id { get; set; }

	[Keyword(Normalizer = "keyword_normalizer")]
	[JsonPropertyName("repository")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Repository { get; set; }
}
