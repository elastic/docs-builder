// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;
using SharpYaml;
using SharpYaml.Serialization;

namespace Elastic.Markdown.Myst.Directives.Hub;

/// <summary>
/// A card with a title, an optional link and description, and a list of links. The card
/// schema is YAML-formatted in the directive body for predictable structure.
/// </summary>
/// <example>
/// <code>
/// :::{link-card}
/// title: Discover
/// link: /discover/
/// description: Browse documents, filter, and query your indices in real time.
/// links:
///   - label: Get started with Discover
///     url: /discover/get-started
///   - label: Use ES|QL in Kibana
///     url: /esql
/// :::
/// </code>
/// </example>
public class LinkCardBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context), IBlockTitle
{
	public override string Directive => "link-card";

	public LinkCardData Data { get; private set; } = LinkCardData.Empty;

	public string Title => Data.Title ?? string.Empty;

	public override void FinalizeAndValidate(ParserContext context)
	{
		var yaml = HubYamlBody.Extract(this, new BuildContextFileReader(Build.ReadFileSystem));
		if (yaml is null)
		{
			this.EmitError("{link-card} requires a YAML body. See the link-card directive docs.");
			return;
		}

		try
		{
			Data = YamlSerialization.Deserialize<LinkCardData>(yaml, Build.ProductsConfiguration, this.YamlReporting())
				?? LinkCardData.Empty;
		}
		catch (YamlException ex)
		{
			this.EmitError($"{{link-card}} YAML parse error: {ex.Message}");
			return;
		}

		if (string.IsNullOrWhiteSpace(Data.Title))
			this.EmitError("{link-card} requires a `title` field in its YAML body.");

		Data.Link = DirectiveLinkValidator.ValidateAndResolve(Data.Link, this, context);
		foreach (var link in Data.Links)
			link.Url = DirectiveLinkValidator.ValidateAndResolve(link.Url, this, context);
	}
}

public record LinkCardData
{
	[YamlPropertyName("title")]
	public string? Title { get; set; }

	[YamlPropertyName("link")]
	public string? Link { get; set; }

	[YamlPropertyName("description")]
	public string? Description { get; set; }

	[YamlPropertyName("icon")]
	public string? Icon { get; set; }

	[YamlPropertyName("variant")]
	public string? Variant { get; set; }

	[YamlPropertyName("links")]
	public LinkCardLink[] Links { get; set; } = [];

	[YamlIgnore]
	public static LinkCardData Empty { get; } = new();
}

public record LinkCardLink
{
	[YamlPropertyName("label")]
	public string? Label { get; set; }

	[YamlPropertyName("url")]
	public string? Url { get; set; }
}
