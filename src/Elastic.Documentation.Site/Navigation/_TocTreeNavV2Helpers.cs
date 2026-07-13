// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Navigation;

internal static class TocTreeNavV2Helpers
{
	internal static string NavLinkClass(string? activeNavigationUrl, string url) =>
		NavigationActiveUrlResolver.IsActive(activeNavigationUrl, url)
			? "sidebar-link nav-v2-link current flex w-full items-center gap-2 py-[6px]"
			: "sidebar-link nav-v2-link flex w-full items-center gap-2 py-[6px]";

	internal static bool IsApiExplorerNavUrl(string url) =>
		url.StartsWith("/api/", StringComparison.Ordinal)
		|| url.StartsWith("/docs/api/", StringComparison.Ordinal);
}
