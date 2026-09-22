// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation;
using Elastic.Documentation.Extensions;
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

	/// <summary>
	/// Strips executable HTML from rendered description output. Removes dangerous block elements
	/// (script, style, iframe, …) including their content, event handler attributes (on*),
	/// and javascript:/data:/vbscript: URI schemes from href and src attributes.
	/// Keeps bump.sh formatting tags (span, div, a, br) intact.
	/// </summary>
	internal static string SanitizeHtml(string html)
	{
		if (string.IsNullOrEmpty(html))
			return html;

		var result = DangerousBlockPattern().Replace(html, string.Empty);
		result = EventHandlerAttrPattern().Replace(result, string.Empty);
		result = DangerousUrlSchemePattern().Replace(result, "$1blocked:$3");
		return result;
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

	// Strips block elements and their entire content (script, style, iframe, object, embed, form, base).
	[GeneratedRegex(@"<(script|style|iframe|object|embed|form|base)\b[^>]*>.*?</\1\s*>|<(script|style|iframe|object|embed|form|base)\b[^>]*/?>", RegexOptions.IgnoreCase
		| RegexOptions.Singleline)]
	private static partial Regex DangerousBlockPattern();

	// Strips on* event-handler attributes (e.g. onerror="...", onclick='...', onload=foo).
	[GeneratedRegex(@"\s+on[a-z]\w*\s*=\s*(?:""[^""]*""|'[^']*'|[^\s>""'`=]*)", RegexOptions.IgnoreCase)]
	private static partial Regex EventHandlerAttrPattern();

	// Rewrites javascript:, data:, and vbscript: URI schemes inside href/src attributes to "blocked:".
	[GeneratedRegex(@"((?:href|src|action|formaction)\s*=\s*[""'])(javascript|data|vbscript):", RegexOptions.IgnoreCase)]
	private static partial Regex DangerousUrlSchemePattern();

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
