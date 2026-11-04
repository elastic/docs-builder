// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Landing;
using Elastic.Documentation.Site;

namespace Elastic.ApiExplorer.Infrastructure;

public static class ApiHubSwitcher
{
	/// <summary>The catalog's groups, so the menu and the landing page sort the APIs into the same bands. Each row opens the API's latest numbered version when it has one.</summary>
	public static IReadOnlyList<ApiIdentityProductGroup> Build(IReadOnlyList<ApiCatalogEntry> entries, string? currentApiKey)
	{
		if (entries.Count == 0 || currentApiKey is null)
			return [];

		return [
			.. ApiCatalogViewModel.Group(entries).Select(
				g => new ApiIdentityProductGroup(
					g.Title,
					[
						.. g
							.Entries
							.OrderBy(e => ApiCatalogOrder.Rank(e.Key))
							.ThenBy(e => e.Title, StringComparer.OrdinalIgnoreCase)
							.ThenBy(e => e.Key, StringComparer.Ordinal)
							.Select(e => new ApiIdentityProduct(e.Title, e.OpenUrl, e.Key == currentApiKey, e.IconSvg))
					]
				)
			)
		];
	}
}
