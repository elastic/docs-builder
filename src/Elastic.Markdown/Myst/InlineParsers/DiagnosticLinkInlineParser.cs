// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst.Comments;
using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Myst.InlineParsers;

public static class DirectiveMarkdownBuilderExtensions
{
	public static MarkdownPipelineBuilder UseDiagnosticLinks(this MarkdownPipelineBuilder pipeline)
	{
		pipeline.Extensions.AddIfNotAlready<DiagnosticLinkInlineExtensions>();
		return pipeline;
	}
}

public class DiagnosticLinkInlineExtensions : IMarkdownExtension
{
	public void Setup(MarkdownPipelineBuilder pipeline) =>
		pipeline.InlineParsers.Replace<LinkInlineParser>(new DiagnosticLinkInlineParser());

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer) { }
}

internal partial class LinkRegexExtensions
{

	[GeneratedRegex(@"\s\=(?<width>\d+%?)(?:x(?<height>\d+%?))?$", RegexOptions.IgnoreCase, "en-US")]
	public static partial Regex MatchTitleStylingInstructions();

}

public class DiagnosticLinkInlineParser : LinkInlineParser
{
	// See https://www.iana.org/assignments/uri-schemes/uri-schemes.xhtml for a list of URI schemes
	private static readonly ImmutableHashSet<string> ExcludedSchemes = ["http", "https", "tel", "jdbc"];

	public override bool Match(InlineProcessor processor, ref StringSlice slice)
	{
		var match = base.Match(processor, ref slice);

		if (!match || processor.Inline is not LinkInline link)
			return match;

		var context = processor.GetContext();
		if (IsInCommentBlock(link) || context.SkipValidation)
			return match;

		ValidateAndProcessLink(processor, link, context);

		ParseStylingInstructions(processor, link, context);

		return match;
	}


	private void ParseStylingInstructions(InlineProcessor processor, LinkInline link, ParserContext context)
	{
		if (string.IsNullOrWhiteSpace(link.Title) || link.Title.IndexOf('=') < 0)
			return;

		var matches = LinkRegexExtensions.MatchTitleStylingInstructions().Match(link.Title);
		if (!matches.Success)
			return;

		var width = matches.Groups["width"].Value;
		if (!width.EndsWith("%"))
			width += "px";
		var height = matches.Groups["height"].Value;
		if (string.IsNullOrEmpty(height))
			height = width;
		else if (!height.EndsWith("%"))
			height += "px";
		var title = link.Title[..matches.Index];

		link.Title = title;
		var attributes = link.GetAttributes();
		attributes.AddProperty("width", width);
		attributes.AddProperty("height", height);
	}

	private static bool IsInCommentBlock(LinkInline link) =>
		link.Parent?.ParentBlock is CommentBlock;

	private void ValidateAndProcessLink(InlineProcessor processor, LinkInline link, ParserContext context)
	{
		var url = link.Url;
		var line = link.Line + 1;
		var column = link.Column;
		var length = url?.Length ?? 1;

		if (!ValidateBasicUrl(processor, url, line, column, length))
			return;

		var uri = Uri.TryCreate(url, UriKind.Absolute, out var u) ? u : null;

		if (IsCrossLink(uri))
		{
			ProcessCrossLink(link, context, line, column, length);
			return;
		}

		if (ValidateExternalUri(processor, uri, context, line, column, length))
			return;

		ProcessInternalLink(processor, link, context, line, column, length);
	}

	private bool ValidateBasicUrl(InlineProcessor processor, string? url, int line, int column, int length)
	{
		if (string.IsNullOrEmpty(url))
		{
			processor.EmitWarning(line, column, length, "Found empty url");
			return false;
		}
		if (url.Contains("{{") || url.Contains("}}"))
		{
			processor.EmitWarning(line, column, length,
				"The url contains a template expression. Please do not use template expressions in links. " +
				"See https://github.com/elastic/docs-builder/issues/182 for further information.");
			return false;
		}
		return true;
	}

	private bool ValidateExternalUri(InlineProcessor processor, Uri? uri, ParserContext context, int line, int column, int length)
	{
		if (uri == null || !uri.Scheme.StartsWith("http"))
			return false;

		var baseDomain = uri.Host == "localhost" ? "localhost" : string.Join('.', uri.Host.Split('.')[^2..]);
		if (!context.Configuration.ExternalLinkHosts.Contains(baseDomain))
		{
			processor.EmitWarning(
				line,
				column,
				length,
				$"External URI '{uri}' is not allowed. Add '{baseDomain}' to the " +
				$"'external_hosts' list in the configuration file '{context.Configuration.SourceFile}' " +
				"to allow links to this domain."
			);
		}
		return true;
	}

	private static void ProcessCrossLink(LinkInline link, ParserContext context, int line, int column, int length)
	{
		var url = link.Url;
		if (url != null)
			context.Build.Collector.EmitCrossLink(url);
		// TODO: The link is not rendered correctly yet, will be fixed in a follow-up
	}

	private static void ProcessInternalLink(InlineProcessor processor, LinkInline link, ParserContext context, int line, int column, int length)
	{
		var (url, anchor) = SplitUrlAndAnchor(link.Url ?? string.Empty);
		var includeFrom = GetIncludeFromPath(url, context);

		ValidateInternalUrl(processor, url, includeFrom, line, column, length, context);
		ProcessLinkText(processor, link, context, url, anchor, line, column, length);
		UpdateLinkUrl(link, url, anchor, context.Build.UrlPathPrefix ?? string.Empty);
	}

	private static (string url, string? anchor) SplitUrlAndAnchor(string fullUrl)
	{
		var parts = fullUrl.Split('#');
		return (parts[0], parts.Length > 1 ? parts[1].Trim() : null);
	}

	private static string GetIncludeFromPath(string url, ParserContext context) =>
		url.StartsWith('/')
			? context.Parser.SourcePath.FullName
			: context.Path.Directory!.FullName;

	private static void ValidateInternalUrl(InlineProcessor processor, string url, string includeFrom, int line, int column, int length, ParserContext context)
	{
		if (string.IsNullOrWhiteSpace(url))
			return;

		var pathOnDisk = Path.Combine(includeFrom, url.TrimStart('/'));
		if (!context.Build.ReadFileSystem.File.Exists(pathOnDisk))
			processor.EmitError(line, column, length, $"`{url}` does not exist. resolved to `{pathOnDisk}");
	}

	private static void ProcessLinkText(InlineProcessor processor, LinkInline link, ParserContext context, string url, string? anchor, int line, int column, int length)
	{
		if (link.FirstChild != null && string.IsNullOrEmpty(anchor))
			return;

		var file = ResolveFile(context, url);
		var markdown = context.GetDocumentationFile?.Invoke(file) as MarkdownFile;

		if (markdown == null)
		{
			processor.EmitWarning(line, column, length,
				$"'{url}' could not be resolved to a markdown file while creating an auto text link, '{file.FullName}' does not exist.");
			return;
		}

		var title = markdown.Title;

		if (!string.IsNullOrEmpty(anchor))
		{
			ValidateAnchor(processor, markdown, anchor, line, column, length);
			if (link.FirstChild == null && markdown.TableOfContents.TryGetValue(anchor, out var heading))
				title += " > " + heading.Heading;
		}

		if (link.FirstChild == null && !string.IsNullOrEmpty(title))
			link.AppendChild(new LiteralInline(title));
	}

	private static IFileInfo ResolveFile(ParserContext context, string url) =>
		string.IsNullOrWhiteSpace(url)
			? context.Path
			: url.StartsWith('/')
				? context.Build.ReadFileSystem.FileInfo.New(Path.Combine(context.Build.SourcePath.FullName, url.TrimStart('/')))
				: context.Build.ReadFileSystem.FileInfo.New(Path.Combine(context.Path.Directory!.FullName, url));

	private static void ValidateAnchor(InlineProcessor processor, MarkdownFile markdown, string anchor, int line, int column, int length)
	{
		if (!markdown.Anchors.Contains(anchor))
			processor.EmitError(line, column, length, $"`{anchor}` does not exist in {markdown.FileName}.");
	}

	private static void UpdateLinkUrl(LinkInline link, string url, string? anchor, string urlPathPrefix)
	{
		if (url.EndsWith(".md"))
			url = Path.ChangeExtension(url, ".html");

		if (url.StartsWith("/") && !string.IsNullOrWhiteSpace(urlPathPrefix))
			url = $"{urlPathPrefix.TrimEnd('/')}{url}";

		link.Url = !string.IsNullOrEmpty(anchor) ? $"{url}#{anchor}" : url;
	}

	private static bool IsCrossLink(Uri? uri) =>
		uri != null // This means it's not a local
		&& !ExcludedSchemes.Contains(uri.Scheme)
		&& !uri.IsFile
		&& Path.GetExtension(uri.OriginalString) == ".md";
}
