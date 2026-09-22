// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.RegularExpressions;
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
	public static HtmlString Render(ApiRenderContext context, string? markdown)
	{
		if (string.IsNullOrEmpty(markdown))
			return HtmlString.Empty;

		var rewritten = Prepare(markdown, context.CurrentNavigation.NavigationRoot.Url);
		var source = CreateVirtualSource(context);
		var html = context.MarkdownRenderer.RenderApiDescription(rewritten, source);
		return new HtmlString(SanitizeHtml(html));
	}

	// Allowlist sanitizer: permits only the tags and attributes produced by bump.sh and
	// standard Markdig HTML output. Everything else — script, on*, javascript: hrefs, etc.
	// — is stripped by the library's built-in XSS engine (backed by AngleSharp).
	private static readonly HtmlSanitizer Sanitizer = BuildSanitizer();

	private static HtmlSanitizer BuildSanitizer()
	{
		var s = new HtmlSanitizer();

		// Tags present in bump.sh descriptions and standard Markdig output.
		s.AllowedTags.Clear();
		foreach (var tag in new[]
		{
			"a",
			"abbr",
			"b",
			"blockquote",
			"br",
			"caption",
			"cite",
			"code",
			"col",
			"colgroup",
			"dd",
			"del",
			"details",
			"dfn",
			"div",
			"dl",
			"dt",
			"em",
			"figcaption",
			"figure",
			"h1",
			"h2",
			"h3",
			"h4",
			"h5",
			"h6",
			"hr",
			"i",
			"img",
			"ins",
			"kbd",
			"li",
			"mark",
			"ol",
			"p",
			"pre",
			"q",
			"s",
			"samp",
			"small",
			"span",
			"strong",
			"sub",
			"summary",
			"sup",
			"table",
			"tbody",
			"td",
			"tfoot",
			"th",
			"thead",
			"tr",
			"u",
			"ul",
			"var"
		})
			_ = s.AllowedTags.Add(tag);

		// Attributes safe for the above tags.
		s.AllowedAttributes.Clear();
		foreach (var attr in new[]
		{
			"class",
			"id",
			"href",
			"src",
			"alt",
			"title",
			"width",
			"height",
			"colspan",
			"rowspan",
			"scope",
			"start",
			"type",
			"reversed",
			"aria-label",
			"aria-hidden",
			"role",
			"lang",
			"dir",
			"target",
			"rel"
		})
			_ = s.AllowedAttributes.Add(attr);

		// Only https/http/mailto schemes in href/src; javascript:, data:, vbscript: are rejected.
		s.AllowedSchemes.Clear();
		_ = s.AllowedSchemes.Add("https");
		_ = s.AllowedSchemes.Add("http");
		_ = s.AllowedSchemes.Add("mailto");

		return s;
	}

	/// <summary>
	/// Sanitizes rendered description HTML through an allowlist before it is emitted as
	/// <see cref="HtmlString"/>. Only tags and attributes produced by bump.sh and standard
	/// Markdig output are kept; script, on*, javascript:/data: URIs and similar are removed.
	/// </summary>
	internal static string SanitizeHtml(string html)
	{
		if (string.IsNullOrEmpty(html))
			return html;

		return Sanitizer.Sanitize(html);
	}

	/// <summary>
	/// Keeps CommonMark readable: escape mustache substitutions and rewrite intra-API links.
	/// </summary>
	public static string Prepare(string? markdown, string apiBaseUrl)
	{
		if (string.IsNullOrEmpty(markdown))
			return string.Empty;

		var escaped = MustachePattern().Replace(markdown, match => $"`{match.Value}`");
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

	[GeneratedRegex(@"\]\(\.\./group/([^)#]+)\)")]
	private static partial Regex GroupLinkPattern();

	[GeneratedRegex(@"\]\(\.\./operation/([^)#]+)\)")]
	private static partial Regex OperationLinkPattern();

	[GeneratedRegex(@"(?<prefix>\]\()(?<url>[^)\s]+)")]
	private static partial Regex LinkDestinationPattern();

	// Regex to match mustache-style patterns like {{var}} or {{{var}}} that conflict with docs-builder substitutions
	[GeneratedRegex(@"\{\{\{?[^}]+\}?\}\}")]
	private static partial Regex MustachePattern();

	// Replaces block/break HTML tags with a space so adjacent words stay separated after tag removal.
	[GeneratedRegex(@"</?(?:br|p|div|li|ul|ol|h[1-6]|blockquote|pre|hr|tr|td|th)\b[^>]*>", RegexOptions.IgnoreCase)]
	private static partial Regex BlockTagPattern();

	// Strips remaining HTML tags after the block-tag whitespace pass.
	[GeneratedRegex(@"<[^>]+>")]
	private static partial Regex HtmlTagPattern();

	/// <summary>
	/// Strips HTML from a description for plain-text contexts such as search indexing.
	/// Block and break tags are replaced with a space so word boundaries are preserved;
	/// the remaining tags are then removed.
	/// </summary>
	internal static string StripHtml(string? description)
	{
		if (string.IsNullOrEmpty(description))
			return string.Empty;

		var spaced = BlockTagPattern().Replace(description, " ");
		var stripped = HtmlTagPattern().Replace(spaced, string.Empty);
		return WhitespaceCollapsePattern().Replace(stripped, " ").Trim();
	}

	[GeneratedRegex(@"[ \t]{2,}")]
	private static partial Regex WhitespaceCollapsePattern();
}
