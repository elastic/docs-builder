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
			"isolated-directives",
			"Every directive and role",
			"Isolated",
			"One page that uses every directive and role, rendered by the components that write them."
		),
		new(
			"isolated-branded",
			"Branded documentation page",
			"Isolated",
			"The same page white-labelled, with a copyright line in the footer."
		),
		new(
			"assembler-article",
			"Documentation page",
			"Assembler",
			"A page at the top level of the combined site: the elastic.co header, the docs bar, the tree, the article and the rail. The top level is never an island."
		),
		new(
			"assembler-article-island",
			"Documentation page in an island",
			"Assembler",
			"The same page inside an island: the tree shows only that section, with back links and its title."
		),
		new(
			"assembler-product",
			"Product page",
			"Assembler",
			"A product hub: hero, get started, what's new and the explore sections, with no sidebar."
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
		new("assembler-not-found", "Page not found", "Assembler", "The 404 page."),
		new("api-catalog", "API catalog", "API", "The page that lists every API, one card each."),
		new("api-product", "API landing page", "API", "One API's landing page: its overview and the groups of operations."),
		new(
			"api-operation",
			"Operation with examples",
			"API",
			"An operation page with request and response examples next to the description."
		),
		new("api-operation-plain", "Operation without examples", "API", "An operation page with no request body and no examples."),
		new("codex-landing", "Landing page", "Codex", "The Codex home: hero with the MCP card, then every group of documentation sets."),
		new("codex-group", "Group page", "Codex", "A group of documentation sets, under a hero of its own."),
		new(
			"codex-docs-group",
			"Documentation page in a group",
			"Codex",
			"A page of a documentation set that belongs to a group, so the group is part of its trail."
		),
		new(
			"codex-docs-ungrouped",
			"Documentation page outside a group",
			"Codex",
			"A page of a documentation set that belongs to no group: its trail goes straight from the home page."
		)
	];

	public static PreviewEntry? Find(string id) => Entries.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
}
