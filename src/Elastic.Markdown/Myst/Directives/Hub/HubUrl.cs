// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.Hub;

/// <summary>
/// URL helpers for hub directives ({hero}, {link-card}, {whats-new}) whose YAML body
/// values are emitted verbatim into href attributes and don't go through Markdig's
/// link resolver. Root-relative URLs (e.g. "/deploy-manage/...") need the site's
/// URL path prefix (e.g. "/docs") prepended so they resolve on the assembled site.
/// They also need to opt out of body-level <c>hx-boost</c> because the layout uses
/// <c>hx-swap="none"</c> by default — without an explicit <c>hx-select-oob</c>,
/// boosted clicks fetch the URL but swap nothing.
/// </summary>
internal static class HubUrl
{

	/// <summary>
	/// Prefix a root-relative URL with the site's path prefix. Absolute URLs
	/// (http/https/mailto), anchors, and URLs already under the prefix are
	/// returned unchanged.
	/// </summary>
	public static string? Prefix(string? url, string? sitePathPrefix)
	{
		if (string.IsNullOrEmpty(url))
			return url;
		if (string.IsNullOrEmpty(sitePathPrefix))
			return url;
		if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
			|| url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
			|| url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
			|| url.StartsWith('#'))
			return url;

		var prefix = "/" + sitePathPrefix.Trim('/');
		if (url == prefix || url.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase))
			return url;
		if (!url.StartsWith('/'))
			return url;
		return prefix + url;
	}
}
