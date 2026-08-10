// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.ApiExplorer.Infrastructure;

public static class ApiVersionSwitcher
{
	public static IReadOnlyList<ApiVersionSwitcherItem> Build(
		string? urlPathPrefix,
		string apiKey,
		IReadOnlyList<string> monikers,
		string currentMoniker)
	{
		if (monikers.Count <= 1)
			return [];

		return monikers
			.OrderByDescending(m => m == "main" ? int.MaxValue : ParseMajor(m))
			.Select(m => new ApiVersionSwitcherItem(
				Label: m == "main" ? "Latest" : $"{m}.x",
				Url: $"{ApiUrlBuilder.ProductRoot(urlPathPrefix, ApiUrlBuilder.ProductSuffix(apiKey, m))}/",
				Selected: m == currentMoniker))
			.ToArray();
	}

	private static int ParseMajor(string moniker) =>
		int.TryParse(moniker, out var major) ? major : 0;
}
