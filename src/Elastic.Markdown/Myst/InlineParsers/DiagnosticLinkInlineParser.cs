// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Myst.Directives;
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
	public override bool Match(InlineProcessor processor, ref StringSlice slice)
	{
		var match = base.Match(processor, ref slice);
		if (!match) return false;

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
			processor.EmitWarning(line, column, length, $"external URI: {uri} ");
			return match;
		}

		var includeFrom = context.Path.Directory!.FullName;
		if (url.StartsWith('/'))
			includeFrom = context.Parser.SourcePath.FullName;

		var pathOnDisk = Path.Combine(includeFrom, url.TrimStart('/'));
		if (!context.Build.ReadFileSystem.File.Exists(pathOnDisk))
			processor.EmitError(line, column, length, $"`{url}` does not exist. resolved to `{pathOnDisk}");

		if (link.FirstChild == null)
		{
			var title = context.GetTitle?.Invoke(url);
			if (!string.IsNullOrEmpty(title))
				link.AppendChild(new LiteralInline(title));
		}

		if (url.EndsWith(".md"))
			link.Url = Path.ChangeExtension(url, ".html");

		return match;



	}
}
