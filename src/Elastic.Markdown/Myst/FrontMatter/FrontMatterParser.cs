// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Site;
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
	public LayoutName? Layout { get; set; }

	[YamlMember(Alias = "applies_to")]
	public ApplicableTo? AppliesTo { get; set; }

	[YamlMember(Alias = "mapped_pages")]
	public IReadOnlyCollection<string>? MappedPages { get; set; }

	[YamlMember(Alias = "products")]
	public IReadOnlyCollection<Product>? Products { get; set; }
}
