// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;

namespace Elastic.Markdown.Myst.Directives.PageCard;

public partial class PageCardBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "page-card";

	public string Title { get; private set; } = string.Empty;

	/// <summary>
	/// Site-relative URL resolved from the argument link, ready for use in href.
	/// Resolved using the same path logic as DiagnosticLinkInlineParser.
	/// </summary>
	public string ResolvedUrl { get; private set; } = string.Empty;

	public override void FinalizeAndValidate(ParserContext context)
	{
		var raw = Arguments ?? string.Empty;
		var match = LinkArgumentRegex().Match(raw.Trim());
		if (!match.Success)
		{
			this.EmitError("page-card requires a markdown link argument: [Title](url)");
			return;
		}

		Title = match.Groups[1].Value;
		var url = match.Groups[2].Value;

		if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
		{
			this.EmitError($"page-card url must be a local .md path or crosslink, not an absolute URL: {url}");
			return;
		}

		// No file-existence check: page-card links can target generated pages, for example the
		// CLI reference, which have no markdown file on disk.
		var validated = DirectiveLinkValidator.ResolveWithoutFileCheck(url, this, context) ?? url;

		// A cross-link resolves to a full URL and is already final. Anything else is a path that
		// still needs normalizing against the docset root before it can become an href.
		if (Uri.IsWellFormedUriString(validated, UriKind.Absolute))
		{
			ResolvedUrl = validated;
			return;
		}

		var normalized = NormalizeToDocsetRoot(validated, context);

		// Use the navigation lookup when available so assembled builds get the correct URL.
		// In assembled mode a docset may be rehomed at a path_prefix (e.g. reference/elastic-cli/),
		// and the docset-relative path alone does not include that prefix. FileNavigationLeaf.Url
		// already bakes in the full assembled path, so using it here mirrors what
		// DiagnosticLinkInlineParser.UpdateLinkUrl does for ordinary inline links.
		var relPath = normalized.TrimStart('/');
		foreach (var candidate in ProbeRelativePaths(relPath))
		{
			if (context.TryFindDocumentByRelativePath(candidate) is not MarkdownFile found)
				continue;
			if (!context.NavigationTraversable.NavigationDocumentationFileLookup.TryGetValue(found, out var navLeaf))
				continue;
			if (string.IsNullOrEmpty(navLeaf.Url))
				continue;
			var navUrl = navLeaf.Url;
			var sitePrefix = context.Build.UrlPathPrefix ?? string.Empty;
			if (!string.IsNullOrWhiteSpace(sitePrefix) && !navUrl.StartsWith(sitePrefix, StringComparison.OrdinalIgnoreCase))
				navUrl = $"{sitePrefix.TrimEnd('/')}{navUrl}";
			ResolvedUrl = navUrl;
			return;
		}

		ResolvedUrl = DirectiveLinkValidator.ToHref(normalized, context.Build.UrlPathPrefix) ?? validated;
	}

	private static string NormalizeToDocsetRoot(string url, ParserContext context)
	{
		var sourceDirectory = context.Build.DocumentationSourceDirectory.FullName;
		var includeFrom = url.StartsWith('/') ? sourceDirectory : context.MarkdownSourcePath.Directory!.FullName;
		var resolvedDiskPath = Path.GetFullPath(Path.Join(includeFrom, url));
		return "/" + Path.GetRelativePath(sourceDirectory, resolvedDiskPath).Replace('\\', '/');
	}

	private static string[] ProbeRelativePaths(string path)
	{
		if (path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
			return [path];
		return [path, path + ".md", path.TrimEnd('/') + "/index.md"];
	}

	[GeneratedRegex(@"^\[([^\]]+)\]\(([^)]+)\)$")]
	private static partial Regex LinkArgumentRegex();
}
