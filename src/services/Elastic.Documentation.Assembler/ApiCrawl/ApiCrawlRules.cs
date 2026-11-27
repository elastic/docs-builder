// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.Documentation.Assembler.Building;

namespace Elastic.Documentation.Assembler.ApiCrawl;

/// <summary>One redirect response: its status and where it pointed, as a path when it stays on the crawled host.</summary>
public sealed record RedirectHop(int Status, string Location);

/// <summary>
/// What the site answered for one path. <see cref="Status"/> is the status of the last response after <see cref="Hops"/>,
/// or 0 with <see cref="Error"/> when no response came back.
/// </summary>
public sealed record ObservedPage(
	string Path,
	int Status,
	IReadOnlyList<RedirectHop> Hops,
	string? Canonical,
	string? Robots,
	string? Error = null
);

public enum CrawlField
{
	Status,
	Hops,
	Canonical,
	Robots,
	Sitemap,
	UnknownSitemapUrl,
	SitemapIndex,
	RedirectStatus,
	RedirectTarget
}

/// <summary>One way a response differs from the page-type table. <see cref="Type"/> is null for redirect and sitemap checks.</summary>
public sealed record CrawlDifference(string Path, ApiPageType? Type, CrawlField Field, string Expected, string Actual)
{
	public override string ToString() =>
		$"{Path}{(Type is { } type ? $" [{type}]" : string.Empty)} {Field}: expected {Expected}, got {Actual}";
}

/// <summary>The checks of the crawl, as pure functions of what was expected and what the site answered.</summary>
public static class ApiCrawlRules
{
	private const int Ok = 200;
	private const int MaxListedSitemaps = 3;

	/// <summary>Checks one API page: a direct 200, its canonical URL, its robots value and whether the sitemap lists it.</summary>
	public static IEnumerable<CrawlDifference> Compare(
		ExpectedApiPage expected,
		ObservedPage observed,
		bool inSitemap,
		bool buildAllowsIndexing
	)
	{
		var type = expected.Type;
		var listed = ApiPageTypes.Indexing(type).InSitemap;
		if (listed != inSitemap)
			yield return new(expected.Path, type, CrawlField.Sitemap, Listing(listed), Listing(inSitemap));

		if (observed.Status != Ok || observed.Hops.Count > 0)
		{
			yield return new(expected.Path, type, CrawlField.Status, "200 with no redirect", Describe(observed));
			yield break;
		}

		if (observed.Canonical != expected.CanonicalUrl)
			yield return new(expected.Path, type, CrawlField.Canonical, expected.CanonicalUrl, observed.Canonical ?? "none");

		var robots = ApiPageTypes.Robots(type, buildAllowsIndexing);
		if (!string.Equals(robots, observed.Robots, StringComparison.Ordinal))
			yield return new(expected.Path, type, CrawlField.Robots, robots, observed.Robots ?? "none");
	}

	/// <summary>
	/// Checks that the sitemap index at <paramref name="indexUrl"/> lists <see cref="SitemapBuilder.ApiFileName"/> beside itself,
	/// the URL search engines follow, and that every URL in the API sitemap belongs to an API tree.
	/// <paramref name="indexLocations"/> are the <c>loc</c> values of the index and <paramref name="apiPaths"/> the paths of
	/// the API sitemap.
	/// </summary>
	public static IEnumerable<CrawlDifference> CompareSitemap(
		Uri indexUrl,
		IReadOnlyCollection<string> indexLocations,
		IEnumerable<string> apiPaths,
		ApiUrlModel model
	)
	{
		var indexPath = indexUrl.AbsolutePath;
		var expected = new Uri(indexUrl, SitemapBuilder.ApiFileName).AbsoluteUri;
		if (!indexLocations.Contains(expected, StringComparer.Ordinal))
		{
			var listed = indexLocations.Count switch
			{
				0 => "no sitemaps",
				// An entry with the right name on the wrong host or path sends search engines to a broken URL.
				_ when indexLocations.FirstOrDefault(loc => FileName(loc) == SitemapBuilder.ApiFileName) is { } misplaced => misplaced,
				<= MaxListedSitemaps => string.Join(", ", indexLocations.Select(FileName)),
				// A site without an index serves its pages here; naming thousands of them says nothing more.
				_ =>
					$"{indexLocations.Count} entries, starting {string.Join(", ", indexLocations.Take(MaxListedSitemaps).Select(FileName))}"
			};
			yield return new(indexPath, null, CrawlField.SitemapIndex, expected, listed);
		}

		foreach (var path in apiPaths)
		{
			if (!model.TryClassify(path, out _))
				yield return new(path, null, CrawlField.UnknownSitemapUrl, "a URL in an API tree", "no API tree serves it");
		}
	}

	/// <summary>
	/// Checks one old URL: exactly one redirect, with <paramref name="expectedStatus"/>, to <see cref="RedirectCheck.Target"/>,
	/// which answers 200.
	/// </summary>
	public static IEnumerable<CrawlDifference> CompareRedirect(RedirectCheck check, ObservedPage observed, int expectedStatus)
	{
		var path = check.OldPath;
		if (observed.Hops.Count != 1)
			yield return new(path, null, CrawlField.Hops, "1 redirect", Describe(observed));
		if (observed.Hops.Count == 0)
			yield break;

		var first = observed.Hops[0];
		if (first.Status != expectedStatus)
			yield return new(
				path,
				null,
				CrawlField.RedirectStatus,
				expectedStatus.ToString(CultureInfo.InvariantCulture),
				first.Status.ToString(CultureInfo.InvariantCulture)
			);
		if (first.Location != check.Target)
			yield return new(path, null, CrawlField.RedirectTarget, check.Target, first.Location);
		if (observed.Status != Ok)
			yield return new(path, null, CrawlField.Status, "200 after the redirect", Describe(observed));
	}

	private static string Listing(bool listed) => listed ? "listed" : "not listed";

	private static string FileName(string location) => location[(location.LastIndexOf('/') + 1)..];

	private static string Describe(ObservedPage observed)
	{
		var final = observed.Error is { } error ? $"error ({error})" : observed.Status.ToString(CultureInfo.InvariantCulture);
		return observed.Hops.Count == 0
			? final
			: string.Join(" -> ", observed.Hops.Select(h => $"{h.Status} {h.Location}")) + " -> " + final;
	}
}
