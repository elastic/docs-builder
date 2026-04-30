// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Markdown.Diagnostics;

namespace Elastic.Markdown.Myst.Directives.PageCard;

public partial class PageCardBlock(DirectiveBlockParser parser, ParserContext context)
	: DirectiveBlock(parser, context)
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

		if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
			url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
		{
			this.EmitError($"page-card url must be a local .md path or crosslink, not an absolute URL: {url}");
			return;
		}

		// Resolve relative to the source file's directory (same logic as DiagnosticLinkInlineParser)
		var includeFrom = url.StartsWith('/')
			? context.Build.DocumentationSourceDirectory.FullName
			: context.MarkdownSourcePath.Directory!.FullName;

		var resolvedDiskPath = Path.GetFullPath(Path.Join(includeFrom, url));
		var relativeToSource = Path.GetRelativePath(
			context.Build.DocumentationSourceDirectory.FullName, resolvedDiskPath);

		// Strip .md extension for the final href (same as normal link rendering)
		var withoutExtension = relativeToSource.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
			? relativeToSource[..^3]
			: relativeToSource;

		ResolvedUrl = "/" + withoutExtension.Replace('\\', '/');
	}

	[GeneratedRegex(@"^\[([^\]]+)\]\(([^)]+)\)$")]
	private static partial Regex LinkArgumentRegex();
}
