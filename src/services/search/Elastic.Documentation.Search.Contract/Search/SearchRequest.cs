// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// Full-page search request — supports filters, sorting, aggregations, and (optional) hybrid
/// lex+semantic execution when the target index has <c>semantic_text</c> fields.
/// </summary>
public record SearchRequest
{
	public required string Query { get; init; }
	public int PageNumber { get; init; } = 1;
	public int PageSize { get; init; } = 20;

	/// <summary><c>content_type</c> values (OR-joined). Empty = no filter.</summary>
	public string[] TypeFilter { get; init; } = [];

	/// <summary><c>navigation_section</c> values (OR-joined). Empty = no filter.</summary>
	public string[] SectionFilter { get; init; } = [];

	/// <summary>Product ids matched against <c>related_products.id</c> (AND-joined — page must mention every product). Docs-only.</summary>
	public string[] ProductFilter { get; init; } = [];

	/// <summary><c>applies_to.type</c> deployment values (e.g. ess, eck). Docs-only.</summary>
	public string[] DeploymentFilter { get; init; } = [];

	/// <summary><c>applies_to.version</c> single value. Docs-only.</summary>
	public string? VersionFilter { get; init; }

	public SortMode SortBy { get; init; } = SortMode.Relevance;
	public bool IncludeHighlighting { get; init; } = true;

	/// <summary>Run the hybrid semantic query even when the query does not match the normal natural-language heuristic.</summary>
	public bool ForceSemantic { get; init; }

	/// <summary>
	/// Diagnostic probe bitmask — see <see cref="SearchQueryComponents"/> for bit definitions.
	/// When <c>null</c> (the default) the normal production query is built verbatim.
	/// Set to <c>0</c> for a <c>match_all</c> floor; OR individual bits together to add clauses
	/// back in one at a time. Never intended for production traffic.
	/// </summary>
	public SearchQueryComponents? Components { get; init; }
}
