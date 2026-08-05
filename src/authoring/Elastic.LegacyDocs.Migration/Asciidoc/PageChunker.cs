// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.LegacyDocs.Migration.Asciidoc.Ast;
using Slugify;

namespace Elastic.LegacyDocs.Migration.Asciidoc;

public record PageOutput(string Slug, string Title, string MarkdownContent);

public static class PageChunker
{
	private static readonly SlugHelper SlugHelper = new();

	public static IReadOnlyList<PageOutput> Chunk(AsciidocDocument document, int chunkLevel, MarkdownEmitter emitter)
	{
		if (chunkLevel <= 0)
			return [new PageOutput("index", document.Title ?? "Index", emitter.Emit(document))];

		var anchorMap = BuildAnchorMap(document.Children, chunkLevel);
		emitter.UpdateAnchorMap(anchorMap);

		var (pages, remaining) = ExtractPages(document.Children, chunkLevel, emitter);

		var indexDoc = document with { Children = remaining.ToList() };
		var indexContent = emitter.Emit(indexDoc);
		var indexPage = new PageOutput("index", document.Title ?? "Index", indexContent);

		return [indexPage, .. pages];
	}

	private static Dictionary<string, string> BuildAnchorMap(IReadOnlyList<IAsciidocNode> children, int chunkLevel)
	{
		var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		CollectAnchors(children, chunkLevel, map);
		return map;
	}

	private static void CollectAnchors(IReadOnlyList<IAsciidocNode> children, int chunkLevel, Dictionary<string, string> map)
	{
		foreach (var child in children)
		{
			if (child is not SectionNode section)
				continue;

			if (section.Level == 0)
			{
				CollectAnchors(section.Children, chunkLevel, map);
				continue;
			}

			if (section.Level <= chunkLevel)
			{
				var slug = section.Id ?? GenerateSlug(section.Title);
				if (section.Id is not null)
					map[section.Id] = slug;

				CollectChildAnchors(section.Children, slug, map);
				CollectAnchors(section.Children, chunkLevel, map);
			}
		}
	}

	private static void CollectChildAnchors(IReadOnlyList<IAsciidocNode> children, string parentSlug, Dictionary<string, string> map)
	{
		foreach (var child in children)
		{
			if (child is SectionNode sub && sub.Id is not null)
			{
				map[sub.Id] = parentSlug;
				CollectChildAnchors(sub.Children, parentSlug, map);
			}
			else if (child is AnchoredBlock anchored)
			{
				map[anchored.Id] = parentSlug;
			}
		}
	}

	private static (List<PageOutput> Pages, List<IAsciidocNode> Remaining) ExtractPages(
		IReadOnlyList<IAsciidocNode> children,
		int chunkLevel,
		MarkdownEmitter emitter
	)
	{
		var pages = new List<PageOutput>();
		var remaining = new List<IAsciidocNode>();

		foreach (var child in children)
		{
			if (child is not SectionNode section)
			{
				remaining.Add(child);
				continue;
			}

			if (section.Level == 0)
			{
				var (innerPages, innerRemaining) = ExtractPages(section.Children, chunkLevel, emitter);
				pages.AddRange(innerPages);
				remaining.Add(section with { Children = innerRemaining.ToList() });
				continue;
			}

			if (section.Level > chunkLevel)
			{
				remaining.Add(child);
				continue;
			}

			var (subPages, sectionRemaining) = ExtractPages(section.Children, chunkLevel, emitter);

			var trimmedSection = section with { Children = sectionRemaining.ToList(), Level = 0 };
			var slug = section.Id ?? GenerateSlug(section.Title);
			var content = emitter.Emit(trimmedSection);

			pages.Add(new PageOutput(slug, section.Title, content));
			pages.AddRange(subPages);
		}

		return (pages, remaining);
	}

	private static string GenerateSlug(string title)
	{
		var slug = SlugHelper.GenerateSlug(title);
		return string.IsNullOrEmpty(slug) ? "section" : slug;
	}
}
