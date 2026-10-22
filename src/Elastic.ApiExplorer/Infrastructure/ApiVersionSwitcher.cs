// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;

namespace Elastic.ApiExplorer.Infrastructure;

public static class ApiVersionSwitcher
{
	public static IReadOnlyList<ApiVersionSwitcherItem> Build(
		string? urlPathPrefix,
		string apiKey,
		IReadOnlyList<string> monikers,
		string currentMoniker,
		IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>>? pageNames = null
	)
	{
		if (monikers.Count <= 1)
			return [];

		return monikers
			.OrderByDescending(m => m == "main" ? int.MaxValue : ParseMajor(m))
			.Select(
				m => new ApiVersionSwitcherItem(
					Label: m == "main" ? m : $"v{m}",
					Url: ApiUrlBuilder.ProductRoot(urlPathPrefix, ApiUrlBuilder.ProductSuffix(apiKey, m)),
					Selected: m == currentMoniker
				)
				{
					PageNames = pageNames is not null && pageNames.TryGetValue(m, out var names)
						? names
						: FrozenDictionary<string, IReadOnlyList<string>>.Empty
				}
			)
			.ToArray();
	}

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

	private static int ParseMajor(string moniker) => int.TryParse(moniker, out var major) ? major : 0;
}
