// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Mapping;

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// Concrete document type for <c>/docs</c> pages indexed by docs-builder. Carries the docs-specific
/// fields (applicability matrix, primary/related products, change tracking, outbound links) on top
/// of <see cref="SearchDocumentBase"/>.
/// </summary>
public record DocumentationDocument : SearchDocumentBase
{
	[JsonIgnore]
	public override string Type { get; } = "docs";

	/// <summary>Canonical product id for this page (the product the page is primarily about).</summary>
	[Keyword(Normalizer = "keyword_normalizer")]
	[JsonPropertyName("product")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Product { get; set; }

	/// <summary>All related products discovered through inference (primary + cross-references).</summary>
	[JsonPropertyName("related_products")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public IndexedProduct[]? RelatedProducts { get; set; }

	/// <summary>Applicability matrix entries (deployment type / sub-type / lifecycle / version).</summary>
	[Nested]
	[JsonPropertyName("applies_to")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public IReadOnlyCollection<AppliesToEntry>? Applies { get; set; }

	/// <summary>Outbound link URLs harvested from the page body.</summary>
	[JsonPropertyName("links")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string[]? Links { get; set; }

	/// <summary>
	/// Whitespace-normalized hash of the page body. Drives the docs change feed —
	/// only advances when the meaningful content changes.
	/// </summary>
	[Keyword]
	[JsonPropertyName("content_hash")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string ContentBodyHash { get; set; } = string.Empty;

	/// <summary>
	/// Timestamp of the last meaningful content change. Drives the docs change feed
	/// (cursor-paginated diff API).
	/// </summary>
	[JsonPropertyName("content_last_updated")]
	public DateTimeOffset ContentLastUpdated { get; set; }
}
