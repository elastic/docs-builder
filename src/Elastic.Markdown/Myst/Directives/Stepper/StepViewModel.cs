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
		// Parse the title as markdown inline content
		var document = Markdig.Markdown.Parse(Title, MarkdownParser.Pipeline);
		var firstBlock = document.FirstOrDefault() as Markdig.Syntax.ParagraphBlock;
		var inline = firstBlock?.Inline;

		if (firstBlock == null)
			return new(Title);

		// Use the HTML renderer to render the inline content with full processing
		var subscription = DocumentationObjectPoolProvider.HtmlRendererPool.Get();
		_ = subscription.HtmlRenderer.WriteLeafInline(firstBlock);

		var result = subscription.RentedStringBuilder?.ToString() ?? Title;
		DocumentationObjectPoolProvider.HtmlRendererPool.Return(subscription);
		return new(result);
	}
}
