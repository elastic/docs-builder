// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>Builds left-nav version switcher options from resolved API version monikers.</summary>
public static class ApiVersionSwitcher
{
	public static IReadOnlyList<ApiVersionSwitcherItem> Build(
		string? urlPathPrefix,
		IReadOnlyList<(string Moniker, string ApiUrlSuffix)> versions,
		string currentMoniker)
	{
		if (versions.Count <= 1)
			return [];

		return versions
			.OrderByDescending(v => v.Moniker == "main" ? int.MaxValue : ParseMajor(v.Moniker))
			.Select(v => new ApiVersionSwitcherItem(
				Label: v.Moniker == "main" ? "Latest" : $"{v.Moniker}.x",
				Url: $"{ApiUrlBuilder.ProductRoot(urlPathPrefix, v.ApiUrlSuffix)}/",
				Selected: v.Moniker == currentMoniker))
			.ToArray();
	}

	private static int ParseMajor(string moniker) =>
		int.TryParse(moniker, out var major) ? major : 0;
}
