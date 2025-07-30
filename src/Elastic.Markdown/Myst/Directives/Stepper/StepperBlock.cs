// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Helpers;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst.Directives.Stepper;

public class StepperBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "stepper";

	public override void FinalizeAndValidate(ParserContext context)
	{
	}
}

public class StepBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context), IBlockTitle
{
	public override string Directive => "step";
	public string Title { get; private set; } = string.Empty;
	public string Anchor { get; private set; } = string.Empty;
	public int HeadingLevel { get; private set; } = 3; // Default to h3

	public override void FinalizeAndValidate(ParserContext context)
	{
		Title = Arguments ?? string.Empty;

		// Apply substitutions to the title
		Title = Title.ReplaceSubstitutions(context);

		Anchor = Prop("anchor") ?? Title.Slugify();

		// Calculate heading level based on preceding heading
		HeadingLevel = CalculateHeadingLevel();

		// Set CrossReferenceName so this step can be found by ToC generation
		if (!string.IsNullOrEmpty(Title))
		{
			CrossReferenceName = Anchor;
		}
	}

	private int CalculateHeadingLevel()
	{
		// Find the document root
		var current = (ContainerBlock)this;
		while (current.Parent != null)
			current = current.Parent;

		// Find all headings that come before this step in document order
		var allBlocks = current.Descendants().ToList();
		var thisIndex = allBlocks.IndexOf(this);

		if (thisIndex == -1)
			return 3; // Default fallback

		// Look backwards for the most recent heading
		for (var i = thisIndex - 1; i >= 0; i--)
		{
			if (allBlocks[i] is HeadingBlock heading)
			{
				// Step should be one level deeper than the preceding heading
				return Math.Min(heading.Level + 1, 6); // Cap at h6
			}
		}

		// No preceding heading found, default to h2 (level 2)
		return 2;
	}
}
