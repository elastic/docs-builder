// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Helpers;
using Microsoft.AspNetCore.Html;

namespace Elastic.Markdown.Myst.CodeBlocks;

public class CodeViewModel
{
	public required List<ApiSegment> ApiSegments { get; init; }
	public required string? Caption { get; init; }
	public required string Language { get; init; }
	public required string? CrossReferenceName { get; init; }
	public string? RawIncludedFileContents { get; init; }
	public EnhancedCodeBlock? EnhancedCodeBlock { get; set; }

	public HtmlString RenderBlock()
	{
		if (!string.IsNullOrWhiteSpace(RawIncludedFileContents))
			return new HtmlString(RawIncludedFileContents);
		if (EnhancedCodeBlock == null)
			return HtmlString.Empty;

		var subscription = DocumentationObjectPoolProvider.HtmlRendererPool.Get();
		EnhancedCodeBlockHtmlRenderer.RenderCodeBlockLines(subscription.HtmlRenderer, EnhancedCodeBlock);
		var result = subscription.RentedStringBuilder?.ToString();
		DocumentationObjectPoolProvider.HtmlRendererPool.Return(subscription);
		return new HtmlString(result);
	}

	public HtmlString RenderConsoleCallouts(int lineNumber)
	{
		if (EnhancedCodeBlock?.CallOuts == null)
			return HtmlString.Empty;

		var callouts = EnhancedCodeBlock.CallOuts.Where(c => c.Line == lineNumber);
		if (!callouts.Any())
			return HtmlString.Empty;

		var html = new System.Text.StringBuilder();
		foreach (var callout in callouts)
		{
			_ = html.Append($"<span class=\"code-callout\" data-index=\"{callout.Index}\"></span>");
		}
		return new HtmlString(html.ToString());
	}
}
