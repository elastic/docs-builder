// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Navigation;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>One crumb. <see cref="Url"/> is null for the current page (not a link).</summary>
public sealed record ApiBreadcrumb(string Title, string? Url)
{
	public bool IsCurrent => Url is null;
}

/// <summary>
/// Visible crumbs plus optional overflow (EUI Page breadcrumbs, max 4).
/// When overflowing: first two, ellipsis, last two.
/// </summary>
public sealed record ApiBreadcrumbTrail(
	IReadOnlyList<ApiBreadcrumb> Head,
	IReadOnlyList<ApiBreadcrumb> Overflow,
	IReadOnlyList<ApiBreadcrumb> Tail
)
{
	public static readonly ApiBreadcrumbTrail Empty = new([], [], []);

	public bool HasOverflow => Overflow.Count > 0;

	public bool IsEmpty => Head.Count == 0 && Tail.Count == 0;
}

public sealed record ApiBreadcrumbsView(ApiBreadcrumbTrail Trail, string HxAttributes);

public static class ApiBreadcrumbBuilder
{
	public const int MaxVisible = 4;

	public static ApiBreadcrumbTrail Build(INavigationItem current, string currentTitle, string? rootTitle)
	{
		var items = Collect(current, currentTitle, rootTitle);
		if (items.Count == 0)
			return ApiBreadcrumbTrail.Empty;
		return Split(items);
	}

	internal static IReadOnlyList<ApiBreadcrumb> Collect(INavigationItem current, string currentTitle, string? rootTitle)
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

			var title = parent.Parent is null && !string.IsNullOrWhiteSpace(rootTitle)
				? rootTitle
				: parent.NavigationTitle;
			if (string.IsNullOrWhiteSpace(title))
				continue;
			items.Add(new ApiBreadcrumb(title, parent.Url));
		}

		var currentLabel = string.IsNullOrWhiteSpace(currentTitle) ? current.NavigationTitle : currentTitle;
		items.Add(new ApiBreadcrumb(currentLabel, null));
		return items;
	}

	internal static ApiBreadcrumbTrail Split(IReadOnlyList<ApiBreadcrumb> items)
	{
		if (items.Count <= MaxVisible)
			return new ApiBreadcrumbTrail(items, [], []);

		return new ApiBreadcrumbTrail(
			items.Take(2).ToArray(),
			items.Skip(2).Take(items.Count - 4).ToArray(),
			items.TakeLast(2).ToArray()
		);
	}
}
