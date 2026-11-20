// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using Elastic.ApiExplorer.Infrastructure;

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
	public const string ApiSitemapFileName = "sitemap-api.xml";

	private const int Ok = 200;

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
		if (!SameRobots(robots, observed.Robots))
			yield return new(expected.Path, type, CrawlField.Robots, robots, observed.Robots ?? "none");
	}

	/// <summary>
	/// Checks that the sitemap index lists <see cref="ApiSitemapFileName"/> and that every URL in it belongs to an API tree.
	/// <paramref name="indexLocations"/> are the <c>loc</c> values of the index and <paramref name="apiPaths"/> the paths of
	/// the API sitemap.
	/// </summary>
	public static IEnumerable<CrawlDifference> CompareSitemap(
		string indexPath,
		IReadOnlyCollection<string> indexLocations,
		IEnumerable<string> apiPaths,
		ApiUrlModel model
	)
	{
		if (!indexLocations.Any(loc => loc.EndsWith("/" + ApiSitemapFileName, StringComparison.Ordinal)))
		{
			var listed = indexLocations.Count == 0 ? "no sitemaps" : string.Join(", ", indexLocations.Select(FileName));
			yield return new(indexPath, null, CrawlField.SitemapIndex, $"a {ApiSitemapFileName} entry", listed);
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
		if (observed.Hops.Count == 0)
		{
			yield return new(path, null, CrawlField.Hops, "1 redirect", Describe(observed));
			yield break;
		}

		if (observed.Hops.Count != 1)
			yield return new(path, null, CrawlField.Hops, "1 redirect", Describe(observed));

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

	// Pages write the value with a space after the comma; a value without it means the same thing.
	private static bool SameRobots(string expected, string? actual) => actual is not null && Normalize(expected) == Normalize(actual);

	private static string Normalize(string robots) =>
		string.Join(',', robots.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

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
