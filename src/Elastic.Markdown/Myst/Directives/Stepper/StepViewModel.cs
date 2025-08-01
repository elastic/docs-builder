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
	public HtmlString RenderTitle() =>
		// For now, just return the title as-is since substitutions are already applied in FinalizeAndValidate
		// In the future, this could be extended to handle full markdown processing using the HTML renderer
		new(Title);
}
