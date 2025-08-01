// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Helpers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Html;

namespace Elastic.Markdown.Myst.Directives.Stepper;

public class StepViewModel : DirectiveViewModel
{
	public required string Title { get; init; }
	public required string Anchor { get; init; }
	public required int HeadingLevel { get; init; }

	/// <summary>
	/// Renders the title with full markdown processing (substitutions, links, emphasis, etc.)
	/// </summary>
	public HtmlString RenderTitle()
	{
		// Apply substitutions first, then parse with Markdig
		var directiveBlock = (DirectiveBlock)DirectiveBlock;
		var span = Title.AsSpan();
		var processedTitle = span.ReplaceSubstitutions(directiveBlock.Build.Configuration.Substitutions, directiveBlock.Build.Collector, out var replacement)
			? replacement : Title;

		// Parse the processed title as markdown inline content
		var document = Markdig.Markdown.Parse(processedTitle, MarkdownParser.Pipeline);

		if (document.FirstOrDefault() is not Markdig.Syntax.ParagraphBlock firstBlock)
			return new(processedTitle);

		// Use the HTML renderer to render the inline content with full processing
		var subscription = DocumentationObjectPoolProvider.HtmlRendererPool.Get();
		_ = subscription.HtmlRenderer.WriteLeafInline(firstBlock);

		var result = subscription.RentedStringBuilder?.ToString() ?? processedTitle;
		DocumentationObjectPoolProvider.HtmlRendererPool.Return(subscription);
		return new(result);
	}
}
