// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>The newest major, as the notice links to it.</summary>
/// <param name="Label">The short label of the newest major, such as <c>v9</c>.</param>
/// <param name="Url">The same page in the newest major, or its landing page when the page does not exist there.</param>
public sealed record LatestVersionLink(string Label, string Url);

/// <summary>The notice beside an API page's title that names a version other than the newest major.</summary>
/// <param name="Latest">Where the newest major is, or null when that major did not render, so the notice has nothing to link to.</param>
public closed record ApiVersionNotice(LatestVersionLink? Latest);

/// <param name="Version">The major the page belongs to, such as <c>8</c>.</param>
public sealed record OlderMajorNotice(string Version, LatestVersionLink? Latest) : ApiVersionNotice(Latest);

public sealed record UnreleasedNotice(LatestVersionLink? Latest) : ApiVersionNotice(Latest);

public static class ApiVersionNotices
{
	/// <summary>
	/// The notice of a page, or null when the page is the newest major, its numbered copy, or has no version.
	/// <paramref name="latestMoniker"/> is the newest major the API declares. A tree that failed to load has no switcher item,
	/// so the notice keeps the page's own version and drops the link, and never names an older major as the latest.
	/// <paramref name="pageItems"/> are the switcher items already pointed at the current page by <see cref="ApiVersionSwitcher.ForPage"/>.
	/// </summary>
	public static ApiVersionNotice? For(
		ApiPageType type,
		string? moniker,
		string? latestMoniker,
		IReadOnlyList<ApiVersionSwitcherItem> pageItems
	)
	{
		if (type is ApiPageType.Versionless or ApiPageType.NewestMajorCopy || moniker is null)
			return null;

		var item = pageItems.FirstOrDefault(i => i.Moniker == latestMoniker);
		var latest = item is null ? null : new LatestVersionLink(item.ShortLabel ?? item.Label, item.Url);
		return type switch
		{
			ApiPageType.OlderMajor => new OlderMajorNotice(moniker, latest),
			ApiPageType.Unreleased => new UnreleasedNotice(latest),
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
		};
	}
}
