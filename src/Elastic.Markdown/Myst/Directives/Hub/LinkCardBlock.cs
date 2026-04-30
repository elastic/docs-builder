// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Elastic.Markdown.Myst.Directives.Hub;

/// <summary>
/// A rich card with title, link, description, primary-link list, and an optional
/// aside (e.g. "Panel types: A · B · C"). The card schema is YAML-formatted in the
/// directive body for predictable structure.
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
/// aside:
///   label: Panel types
///   links:
///     - label: Visualizations
///       url: /viz
///     - label: Maps
///       url: /maps
/// :::
/// </code>
/// </example>
public class LinkCardBlock(DirectiveBlockParser parser, ParserContext context)
	: DirectiveBlock(parser, context), IBlockTitle
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
			Data = YamlSerialization.Deserialize<LinkCardData>(yaml, Build.ProductsConfiguration) ?? LinkCardData.Empty;
		}
		catch (YamlException ex)
		{
			this.EmitError($"{{link-card}} YAML parse error: {ex.Message}");
			return;
		}

		if (string.IsNullOrWhiteSpace(Data.Title))
			this.EmitError("{link-card} requires a `title` field in its YAML body.");
	}
}

[YamlSerializable]
public record LinkCardData
{
	[YamlMember(Alias = "title")]
	public string? Title { get; set; }

	[YamlMember(Alias = "link")]
	public string? Link { get; set; }

	[YamlMember(Alias = "description")]
	public string? Description { get; set; }

	[YamlMember(Alias = "icon")]
	public string? Icon { get; set; }

	[YamlMember(Alias = "variant")]
	public string? Variant { get; set; }

	[YamlMember(Alias = "links")]
	public LinkCardLink[] Links { get; set; } = [];

	[YamlMember(Alias = "aside")]
	public LinkCardAside? Aside { get; set; }

	public static LinkCardData Empty { get; } = new();
}

[YamlSerializable]
public record LinkCardLink
{
	[YamlMember(Alias = "label")]
	public string? Label { get; set; }

	[YamlMember(Alias = "url")]
	public string? Url { get; set; }
}

[YamlSerializable]
public record LinkCardAside
{
	[YamlMember(Alias = "label")]
	public string? Label { get; set; }

	[YamlMember(Alias = "links")]
	public LinkCardLink[] Links { get; set; } = [];
}
