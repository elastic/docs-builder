// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Landing;
using Elastic.Documentation.Configuration.Toc;

namespace Elastic.ApiExplorer.Infrastructure;

public static class ApiHubSwitcher
{
	public static IReadOnlyList<ApiCatalogEntry> CollectDeclaredEntries(
		string? urlPathPrefix,
		IReadOnlyDictionary<string, ResolvedApiConfiguration>? apiConfigurations
	)
	{
		if (apiConfigurations is null || apiConfigurations.Count == 0)
			return [];

		var entries = new List<ApiCatalogEntry>(apiConfigurations.Count);
		foreach (var (key, config) in apiConfigurations)
		{
			entries.Add(new(key, config.Product.DisplayName, $"{ApiUrlBuilder.ProductRoot(urlPathPrefix, key)}/"));
		}

		return entries;
	}

	public static IReadOnlyList<ApiVersionSwitcherItem> Build(IReadOnlyList<ApiCatalogEntry> entries, string? currentApiKey, string hubUrl)
	{
		if (entries.Count == 0 || currentApiKey is null)
			return [];

		var items = new List<ApiVersionSwitcherItem>(entries.Count + 1) { new("Back to hub", hubUrl, Selected: false) };

		foreach (var entry in entries.OrderBy(e => e.Title, StringComparer.OrdinalIgnoreCase))
			items.Add(new(entry.Title, entry.Url, Selected: entry.Key == currentApiKey));

		return items;
	}
}
