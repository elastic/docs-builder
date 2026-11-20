// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Assembler.Building;

/// <summary>
/// The API pages of the sitemap with the content dates of the documents that stand for them. <paramref name="Missing"/> are
/// pages no document stands for: they keep the date the build gave them.
/// </summary>
public record ApiSitemapResolution(IReadOnlyDictionary<string, DateTimeOffset> Entries, IReadOnlyList<string> Missing);

public static class ApiSitemapDates
{
	/// <summary>
	/// Dates each page of <paramref name="buildPages"/>, the API sitemap the build wrote, from <paramref name="documents"/>.
	/// An API document stands for its page in every tree it lists. The hub at <paramref name="hubUrl"/> lists the APIs, so it
	/// takes the newest date of their landing pages.
	/// </summary>
	public static ApiSitemapResolution Resolve(
		IReadOnlyDictionary<string, DateTimeOffset> buildPages,
		IEnumerable<SitemapEntry> documents,
		string hubUrl
	)
	{
		var dates = new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal);
		DateTimeOffset? newestLanding = null;
		foreach (var document in documents)
		{
			foreach (var url in PageUrls(document))
				dates[url] = document.LastUpdated;
			if (document.ApiLanding && (newestLanding is null || document.LastUpdated > newestLanding))
				newestLanding = document.LastUpdated;
		}
		if (newestLanding is { } hubDate)
			_ = dates.TryAdd(hubUrl, hubDate);

		var entries = new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal);
		List<string> missing = [];
		foreach (var (url, buildDate) in buildPages)
		{
			if (dates.TryGetValue(url, out var date))
				entries[url] = date;
			else
			{
				entries[url] = buildDate;
				missing.Add(url);
			}
		}

		missing.Sort(StringComparer.Ordinal);
		return new ApiSitemapResolution(entries, missing);
	}

	/// <summary>
	/// Whether the API sitemap of the build is gone although the environment renders API pages and the index has API documents,
	/// so the sitemap index would silently drop the API pages.
	/// </summary>
	public static bool BuildSitemapMissing(
		IReadOnlyDictionary<string, DateTimeOffset> buildPages,
		IEnumerable<SitemapEntry> documents,
		bool apiExplorerEnabled
	) => buildPages.Count == 0 && apiExplorerEnabled && documents.Any(d => d.ApiTreeRoots.Count > 0);

	/// <summary>The document's URL moved into each tree it lists. Its URL points into the first.</summary>
	private static IEnumerable<string> PageUrls(SitemapEntry document)
	{
		if (document.ApiTreeRoots is not [var lead, ..] || !document.Url.StartsWith(lead, StringComparison.Ordinal))
			return [document.Url];

		var below = document.Url[lead.Length..];
		return document.ApiTreeRoots.Select(root => root + below);
	}
}
