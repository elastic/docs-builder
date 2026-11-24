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

	public static (string Body, string[] Headings) Read(string markdown)
	{
		var document = Markdown.Parse(markdown, Pipeline);
		string[] headings =
		[
			.. document
				.Descendants<HeadingBlock>()
				.Where(static h => h.Level > 1)
				.Select(h => ToPlainText(markdown.Substring(h.Span.Start, h.Span.Length)))
				.Where(static h => h.Length > 0)
				.Distinct(StringComparer.Ordinal)
		];
		return (ToPlainText(markdown), headings);
	}

	private static string ToPlainText(string markdown)
	{
		using var writer = new StringWriter();
		_ = Markdown.ToPlainText(markdown, writer, Pipeline);
		return writer.ToString().Trim();
	}
}
