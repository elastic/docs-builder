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

		var (pages, remaining) = ExtractPages(document.Children, chunkLevel, emitter);

		var indexDoc = document with { Children = remaining.ToList() };
		var indexContent = emitter.Emit(indexDoc);
		var indexPage = new PageOutput("index", document.Title ?? "Index", indexContent);

		return [indexPage, .. pages];
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
			if (child is not SectionNode section || section.Level > chunkLevel)
			{
				remaining.Add(child);
				continue;
			}

			var (subPages, sectionRemaining) = ExtractPages(section.Children, chunkLevel, emitter);

			var trimmedSection = section with { Children = sectionRemaining.ToList() };
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
