// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.ReleaseSchedule;

namespace Elastic.Documentation.Isolated;

/// <summary>A product and the versions it has bundles for, newest first.</summary>
public sealed record ProductVersions(Product Product, IReadOnlyList<string> Versions);

/// <summary>
/// Synthesizes the markdown and navigation files of the unified release notes docset in memory.
/// Every product with <c>release-notes: dra</c> gets the same pages, so onboarding a product is a
/// one-line change in <c>products.yml</c>. A version has a page of its own, and its known issues,
/// breaking changes, and deprecations are pages under that version, so a version can be shared as
/// <c>/kibana/9.6.0</c>.
/// </summary>
public static class UnifiedReleaseNotesDocset
{
	public const string BundlesFolder = "changelog/bundles";

	private const string UnreleasedNotice =
		":::{warning}\nThis is a preview of an unreleased version. The release notes can change until the version ships.\n:::";

	private const string VersionOptions = ":highlights:\n:subsections:";

	private static readonly (string FileName, string NavigationTitle, string Heading, string Options)[] Pages =
	[
		("known-issues.md", "Known issues", "known issues", ":type: known-issue\n:subsections:"),
		("breaking-changes.md", "Breaking changes", "breaking changes", ":type: breaking-change\n:dropdowns:\n:subsections:"),
		("deprecations.md", "Deprecations", "deprecations", ":type: deprecation\n:subsections:")
	];

	/// <summary>The products that appear in the docset, in navigation order.</summary>
	public static IReadOnlyList<Product> SelectProducts(IEnumerable<Product> products) =>
		[.. UnifiedBundlePlanner.SelectDraProducts(products).OrderBy(p => p.Id, StringComparer.Ordinal)];

	/// <summary>Where the bundle files of a product live, relative to the docset root.</summary>
	public static string BundlesPath(string productId) => $"{BundlesFolder}/{productId}";

	/// <summary>Generates every file of the docset, keyed by its path relative to the docset root.</summary>
	public static IReadOnlyDictionary<string, string> Generate(IReadOnlyList<ProductVersions> products)
	{
		var files = new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["docset.yml"] = "project: unified-release-notes\n" + GenerateToc(products),
			["index.md"] = GenerateRootIndex(products)
		};

		foreach (var (product, versions) in products)
		{
			var folder = product.Id;
			files[$"{folder}/toc.yml"] = GenerateProductToc(versions);
			files[$"{folder}/index.md"] = GenerateProductIndex(product, versions);
			foreach (var version in versions)
			{
				files[$"{folder}/{version}.md"] = GenerateVersionPage(product, version);
				foreach (var page in Pages)
					files[$"{folder}/{version}/{page.FileName}"] = GenerateVersionSectionPage(product, version, page);
			}
		}

		return files;
	}

	private static string GenerateToc(IReadOnlyList<ProductVersions> products)
	{
		var lines = new List<string> { "toc:", "  - file: index.md" };
		lines.AddRange(products.Select(p => $"  - toc: {p.Product.Id}"));
		return string.Join('\n', lines) + "\n";
	}

	private static string GenerateProductToc(IReadOnlyList<string> versions)
	{
		var lines = new List<string> { "toc:", "  - file: index.md" };
		foreach (var version in versions)
		{
			lines.Add($"  - file: {version}.md");
			lines.Add("    children:");
			lines.AddRange(Pages.Select(page => $"      - file: {version}/{page.FileName}"));
		}

		return string.Join('\n', lines) + "\n";
	}

	private static string GenerateRootIndex(IReadOnlyList<ProductVersions> products)
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
			UnreleasedNotice,
			"",
			"## Products",
			""
		};
		lines.AddRange(products.Select(p => $"- [{p.Product.DisplayName}]({p.Product.Id}/index.md)"));
		return string.Join('\n', lines) + "\n";
	}

	private static string GenerateProductIndex(Product product, IReadOnlyList<string> versions) =>
		string.Join(
			'\n',
			new[]
			{
				"---",
				$"navigation_title: {product.DisplayName}",
				"---",
				"",
				$"# {product.DisplayName} release notes",
				"",
				UnreleasedNotice,
				"",
				"## Versions",
				""
			}.Concat(versions.Select(v => $"- [{v}]({v}.md)")).Append("")
		);

	private static string GenerateVersionPage(Product product, string version) =>
		string.Join(
			'\n',
			"---",
			$"navigation_title: {version}",
			"---",
			"",
			$"# {product.DisplayName} {version}",
			"",
			UnreleasedNotice,
			"",
			":::{changelog} /" + BundlesPath(product.Id),
			$":version: {version}",
			VersionOptions,
			":::",
			""
		);

	private static string GenerateVersionSectionPage(
		Product product,
		string version,
		(string FileName, string NavigationTitle, string Heading, string Options) page
	) =>
		string.Join(
			'\n',
			"---",
			$"navigation_title: {page.NavigationTitle}",
			"---",
			"",
			$"# {product.DisplayName} {version} {page.Heading}",
			"",
			UnreleasedNotice,
			"",
			":::{changelog} /" + BundlesPath(product.Id),
			$":version: {version}",
			page.Options,
			":::",
			""
		);
}
