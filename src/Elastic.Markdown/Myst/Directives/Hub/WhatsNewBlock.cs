// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Elastic.Markdown.Myst.Directives.Hub;

/// <summary>
/// Renders the "What's new" panel: a header with a New badge, a section title,
/// optional release-notes links, and a list of items with title / description /
/// per-item meta pill (e.g. "9.4 preview", "Mar 2026").
/// </summary>
/// <example>
/// <code>
/// :::{whats-new}
/// title: What's new in Kibana
/// id: whats-new
/// release-links:
///   - label: 9.4
///     url: /release-notes/kibana
///   - label: Serverless
///     url: /release-notes/serverless
/// items:
///   - title: Dashboards APIs
///     description: Programmatically create dashboards
///     link: /api/dashboards
///     meta: 9.4 preview
/// :::
/// </code>
/// </example>
public class WhatsNewBlock(DirectiveBlockParser parser, ParserContext context)
	: DirectiveBlock(parser, context)
{
	public override string Directive => "whats-new";

	public WhatsNewData Data { get; private set; } = WhatsNewData.Empty;

	public override void FinalizeAndValidate(ParserContext context)
	{
		var yaml = HubYamlBody.Extract(this, new BuildContextFileReader(Build.ReadFileSystem));
		if (yaml is null)
		{
			this.EmitError("{whats-new} requires a YAML body. See the whats-new directive docs.");
			return;
		}

		try
		{
			Data = YamlSerialization.Deserialize<WhatsNewData>(yaml, Build.ProductsConfiguration) ?? WhatsNewData.Empty;
		}
		catch (YamlException ex)
		{
			this.EmitError($"{{whats-new}} YAML parse error: {ex.Message}");
		}
	}

	public override IEnumerable<string> GeneratedAnchors =>
		string.IsNullOrWhiteSpace(Data.Id) ? [] : [Data.Id!];
}

[YamlSerializable]
public record WhatsNewData
{
	[YamlMember(Alias = "title")]
	public string? Title { get; set; }

	[YamlMember(Alias = "id")]
	public string? Id { get; set; }

	[YamlMember(Alias = "badge")]
	public string? Badge { get; set; } = "New";

	[YamlMember(Alias = "release-links")]
	public LinkCardLink[] ReleaseLinks { get; set; } = [];

	[YamlMember(Alias = "items")]
	public WhatsNewItem[] Items { get; set; } = [];

	public static WhatsNewData Empty { get; } = new();
}

[YamlSerializable]
public record WhatsNewItem
{
	[YamlMember(Alias = "title")]
	public string? Title { get; set; }

	[YamlMember(Alias = "description")]
	public string? Description { get; set; }

	[YamlMember(Alias = "link")]
	public string? Link { get; set; }

	[YamlMember(Alias = "meta")]
	public string? Meta { get; set; }
}
