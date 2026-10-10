// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.StyleGuide;

/// <param name="Id">The slug under <c>/style-guide/preview</c>.</param>
/// <param name="Build">The kind of build the page belongs to: isolated, assembler or codex.</param>
public sealed record PreviewEntry(string Id, string Title, string Build, string Description);

/// <summary>The whole-page previews the style guide links to. Each is a real layout around placeholder content, with no repository behind it.</summary>
public static class StyleGuidePreviewCatalog
{
	public const string Route = "/style-guide/preview";

	public static IReadOnlyList<PreviewEntry> Entries { get; } =
	[
		new(
			"isolated-article",
			"Documentation page",
			"Isolated",
			"A page in a docs-builder site for one repository: header with deployment details, sidebar, article and the right rail."
		),
		new(
			"isolated-branded",
			"Branded documentation page",
			"Isolated",
			"The same page white-labelled with a copyright line in the footer."
		),
		new(
			"assembler-article",
			"Documentation page",
			"Assembler",
			"A page of the combined site: the elastic.co header, the docs bar, sidebar, article and rail."
		),
		new(
			"assembler-landing",
			"Landing page",
			"Assembler",
			"The docs home page: hero, notice, featured slab and the grids of solutions, guides, reference and resources."
		),
		new(
			"assembler-archive",
			"Archive",
			"Assembler",
			"The page that lists the versions of documentation that are no longer maintained."
		),
		new(
			"assembler-search",
			"Search results",
			"Assembler",
			"The full search page: facets, an AI answer and results. It needs the search service to show results."
		),
		new("assembler-not-found", "Page not found", "Assembler", "The 404 page."),
		new("codex-landing", "Landing page", "Codex", "The Codex home: hero with the MCP card, then every group of documentation sets."),
		new("codex-group", "Group page", "Codex", "A group of documentation sets, under a hero of its own.")
	];

	public static PreviewEntry? Find(string id) => Entries.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
}
