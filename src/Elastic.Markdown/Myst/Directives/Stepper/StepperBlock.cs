// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Helpers;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst.Directives.Stepper;

public class StepperBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "stepper";

	public override void FinalizeAndValidate(ParserContext context)
	{
		// Calculate the heading level once for the whole stepper and push it to every child
		// step. All steps share the same preceding-heading context, so there is no need for
		// each StepBlock to walk the document independently.
		var stepLevel = CalculatePrecedingHeadingLevel();
		foreach (var step in this.OfType<StepBlock>())
		{
			step.HeadingLevel = stepLevel;
			AdjustInternalHeadings(step, stepLevel);
		}
	}

	// Headings inside a step must be subordinate to the step's own rendered level.
	// If the author wrote a heading at the same level or higher, adjust it and emit a hint
	// so they know what level to use in the source.
	private void AdjustInternalHeadings(StepBlock step, int stepLevel)
	{
		var adjusted = System.Math.Min(stepLevel + 1, 6);
		foreach (var heading in step.Descendants<HeadingBlock>())
		{
			// Headings inside a nested StepBlock are handled by that stepper's own
			// FinalizeAndValidate — skip them here to avoid double-adjustment.
			if (IsInsideNestedStep(heading, step))
				continue;

			if (heading.Level > stepLevel)
				continue;

			if (SkipValidation)
			{
				heading.Level = adjusted;
				continue;
			}

			var hashes = new string('#', adjusted);
			Build.Collector.Write(new Diagnostic
			{
				Severity = Severity.Hint,
				File = CurrentFile.FullName,
				Line = heading.Line + 1,
				Column = heading.Column,
				Length = heading.Level,
				Message = $"Heading level h{heading.Level} inside a step renders at the same or higher level as the step itself (h{stepLevel}). " +
						  $"It has been adjusted to h{adjusted} — write it as '{hashes}' to avoid this hint."
			});
			heading.Level = adjusted;
		}
	}

	private static bool IsInsideNestedStep(Block block, StepBlock outerStep)
	{
		var parent = block.Parent;
		while (parent != null && parent != outerStep)
		{
			if (parent is StepBlock)
				return true;
			parent = parent.Parent;
		}
		return false;
	}

	private int CalculatePrecedingHeadingLevel()
	{
		// Walk up to the document root so we can search the full flat block list.
		var root = (ContainerBlock)this;
		while (root.Parent != null)
			root = root.Parent;

		// Descendants() is pre-order: every block appears before its own children, so the
		// stepper's index is always before any headings that live inside it. Looking backward
		// from that index finds only document-level predecessors — no filtering needed.
		var allBlocks = root.Descendants().ToList();
		var stepperIndex = allBlocks.IndexOf(this);

		if (stepperIndex == -1)
			return 2;

		for (var i = stepperIndex - 1; i >= 0; i--)
		{
			if (allBlocks[i] is HeadingBlock heading)
				return System.Math.Min(heading.Level + 1, 6); // Cap at h6
		}

		return 2; // No preceding heading — default to h2
	}
}

public class StepBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context), IBlockTitle
{
	public override string Directive => "step";
	public string Title { get; private set; } = string.Empty;
	public string Anchor { get; private set; } = string.Empty;
	public int HeadingLevel { get; internal set; } = 2; // Set by parent StepperBlock.FinalizeAndValidate

	public override void FinalizeAndValidate(ParserContext context)
	{
		Title = Arguments ?? string.Empty;

		Anchor = Prop("anchor") ?? Title.Slugify();

		// Set CrossReferenceName so this step can be found by ToC generation
		if (!string.IsNullOrEmpty(Title))
			CrossReferenceName = Anchor;
	}
}
