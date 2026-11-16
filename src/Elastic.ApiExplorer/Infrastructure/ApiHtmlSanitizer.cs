// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AngleSharp.Dom;
using Ganss.Xss;
using Microsoft.AspNetCore.Html;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>
/// Sanitizes rendered description HTML through an allowlist before it is emitted as <see cref="HtmlString"/>.
/// Relies on <see cref="HtmlSanitizer"/> defaults (99 allowed tags, safe attributes, http/https schemes only),
/// with <c>class</c> added for bump.sh badges.
/// </summary>
internal static class ApiHtmlSanitizer
{
	// HtmlSanitizer defaults already cover all standard HTML tags and exclude script/on*/etc.
	// Add class so bump.sh verb/path badges (class="operation-verb get") are kept.
	private static readonly HtmlSanitizer Sanitizer = CreateSanitizer();

	private static HtmlSanitizer CreateSanitizer()
	{
		var sanitizer = new HtmlSanitizer();
		_ = sanitizer.AllowedAttributes.Add("class");
		sanitizer.PostProcessNode += (_, e) => DropSelfLinkWithoutAnchor(e.Node);
		return sanitizer;
	}

	// A heading that lost its id would otherwise link to nothing, or to a page section that owns the same slug.
	private static void DropSelfLinkWithoutAnchor(INode node)
	{
		if (
			node is not IElement { LocalName: "div" } wrapper
			|| !wrapper.ClassList.Contains("heading-wrapper")
			|| wrapper.HasAttribute("id")
		)
			return;

		foreach (var link in wrapper.QuerySelectorAll("a.headerlink"))
			_ = link.RemoveAttribute("href");
	}

	/// <summary>A heading wrapper keeps its <c>id</c> only when <paramref name="headingAnchors"/> lists it.</summary>
	public static string Sanitize(string html, IReadOnlySet<string>? headingAnchors = null)
	{
		if (string.IsNullOrEmpty(html))
			return html;
		if (headingAnchors is not { Count: > 0 })
			return Sanitizer.Sanitize(html);

		// The kept ids differ per page, so the attribute hook needs its own sanitizer; group intros are few.
		var sanitizer = CreateSanitizer();
		sanitizer.RemovingAttribute += (_, e) => e.Cancel = IsHeadingAnchor(e.Tag, e.Attribute, headingAnchors);
		return sanitizer.Sanitize(html);
	}

	// Only ids the sidebar links to survive, so raw description HTML cannot claim any other id on the page.
	private static bool IsHeadingAnchor(IElement tag, IAttr attribute, IReadOnlySet<string> headingAnchors) =>
		attribute.Name == "id"
			&& tag.LocalName == "div"
			&& tag.ClassList.Contains("heading-wrapper")
			&& headingAnchors.Contains(attribute.Value);
}
