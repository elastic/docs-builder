// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.StyleGuide;
using Elastic.Markdown.Components.Admonition;
using Elastic.Markdown.Components.AppliesBadge;
using Elastic.Markdown.Components.AppliesPopover;
using Elastic.Markdown.Components.ButtonGroup;
using Elastic.Markdown.Components.CodeBlock;
using Elastic.Markdown.Components.Disclosure;
using Elastic.Markdown.Components.Image;
using Elastic.Markdown.Components.Listing;
using Elastic.Markdown.Components.PageCard;
using Elastic.Markdown.Components.Prose;
using Elastic.Markdown.Components.Stepper;
using Elastic.Markdown.Components.Tabs;
using Elastic.Markdown.Components.VectorSizing;

namespace Elastic.Markdown.StyleGuide;

/// <summary>The style-guide sections of the components that render Markdown, which live in this project rather than in the site project.</summary>
public static class MarkdownComponentSections
{
	/// <summary>Adds the sections to the style guide's component pages. Safe to call more than once.</summary>
	public static void Register()
	{
		StyleGuideCatalog.Add(
			"content",
			AdmonitionExamples.Admonitions,
			DisclosureExamples.Disclosures,
			StepperExamples.Steppers,
			TabsExamples.Tabs,
			ButtonDirectiveExamples.Buttons,
			CodeBlockExamples.CodeBlocks,
			ProseExamples.Articles,
			VectorSizingExamples.Calculators,
			PageCardExamples.Cards,
			ImageExamples.Images,
			ListingExamples.Listings
		);
		StyleGuideCatalog.Add("badges", AppliesBadgeExamples.Badges, AppliesPopoverExamples.Popovers);
	}
}
