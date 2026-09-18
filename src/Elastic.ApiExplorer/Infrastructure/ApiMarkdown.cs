// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text;
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

	internal const string OperationListLabel = "All methods and paths for this operation:";
	internal const string OperationListMarkdownHeader = "**All methods and paths for this operation:**";

	[GeneratedRegex(@"<span class=""operation-verb (\w+)"">(\w+)</span>\s*<span class=""operation-path"">([^<]+)</span>", RegexOptions.IgnoreCase)]
	private static partial Regex OperationVerbPathRegex();

	/// <summary>
	/// Extracts the "All methods and paths for this operation" HTML block from an OpenAPI
	/// description, returning both the cleaned description and the parsed verb/path pairs.
	/// Returns the original description and an empty list when no such block is found.
	/// </summary>
	internal static (string Description, IReadOnlyList<(string Method, string Route)> Urls) ExtractOperationList(string? description)
	{
		if (string.IsNullOrEmpty(description))
			return (description ?? string.Empty, []);

		if (!description.Contains(OperationListMarkdownHeader, StringComparison.Ordinal))
			return (description, []);

		var matches = OperationVerbPathRegex().Matches(description);
		if (matches.Count == 0)
			return (description, []);

		var htmlStartIndex = description.IndexOf("<div>", StringComparison.Ordinal);
		var lastMatchEnd = matches[^1].Index + matches[^1].Length;
		var htmlEndIndex = description.IndexOf("</div>", lastMatchEnd, StringComparison.Ordinal);
		if (htmlEndIndex == -1 || htmlStartIndex == -1)
			return (description, []);

		// Strip the header line and HTML block; keep any text that follows
		var headerStart = description.LastIndexOf(OperationListMarkdownHeader, htmlStartIndex, StringComparison.Ordinal);
		var beforeHeader = headerStart > 0 ? description[..headerStart].Trim() : string.Empty;
		var afterHtml = description[(htmlEndIndex + 6)..].Trim();

		var cleanDescription = (beforeHeader, afterHtml) switch
		{
			({ Length: 0 }, _) => afterHtml,
			(_, { Length: 0 }) => beforeHeader,
			_ => $"{beforeHeader}\n\n{afterHtml}"
		};

		var urls = matches.Select(static m => (Method: m.Groups[1].Value.ToLowerInvariant(), Route: m.Groups[3].Value.Trim())).ToArray();

		return (cleanDescription.Trim(), urls);
	}

	/// <summary>
	/// Transforms HTML operation lists in descriptions to markdown format.
	/// Used by the search export pipeline where only markdown output is needed.
	/// </summary>
	internal static string TransformOperationListToMarkdown(string? description)
	{
		var (clean, urls) = ExtractOperationList(description);
		if (urls.Count == 0)
			return clean;

		var result = new StringBuilder();
		_ = result.AppendLine(OperationListMarkdownHeader);
		_ = result.AppendLine();
		foreach (var (method, route) in urls)
			_ = result.AppendLine($"- **{method.ToUpperInvariant()}** `{route}`");

		if (!string.IsNullOrWhiteSpace(clean))
		{
			_ = result.AppendLine();
			_ = result.Append(clean);
		}

		return result.ToString().Trim();
	}
}
