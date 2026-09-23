// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
	public static HtmlString Render(ApiRenderContext context, string? markdown)
	{
		if (string.IsNullOrEmpty(markdown))
			return HtmlString.Empty;

		var rewritten = Prepare(markdown, context.CurrentNavigation.NavigationRoot.Url);
		var source = CreateVirtualSource(context);
		var html = context.MarkdownRenderer.RenderApiDescription(rewritten, source);
		return new HtmlString(SanitizeHtml(html));
	}

	// HtmlSanitizer defaults already cover all standard HTML tags and exclude script/on*/etc.
	// Add class so bump.sh verb/path badges (class="operation-verb get") are kept.
	private static readonly HtmlSanitizer Sanitizer = new();

	static ApiMarkdown() => Sanitizer.AllowedAttributes.Add("class");

	/// <summary>
	/// Sanitizes rendered description HTML through an allowlist before it is emitted as
	/// <see cref="HtmlString"/>. Relies on <see cref="HtmlSanitizer"/> defaults (99 allowed
	/// tags, safe attributes, http/https schemes only) with <c>class</c> added for bump.sh badges.
	/// </summary>
	internal static string SanitizeHtml(string html) => string.IsNullOrEmpty(html) ? html : Sanitizer.Sanitize(html);

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
