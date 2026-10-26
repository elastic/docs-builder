// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Buffers;
using System.IO.Abstractions;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation;
using Elastic.Documentation.Extensions;
using Ganss.Xss;
using Microsoft.AspNetCore.Html;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>
/// Renders OpenAPI description markdown to HTML, escaping mustache-style patterns that would
/// otherwise be interpreted as docs-builder substitutions.
/// </summary>
public static partial class ApiMarkdown
{
	/// <summary>An HTML description cut before its first heading. <see cref="Remainder"/> is null when it has none.</summary>
	public sealed record HtmlSplit(string Lead, string? Remainder);

	public static HtmlString Render(ApiRenderContext context, string? markdown) => Render(context, markdown, headingAnchors: null);

	/// <summary>
	/// Renders a group intro and keeps the heading anchor ids in <paramref name="headingAnchors"/>, the slugs its sidebar
	/// links target. Other pages strip them, because an operation or schema page owns ids such as <c>responses</c> that a
	/// description heading could duplicate.
	/// </summary>
	public static HtmlString RenderWithHeadingAnchors(ApiRenderContext context, string? markdown, IReadOnlySet<string> headingAnchors) =>
		Render(context, markdown, headingAnchors);

	private static HtmlString Render(ApiRenderContext context, string? markdown, IReadOnlySet<string>? headingAnchors)
	{
		if (string.IsNullOrEmpty(markdown))
			return HtmlString.Empty;

		var rewritten = ApiAdmonitions.Convert(Prepare(markdown, context.CurrentNavigation.NavigationRoot.Url));
		var source = CreateVirtualSource(context);
		var html = context.MarkdownRenderer.RenderApiDescription(rewritten, source);
		if (string.IsNullOrEmpty(html))
			return new HtmlString(html);

		// Each group intro renders once, so only the shared descriptions go through the cache.
		return new HtmlString(
			headingAnchors is { Count: > 0 }
				? SanitizeHtml(html, headingAnchors)
				: context.SanitizedHtmlCache.GetOrAdd(html, static h => SanitizeHtml(h))
		);
	}

	/// <summary>
	/// Splits rendered HTML before its first top-level heading. The description is rendered as one document first, so
	/// reference-style links and the dropped page-title h1 behave exactly as without a split. A heading inside a
	/// blockquote, list, or HTML wrapper is not a split point, so both fragments stay balanced; HTML that never
	/// balances stays whole in the lead.
	/// </summary>
	public static HtmlSplit SplitAtFirstHeading(string html)
	{
		var depth = 0;
		foreach (var tag in HtmlTag().EnumerateMatches(html))
		{
			var text = html.AsSpan(tag.Index, tag.Length);
			if (text[1] == '/')
				depth = Math.Max(0, depth - 1);
			else if (depth == 0 && StartsSection(text))
				return new HtmlSplit(html[..tag.Index], html[tag.Index..]);
			else if (!IsVoidOrSelfClosing(text))
				depth++;
		}

		return new HtmlSplit(html, null);
	}

	// The Myst renderer wraps each heading in <div class="heading-wrapper">, so that wrapper is where a section starts.
	private static bool StartsSection(ReadOnlySpan<char> tag) =>
		IsHeading(tag) || (tag.StartsWith("<div", StringComparison.Ordinal) && tag.Contains("heading-wrapper", StringComparison.Ordinal));

	private static bool IsHeading(ReadOnlySpan<char> tag) =>
		tag.Length > 3 && tag[1] == 'h' && tag[2] is >= '1' and <= '6' && !char.IsLetterOrDigit(tag[3]);

	private static bool IsVoidOrSelfClosing(ReadOnlySpan<char> tag)
	{
		if (tag[^2] == '/')
			return true;

		var name = tag[1..];
		var end = name.IndexOfAny(TagNameEnd);
		name = end < 0 ? name : name[..end];
		return VoidElements.Contains(name.ToString(), StringComparer.OrdinalIgnoreCase);
	}

	private static readonly SearchValues<char> TagNameEnd = SearchValues.Create(" \t\r\n>/");

	private static readonly string[] VoidElements =
	[
		"area",
		"base",
		"br",
		"col",
		"embed",
		"hr",
		"img",
		"input",
		"link",
		"meta",
		"source",
		"track",
		"wbr"
	];

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

	/// <summary>
	/// Sanitizes rendered description HTML through an allowlist before it is emitted as
	/// <see cref="HtmlString"/>. Relies on <see cref="HtmlSanitizer"/> defaults (99 allowed
	/// tags, safe attributes, http/https schemes only) with <c>class</c> added for bump.sh badges.
	/// A heading wrapper keeps its <c>id</c> only when <paramref name="headingAnchors"/> lists it.
	/// </summary>
	internal static string SanitizeHtml(string html, IReadOnlySet<string>? headingAnchors = null)
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

	/// <summary>
	/// Keeps CommonMark readable: escape mustache substitutions and rewrite intra-API links.
	/// </summary>
	public static string Prepare(string? markdown, string apiBaseUrl)
	{
		if (string.IsNullOrEmpty(markdown))
			return string.Empty;

		var escaped = MustachePattern().Replace(markdown, match => ApiCommonMark.CodeSpan(match.Value));
		return RewriteIntraApiLinks(escaped, apiBaseUrl);
	}

	internal static string RewriteIntraApiLinks(string markdown, string apiBaseUrl)
	{
		var baseUrl = apiBaseUrl.TrimEnd('/') + "/";
		var rewritten = GroupLinkPattern().Replace(markdown, match => $"]({baseUrl}group/{match.Groups[1].Value})");
		return OperationLinkPattern().Replace(rewritten, match => $"]({baseUrl}operation/{match.Groups[1].Value})");
	}

	internal static string CanonicalizeLinks(string markdown, Uri? canonicalBaseUrl) =>
		LinkDestinationPattern().Replace(markdown, match =>
		{
			var url = match.Groups["url"].Value;
			var absolute = UrlPath.MakeAbsolute(canonicalBaseUrl, url);
			return match.Groups["prefix"].Value + absolute;
		});

	private static IFileInfo CreateVirtualSource(ApiRenderContext context)
	{
		var relativePath = context.CurrentNavigation.Url.TrimStart('/').TrimEnd('/');
		if (string.IsNullOrEmpty(relativePath))
			relativePath = "api";

		var fullPath = Path.Join(context.BuildContext.OutputDirectory.FullName, relativePath, "description.md");
		return context.BuildContext.WriteFileSystem.FileInfo.New(fullPath);
	}

	// Tag tokens, with quoted attribute values skipped so a ">" inside one does not end the tag.
	[GeneratedRegex(@"</?[a-zA-Z][a-zA-Z0-9]*(?:[^>""']|""[^""]*""|'[^']*')*>")]
	private static partial Regex HtmlTag();

	[GeneratedRegex(@"\]\(\.\./group/([^)#]+)\)")]
	private static partial Regex GroupLinkPattern();

	[GeneratedRegex(@"\]\(\.\./operation/([^)#]+)\)")]
	private static partial Regex OperationLinkPattern();

	[GeneratedRegex(@"(?<prefix>\]\()(?<url>[^)\s]+)")]
	private static partial Regex LinkDestinationPattern();

	// Regex to match mustache-style patterns like {{var}} or {{{var}}} that conflict with docs-builder substitutions
	[GeneratedRegex(@"\{\{\{?[^}]+\}?\}\}")]
	private static partial Regex MustachePattern();

	private static readonly HtmlParser DescriptionParser = new();

	// Tags that represent block or break boundaries: a space is injected before descending.
	private static readonly HashSet<string> BlockElements =
	[
		with(StringComparer.OrdinalIgnoreCase),
		"br",
		"p",
		"div",
		"li",
		"ul",
		"ol",
		"h1",
		"h2",
		"h3",
		"h4",
		"h5",
		"h6",
		"blockquote",
		"pre",
		"hr",
		"tr",
		"td",
		"th"
	];

	/// <summary>
	/// Extracts plain text from an HTML description for search indexing.
	/// Uses AngleSharp's DOM so only real HTML nodes are removed; non-HTML
	/// angle-bracket sequences like <c>&lt;index&gt;</c> are preserved as text.
	/// Block and break elements inject a space so word boundaries are not lost.
	/// </summary>
	internal static string StripHtml(string? description)
	{
		if (string.IsNullOrEmpty(description))
			return string.Empty;

		using var document = DescriptionParser.ParseDocument(description);
		var body = document.Body;
		if (body is null)
			return string.Empty;

		var sb = new StringBuilder();
		AppendText(body, sb);
		return WhitespaceCollapsePattern().Replace(sb.ToString(), " ").Trim();
	}

	private static void AppendText(INode node, StringBuilder sb)
	{
		foreach (var child in node.ChildNodes)
		{
			if (child.NodeType == NodeType.Text)
				_ = sb.Append(child.TextContent);
			else if (child is IElement element)
			{
				if (BlockElements.Contains(element.LocalName))
					_ = sb.Append(' ');
				AppendText(element, sb);
			}
		}
	}

	[GeneratedRegex(@"[ \t]{2,}")]
	private static partial Regex WhitespaceCollapsePattern();
}
