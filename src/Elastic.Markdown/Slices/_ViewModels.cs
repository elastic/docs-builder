// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Legacy;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.Navigation;
using Elastic.Markdown.Myst.FrontMatter;

namespace Elastic.Markdown.Slices;

public class IndexViewModel
{
	public required string SiteName { get; init; }
	public required string DocSetName { get; init; }
	public required string Title { get; init; }
	public required string Description { get; init; }
	public required string TitleRaw { get; init; }
	public required string MarkdownHtml { get; init; }
	public required DocumentationGroup Tree { get; init; }
	public required IReadOnlyCollection<PageTocItem> PageTocItems { get; init; }
	public required MarkdownFile CurrentDocument { get; init; }
	public required MarkdownFile? PreviousDocument { get; init; }
	public required MarkdownFile? NextDocument { get; init; }
	public required MarkdownFile[] Parents { get; init; }

	public required string NavigationHtml { get; init; }
	public required LegacyPageMapping? LegacyPage { get; init; }
	public required string? UrlPathPrefix { get; init; }
	public required string? GithubEditUrl { get; init; }
	public required string? ReportIssueUrl { get; init; }
	public required ApplicableTo? AppliesTo { get; init; }
	public required bool AllowIndexing { get; init; }
	public required Uri? CanonicalBaseUrl { get; init; }

	public required GoogleTagManagerConfiguration GoogleTagManager { get; init; }

	public required FeatureFlags Features { get; init; }
	public required StaticFileContentHashProvider StaticFileContentHashProvider { get; init; }

	public required HashSet<Product> Products { get; init; }
}

public class LayoutViewModel
{
	public required string DocSetName { get; init; }

	/// Used to identify the navigation for the current compilation
	/// We want to reset users sessionStorage every time this changes to invalidate
	/// the guids that no longer exist
	public static string CurrentNavigationId { get; } = Guid.NewGuid().ToString("N")[..8];
	public string Title { get; set; } = "Elastic Documentation";
	public required string Description { get; init; }
	public required IReadOnlyCollection<PageTocItem> PageTocItems { get; init; }
	public required MarkdownFile CurrentDocument { get; init; }
	public required MarkdownFile? Previous { get; init; }
	public required MarkdownFile? Next { get; init; }
	public required string NavigationHtml { get; init; }
	public required LegacyPageMapping? LegacyPage { get; init; }
	public required string? UrlPathPrefix { get; init; }
	public required string? GithubEditUrl { get; init; }
	public required string? ReportIssueUrl { get; init; }
	public required bool AllowIndexing { get; init; }
	public required Uri? CanonicalBaseUrl { get; init; }
	public required GoogleTagManagerConfiguration GoogleTagManager { get; init; }
	public string? CanonicalUrl => CanonicalBaseUrl is not null ? new Uri(CanonicalBaseUrl, CurrentDocument.Url).ToString().TrimEnd('/') : null;
	public required FeatureFlags Features { get; init; }

	public required MarkdownFile[] Parents { get; init; }

	public required string? Products { get; init; }

	public string Static(string path)
	{
		var staticPath = $"_static/{path.TrimStart('/')}";
		var contentHash = StaticFileContentHashProvider.GetContentHash(path.TrimStart('/'));
		return string.IsNullOrEmpty(contentHash)
			? $"{UrlPathPrefix}/{staticPath}"
			: $"{UrlPathPrefix}/{staticPath}?v={contentHash}";
	}

	public required StaticFileContentHashProvider StaticFileContentHashProvider { get; init; }

	public string Link(string path)
	{
		path = path.AsSpan().TrimStart('/').ToString();
		return $"{UrlPathPrefix}/{path}";
	}

	public bool IsLandingPage => Features.IsLandingPageEnabled && CurrentDocument.Url == Link("/");
}

public record PageTocItem
{
	public required string Heading { get; init; }
	public required string Slug { get; init; }
	public required int Level { get; init; }
}


public class NavigationViewModel
{
	public required string Title { get; init; }
	public required string TitleUrl { get; init; }
	public required INavigationGroup Tree { get; init; }
	//public required MarkdownFile CurrentDocument { get; init; }
	/// controls whether to split tree automatically
	public required bool IsPrimaryNavEnabled { get; init; }
	public required bool IsGlobalAssemblyBuild { get; init; }
	public required IEnumerable<GroupNavigationItem> TopLevelItems { get; init; }
}

public class NavigationTreeItem
{
	public required int Level { get; init; }
	//public required MarkdownFile CurrentDocument { get; init; }
	public required INavigationGroup SubTree { get; init; }
	public required bool IsPrimaryNavEnabled { get; init; }
	public required bool IsGlobalAssemblyBuild { get; init; }
	public required string RootNavigationId { get; set; }
}

public class PrimaryNavViewModel
{
	public required List<PrimaryNavItemViewModel> Items { get; init; } = [];
}

public class PrimaryNavItemViewModel
{
	public required string Title { get; init; }
	public string? Url { get; init; }
	public string? HtmxAttributes { get; init; }
	public List<PrimaryNavDropdownItemViewModel> DropdownItems { get; init; } = [];
}

public class PrimaryNavDropdownItemViewModel
{
	public required string Title { get; init; }
	public required string Description { get; init; }
	public string? IconPath { get; init; }
	public string? IconAlt { get; init; }
	public required string Url { get; init; }
	public required string HtmxAttributes { get; init; }
}
