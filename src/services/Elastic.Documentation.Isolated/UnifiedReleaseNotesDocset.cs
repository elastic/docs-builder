// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Products;

namespace Elastic.Documentation.Isolated;

/// <summary>
/// Synthesizes the markdown and navigation files of the unified release notes docset in memory.
/// Every product with <c>release-notes: dra</c> gets the same four pages, so onboarding a product
/// is a one-line change in <c>products.yml</c>.
/// </summary>
public static class UnifiedReleaseNotesDocset
{
	public const string BundlesFolder = "changelog/bundles";

	private static readonly (string FileName, string NavigationTitle, string Heading, string Options)[] Pages =
	[
		("index.md", "Release notes", "release notes", ":highlights:"),
		("known-issues.md", "Known issues", "known issues", ":type: known-issue"),
		("breaking-changes.md", "Breaking changes", "breaking changes", ":type: breaking-change\n:dropdowns:"),
		("deprecations.md", "Deprecations", "deprecations", ":type: deprecation")
	];

	/// <summary>The products that appear in the docset, in navigation order.</summary>
	public static IReadOnlyList<Product> SelectProducts(IEnumerable<Product> products) =>
		[
			.. products.Where(p => p.Features.ReleaseNotes == ReleaseNotesPath.DailyReleasableArtifacts).OrderBy(
				p => p.Id,
				StringComparer.Ordinal
			)
		];

	/// <summary>Where the bundle files of a product live, relative to the docset root.</summary>
	public static string BundlesPath(string productId) => $"{BundlesFolder}/{productId}";

	/// <summary>Generates every file of the docset, keyed by its path relative to the docset root.</summary>
	public static IReadOnlyDictionary<string, string> Generate(IReadOnlyList<Product> products)
	{
		var files = new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["docset.yml"] = "project: unified-release-notes\n" + GenerateToc(products),
			["release-notes/index.md"] = GenerateRootIndex(products)
		};

		foreach (var product in products)
		{
			var folder = $"release-notes/{product.Id}";
			files[$"{folder}/toc.yml"] = GenerateProductToc();
			foreach (var page in Pages)
				files[$"{folder}/{page.FileName}"] = GeneratePage(product, page);
		}

		return files;
	}

	private static string GenerateToc(IReadOnlyList<Product> products)
	{
		var lines = new List<string> { "toc:", "  - folder: release-notes", "    children:", "      - file: index.md" };
		lines.AddRange(products.Select(p => $"      - toc: {p.Id}"));
		return string.Join('\n', lines) + "\n";
	}

	private static string GenerateProductToc() => string.Join('\n', Pages.Select(p => $"  - file: {p.FileName}").Prepend("toc:")) + "\n";

	private static string GenerateRootIndex(IReadOnlyList<Product> products)
	{
		var lines = new List<string>
		{
			"---",
			"navigation_title: Release notes",
			"---",
			"",
			"# Unified release notes preview",
			"",
			"This site previews release notes for Elastic products that publish through the DRA (Daily Releasable Artifacts) pipeline.",
			"",
			"A scheduled workflow rebuilds it whenever a new build candidate is cut or a SNAPSHOT advances.",
			"",
			"## Products",
			""
		};
		lines.AddRange(products.Select(p => $"- [{p.DisplayName}]({p.Id}/index.md)"));
		return string.Join('\n', lines) + "\n";
	}

	private static string GeneratePage(Product product, (string FileName, string NavigationTitle, string Heading, string Options) page) =>
		string.Join(
			'\n',
			"---",
			$"navigation_title: {page.NavigationTitle}",
			"---",
			"",
			$"# {product.DisplayName} {page.Heading}",
			"",
			":::{changelog} /" + BundlesPath(product.Id),
			page.Options,
			":::",
			""
		);
}
