// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System.Text.RegularExpressions;
using Elastic.LegacyDocs.Migration.Asciidoc.Ast;

namespace Elastic.LegacyDocs.Migration.Asciidoc;

public record PageOutput(
	string Slug,
	string Title,
	string? NavigationTitle,
	string MarkdownContent,
	IReadOnlyList<PageOutput> Children);

public static partial class PageChunker
{
	[GeneratedRegex(@"^<titleabbrev>(.*)</titleabbrev>\s*$", RegexOptions.Singleline)]
	private static partial Regex TitleAbbrevRegex();

	[GeneratedRegex(@"[^a-z0-9]+", RegexOptions.None)]
	private static partial Regex SlugNonAlphanumericRegex();

	/// <summary>
	/// Chunks the document into a tree of pages using the same rule as the legacy asciidoctor
	/// chunker: a section becomes its own page iff it is not [discrete]/[float] and its level
	/// is &lt;= chunkLevel + 1 (where chunkLevel == conf.yaml chunk:).
	/// </summary>
	public static IReadOnlyList<PageOutput> Chunk(
		AsciidocDocument document, int chunkLevel, MarkdownEmitter emitter,
		Action<string>? onDiagnostic = null)
	{
		if (chunkLevel <= 0)
		{
			emitter.UpdatePageSlug("index");
			emitter.UpdateHeadingBase(0);
			return [new PageOutput("index", document.Title ?? "Index", null, emitter.Emit(document), [])];
		}

		// Effective chunk level in AST terms: chunk:1 => sections at level <= 2 are pages.
		var effectiveChunkLevel = chunkLevel + 1;

		var slugMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		var titleMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		var allocatedSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		_ = allocatedSlugs.Add("index");

		// Detect the book root: a single Level-0 section in document.Children is always the
		// book root / index wrapper, whether it arrived inline (IsIncludeRoot=false) or via a
		// top-level include (IsIncludeRoot=true, e.g. index.x.asciidoc → index.asciidoc).
		// Map its id to "index" so cross-references like [[elasticsearch-reference]] resolve
		// to index.md, then traverse its children instead of the document root's children.
		SectionNode? bookRoot = null;
		if (document.Children is [SectionNode { Level: 0 } candidate])
			bookRoot = candidate;

		if (document.Id is not null)
			slugMap[document.Id] = "index";
		if (bookRoot?.Id is not null)
			slugMap[bookRoot.Id] = "index";

		var rootTitle = bookRoot?.Title ?? document.Title ?? "Index";
		var rootChildren = bookRoot is not null ? bookRoot.Children : document.Children;

		// Single traversal: build the page tree, anchor maps, and inline content together.
		var (rootPageNodes, indexInlineContent) = Traverse(
			rootChildren, effectiveChunkLevel, slugMap, titleMap, allocatedSlugs, onDiagnostic);

		// Map any inline content at the doc root to the index slug.
		CollectChildAnchors(indexInlineContent, "index", slugMap, titleMap);

		// Publish anchor maps to the emitter before any emission.
		emitter.UpdateAnchorMap(slugMap, titleMap);

		// Emit the index page using the book root section so its H1 and id are included.
		var indexSection = bookRoot is not null
			? bookRoot with { Children = indexInlineContent, Level = 0 }
			: null;
		emitter.UpdatePageSlug("index");
		emitter.UpdateHeadingBase(0);
		var indexContent = indexSection is not null
			? emitter.Emit(indexSection)
			: emitter.Emit(document with { Children = indexInlineContent });
		var indexPage = new PageOutput("index", rootTitle, null, indexContent, []);

		var childPages = EmitPageNodes(rootPageNodes, emitter);
		return [indexPage, .. childPages];
	}

	// ── Internal page node (pre-emission) ─────────────────────────────────────

	private sealed record PageNode(
		SectionNode Section,
		string Slug,
		string? NavigationTitle,
		List<IAsciidocNode> InlineContent,
		List<PageNode> ChildPages);

	// ── Traversal ─────────────────────────────────────────────────────────────

	/// <summary>
	/// Recursively splits AST children into page nodes and inline content.
	/// Pages become their own entries in the nav tree; inline content stays in the parent page body.
	/// </summary>
	private static (List<PageNode> Pages, List<IAsciidocNode> Inline) Traverse(
		IReadOnlyList<IAsciidocNode> children, int effectiveChunkLevel,
		Dictionary<string, string> slugMap, Dictionary<string, string> titleMap,
		HashSet<string> allocatedSlugs, Action<string>? onDiagnostic)
	{
		var pages = new List<PageNode>();
		var inline = new List<IAsciidocNode>();

		foreach (var child in children)
		{
			switch (child)
			{
				case OpenBlockNode open:
					{
						// Transparent container: hoist inner pages up; rewrap remaining inline nodes.
						var (innerPages, innerInline) = Traverse(
							open.Children, effectiveChunkLevel, slugMap, titleMap, allocatedSlugs, onDiagnostic);
						pages.AddRange(innerPages);
						if (innerInline.Count > 0)
							inline.Add(open with { Children = innerInline });
						break;
					}

				case SectionNode section when IsPage(section, effectiveChunkLevel):
					{
						// This section becomes its own page.
						var slug = AllocateSlug(section, allocatedSlugs, onDiagnostic);
						if (section.Id is not null)
						{
							slugMap[section.Id] = slug;
							titleMap[section.Id] = ExtractDisplayTitle(section);
						}

						var (subPages, subInline) = Traverse(
							section.Children, effectiveChunkLevel, slugMap, titleMap, allocatedSlugs, onDiagnostic);

						// Map all inline sub-content's anchors to this page's slug.
						CollectChildAnchors(subInline, slug, slugMap, titleMap);

						var navTitle = ExtractDisplayTitle(section);
						var navTitleOut = navTitle == section.Title ? null : navTitle;

						pages.Add(new PageNode(section, slug, navTitleOut, subInline, subPages));
						break;
					}

				case SectionNode section:
					{
						var (innerPages, innerInline) = Traverse(
							section.Children, effectiveChunkLevel, slugMap, titleMap, allocatedSlugs, onDiagnostic);
						pages.AddRange(innerPages);
						if (section.Level == 0 && !section.IsIncludeRoot)
						{
							// Transparent book-root wrapper: its inline content flows into the index page
							// directly (no wrapper heading), so we hoist both pages and inline nodes.
							inline.AddRange(innerInline);
						}
						else
						{
							// Non-page discrete/too-deep section: keep the section heading inline.
							inline.Add(section with { Children = innerInline });
						}
						break;
					}

				default:
					inline.Add(child);
					break;
			}
		}

		return (pages, inline);
	}

	// ── Anchor collection ─────────────────────────────────────────────────────

	/// <summary>Maps every block anchor and non-page sub-section id to the parent page's slug.</summary>
	private static void CollectChildAnchors(
		IReadOnlyList<IAsciidocNode> children, string parentSlug,
		Dictionary<string, string> slugMap, Dictionary<string, string> titleMap)
	{
		foreach (var child in children)
		{
			switch (child)
			{
				case OpenBlockNode open:
					CollectChildAnchors(open.Children, parentSlug, slugMap, titleMap);
					break;
				case AnchoredBlock anchored:
					slugMap[anchored.Id] = parentSlug;
					break;
				case SectionNode sub:
					if (sub.Id is not null)
					{
						slugMap[sub.Id] = parentSlug;
						titleMap[sub.Id] = ExtractDisplayTitle(sub);
					}
					CollectChildAnchors(sub.Children, parentSlug, slugMap, titleMap);
					break;
			}
		}
	}

	// ── Emission ──────────────────────────────────────────────────────────────

	private static IReadOnlyList<PageOutput> EmitPageNodes(
		IReadOnlyList<PageNode> nodes, MarkdownEmitter emitter)
	{
		var result = new List<PageOutput>(nodes.Count);
		foreach (var node in nodes)
		{
			// Render only the inline content (sub-pages are emitted separately).
			var section = node.Section with { Children = node.InlineContent };
			emitter.UpdatePageSlug(node.Slug);
			emitter.UpdateHeadingBase(node.Section.Level);

			var sb = new StringBuilder();
			if (node.NavigationTitle is not null)
			{
				_ = sb.Append("---\n");
				_ = sb.Append("navigation_title: \"").Append(node.NavigationTitle.Replace("\"", "\\\"")).Append("\"\n");
				_ = sb.Append("---\n");
				_ = sb.Append('\n');
			}
			_ = sb.Append(emitter.Emit(section));

			var children = EmitPageNodes(node.ChildPages, emitter);
			result.Add(new PageOutput(node.Slug, node.Section.Title, node.NavigationTitle, sb.ToString(), children));
		}
		return result;
	}

	// ── Helpers ───────────────────────────────────────────────────────────────

	/// <summary>
	/// Determines whether a section becomes its own page, replicating the legacy asciidoctor chunker rule:
	/// - Never page if [discrete]/[float].
	/// - Level 0 sections: page only when they come from an include (IsIncludeRoot), i.e. they are a
	///   standalone part included into the book root. The non-include-root Level-0 section is the
	///   transparent book-root wrapper that becomes the index page.
	/// - Level > 0: page when level &lt;= effectiveChunkLevel (== conf.yaml chunk + 1).
	/// </summary>
	private static bool IsPage(SectionNode section, int effectiveChunkLevel) =>
		!section.IsDiscrete &&
		(section.Level == 0 ? section.IsIncludeRoot : section.Level <= effectiveChunkLevel);

	private static string AllocateSlug(
		SectionNode section, HashSet<string> allocatedSlugs, Action<string>? onDiagnostic)
	{
		var baseSlug = section.Id ?? AutoId(section.Title);
		if (allocatedSlugs.Add(baseSlug))
			return baseSlug;

		for (var i = 2; i < 10_000; i++)
		{
			var candidate = $"{baseSlug}_{i}";
			if (allocatedSlugs.Add(candidate))
			{
				onDiagnostic?.Invoke(
					$"Slug collision for '{baseSlug}' (section '{section.Title}'); using '{candidate}'");
				return candidate;
			}
		}
		throw new InvalidOperationException($"Cannot allocate a unique slug for '{baseSlug}'");
	}

	/// <summary>Reproduces asciidoctor's default section id: lowercase, non-alphanumeric runs → '_'.</summary>
	private static string AutoId(string title)
	{
		var lower = title.ToLowerInvariant();
		var slug = SlugNonAlphanumericRegex().Replace(lower, "_").Trim('_');
		return string.IsNullOrEmpty(slug) ? "section" : slug;
	}

	private static string ExtractDisplayTitle(SectionNode section)
	{
		foreach (var child in section.Children)
		{
			// <titleabbrev> can be a PassthroughNode (passthrough block) or a ParagraphNode
			// (plain text line) depending on how the source was written.
			var raw = child switch
			{
				PassthroughNode p => p.Content.Trim(),
				ParagraphNode { Inlines: [TextInline { Text: var t }] } => t.Trim(),
				_ => null
			};
			if (raw is null)
				continue;
			var m = TitleAbbrevRegex().Match(raw);
			if (m.Success)
				return m.Groups[1].Value;
		}
		return section.Title;
	}
}
