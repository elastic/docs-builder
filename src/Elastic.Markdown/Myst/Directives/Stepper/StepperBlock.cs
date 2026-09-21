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

	/// <summary>
	/// When <see langword="false"/>, step titles stay in the procedure but are not HTML headings
	/// and are omitted from the page table of contents. Defaults to <see langword="true"/>.
	/// </summary>
	public bool IncludeInToc { get; private set; } = true;

	public override void FinalizeAndValidate(ParserContext context)
	{
		// `:toc: false` opts out. An absent property keeps the historical default (in the ToC).
		IncludeInToc = TryPropBool("toc") ?? true;

		// Calculate the heading level once for the whole stepper and push it to every child
		// step. All steps share the same preceding-heading context, so there is no need for
		// each StepBlock to walk the document independently.
		var precedingLevel = FindPrecedingHeadingLevel();
		var stepLevel = precedingLevel == 0 ? 2 : System.Math.Min(precedingLevel + 1, 6);
		// Steps that render as headings occupy stepLevel. Steps that do not are absent from
		// the outline, so internal headings are subordinate to the preceding real heading.
		var outlineLevel = IncludeInToc ? stepLevel : (precedingLevel == 0 ? 1 : precedingLevel);
		foreach (var step in this.OfType<StepBlock>())
		{
			step.HeadingLevel = stepLevel;
			step.RenderAsHeading = IncludeInToc;
			AdjustInternalHeadings(step, outlineLevel);
		}
	}

	// Headings inside a step must be subordinate to the outline level.
	// When the step renders as a heading, that level is the step itself.
	// When it does not, that level is the preceding heading in the document.
	// If the author wrote a heading at that level or higher, adjust it and emit a hint
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
			var message = step.RenderAsHeading
				? $"Heading level h{heading.Level} inside a step renders at the same or higher level as the step itself (h{stepLevel}). "
					+ $"It has been adjusted to h{adjusted} — write it as '{hashes}' to avoid this hint."
				: $"Heading level h{heading.Level} inside a step renders at the same or higher level as the preceding heading (h{stepLevel}). "
					+ $"It has been adjusted to h{adjusted}. Write it as '{hashes}' to avoid this hint.";
			Build.Collector.Write(new Diagnostic
			{
				Severity = Severity.Hint,
				File = CurrentFile.FullName,
				Line = heading.Line + 1,
				Column = heading.Column,
				Length = heading.Level,
				Message = message
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

	// Returns the level of the nearest preceding heading, or 0 when the stepper has none.
	// Callers add one for the step's own level and cap at h6.
	private int FindPrecedingHeadingLevel()
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
			return 0;

		for (var i = stepperIndex - 1; i >= 0; i--)
		{
			if (allBlocks[i] is HeadingBlock heading)
				return heading.Level;
		}

		return 0;
	}
}

public class StepBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context), IBlockTitle
{
	public override string Directive => "step";
	public string Title { get; private set; } = string.Empty;
	public string Anchor { get; private set; } = string.Empty;
	public int HeadingLevel { get; internal set; } = 2; // Set by parent StepperBlock.FinalizeAndValidate

	/// <summary>
	/// When <see langword="false"/>, the title renders as a non-heading element and is omitted
	/// from the page table of contents. The anchor is preserved.
	/// </summary>
	public bool RenderAsHeading { get; internal set; } = true;

	public override void FinalizeAndValidate(ParserContext context)
	{
		Title = Arguments ?? string.Empty;

		Anchor = Prop("anchor") ?? Title.Slugify();

		// Set CrossReferenceName so this step can be found by ToC generation
		if (!string.IsNullOrEmpty(Title))
			CrossReferenceName = Anchor;
	}
}
