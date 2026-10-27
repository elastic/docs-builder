// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Versions;
using SharpYaml.Serialization;

namespace Elastic.Markdown.Myst.FrontMatter;

public class YamlFrontMatter
{
	[YamlPropertyName("title")]
	public string? Title { get; set; }

	[YamlPropertyName("description")]
	public string? Description { get; set; }

	[YamlPropertyName("navigation_title")]
	public string? NavigationTitle { get; set; }

	[YamlPropertyName("meta_title")]
	public string? MetaTitle { get; set; }

	[YamlPropertyName("sub")]
	public Dictionary<string, string>? Properties { get; set; }

	[YamlPropertyName("layout")]
	public MarkdownPageLayout? Layout { get; set; }

	[YamlPropertyName("applies_to")]
	public ApplicableTo? AppliesTo { get; set; }

	[YamlPropertyName("mapped_pages")]
	public IReadOnlyCollection<string>? MappedPages { get; set; }

	[YamlPropertyName("products")]
	public IReadOnlyCollection<Product>? Products { get; set; }

	[YamlPropertyName("noindex")]
	public bool? NoIndex { get; set; }

	/// <summary>
	/// Selects a right-gutter CTA template declared in the docset's <c>docset.yml</c> <c>cta</c> map.
	/// Falls back to the built-in <c>trial</c> default when omitted or unknown.
	/// </summary>
	[YamlPropertyName("cta")]
	public CtaFrontMatter? Cta { get; set; }

	/// <summary>
	/// Declares which listing group this page belongs to.
	/// Accepts both shorthand (<c>listing: group-name</c>) and full form (<c>listing: {group: group-name}</c>).
	/// </summary>
	[YamlPropertyName("listing")]
	public ListingFrontMatter? Listing { get; set; }
}

/// <summary>
/// Selects a right-gutter CTA template by <see cref="Id"/>. A dedicated type (rather than a bare string)
/// so future per-page CTA options can be added here without another frontmatter format change.
/// </summary>
public class CtaFrontMatter
{
	[YamlPropertyName("id")]
	public string? Id { get; set; }
}
