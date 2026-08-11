// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Buffers;
using Markdig;

namespace Elastic.Markdown.Helpers;

public static class MarkdownStringExtensions
{
	// A default (empty) pipeline is sufficient for stripping markdown from short title strings.
	// Shared across calls to avoid building a MarkdownPipeline per call.
	private static readonly MarkdownPipeline PlainTextPipeline = new MarkdownPipelineBuilder().Build();

	// Characters that signal the string contains markdown syntax and must go through Markdig.
	// The backslash covers escaped spans like \*literal\*.
	private static readonly SearchValues<char> MarkdownChars = SearchValues.Create("`*_[]<&\\");

	public static string StripMarkdown(this string markdown)
	{
		// Titles are overwhelmingly plain text (e.g. the ctor's RelativePath assignment).
		// Skip the pipeline entirely for strings that cannot contain markdown syntax.
		if (markdown.AsSpan().IndexOfAny(MarkdownChars) < 0)
			return markdown;

		using var writer = new StringWriter();
		_ = Markdig.Markdown.ToPlainText(markdown, writer, PlainTextPipeline);
		return writer.ToString().TrimEnd('\n');
	}
}
