// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.ReleaseNotes;
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

	/// <summary>
	/// The products that appear in the docset, in navigation order: the DRA products, and the products named in
	/// <paramref name="additionalProductIds"/>, such as the serverless products whose releases are labels, not versions.
	/// </summary>
	public static IReadOnlyList<Product> SelectProducts(
		IEnumerable<Product> products,
		IReadOnlyCollection<string>? additionalProductIds = null
	)
	{
		var all = products.ToArray();
		var additional = all.Where(p => additionalProductIds?.Contains(p.Id, StringComparer.Ordinal) ?? false);
		return [.. UnifiedBundlePlanner.SelectDraProducts(all).Concat(additional).Distinct().OrderBy(p => p.Id, StringComparer.Ordinal)];
	}

	/// <summary>
	/// The title of a release in navigation and headings. A version, or a date, is its own title. The preview labels of the
	/// serverless products name an environment, so they read as words.
	/// </summary>
	public static string VersionTitle(string version) => version switch
	{
		"staging" => "Staging",
		"edge" => "Edge",
		_ => version
	};

	/// <summary>Where the bundle files of a product live, relative to the docset root.</summary>
	public static string BundlesPath(string productId) => $"{BundlesFolder}/{productId}";

	/// <summary>Generates every file of the docset, keyed by its path relative to the docset root.</summary>
	public static IReadOnlyDictionary<string, string> Generate(
		IReadOnlyList<ProductVersions> products,
		IReadOnlyList<PreviewProblem>? problems = null,
		DateOnly? previewDate = null
	)
	{
		var files = new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["docset.yml"] = "project: unified-release-notes\n" + GenerateToc(products, problems is not null),
			["index.md"] = GenerateRootIndex(products, problems?.Count)
		};

		// A page that checked and found nothing is still a page: it says what it is for, and that all is well.
		if (problems is not null)
			files[UnifiedReleaseNotesDiagnosticsPage.FileName] = UnifiedReleaseNotesDiagnosticsPage.Generate(products, problems);

		foreach (var (product, versions) in products)
		{
			var folder = product.Id;
			files[$"{folder}/toc.yml"] = GenerateProductToc(versions);
			files[$"{folder}/index.md"] = GenerateProductIndex(product, versions, previewDate);
			foreach (var version in versions)
			{
				files[$"{folder}/{version}.md"] = GenerateVersionPage(product, version, previewDate);
				foreach (var page in Pages)
					files[$"{folder}/{version}/{page.FileName}"] = GenerateVersionSectionPage(product, version, page);
			}
		}

		return files;
	}

	private static string GenerateToc(IReadOnlyList<ProductVersions> products, bool hasDiagnostics)
	{
		var lines = new List<string> { "toc:", "  - file: index.md" };
		if (hasDiagnostics)
			lines.Add($"  - file: {UnifiedReleaseNotesDiagnosticsPage.FileName}");
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

	private static string GenerateRootIndex(IReadOnlyList<ProductVersions> products, int? problemCount)
	{
		var lines = new List<string>
		{
			"---",
			"navigation_title: Release notes",
			"---",
			"",
			"# Unified release notes preview",
			"",
			IntroductionOf(products),
			"",
			"A scheduled workflow rebuilds it whenever a new build candidate is cut or a SNAPSHOT advances.",
			"",
			UnreleasedNotice,
			"",
			"## Products",
			""
		};
		lines.AddRange(products.Select(p => $"- [{p.Product.DisplayName}]({p.Product.Id}/index.md)"));
		if (problemCount is { } count)
			lines.AddRange([
				"",
				"## Diagnostics",
				"",
				count == 0
					? $"No pull request lacks a label right now. [See what the diagnostics page checks]({UnifiedReleaseNotesDiagnosticsPage.FileName})."
					: $"Some pull requests lack a label that puts their release note in the right place. [See which and what to add]({UnifiedReleaseNotesDiagnosticsPage.FileName})."
			]);
		return string.Join('\n', lines) + "\n";
	}

	private static string IntroductionOf(IReadOnlyList<ProductVersions> products) =>
		products.Any(p => p.Product.Features.ReleaseNotes != ReleaseNotesPath.DailyReleasableArtifacts)
			? "This site previews release notes for Elastic products that publish through the DRA (Daily Releasable Artifacts) pipeline, "
				+ "and for Elastic Cloud Serverless and Elastic Cloud Hosted. A serverless or hosted release is previewed as "
				+ "**Staging**, the changes running on the staging environment that are not in production yet, and **Edge**, everything "
				+ "merged to the main branch that is not in production yet. Neither has a version number: a release is named by the day it ships."
			: "This site previews release notes for Elastic products that publish through the DRA (Daily Releasable Artifacts) pipeline.";

	private static string GenerateProductIndex(Product product, IReadOnlyList<string> versions, DateOnly? previewDate) =>
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
			}.Concat(versions.Select(v => $"- [{VersionTitle(v)}]({v}.md){DatedIfShippedToday(v, previewDate)}")).Append("")
		);

	private static string GenerateVersionPage(Product product, string version, DateOnly? previewDate) =>
		string.Join(
			'\n',
			new[]
			{
				"---",
				$"navigation_title: {VersionTitle(version)}",
				"---",
				"",
				$"# {product.DisplayName} {VersionTitle(version)}",
				"",
				UnreleasedNotice,
				""
			}.Concat(PreviewDescription(version, previewDate) is { } description ? [description, ""] : []).Concat([
				":::{changelog} /" + BundlesPath(product.Id),
				$":version: {version}",
				VersionOptions,
				":::",
				""
			])
		);

	/// <summary>
	/// What a preview release of a serverless product is, in words a visitor needs, and the version it would get if it shipped today: a
	/// serverless release is named by the day it ships. <c>null</c> for a version, which is its own explanation.
	/// </summary>
	private static string? PreviewDescription(string version, DateOnly? today)
	{
		var what = version switch
		{
			"staging" =>
				"These are the changes running on the staging environment that are not in production yet. They are next to be released.",
			"edge" =>
				"These are all the changes merged to the main branch that are not in production yet. They include the changes on staging.",
			_ => null
		};
		return what is null || today is not { } date ? what : $"{what} If released today, this release would be dated **{Format(date)}**.";
	}

	private static string DatedIfShippedToday(string version, DateOnly? today) =>
		today is { } date && PreviewDescription(version, null) is not null ? $", dated {Format(date)} if released today" : string.Empty;

	private static string Format(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

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
			$"# {product.DisplayName} {VersionTitle(version)} {page.Heading}",
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
