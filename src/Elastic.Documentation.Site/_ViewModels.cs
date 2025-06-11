// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Legacy;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;

namespace Elastic.Documentation.Site;

public class GlobalLayoutViewModel
{
	public required string DocSetName { get; init; }
	public string Title { get; set; } = "Elastic Documentation";
	public required string Description { get; init; }
	public required LayoutName? Layout { get; init; }

	public required IReadOnlyCollection<PageTocItem> PageTocItems { get; init; }
	public required INavigationItem? CurrentNavigationItem { get; init; }
	public required INavigationItem? Previous { get; init; }
	public required INavigationItem? Next { get; init; }
	public required string NavigationHtml { get; init; }
	public required LegacyPageMapping? LegacyPage { get; init; }
	public required string? UrlPathPrefix { get; init; }
	public required string? GithubEditUrl { get; init; }
	public required string? ReportIssueUrl { get; init; }
	public required bool AllowIndexing { get; init; }
	public required Uri? CanonicalBaseUrl { get; init; }
	public required GoogleTagManagerConfiguration GoogleTagManager { get; init; }
	public string? CanonicalUrl => CanonicalBaseUrl is not null ?
		new Uri(CanonicalBaseUrl, CurrentNavigationItem?.Url).ToString().TrimEnd('/') : null;
	public required FeatureFlags Features { get; init; }

	public required INavigationItem[] Parents { get; init; }

	public required string? Products { get; init; }

	public string Static(string path)
	{
		var staticPath = $"_static/{path.TrimStart('/')}";
		var contentHash = StaticFileContentHashProvider.GetContentHash(path.TrimStart('/'));
		return string.IsNullOrEmpty(contentHash)
			? $"{UrlPathPrefix}/{staticPath}"
			: $"{UrlPathPrefix}/{staticPath}?v={contentHash}";
	}

	// TODO move to @inject
	public required StaticFileContentHashProvider StaticFileContentHashProvider { get; init; }

	public string Link(string path)
	{
		path = path.AsSpan().TrimStart('/').ToString();
		return $"{UrlPathPrefix}/{path}";
	}
}

public record PageTocItem
{
	public required string Heading { get; init; }
	public required string Slug { get; init; }
	public required int Level { get; init; }
}
