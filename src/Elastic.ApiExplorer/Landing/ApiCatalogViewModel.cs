// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Infrastructure;
using Elastic.Documentation.Site.Icons;

namespace Elastic.ApiExplorer.Landing;

public sealed record ApiCatalogTile(string Key, string Title, string Url, string? IconSvg, string? Description);

public class ApiCatalogViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	public required IReadOnlyList<ApiCatalogTile> Tiles { get; init; }

	public static ApiCatalogViewModel FromEntries(ApiRenderContext context, IReadOnlyList<ApiCatalogEntry> entries) =>
		new(context) { Tiles = entries.OrderBy(e => e.Key, StringComparer.Ordinal).Select(ToTile).ToArray() };

	private static ApiCatalogTile ToTile(ApiCatalogEntry entry)
	{
		var iconKey = entry.ProductId ?? entry.Key;
		return new(entry.Key, entry.Title, entry.Url, ProductIcons.Get(iconKey), entry.Description);
	}
}
