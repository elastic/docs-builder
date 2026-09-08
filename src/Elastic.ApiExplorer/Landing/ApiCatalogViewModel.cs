// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Infrastructure;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Site.Icons;

namespace Elastic.ApiExplorer.Landing;

public sealed record ApiCatalogCategoryChip(string Key, string Label);

public sealed record ApiCatalogTile(
	string Key,
	string Title,
	string Url,
	string? IconSvg,
	string? Description,
	IReadOnlyList<ApiCatalogCategoryChip> Categories,
	string JsonUrl,
	string YamlUrl
);

public class ApiCatalogViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	public required IReadOnlyList<ApiCatalogTile> Tiles { get; init; }

	public required IReadOnlyList<ApiCatalogCategoryChip> FilterChips { get; init; }

	public static ApiCatalogViewModel FromEntries(ApiRenderContext context, IReadOnlyList<ApiCatalogEntry> entries)
	{
		var tiles = entries.OrderBy(e => e.Key, StringComparer.Ordinal).Select(ToTile).ToArray();
		return new(context) { Tiles = tiles, FilterChips = ToChips(tiles.SelectMany(t => t.Categories.Select(c => c.Key))) };
	}

	private static ApiCatalogTile ToTile(ApiCatalogEntry entry) =>
		new(
			entry.Key,
			entry.Title,
			entry.Url,
			ProductIcons.Get(entry.ProductId ?? entry.Key),
			entry.Description,
			ToChips(entry.CatalogCategories),
			ApiOutputPaths.JsonUrl(entry.Url),
			ApiOutputPaths.YamlUrl(entry.Url)
		);

	private static ApiCatalogCategoryChip[] ToChips(IEnumerable<string> keys)
	{
		var used = keys as HashSet<string> ?? keys.ToHashSet(StringComparer.Ordinal);
		return [
			.. ApiCatalogCategory
				.DisplayOrder
				.Where(used.Contains)
				.Select(key => new ApiCatalogCategoryChip(key, ApiCatalogCategory.DisplayName(key)))
		];
	}
}
