// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Legacy;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;
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

	public required INavigationItem CurrentNavigationItem { get; init; }
	public required INavigationItem? PreviousDocument { get; init; }
	public required INavigationItem? NextDocument { get; init; }
	public required INavigationItem[] Parents { get; init; }

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
