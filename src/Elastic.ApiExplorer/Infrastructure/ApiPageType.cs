// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>What an API page is for search engines. One value per rendered tree decides canonical, robots and sitemap.</summary>
public enum ApiPageType
{
	/// <summary>The tree at <c>/api/{key}</c>: the newest released major, or the only tree of a versionless API.</summary>
	Versionless,

	/// <summary>The numbered copy of the newest major, <c>/api/{key}/v{N}</c>, which repeats the versionless page.</summary>
	NewestMajorCopy,

	/// <summary>A numbered major older than the newest one.</summary>
	OlderMajor,

	/// <summary>The unreleased <c>main</c> tree served at <c>/next</c>.</summary>
	Unreleased
}

/// <param name="CanonicalIsVersionless">The page names the same page under the versionless root as canonical, not itself.</param>
/// <param name="Index">Whether search engines may index the page. A page that does not allow it names itself as canonical.</param>
/// <param name="InSitemap">Whether the page belongs in the sitemap. True only for an indexable page that names itself.</param>
public readonly record struct ApiPageIndexing(bool CanonicalIsVersionless, bool Index, bool InSitemap);

public static class ApiPageTypes
{
	/// <summary>The page type of a rendered tree. <paramref name="isMirror"/> is the numbered copy of the default major.</summary>
	public static ApiPageType Of(ApiVersionRoutes routes, string moniker, bool isMirror) =>
		isMirror
			? ApiPageType.NewestMajorCopy
			: moniker == routes.DefaultMoniker
				? ApiPageType.Versionless
				: routes.IsNext(moniker) ? ApiPageType.Unreleased : ApiPageType.OlderMajor;

	public static ApiPageIndexing Indexing(ApiPageType type) => type switch
	{
		ApiPageType.Versionless => new(CanonicalIsVersionless: false, Index: true, InSitemap: true),
		ApiPageType.NewestMajorCopy => new(CanonicalIsVersionless: true, Index: true, InSitemap: false),
		ApiPageType.OlderMajor => new(CanonicalIsVersionless: false, Index: true, InSitemap: true),
		ApiPageType.Unreleased => new(CanonicalIsVersionless: false, Index: false, InSitemap: false),
		_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
	};

	/// <summary>
	/// The robots value of a page. A build that disallows indexing wins on every page. <c>noindex</c> stands alone:
	/// a long-term <c>noindex,follow</c> is read as <c>noindex,nofollow</c>. <paramref name="excludeFromIndexing"/> is the page's own
	/// <c>ExcludeFromIndexing</c>, such as Servers, which is kept only so old links land.
	/// </summary>
	public static string Robots(ApiPageType type, bool buildAllowsIndexing, bool excludeFromIndexing) =>
		!buildAllowsIndexing ? "noindex, nofollow" : Indexing(type).Index && !excludeFromIndexing ? "index, follow" : "noindex";

	/// <summary>
	/// The path a page names as canonical. <paramref name="treeRootUrl"/> is the root of the page's own tree and
	/// <paramref name="versionlessRootUrl"/> the root of the default tree. A page keeps its path below the tree root.
	/// </summary>
	public static string CanonicalPath(ApiPageType type, string pageUrl, string treeRootUrl, string? versionlessRootUrl)
	{
		if (!Indexing(type).CanonicalIsVersionless || versionlessRootUrl is null)
			return pageUrl;
		var root = treeRootUrl.TrimEnd('/');
		return pageUrl.StartsWith(root, StringComparison.Ordinal) ? versionlessRootUrl.TrimEnd('/') + pageUrl[root.Length..] : pageUrl;
	}
}
