// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Diagnostics;
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

		if (Admonition is "dropdown")
			Title = Title.StripMarkdown();
	}

	private ApplicableTo? ParseApplicableTo(string yaml)
	{
		try
		{
			var applicableTo = YamlSerialization.Deserialize<ApplicableTo>(yaml, Build.Catalog.Products);
			// Admonitions and dropdowns never reported what the reader found, and `{ ece:, ess: }` reads as the key `ece:`, which would be an error
			// that fails strict builds of existing docs. Only the hints are reported until those blocks are cleaned up.
			foreach (var d in ApplicableToReadConstraints.Check(applicableTo).Where(d => d.Severity == Severity.Hint))
				this.Emit(d.Severity, d.Message, ruleId: d.RuleId);
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
