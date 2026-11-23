// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;

namespace Elastic.ApiExplorer.Infrastructure;

public static class ApiVersionSwitcher
{
	/// <summary>
	/// One entry per version, for the page tree served at <paramref name="currentSuffix"/>. Each entry links to the
	/// version's URL from <paramref name="routes"/>, except the selected one, which links to the current tree's own root,
	/// so the numbered copy of the default major stays on its own URLs. The unreleased tree is listed only on its own
	/// pages: it is a preview opened by URL, so no released page links to it.
	/// </summary>
	public static IReadOnlyList<ApiVersionSwitcherItem> Build(
		string? urlPathPrefix,
		ApiVersionRoutes routes,
		IReadOnlyList<string> monikers,
		string currentSuffix,
		IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>>? pageNames = null
	)
	{
		var listed = monikers.Where(m => !routes.IsNext(m) || routes.Suffixes(m).Contains(currentSuffix)).ToArray();
		if (listed.Length <= 1)
			return [];

		var latest = ApiVersions.LatestMajor(monikers);
		return listed
			.OrderByDescending(ApiVersions.Recency)
			.Select(m => (Moniker: m, Selected: routes.Suffixes(m).Contains(currentSuffix)))
			.Select(
				v => new ApiVersionSwitcherItem(
					Moniker: v.Moniker,
					Label: LabelFor(v.Moniker, latest),
					Url: ApiUrlBuilder.ProductRoot(urlPathPrefix, v.Selected ? currentSuffix : routes.Suffix(v.Moniker)),
					Selected: v.Selected,
					ShortLabel: ShortLabelFor(v.Moniker)
				)
				{
					PageNames = pageNames is not null && pageNames.TryGetValue(v.Moniker, out var names)
						? names
						: FrozenDictionary<string, IReadOnlyList<string>>.Empty
				}
			)
			.ToArray();
	}

	private static string ShortLabelFor(string moniker) => moniker == "main" ? "Next" : $"v{moniker}";

	/// <summary>The highest numeric major reads as the latest. <c>main</c> is the unreleased build served at <c>/next</c>.</summary>
	private static string LabelFor(string moniker, string? latest) =>
		moniker == "main" ? "Next (unreleased)" : moniker == latest ? $"v{moniker} (latest)" : $"v{moniker}";

	/// <summary>
	/// Points each version at the same page as <paramref name="pageUrl"/> when that version renders it,
	/// and at the version's landing page otherwise.
	/// </summary>
	public static IReadOnlyList<ApiVersionSwitcherItem> ForPage(IReadOnlyList<ApiVersionSwitcherItem> items, string pageUrl)
	{
		var selected = items.FirstOrDefault(static i => i.Selected);
		if (selected is null)
			return items;

		var page = pageUrl.TrimEnd('/');
		if (!page.StartsWith(selected.Url, StringComparison.Ordinal))
			return items;

		var path = page[selected.Url.Length..];
		if (path.Length > 0 && path[0] != '/')
			return items;

		if (path.Length == 0 || !selected.PageNames.TryGetValue(path, out var names))
			return items;

		return [.. items.Select(i => LinkToPage(i, names))];
	}

	private static ApiVersionSwitcherItem LinkToPage(ApiVersionSwitcherItem item, IReadOnlyList<string> names)
	{
		foreach (var name in names)
		{
			if (item.PageNames.TryGetValue(name, out var target))
				return item with { Url = item.Url + target[0] };
		}

		return item;
	}
}
