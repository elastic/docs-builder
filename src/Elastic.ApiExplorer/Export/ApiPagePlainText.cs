// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Markdig;
using Markdig.Syntax;

namespace Elastic.ApiExplorer.Export;

/// <summary>The search text of an API page's Markdown export: the page as plain text, and the titles of its sections.</summary>
internal static class ApiPagePlainText
{
	private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().Build();

	// A title or description may be plain text already, where `<ping>` is literal text and not a tag.
	private static readonly MarkdownPipeline LinePipeline = new MarkdownPipelineBuilder().DisableHtml().Build();

	public static (string Body, string[] Headings) Read(string markdown)
	{
		var document = Markdown.Parse(markdown, Pipeline);
		string[] headings =
		[
			.. document
				.Descendants<HeadingBlock>()
				.Where(static h => h.Level > 1)
				.Select(h => Text(markdown.Substring(h.Span.Start, h.Span.Length), Pipeline))
				.Where(static h => h.Length > 0)
				.Distinct(StringComparer.Ordinal)
		];
		return (Text(markdown, Pipeline), headings);
	}

	/// <summary>A title or description as plain text, without link targets, whether it arrives as Markdown or as plain text.</summary>
	public static string Line(string text) => Text(text, LinePipeline);

	private static string Text(string markdown, MarkdownPipeline pipeline)
	{
		using var writer = new StringWriter();
		_ = Markdown.ToPlainText(markdown, writer, pipeline);
		return writer.ToString().Trim();
	}
}
