// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Landing;
using Elastic.Documentation.Site;

namespace Elastic.ApiExplorer.Infrastructure;

public static class ApiHubSwitcher
{
	public static IReadOnlyList<ApiContextProduct> Build(IReadOnlyList<ApiCatalogEntry> entries, string? currentApiKey)
	{
		if (entries.Count == 0 || currentApiKey is null)
			return [];

		return [
			.. entries
				.OrderBy(e => ApiCatalogOrder.Rank(e.Key))
				.ThenBy(e => e.Title, StringComparer.OrdinalIgnoreCase)
				.ThenBy(e => e.Key, StringComparer.Ordinal)
				.Select(e => new ApiContextProduct(e.Title, e.Url, e.Key == currentApiKey, e.IconSvg))
		];
	}
}
