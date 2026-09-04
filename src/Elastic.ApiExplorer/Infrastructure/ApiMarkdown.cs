// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
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
	private const string AllMethodsHeading = "**All methods and paths for this operation:**";

	private static readonly FrozenDictionary<string, string> AdmonitionDirectives = new Dictionary<string, string>(
		StringComparer.OrdinalIgnoreCase
	)
	{
		["NOTE"] = "note",
		["TIP"] = "tip",
		["WARNING"] = "warning",
		["IMPORTANT"] = "important",
		["CAUTION"] = "warning"
	}.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

	public static HtmlString Render(ApiRenderContext context, string? markdown)
	{
		if (string.IsNullOrEmpty(markdown))
			return HtmlString.Empty;

		var rewritten = Prepare(markdown, context.CurrentNavigation.NavigationRoot.Url);
		var source = CreateVirtualSource(context);
		var html = context.MarkdownRenderer.RenderApiDescription(rewritten, source);
		return new HtmlString(html);
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

	/// <summary>
	/// Spec description in. Markdown out. Never null. Idempotent.
	/// </summary>
	internal static string Clean(string? markdown)
	{
		if (string.IsNullOrEmpty(markdown))
			return string.Empty;

		var withoutIsland = StripBumpIsland(markdown);
		var withoutHeading = withoutIsland.Replace(AllMethodsHeading, string.Empty, StringComparison.Ordinal);
		var withoutMarkup = StripLeftoverBumpMarkup(withoutHeading);
		var withAdmonitions = AdmonitionPrefixRegex().Replace(withoutMarkup, ReplaceAdmonition);
		return CollapseBlankLines(withAdmonitions).Trim();
	}

	private static string StripBumpIsland(string markdown)
	{
		var matches = OperationVerbPathRegex().Matches(markdown);
		if (matches.Count == 0)
			return markdown;

		var htmlStartIndex = markdown.IndexOf("<div>", StringComparison.Ordinal);
		var lastMatchEnd = matches[^1].Index + matches[^1].Length;
		var htmlEndIndex = markdown.IndexOf("</div>", lastMatchEnd, StringComparison.Ordinal);
		if (htmlStartIndex == -1 || htmlEndIndex == -1)
			return markdown;

		return markdown[..htmlStartIndex] + markdown[(htmlEndIndex + "</div>".Length)..];
	}

	private static string StripLeftoverBumpMarkup(string markdown)
	{
		var withoutSpans = LeftoverOperationSpanRegex().Replace(markdown, string.Empty);
		return EmptyDivRegex().Replace(withoutSpans, string.Empty);
	}

	private static string ReplaceAdmonition(Match match)
	{
		var marker = match.Groups["marker"].Value;
		if (!AdmonitionDirectives.TryGetValue(marker, out var directive))
			return match.Value;

		return $":::{{{directive}}}\n{match.Groups["body"].Value}\n:::";
	}

	private static string CollapseBlankLines(string markdown) => ExtraBlankLinesRegex().Replace(markdown, "\n\n");

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

	[GeneratedRegex(@"<span class=""operation-verb (\w+)"">(\w+)</span>\s*<span class=""operation-path"">([^<]+)</span>", RegexOptions.IgnoreCase)]
	private static partial Regex OperationVerbPathRegex();

	[GeneratedRegex(@"<span class=""operation-(?:verb[^""]*|path)"">[^<]*</span>", RegexOptions.IgnoreCase)]
	private static partial Regex LeftoverOperationSpanRegex();

	[GeneratedRegex(@"<div>\s*</div>", RegexOptions.IgnoreCase)]
	private static partial Regex EmptyDivRegex();

	[GeneratedRegex(@"^(?<marker>NOTE|TIP|WARNING|IMPORTANT|CAUTION):\s+(?<body>.+)$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
	private static partial Regex AdmonitionPrefixRegex();

	[GeneratedRegex(@"(?:\r?\n){3,}")]
	private static partial Regex ExtraBlankLinesRegex();
}
