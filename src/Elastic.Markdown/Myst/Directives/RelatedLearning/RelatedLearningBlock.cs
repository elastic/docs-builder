// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.RelatedLearning;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Helpers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Myst.Directives.RelatedLearning;

public class RelatedLearningBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public const string DefaultHeading = "Related learning";
	public const string DefaultSlug = "related-learning-heading";

	public override string Directive => "related-learning";

	public string Heading { get; private set; } = DefaultHeading;

	public string Slug { get; private set; } = DefaultSlug;

	public IReadOnlyList<RelatedLearningLink> Items { get; private set; } = [];

	public override IEnumerable<string> GeneratedAnchors => Items.Count == 0 ? [] : [Slug];

	/// <summary>
	/// Inserts a real H2 before each resolved directive so GetAnchors and HTML share one slug.
	/// Call on both the minimal-parse and full-parse documents.
	/// </summary>
	public static void InsertHeadings(MarkdownDocument document)
	{
		var blocks = document.Descendants<RelatedLearningBlock>().Where(b => b.Items.Count > 0).ToArray();
		foreach (var block in blocks)
			InsertHeading(block);
	}

	public override void FinalizeAndValidate(ParserContext context)
	{
		var rawHeading = Prop("heading");
		var customizedHeading = !string.IsNullOrWhiteSpace(rawHeading);
		Heading = customizedHeading ? rawHeading!.Trim() : DefaultHeading;
		Slug = customizedHeading ? Heading.Slugify() : DefaultSlug;
		Items = ResolveItems(context);
	}

	private IReadOnlyList<RelatedLearningLink> ResolveItems(ParserContext context)
	{
		var raw = Arguments?.Trim();
		if (string.IsNullOrWhiteSpace(raw))
		{
			this.EmitError("{related-learning} requires at least one catalog ID as an argument.");
			return [];
		}

		var ids = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		if (ids.Length == 0)
		{
			this.EmitError("{related-learning} requires at least one catalog ID as an argument.");
			return [];
		}

		var catalog = context.Build.RelatedLearningConfiguration;
		var seen = new HashSet<string>(StringComparer.Ordinal);
		var items = new List<RelatedLearningLink>(ids.Length);
		foreach (var id in ids)
		{
			if (!seen.Add(id))
			{
				this.EmitWarning($"{{related-learning}} duplicate catalog ID '{id}' was skipped.");
				continue;
			}

			if (!catalog.TryGet(id, out var link))
			{
				this.EmitError($"{{related-learning}} unknown catalog ID '{id}'.");
				continue;
			}

			items.Add(link);
		}

		return items;
	}

	private static void InsertHeading(RelatedLearningBlock block)
	{
		if (block.Parent is not { } parent)
			return;

		var index = parent.IndexOf(block);
		if (index < 0)
			return;

		if (index > 0 && parent[index - 1] is HeadingBlock existing && (existing.GetData("anchor") as string) == block.Slug)
			return;

		var heading = new HeadingBlock(null!) { Level = 2, Line = block.Line };
		heading.SetData("header", block.Heading);
		heading.SetData("anchor", block.Slug);
		heading.Inline = new ContainerInline();
		_ = heading.Inline.AppendChild(new LiteralInline(block.Heading));
		parent.Insert(index, heading);
	}
}
