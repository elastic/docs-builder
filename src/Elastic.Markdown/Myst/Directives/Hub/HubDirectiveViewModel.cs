// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.Documentation.Site;
using Microsoft.AspNetCore.Html;

namespace Elastic.Markdown.Myst.Directives.Hub;

/// <summary>
/// Shared base for hub directive view models. Hub links come from directive options and YAML
/// bodies, so they are written straight into an href rather than going through Markdig's link
/// renderer. This centralises the attributes an anchor needs, so every hub link behaves the
/// same way as an inline link.
/// </summary>
public abstract class HubDirectiveViewModel : DirectiveViewModel
{
	public required string? SitePathPrefix { get; init; }

	/// <summary>Resolve a URL to a final href, applying the site path prefix.</summary>
	public string? PrefixUrl(string? url) => DirectiveLinkValidator.ToHref(url, SitePathPrefix);

	/// <summary>
	/// Render the full attribute set for a hub link: the resolved href, plus the same treatment
	/// inline links get. An external link opens in a new tab and is not preloaded. An in-page
	/// anchor is not preloaded either. Only a link this site serves is worth preloading.
	/// </summary>
	public HtmlString LinkAttributes(string? url)
	{
		var href = PrefixUrl(url) ?? string.Empty;
		var attributes = new StringBuilder();
		_ = attributes.Append("href=\"").Append(HtmlEncoder.Encode(href)).Append('"');

		// A cross-link resolves to a full URL but still points at documentation this site serves,
		// so it is not external. Inline links make the same distinction.
		if (IsExternal(url) && !DirectiveLinkValidator.IsResolvedCrossLink((DirectiveBlock)DirectiveBlock, url))
			_ = attributes.Append(" target=\"_blank\" rel=\"noopener noreferrer\"");
		else if (!IsAnchor(url))
			_ = attributes.Append(" preload=\"").Append(Htmx.Preload).Append('"');

		return new HtmlString(attributes.ToString());
	}

	private static bool IsExternal(string? url) => url is not null && url.StartsWith("http", StringComparison.OrdinalIgnoreCase);

	private static bool IsAnchor(string? url) => url is ['#', ..];

	private static System.Text.Encodings.Web.HtmlEncoder HtmlEncoder => System.Text.Encodings.Web.HtmlEncoder.Default;
}
