// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Extensions;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Helpers;
using Elastic.Markdown.Myst.CodeBlocks;
using Elastic.Markdown.Myst.Comments;
using Elastic.Markdown.Myst.Directives.AppliesTo;
using Elastic.Markdown.Myst.Directives.Contributors;

namespace Elastic.Markdown.Myst.Directives.AppliesSwitch;

public class AppliesSwitchBlock(DirectiveBlockParser parser, ParserContext context)
	: DirectiveBlock(parser, context)
{
	public override string Directive => "applies-switch";

	public int Index { get; set; }
	public bool IsDropdown { get; private set; }
	public string GetGroupKey() => Prop("group") ?? "applies-switches";

	public override void FinalizeAndValidate(ParserContext context)
	{
		Index = FindIndex();
		IsDropdown = ParseAppearance() && ValidateDropdownItems();
		if (this.OfType<AppliesItemBlock>().Count(i => i.Selected) > 1)
			this.EmitWarning("{applies-switch} has multiple items marked :selected:, only the first one is selected.");
	}

	private bool ParseAppearance()
	{
		var appearance = Prop("appearance");
		switch (appearance)
		{
			case null or "" or "tabs":
				return false;
			case "dropdown":
				return true;
			default:
				this.EmitWarning($"{{applies-switch}} appearance '{appearance}' is not supported. Valid appearances are: tabs, dropdown. Defaulting to 'tabs'.");
				return false;
		}
	}

	/// The dropdown appearance attaches the selector chip to the top edge of a
	/// code block; other leading content has no edge to attach to and renders
	/// poorly, so it falls back to tabs with a warning.
	private bool ValidateDropdownItems()
	{
		foreach (var item in this.OfType<AppliesItemBlock>())
		{
			var first = item.FirstOrDefault(c => c is not CommentBlock);
			if (first is EnhancedCodeBlock { Language: not "mermaid" } and not AppliesToDirective and not ContributorsBlock)
				continue;

			this.EmitWarning(
				$"{{applies-switch}} dropdown appearance requires every {{applies-item}} to start with a code block. " +
				$"Item '{item.AppliesToDefinition}' does not, falling back to tabs.");
			return false;
		}

		return true;
	}

	private int _index = -1;

	public int FindIndex()
	{
		if (_index > -1)
			return _index;

		_index = GetUniqueLineIndex();
		return _index;
	}
}

public class AppliesItemBlock(DirectiveBlockParser parser, ParserContext context)
	: DirectiveBlock(parser, context), IBlockTitle, IBlockAppliesTo
{
	public override string Directive => "applies-item";

	public string? AppliesToDefinition { get; private set; }
	public ApplicableTo? AppliesTo { get; private set; }
	public string Title => AppliesToDefinition ?? string.Empty; // IBlockTitle implementation
	public int Index { get; private set; }
	public int AppliesSwitchIndex { get; private set; }
	public string? AppliesSwitchGroupKey { get; private set; }
	public string? SyncKey { get; private set; }
	public bool Selected { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		if (string.IsNullOrWhiteSpace(Arguments))
			this.EmitError("{applies-item} requires an argument with applies_to definition.");

		AppliesToDefinition = (Arguments ?? "{undefined}").ReplaceSubstitutions(context);
		Index = Parent!.OfType<AppliesItemBlock>().ToList().IndexOf(this);

		var appliesSwitch = Parent as AppliesSwitchBlock;

		AppliesSwitchIndex = appliesSwitch?.FindIndex() ?? -1;
		AppliesSwitchGroupKey = appliesSwitch?.GetGroupKey();

		// Auto-generate sync key from applies_to definition if not provided
		SyncKey = Prop("sync") ?? GenerateSyncKey(AppliesToDefinition, Build.ProductsConfiguration);
		Selected = PropBool("selected");

		// Parse the ApplicableTo object for IBlockAppliesTo
		if (!string.IsNullOrEmpty(AppliesToDefinition))
			AppliesTo = ParseApplicableTo(AppliesToDefinition);
	}

	private ApplicableTo? ParseApplicableTo(string yaml)
	{
		try
		{
			var applicableTo = YamlSerialization.Deserialize<ApplicableTo>(yaml, Build.ProductsConfiguration);
			return applicableTo;
		}
		catch (FormatException e)
		{
			this.EmitError($"Unable to parse applies_to definition: {yaml}", e);
			return null;
		}
		catch (InvalidOperationException e)
		{
			this.EmitError($"Unable to parse applies_to definition: {yaml}", e);
			return null;
		}
	}

	public static string GenerateSyncKey(string appliesToDefinition, ProductsConfiguration productsConfiguration)
	{
		var applicableTo = YamlSerialization.Deserialize<ApplicableTo>(appliesToDefinition, productsConfiguration);
		// Use ShortId.Create for a stable, deterministic hash based on the normalized ToString()
		// ToString() normalizes different YAML representations into a canonical form,
		// ensuring semantically equivalent definitions get the same sync key
		return $"applies-{ShortId.Create(applicableTo.ToString())}";
	}
}
