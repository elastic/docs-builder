// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.RelatedLearning;
using Elastic.Markdown;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Helpers;

namespace Elastic.Markdown.Myst.Directives.RelatedLearning;

public class RelatedLearningBlock(DirectiveBlockParser parser, ParserContext context)
	: DirectiveBlock(parser, context)
{
	public const string DefaultHeading = "Related learning";
	public const string DefaultSlug = "related-learning-heading";

	public override string Directive => "related-learning";

	public string Heading { get; private set; } = DefaultHeading;

	public string Slug { get; private set; } = DefaultSlug;

	public IReadOnlyList<RelatedLearningLink> Items { get; private set; } = [];

	public IEnumerable<PageTocItem> GeneratedTableOfContent =>
		Items.Count == 0
			? []
			:
			[
				new PageTocItem
				{
					Heading = Heading,
					Slug = Slug,
					Level = 2
				}
			];

	public override IEnumerable<string> GeneratedAnchors =>
		Items.Count == 0 ? [] : [Slug];

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
		var raw = Prop("ids");
		if (string.IsNullOrWhiteSpace(raw))
		{
			this.EmitError("{related-learning} requires :ids: with at least one catalog ID.");
			return [];
		}

		var ids = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		if (ids.Length == 0)
		{
			this.EmitError("{related-learning} requires :ids: with at least one catalog ID.");
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
}
