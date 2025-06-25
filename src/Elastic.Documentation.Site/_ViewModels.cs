// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;

namespace Elastic.Documentation.Site;

public static class GlobalSections
{
	public const string Head = "head";
	public const string Footer = "footer";
}

public record GlobalLayoutViewModel
{
	public required string DocSetName { get; init; }
	public string Title { get; set; } = "Elastic Documentation";
	public required string Description { get; init; }

	public required INavigationItem CurrentNavigationItem { get; init; }
	public required INavigationItem? Previous { get; init; }
	public required INavigationItem? Next { get; init; }

	public required string NavigationHtml { get; init; }
	public required string? UrlPathPrefix { get; init; }
	public required Uri? CanonicalBaseUrl { get; init; }
	public string? CanonicalUrl => CanonicalBaseUrl is not null ?
		new Uri(CanonicalBaseUrl, CurrentNavigationItem.Url).ToString().TrimEnd('/') : null;

	public required FeatureFlags Features { get; init; }
	// TODO move to @inject
	public required GoogleTagManagerConfiguration GoogleTagManager { get; init; }
	public required bool AllowIndexing { get; init; }
	public required StaticFileContentHashProvider StaticFileContentHashProvider { get; init; }

	public bool RenderHamburgerIcon { get; init; } = true;

	public string Static(string path)
	{
		var staticPath = $"_static/{path.TrimStart('/')}";
		var contentHash = StaticFileContentHashProvider.GetContentHash(path.TrimStart('/'));
		return string.IsNullOrEmpty(contentHash)
			? $"{UrlPathPrefix}/{staticPath}"
			: $"{UrlPathPrefix}/{staticPath}?v={contentHash}";
	}

	public string Link(string path)
	{
		path = path.AsSpan().TrimStart('/').ToString();
		return $"{UrlPathPrefix}/{path}";
	}
}
