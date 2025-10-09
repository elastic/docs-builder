// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.AppliesTo;
using Elastic.Markdown.Helpers;

namespace Elastic.Markdown.Myst.Directives.Admonition;

public class DropdownBlock(DirectiveBlockParser parser, ParserContext context) : AdmonitionBlock(parser, "dropdown", context);

public class AdmonitionBlock : DirectiveBlock, IBlockTitle, IBlockAppliesTo
{
	public AdmonitionBlock(DirectiveBlockParser parser, string admonition, ParserContext context) : base(parser, context)
	{
		Admonition = admonition;
		if (Admonition is "admonition")
			Classes = "plain";

		var t = Admonition;
		var title = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(t);
		Title = title;
	}

	public string Admonition { get; }

	public override string Directive => Admonition;

	public string? Classes { get; protected set; }

	public bool? DropdownOpen { get; private set; }

	public string Title { get; private set; }

	public string? AppliesToDefinition { get; private set; }

	public ApplicableTo? AppliesTo { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		CrossReferenceName = Prop("name");
		DropdownOpen = TryPropBool("open");
		if (DropdownOpen.HasValue)
			Classes = "dropdown";

		// Parse applies_to property if present
		AppliesToDefinition = Prop("applies_to");
		if (!string.IsNullOrEmpty(AppliesToDefinition))
			AppliesTo = ParseApplicableTo(AppliesToDefinition);

		if (Admonition is "admonition" or "dropdown" && !string.IsNullOrEmpty(Arguments))
			Title = Arguments;
		else if (!string.IsNullOrEmpty(Arguments))
			Title += $" {Arguments}";
		Title = Title.ReplaceSubstitutions(context);

		// Auto-generate CrossReferenceName for dropdowns without explicit name, same as headings
		if (string.IsNullOrEmpty(CrossReferenceName) && (Admonition == "dropdown" || Classes == "dropdown"))
			CrossReferenceName = Title.Slugify();
	}

	private ApplicableTo? ParseApplicableTo(string yaml)
	{
		try
		{
			var applicableTo = YamlSerialization.Deserialize<ApplicableTo>(yaml, Build.ProductsConfiguration);
			return applicableTo;
		}
		catch
		{
			// If parsing fails, return null
			// Note: Error handling is done in the YamlSerialization.Deserialize method
			return null;
		}
	}
}
