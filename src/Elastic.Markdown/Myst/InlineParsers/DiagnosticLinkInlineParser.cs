// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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

		ValidateAndProcessLink(link, processor, context);

		ParseStylingInstructions(link);

		return match;
	}


	private void ParseStylingInstructions(LinkInline link)
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

	private void ValidateAndProcessLink(LinkInline link, InlineProcessor processor, ParserContext context)
	{
		var url = link.Url;

		if (!ValidateBasicUrl(link, processor, url))
			return;

		var uri = Uri.TryCreate(url, UriKind.Absolute, out var u) ? u : null;

		if (IsCrossLink(uri))
		{
			ProcessCrossLink(link, processor, context, uri);
			return;
		}

		if (ValidateExternalUri(link, processor, context, uri))
			return;

		ProcessInternalLink(link, processor, context);
	}

	private bool ValidateBasicUrl(LinkInline link, InlineProcessor processor, string? url)
	{
		if (string.IsNullOrEmpty(url))
		{
			processor.EmitWarning(link, "Found empty url");
			return false;
		}
		if (url.Contains("{{") || url.Contains("}}"))
		{
			processor.EmitWarning(link,
				"The url contains a template expression. Please do not use template expressions in links. " +
				"See https://github.com/elastic/docs-builder/issues/182 for further information.");
			return false;
		}
		return true;
	}

	private bool ValidateExternalUri(LinkInline link, InlineProcessor processor, ParserContext context, Uri? uri)
	{
		if (uri == null)
			return false;

		if (!uri.Scheme.StartsWith("http") && !uri.Scheme.StartsWith("mailto"))
			return false;

		var baseDomain = uri.Host == "localhost" ? "localhost" : string.Join('.', uri.Host.Split('.')[^2..]);
		if (!context.Configuration.ExternalLinkHosts.Contains(baseDomain))
		{
			processor.EmitWarning(
				link,
				$"External URI '{uri}' is not allowed. Add '{baseDomain}' to the " +
				$"'external_hosts' list in the configuration file '{context.Configuration.SourceFile}' " +
				"to allow links to this domain."
			);
		}
		return true;
	}

	private static void ProcessCrossLink(LinkInline link, InlineProcessor processor, ParserContext context, Uri uri)
	{
		var url = link.Url;
		if (url != null)
			context.Build.Collector.EmitCrossLink(url);

		var scheme = uri.Scheme;
		if (!context.Configuration.CrossLinkRepositories.Contains(scheme))
		{
			processor.EmitWarning(
				link,
				$"Cross link URI{uri}' is not allowed. Add '{scheme}' to the " +
				$"'cross_link' list in the configuration file '{context.Configuration.SourceFile}' " +
				"to allow cross links to this external documentation set."
			);
			return;
		}
		if (context.LinksResolver.TryResolve(uri, out var resolvedUri))
			link.Url = resolvedUri.ToString();
	}

	private static void ProcessInternalLink(LinkInline link, InlineProcessor processor, ParserContext context)
	{
		var (url, anchor) = SplitUrlAndAnchor(link.Url ?? string.Empty);
		var includeFrom = GetIncludeFromPath(url, context);
		var file = ResolveFile(context, url);
		ValidateInternalUrl(processor, url, includeFrom, link, context);
		ProcessLinkText(processor, link, context, url, anchor, file);
		UpdateLinkUrl(link, url, context, anchor, file);
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

	private static void ValidateInternalUrl(InlineProcessor processor, string url, string includeFrom, LinkInline link, ParserContext context)
	{
		if (string.IsNullOrWhiteSpace(url))
			return;

		var pathOnDisk = Path.Combine(includeFrom, url.TrimStart('/'));
		if (!context.Build.ReadFileSystem.File.Exists(pathOnDisk))
			processor.EmitError(link, $"`{url}` does not exist. resolved to `{pathOnDisk}");
	}

	private static void ProcessLinkText(InlineProcessor processor, LinkInline link, ParserContext context, string url, string? anchor, IFileInfo file)
	{
		if (link.FirstChild != null && string.IsNullOrEmpty(anchor))
			return;

		var markdown = context.GetDocumentationFile?.Invoke(file) as MarkdownFile;

		if (markdown == null)
		{
			processor.EmitWarning(link, $"'{url}' could not be resolved to a markdown file while creating an auto text link, '{file.FullName}' does not exist.");
			return;
		}

		var title = markdown.Title;

		if (!string.IsNullOrEmpty(anchor))
		{
			ValidateAnchor(processor, markdown, anchor, link);
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

	private static void ValidateAnchor(InlineProcessor processor, MarkdownFile markdown, string anchor, LinkInline link)
	{
		if (!markdown.Anchors.Contains(anchor))
			processor.EmitError(link, $"`{anchor}` does not exist in {markdown.FileName}.");
	}

	private static void UpdateLinkUrl(LinkInline link, string url, ParserContext context, string? anchor, IFileInfo file)
	{
		var urlPathPrefix = context.Build.UrlPathPrefix ?? string.Empty;

		if (!url.StartsWith('/') && !string.IsNullOrEmpty(url))
			url = GetRootRelativePath(context, file);

		if (url.EndsWith(".md"))
		{
			url = url.EndsWith("/index.md")
				? url.Remove(url.LastIndexOf("index.md", StringComparison.Ordinal), "index.md".Length)
				: url.Remove(url.LastIndexOf(".md", StringComparison.Ordinal), ".md".Length);
		}

		if (!string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(urlPathPrefix))
			url = $"{urlPathPrefix.TrimEnd('/')}{url}";

		link.Url = string.IsNullOrEmpty(anchor) ? url : $"{url}#{anchor}";
	}

	private static string GetRootRelativePath(ParserContext context, IFileInfo file)
	{
		var docsetDirectory = context.Configuration.SourceFile.Directory;
		return file.FullName.Replace(docsetDirectory!.FullName, string.Empty);
	}

	private static bool IsCrossLink([NotNullWhen(true)]Uri? uri) =>
		uri != null // This means it's not a local
		&& !ExcludedSchemes.Contains(uri.Scheme)
		&& !uri.IsFile
		&& Path.GetExtension(uri.OriginalString) == ".md";
}
