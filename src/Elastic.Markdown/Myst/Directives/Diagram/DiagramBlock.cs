// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;

namespace Elastic.Markdown.Myst.Directives.Diagram;

public class DiagramBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "diagram";

	/// <summary>
	/// The diagram type (e.g., "mermaid", "d2", "graphviz", "plantuml")
	/// </summary>
	public string? DiagramType { get; private set; }

	/// <summary>
	/// The raw diagram content
	/// </summary>
	public string? Content { get; private set; }

	/// <summary>
	/// The encoded diagram URL for Kroki service
	/// </summary>
	public string? EncodedUrl { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		// Call the DirectiveBlock's FinalizeAndValidate
		// for setup common to all the directive blocks
		base.FinalizeAndValidate(context);

		// Extract diagram type from arguments or default to "mermaid"
		DiagramType = !string.IsNullOrWhiteSpace(Arguments) ? Arguments.ToLowerInvariant() : "mermaid";

		// Extract content from the directive body
		Content = ExtractContent();

		if (string.IsNullOrWhiteSpace(Content))
		{
			this.EmitError("Diagram directive requires content.");
			return;
		}

		// Generate the encoded URL for Kroki
		try
		{
			EncodedUrl = DiagramEncoder.GenerateKrokiUrl(DiagramType, Content);
		}
		catch (Exception ex)
		{
			this.EmitError($"Failed to encode diagram: {ex.Message}", ex);
		}
	}

	private string? ExtractContent()
	{
		if (!this.Any())
			return null;

		var lines = new List<string>();
		foreach (var block in this)
		{
			if (block is Markdig.Syntax.LeafBlock leafBlock)
			{
				var content = leafBlock.Lines.ToString();
				if (!string.IsNullOrWhiteSpace(content))
					lines.Add(content);
			}
		}

		return lines.Count > 0 ? string.Join("\n", lines) : null;
	}
}
