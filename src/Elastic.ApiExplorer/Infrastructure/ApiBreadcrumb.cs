// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>One crumb. <see cref="Url"/> is null for the current page (not a link).</summary>
public sealed record ApiBreadcrumb(string Title, string? Url)
{
	public bool IsCurrent => Url is null;
}

/// <summary>
/// Full crumb list. The first crumb and the current page stay visible; the
/// client collapses middle crumbs into an ellipsis dropdown when the row is tight.
/// </summary>
public sealed record ApiBreadcrumbTrail(IReadOnlyList<ApiBreadcrumb> Items)
{
	public static readonly ApiBreadcrumbTrail Empty = new([]);

	public bool IsEmpty => Items.Count == 0;
}

public sealed record ApiBreadcrumbsView(ApiBreadcrumbTrail Trail, string HxAttributes);

public static class ApiBreadcrumbBuilder
{
	public const string CatalogCrumbTitle = "APIs";

	public static ApiBreadcrumbTrail Build(INavigationItem current, string currentTitle, string? rootTitle, string? catalogUrl = null) =>
		TrailFrom(Collect(current, currentTitle, rootTitle, catalogUrl));

	internal static ApiBreadcrumbTrail TrailFrom(IReadOnlyList<ApiBreadcrumb> items)
	{
		if (items.Count == 0 || items.All(static crumb => crumb.IsCurrent))
			return ApiBreadcrumbTrail.Empty;
		return new ApiBreadcrumbTrail(items);
	}

	internal static IReadOnlyList<ApiBreadcrumb> Collect(
		INavigationItem current,
		string currentTitle,
		string? rootTitle,
		string? catalogUrl = null
	)
	{
		var items = new List<ApiBreadcrumb>();
		foreach (var parent in current.GetParents().Reverse())
		{
			if (parent.Hidden)
				continue;
			if (string.Equals(parent.Url, current.Url, StringComparison.Ordinal))
				continue;
			if (string.Equals(parent.NavigationTitle, currentTitle, StringComparison.OrdinalIgnoreCase))
				continue;

			var title = parent.Parent is null && !string.IsNullOrWhiteSpace(rootTitle) ? rootTitle : parent.NavigationTitle;
			if (string.IsNullOrWhiteSpace(title))
				continue;
			items.Add(new ApiBreadcrumb(title, parent.Url));
		}

		var currentLabel = string.IsNullOrWhiteSpace(currentTitle) ? current.NavigationTitle : currentTitle;
		items.Add(new ApiBreadcrumb(currentLabel, null));

		if (ShouldPrependCatalog(catalogUrl, current.Url, items))
			items.Insert(0, new ApiBreadcrumb(CatalogCrumbTitle, catalogUrl));

		return items;
	}

	internal static string ToJsonLd(IReadOnlyList<ApiBreadcrumb> crumbs, Uri? canonicalBaseUrl)
	{
		var baseUri = canonicalBaseUrl ?? new Uri("http://localhost");
		var position = 1;
		var items = crumbs.Select(
			c => new BreadcrumbListItem
			{
				Position = position++,
				Name = c.Title,
				Item = c.Url is null ? null : new Uri(baseUri, c.Url).ToString()
			}
		).ToList();
		return JsonSerializer.Serialize(new BreadcrumbsList { ItemListElement = items }, BreadcrumbsContext.Default.BreadcrumbsList);
	}

	private static bool ShouldPrependCatalog(string? catalogUrl, string currentUrl, IReadOnlyList<ApiBreadcrumb> items)
	{
		if (string.IsNullOrWhiteSpace(catalogUrl))
			return false;
		if (SameUrl(currentUrl, catalogUrl))
			return false;
		return items.Count == 0 || !SameUrl(items[0].Url, catalogUrl);
	}

	private static bool SameUrl(string? left, string? right)
	{
		if (left is null || right is null)
			return false;
		return string.Equals(left.TrimEnd('/'), right.TrimEnd('/'), StringComparison.Ordinal);
	}
}
