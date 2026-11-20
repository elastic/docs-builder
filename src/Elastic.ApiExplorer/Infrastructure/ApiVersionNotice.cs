// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>The line under an API page's title that names a version other than the newest major and links to it.</summary>
/// <param name="LatestLabel">The short label of the newest major, such as <c>v9</c>.</param>
/// <param name="LatestUrl">The same page in the newest major, or its landing page when the page does not exist there.</param>
public closed record ApiVersionNotice(string LatestLabel, string LatestUrl);

/// <param name="Version">The major the page belongs to, such as <c>8</c>.</param>
public sealed record OlderMajorNotice(string Version, string LatestLabel, string LatestUrl) : ApiVersionNotice(LatestLabel, LatestUrl);

public sealed record UnreleasedNotice(string LatestLabel, string LatestUrl) : ApiVersionNotice(LatestLabel, LatestUrl);

/// <param name="DesignSystem">True when the design system look is on, so the pill takes that look's badge markup.</param>
public sealed record ApiVersionNoticeModel(ApiVersionNotice Notice, bool DesignSystem);

public static class ApiVersionNotices
{
	/// <summary>
	/// The notice of a page, or null when the page is the newest major or its numbered copy.
	/// <paramref name="pageItems"/> are the switcher items already pointed at the current page by <see cref="ApiVersionSwitcher.ForPage"/>.
	/// </summary>
	public static ApiVersionNotice? For(ApiPageType type, IReadOnlyList<ApiVersionSwitcherItem> pageItems)
	{
		var latestMoniker = ApiVersions.LatestMajor(pageItems.Select(static i => i.Moniker));
		var latest = pageItems.FirstOrDefault(i => i.Moniker == latestMoniker);
		var selected = pageItems.FirstOrDefault(static i => i.Selected);
		if (latest is null || selected is null)
			return null;

		var latestLabel = latest.ShortLabel ?? latest.Label;
		return type switch
		{
			ApiPageType.Versionless or ApiPageType.NewestMajorCopy => null,
			ApiPageType.OlderMajor => new OlderMajorNotice(selected.Moniker, latestLabel, latest.Url),
			ApiPageType.Unreleased => new UnreleasedNotice(latestLabel, latest.Url),
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
		};
	}
}
