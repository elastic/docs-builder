// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.LegacyDocs.Migration.Asciidoc.Ast;
using Slugify;

namespace Elastic.LegacyDocs.Migration.Asciidoc;

public record PageOutput(string Slug, string Title, string MarkdownContent);

public static partial class PageChunker
{
	private static readonly SlugHelper SlugHelper = new();

	[GeneratedRegex(@"^<titleabbrev>(.*)</titleabbrev>\s*$", RegexOptions.Singleline)]
	private static partial Regex TitleAbbrevRegex();

	public static IReadOnlyList<PageOutput> Chunk(AsciidocDocument document, int chunkLevel, MarkdownEmitter emitter)
	{
		if (chunkLevel <= 0)
		{
			emitter.UpdatePageSlug("index");
			return [new PageOutput("index", document.Title ?? "Index", emitter.Emit(document))];
		}

		var (slugMap, titleMap) = BuildAnchorMaps(document.Children, chunkLevel);
		emitter.UpdateAnchorMap(slugMap, titleMap);

		var (pages, remaining) = ExtractPages(document.Children, chunkLevel, emitter);

		var indexDoc = document with { Children = remaining.ToList() };
		emitter.UpdatePageSlug("index");
		var indexContent = emitter.Emit(indexDoc);
		var indexPage = new PageOutput("index", document.Title ?? "Index", indexContent);

		return [indexPage, .. pages];
	}

	private static (Dictionary<string, string> SlugMap, Dictionary<string, string> TitleMap) BuildAnchorMaps(
		IReadOnlyList<IAsciidocNode> children, int chunkLevel)
	{
		var slugMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		var titleMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		CollectAnchors(children, chunkLevel, slugMap, titleMap);
		return (slugMap, titleMap);
	}

	private static void CollectAnchors(
		IReadOnlyList<IAsciidocNode> children, int chunkLevel,
		Dictionary<string, string> slugMap, Dictionary<string, string> titleMap)
	{
		foreach (var child in children)
		{
			// Recurse transparently into open blocks — they may contain sections or anchored blocks
			if (child is OpenBlockNode open)
			{
				CollectAnchors(open.Children, chunkLevel, slugMap, titleMap);
				continue;
			}

			if (child is not SectionNode section)
				continue;

			// Non-include-root Level-0 sections are transparent book-root wrappers
			if (section.Level == 0 && !section.IsIncludeRoot)
			{
				CollectAnchors(section.Children, chunkLevel, slugMap, titleMap);
				continue;
			}

			// Include roots and sections within chunk depth each become a page
			if (section.IsIncludeRoot || section.Level <= chunkLevel)
			{
				var slug = section.Id ?? GenerateSlug(section.Title);
				if (section.Id is not null)
				{
					slugMap[section.Id] = slug;
					titleMap[section.Id] = ExtractDisplayTitle(section);
				}

				CollectChildAnchors(section.Children, slug, slugMap, titleMap);
				CollectAnchors(section.Children, chunkLevel, slugMap, titleMap);
			}
			else
			{
				// Inline section deeper than chunkLevel — recurse for nested IsIncludeRoot sections
				CollectAnchors(section.Children, chunkLevel, slugMap, titleMap);
			}
		}
	}

	// Extracts the display title for a section: uses <titleabbrev> if present, otherwise the section title.
	private static string ExtractDisplayTitle(SectionNode section)
	{
		foreach (var child in section.Children)
		{
			if (child is not PassthroughNode passthrough)
				continue;
			var m = TitleAbbrevRegex().Match(passthrough.Content.Trim());
			if (m.Success)
				return m.Groups[1].Value;
		}
		return section.Title;
	}

	private static void CollectChildAnchors(
		IReadOnlyList<IAsciidocNode> children, string parentSlug,
		Dictionary<string, string> slugMap, Dictionary<string, string> titleMap)
	{
		foreach (var child in children)
		{
			if (child is SectionNode sub && sub.Id is not null)
			{
				slugMap[sub.Id] = parentSlug;
				titleMap[sub.Id] = ExtractDisplayTitle(sub);
				CollectChildAnchors(sub.Children, parentSlug, slugMap, titleMap);
			}
			else if (child is AnchoredBlock anchored)
			{
				slugMap[anchored.Id] = parentSlug;
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
			// Recurse transparently into open blocks so nested sections are chunked correctly
			if (child is OpenBlockNode open)
			{
				var (innerPages, innerRemaining) = ExtractPages(open.Children, chunkLevel, emitter);
				pages.AddRange(innerPages);
				if (innerRemaining.Count > 0)
					remaining.Add(open with { Children = innerRemaining.ToList() });
				continue;
			}

			if (child is not SectionNode section)
			{
				remaining.Add(child);
				continue;
			}

			// Non-include-root Level-0 sections are transparent book-root wrappers
			if (section.Level == 0 && !section.IsIncludeRoot)
			{
				var (innerPages, innerRemaining) = ExtractPages(section.Children, chunkLevel, emitter);
				pages.AddRange(innerPages);
				remaining.Add(section with { Children = innerRemaining.ToList() });
				continue;
			}

			// Inline sections above the chunk depth stay in the parent page, but their
			// children may still contain IsIncludeRoot sections that must be extracted.
			if (section.Level > chunkLevel && !section.IsIncludeRoot)
			{
				var (innerPages, innerRemaining) = ExtractPages(section.Children, chunkLevel, emitter);
				pages.AddRange(innerPages);
				remaining.Add(section with { Children = innerRemaining.ToList() });
				continue;
			}

			var (subPages, sectionRemaining) = ExtractPages(section.Children, chunkLevel, emitter);

			var trimmedSection = section with { Children = sectionRemaining.ToList(), Level = 0 };
			var slug = section.Id ?? GenerateSlug(section.Title);
			emitter.UpdatePageSlug(slug);
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
