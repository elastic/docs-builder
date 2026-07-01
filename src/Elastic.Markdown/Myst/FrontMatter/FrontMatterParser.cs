// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Versions;
using YamlDotNet.Serialization;

namespace Elastic.Markdown.Myst.FrontMatter;

[YamlSerializable]
public class YamlFrontMatter
{

	[YamlMember(Alias = "title")]
	public string? Title { get; set; }

	[YamlMember(Alias = "description")]
	public string? Description { get; set; }

	[YamlMember(Alias = "navigation_title")]
	public string? NavigationTitle { get; set; }

	[YamlMember(Alias = "sub")]
	public Dictionary<string, string>? Properties { get; set; }

	[YamlMember(Alias = "layout")]
	public MarkdownPageLayout? Layout { get; set; }

	[YamlMember(Alias = "applies_to")]
	public ApplicableTo? AppliesTo { get; set; }

	[YamlMember(Alias = "mapped_pages")]
	public IReadOnlyCollection<string>? MappedPages { get; set; }

	[YamlMember(Alias = "products")]
	public IReadOnlyCollection<Product>? Products { get; set; }

	[YamlMember(Alias = "noindex")]
	public bool? NoIndex { get; set; }

	/// <summary>
	/// Name of a right-gutter CTA template declared in the docset's <c>docset.yml</c> <c>cta</c> map.
	/// Falls back to the built-in <c>trial</c> default when omitted or unknown.
	/// </summary>
	[YamlMember(Alias = "cta")]
	public string? Cta { get; set; }
}
