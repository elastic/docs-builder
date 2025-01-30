// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst.Comments;
using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
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

public class DiagnosticLinkInlineParser : LinkInlineParser
{
	// See https://www.iana.org/assignments/uri-schemes/uri-schemes.xhtml for a list of URI schemes
	// We can add more schemes as needed
	private static readonly ImmutableHashSet<string> ExcludedSchemes = ["http", "https", "tel", "jdbc"];

	public override bool Match(InlineProcessor processor, ref StringSlice slice)
	{
		var match = base.Match(processor, ref slice);
		if (!match)
			return false;

		if (processor.Inline is not LinkInline link)
			return match;

		// Links in comments should not be validated
		// This works for the current test cases, but we might need to revisit this in case it needs some traversal
		if (link.Parent?.ParentBlock is CommentBlock)
			return match;

		var url = link.Url;
		var line = link.Line + 1;
		var column = link.Column;
		var length = url?.Length ?? 1;

		var context = processor.GetContext();
		if (processor.GetContext().SkipValidation)
			return match;

		if (string.IsNullOrEmpty(url))
		{
			processor.EmitWarning(line, column, length, $"Found empty url");
			return match;
		}

		if (url.Contains("{{") || url.Contains("}}"))
		{
			processor.EmitWarning(line, column, length, "The url contains a template expression. Please do not use template expressions in links. See https://github.com/elastic/docs-builder/issues/182 for further information.");
			return match;
		}

		var uri = Uri.TryCreate(url, UriKind.Absolute, out var u) ? u : null;

		if (IsCrossLink(uri))
			processor.GetContext().Build.Collector.EmitCrossLink(url!);

		if (uri != null && uri.Scheme.StartsWith("http"))
		{
			var baseDomain = uri.Host == "localhost" ? "localhost" : string.Join('.', uri.Host.Split('.')[^2..]);
			if (!context.Configuration.ExternalLinkHosts.Contains(baseDomain))
			{
				processor.EmitWarning(
					line,
					column,
					length,
					$"External URI '{uri}' is not allowed. Add '{baseDomain}' to the " +
					$"'external_hosts' list in {context.Configuration.SourceFile} to " +
					"allow links to this domain.");
			}
			return match;
		}

		var includeFrom = context.Path.Directory!.FullName;
		if (url.StartsWith('/'))
			includeFrom = context.Parser.SourcePath.FullName;

		var anchors = url.Split('#');
		var anchor = anchors.Length > 1 ? anchors[1].Trim() : null;
		url = anchors[0];

		if (!string.IsNullOrWhiteSpace(url))
		{
			var pathOnDisk = Path.Combine(includeFrom, url.TrimStart('/'));
			if ((uri is null || uri.IsFile) && !context.Build.ReadFileSystem.File.Exists(pathOnDisk))
				processor.EmitError(line, column, length, $"`{url}` does not exist. resolved to `{pathOnDisk}");
		}
		else
		{
			if (string.IsNullOrEmpty(anchor))
				processor.EmitWarning(line, column, length, $"No url was specified for the link.");
			link.Url = "";
		}

		if (link.FirstChild == null || !string.IsNullOrEmpty(anchor))
		{
			var file = string.IsNullOrWhiteSpace(url)
				? context.Path
				: url.StartsWith('/')
					? context.Build.ReadFileSystem.FileInfo.New(Path.Combine(context.Build.SourcePath.FullName, url.TrimStart('/')))
					: context.Build.ReadFileSystem.FileInfo.New(Path.Combine(context.Path.Directory!.FullName, url));
			var markdown = context.GetDocumentationFile?.Invoke(file) as MarkdownFile;
			if (markdown == null)
			{
				processor.EmitWarning(line,
					column,
					length,
					$"'{url}' could not be resolved to a markdown file while creating an auto text link, '{file.FullName}' does not exist.");
			}

			var title = markdown?.Title;

			if (!string.IsNullOrEmpty(anchor))
			{
				if (markdown == null || !markdown.Anchors.Contains(anchor))
					processor.EmitError(line, column, length, $"`{anchor}` does not exist in {markdown?.FileName}.");
				else if (link.FirstChild == null && markdown.TableOfContents.TryGetValue(anchor, out var heading))
					title += " > " + heading.Heading;

			}

			if (link.FirstChild == null && !string.IsNullOrEmpty(title))
				link.AppendChild(new LiteralInline(title));
		}

		if (url.EndsWith(".md"))
			link.Url = Path.ChangeExtension(url, ".html");
		// rooted links might need the configured path prefix to properly link
		var prefix = processor.GetBuildContext().UrlPathPrefix;
		if (url.StartsWith("/") && !string.IsNullOrWhiteSpace(prefix))
			link.Url = $"{prefix}{link.Url}";

		if (!string.IsNullOrEmpty(anchor))
			link.Url += $"#{anchor}";

		return match;
	}

	private static bool IsCrossLink(Uri? uri) =>
		uri != null
		&& !ExcludedSchemes.Contains(uri.Scheme)
		&& !uri.IsFile
		&& Path.GetExtension(uri.OriginalString) == ".md";
}
