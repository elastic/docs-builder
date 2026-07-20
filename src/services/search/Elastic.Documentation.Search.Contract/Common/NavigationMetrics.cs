// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// Rank-feature signals derived from a page's position in the navigation tree. Nested under
/// <see cref="SearchDocumentBase.Navigation"/> (JSON <c>navigation.*</c>).
/// </summary>
public record NavigationMetrics
{
	/// <summary>
	/// URL path segment depth. Mapped as <c>rank_feature</c> with negative score impact — deeper
	/// pages rank lower. Defaults to 50 so documents without explicit navigation metadata are
	/// penalised.
	/// </summary>
	[JsonPropertyName("depth")]
	public int Depth { get; set; } = 50;

	/// <summary>
	/// Number of headings on the page. Mapped as <c>rank_feature</c> with negative score impact.
	/// Defaults to 50 so documents without explicit navigation metadata are penalised.
	/// </summary>
	[JsonPropertyName("table_of_contents")]
	public int TableOfContents { get; set; } = 50;
}
