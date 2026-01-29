// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site;
using Elastic.Markdown.Diagnostics;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst.Directives.Mermaid;

public class MermaidBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	private static readonly Lazy<MermaidRenderer> Renderer = new(() => new MermaidRenderer());

	public override string Directive => "mermaid";

	/// <summary>
	/// The raw Mermaid diagram content.
	/// </summary>
	public string? Content { get; private set; }

	/// <summary>
	/// The rendered SVG content.
	/// </summary>
	public string? RenderedSvg { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		// Extract content from the directive body
		Content = ExtractContent();

		if (string.IsNullOrWhiteSpace(Content))
		{
			this.EmitError("Mermaid directive requires content.");
			return;
		}

		// Render the Mermaid diagram to SVG
		try
		{
			RenderedSvg = Renderer.Value.Render(Content);
		}
		catch (Exception ex) when (ex is not OutOfMemoryException
								   and not StackOverflowException
								   and not TaskCanceledException)
		{
			this.EmitError($"Failed to render Mermaid diagram: {ex.Message}");
		}
	}

	private string? ExtractContent()
	{
		if (!this.Any())
			return null;

		var lines = this
			.OfType<LeafBlock>()
			.Select(leafBlock => leafBlock.Lines.ToString())
			.Where(content => !string.IsNullOrWhiteSpace(content))
			.ToList();

		return lines.Count > 0 ? string.Join("\n", lines) : null;
	}
}
