// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst.Directives;
using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Syntax;
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
	public override bool Match(InlineProcessor processor, ref StringSlice slice)
	{
		var match = base.Match(processor, ref slice);
		if (!match)
			return false;

		if (processor.Inline is not LinkInline link)
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

		if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme.StartsWith("http"))
		{
			var baseDomain = string.Join('.', uri.Host.Split('.')[^2..]);
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
			if (!context.Build.ReadFileSystem.File.Exists(pathOnDisk))
				processor.EmitError(line, column, length, $"`{url}` does not exist. resolved to `{pathOnDisk}");
			else
			{

			}
		}
		else
			link.Url = "";

		if (link.FirstChild == null || !string.IsNullOrEmpty(anchor))
		{
			var file = string.IsNullOrWhiteSpace(url) ? context.Path
				: context.Build.ReadFileSystem.FileInfo.New(Path.Combine(context.Build.SourcePath.FullName, url.TrimStart('/')));
			var markdown = context.GetDocumentationFile?.Invoke(file) as MarkdownFile;
			var title = markdown?.Title;

			if (!string.IsNullOrEmpty(anchor))
			{
				if (markdown == null || (!markdown.TableOfContents.TryGetValue(anchor, out var heading)
					&& !markdown.AdditionalLabels.Contains(anchor)))
					processor.EmitError(line, column, length, $"`{anchor}` does not exist in {markdown?.FileName}.");

				else if (link.FirstChild == null && heading != null)
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
			link.Url = $"{prefix}/{link.Url}";

		if (!string.IsNullOrEmpty(anchor))
			link.Url += $"#{anchor}";

		return match;



	}
}
