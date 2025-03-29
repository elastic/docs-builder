// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Helpers;
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

internal sealed partial class LinkRegexExtensions
{
	[GeneratedRegex(@"\s\=(?<width>\d+%?)(?:x(?<height>\d+%?))?$", RegexOptions.IgnoreCase, "en-US")]
	public static partial Regex MatchTitleStylingInstructions();
}

public class DiagnosticLinkInlineParser : LinkInlineParser
{
	// See https://www.iana.org/assignments/uri-schemes/uri-schemes.xhtml for a list of URI schemes
	private static readonly ImmutableHashSet<string> ExcludedSchemes = ["http", "https", "tel", "jdbc", "mailto"];

	public override bool Match(InlineProcessor processor, ref StringSlice slice)
	{
		var match = base.Match(processor, ref slice);
		if (!match || processor.Inline is not LinkInline link)
			return match;

		var context = processor.GetContext();
		link.SetData(nameof(context.CurrentUrlPath), context.CurrentUrlPath);

		if (IsInCommentBlock(link) || context.SkipValidation)
			return match;

		ValidateAndProcessLink(link, processor, context);

		ParseStylingInstructions(link);

		return match;
	}


	private static void ParseStylingInstructions(LinkInline link)
	{
		if (!link.IsImage)
			return;

		if (string.IsNullOrWhiteSpace(link.Title) || link.Title.IndexOf('=') < 0)
			return;

		var matches = LinkRegexExtensions.MatchTitleStylingInstructions().Match(link.Title);
		if (!matches.Success)
			return;

		var width = matches.Groups["width"].Value;
		if (!width.EndsWith('%'))
			width += "px";
		var height = matches.Groups["height"].Value;
		if (string.IsNullOrEmpty(height))
			height = width;
		else if (!height.EndsWith('%'))
			height += "px";
		var title = link.Title[..matches.Index];

		link.Title = title;
		var attributes = link.GetAttributes();
		attributes.AddProperty("width", width);
		attributes.AddProperty("height", height);
	}

	private static bool IsInCommentBlock(LinkInline link) =>
		link.Parent?.ParentBlock is CommentBlock;

	private static void ValidateAndProcessLink(LinkInline link, InlineProcessor processor, ParserContext context)
	{
		var url = link.Url;

		if (url?.Contains("{{") == true)
		{
			var replacedUrl = url.ReplaceSubstitutions(processor.GetContext());
			if (replacedUrl.Contains("{{"))
			{
				processor.EmitError(link,
					$"The url contains unresolved template expressions: '{replacedUrl}'. Please check if there is an appropriate global or frontmatter subs variable."
				);
				return;
			}

			if (!replacedUrl.StartsWith("http"))
			{
				processor.EmitError(link, $"Link is resolved to '{replacedUrl}'. Only external links are allowed to be resolved from template expressions.");
				return;
			}
			url = replacedUrl;
			link.Url = replacedUrl;
		}

		if (!ValidateBasicUrl(link, processor, url))
			return;

		var uri = Uri.TryCreate(url, UriKind.Absolute, out var u) ? u : null;

		if (IsCrossLink(uri))
		{
			link.SetData("isCrossLink", true);
			ProcessCrossLink(link, processor, context, uri);
			return;
		}
		link.SetData("isCrossLink", false);

		if (ValidateExternalUri(link, processor, uri))
			return;

		ProcessInternalLink(link, processor, context);
	}

	private static bool ValidateBasicUrl(LinkInline link, InlineProcessor processor, string? url)
	{
		if (string.IsNullOrEmpty(url))
		{
			processor.EmitError(link, "Found empty url");
			return false;
		}
		return true;
	}

	private static bool ValidateExternalUri(LinkInline link, InlineProcessor processor, Uri? uri)
	{
		if (uri == null)
			return false;

		if (!uri.Scheme.StartsWith("http") && !uri.Scheme.StartsWith("mailto"))
			return false;

		var baseDomain = uri.Host == "localhost" ? "localhost" : string.Join('.', uri.Host.Split('.')[^2..]);
		if (uri.Scheme == "mailto" && baseDomain != "elastic.co")
		{
			processor.EmitWarning(
				link,
				$"mailto links should be to elastic.co domains. Found {uri.Host} in {link.Url}. "
			);
		}

		return true;
	}

	private static void ProcessCrossLink(LinkInline link, InlineProcessor processor, ParserContext context, Uri uri)
	{
		var url = link.Url;
		if (url != null)
			context.Build.Collector.EmitCrossLink(url);

		if (context.CrossLinkResolver.TryResolve(
				s => processor.EmitError(link, s),
				s => processor.EmitWarning(link, s),
				uri, out var resolvedUri)
		   )
			link.Url = resolvedUri.ToString();
	}

	private static void ProcessInternalLink(LinkInline link, InlineProcessor processor, ParserContext context)
	{
		if (link.Url != null && link.Url.StartsWith('!'))
		{
			// [](!/already/resolved/url) internal syntax to allow markdown embedding already resolved links
			var verbatimUrl = link.Url[1..];
			link.Url = verbatimUrl;
			var md = ResolveFile(context, verbatimUrl);
			_ = SetLinkData(link, processor, context, md, verbatimUrl);
			return;
		}

		var (url, anchor) = SplitUrlAndAnchor(link.Url ?? string.Empty);
		var includeFrom = GetIncludeFromPath(url, context);
		var file = ResolveFile(context, url);
		ValidateInternalUrl(processor, url, includeFrom, link, context);

		var linkMarkdown = SetLinkData(link, processor, context, file, url);

		ProcessLinkText(processor, link, linkMarkdown, anchor, url, file);
		UpdateLinkUrl(link, url, context, anchor);
	}

	private static MarkdownFile? SetLinkData(LinkInline link, InlineProcessor processor, ParserContext context,
		IFileInfo file, string url)
	{
		if (context.DocumentationFileLookup(context.MarkdownSourcePath) is MarkdownFile currentMarkdown)
		{
			link.SetData(nameof(currentMarkdown.NavigationRoot), currentMarkdown.NavigationRoot);

			if (link.IsImage)
			{
				//TODO make this an error once all offending repositories have been updated
				if (!file.Directory!.FullName.StartsWith(currentMarkdown.ScopeDirectory.FullName + Path.DirectorySeparatorChar))
					processor.EmitHint(link, $"Image '{url}' is referenced out of table of contents scope '{currentMarkdown.ScopeDirectory}'.");
			}
		}


		var linkMarkdown = context.DocumentationFileLookup(file) as MarkdownFile;
		if (linkMarkdown is not null)
			link.SetData($"Target{nameof(currentMarkdown.NavigationRoot)}", linkMarkdown.NavigationRoot);

		ProcessLinkText(processor, link, linkMarkdown, anchor, url, file);
		UpdateLinkUrl(link, url, context, anchor);
		return linkMarkdown;
	}

	private static (string url, string? anchor) SplitUrlAndAnchor(string fullUrl)
	{
		var parts = fullUrl.Split('#');
		return (parts[0].TrimStart('!'), parts.Length > 1 ? parts[1].Trim() : null);
	}

	private static string GetIncludeFromPath(string url, ParserContext context) =>
		url.StartsWith('/')
			? context.Build.DocumentationSourceDirectory.FullName
			: context.MarkdownSourcePath.Directory!.FullName;

	private static void ValidateInternalUrl(InlineProcessor processor, string url, string includeFrom, LinkInline link, ParserContext context)
	{
		if (string.IsNullOrWhiteSpace(url))
			return;

		var pathOnDisk = Path.GetFullPath(Path.Combine(includeFrom, url.TrimStart('/')));
		if (!context.Build.ReadFileSystem.File.Exists(pathOnDisk))
			processor.EmitError(link, $"`{url}` does not exist. resolved to `{pathOnDisk}");
	}

	private static void ProcessLinkText(InlineProcessor processor, LinkInline link, MarkdownFile? markdown, string? anchor, string url, IFileInfo file)
	{
		if (link.FirstChild != null && string.IsNullOrEmpty(anchor))
			return;

		if (markdown is null && link.FirstChild == null)
		{
			processor.EmitWarning(link,
				$"'{url}' could not be resolved to a markdown file while creating an auto text link, '{file.FullName}' does not exist.");
			return;
		}

		var title = markdown?.Title;

		if (!string.IsNullOrEmpty(anchor))
		{
			if (markdown is not null)
				ValidateAnchor(processor, markdown, anchor, link);
			if (link.FirstChild == null && (markdown?.PageTableOfContent.TryGetValue(anchor, out var heading) ?? false))
				title += " > " + heading.Heading;
		}

		if (link.FirstChild == null && !string.IsNullOrEmpty(title))
			_ = link.AppendChild(new LiteralInline(title));
	}

	private static IFileInfo ResolveFile(ParserContext context, string url) =>
		string.IsNullOrWhiteSpace(url)
			? context.MarkdownSourcePath
			: url.StartsWith('/')
				? context.Build.ReadFileSystem.FileInfo.New(Path.Combine(context.Build.DocumentationSourceDirectory.FullName, url.TrimStart('/')))
				: context.Build.ReadFileSystem.FileInfo.New(Path.Combine(context.MarkdownSourcePath.Directory!.FullName, url));

	private static void ValidateAnchor(InlineProcessor processor, MarkdownFile markdown, string anchor, LinkInline link)
	{
		if (!markdown.Anchors.Contains(anchor))
			processor.EmitError(link, $"`{anchor}` does not exist in {markdown.FileName}.");
	}

	private static void UpdateLinkUrl(LinkInline link, string url, ParserContext context, string? anchor)
	{
		var urlPathPrefix = context.Build.UrlPathPrefix ?? string.Empty;

		if (!url.StartsWith('/') && !string.IsNullOrEmpty(url))
		{
			// eat overall path prefix since its gets appended later
			var subPrefix = context.CurrentUrlPath.Length >= urlPathPrefix.Length
				? context.CurrentUrlPath[urlPathPrefix.Length..]
				: urlPathPrefix;

			var markdownPath = context.MarkdownSourcePath.Name;

			// if the current path is an index e.g /reference/cloud-k8s/
			// './' current path lookups should be relative to sub-path.
			// If it's not e.g /reference/cloud-k8s/api-docs/ these links should resolve on folder up.
			var siblingsGoToCurrent = url.StartsWith("./") && markdownPath == "index.md";
			if (!siblingsGoToCurrent)
				subPrefix = subPrefix[..subPrefix.LastIndexOf('/')];

			var combined = '/' + Path.Combine(subPrefix, url).TrimStart('/');
			url = Path.GetFullPath(combined);
		}

		if (url.EndsWith(".md"))
		{
			url = url.EndsWith($"{Path.DirectorySeparatorChar}index.md")
				? url.Remove(url.LastIndexOf("index.md", StringComparison.Ordinal), "index.md".Length)
				: url.Remove(url.LastIndexOf(".md", StringComparison.Ordinal), ".md".Length);
		}

		// When running on Windows, path traversal results must be normalized prior to being used in a URL
		// Path.GetFullPath() will result in the drive letter being appended to the path, which needs to be pruned back.
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			url = url.Replace('\\', '/');
			if (url.Length > 2 && url[1] == ':')
				url = url[2..];
		}

		if (!string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(urlPathPrefix))
			url = $"{urlPathPrefix.TrimEnd('/')}{url}";

		// TODO this is hardcoded should be part of extension system
		if (url.EndsWith(".toml"))
			url = url[..^5];

		link.Url = string.IsNullOrEmpty(anchor) ? url : $"{url}#{anchor}";
	}

	private static bool IsCrossLink([NotNullWhen(true)] Uri? uri) =>
		uri != null // This means it's not a local
		&& !ExcludedSchemes.Contains(uri.Scheme)
		&& !uri.IsFile
		&& !string.IsNullOrEmpty(uri.Scheme);
}
