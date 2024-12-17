// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using YamlDotNet.Serialization;

namespace Elastic.Markdown.Myst.FrontMatter;

[YamlSerializable]
public class YamlFrontMatter
{
	[YamlMember(Alias = "title")]
	public string? Title { get; set; }

	[YamlMember(Alias = "navigation_title")]
	public string? NavigationTitle { get; set; }

	[YamlMember(Alias = "sub")]
	public Dictionary<string, string>? Properties { get; set; }


	[YamlMember(Alias = "applies")]
	public Deployment? AppliesTo { get; set; }
}
