// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Extensions;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Helpers;

namespace Elastic.Markdown.Myst.Directives.AppliesSwitch;

public class AppliesSwitchBlock(DirectiveBlockParser parser, ParserContext context)
	: DirectiveBlock(parser, context)
{
	public override string Directive => "applies-switch";

	public int Index { get; set; }
	public string GetGroupKey() => Prop("group") ?? "applies-switches";

	public override void FinalizeAndValidate(ParserContext context)
	{
		Index = FindIndex();
		SortAppliesItems();
	}

	private int _index = -1;

	public int FindIndex()
	{
		if (_index > -1)
			return _index;

		_index = GetUniqueLineIndex();
		return _index;
	}

	private void SortAppliesItems()
	{
		// Get all applies-item children
		var items = this.OfType<AppliesItemBlock>().ToList();
		if (items.Count <= 1)
			return; // No need to sort if 0 or 1 items

		// Parse ApplicableTo for each item for sorting
		var itemsWithAppliesTo = items.Select(item =>
		{
			try
			{
				var applicableTo = YamlSerialization.Deserialize<ApplicableTo>(
					item.AppliesToDefinition,
					Build.ProductsConfiguration);
				return (Item: item, AppliesTo: (ApplicableTo?)applicableTo);
			}
			catch
			{
				// If parsing fails, keep original order for this item
				return (Item: item, AppliesTo: null);
			}
		}).ToList();

		// Create comparer
		var comparer = new ApplicableToOrderComparer();

		// Sort items based on their ApplicableTo, putting unparseable items at the end
		var sorted = itemsWithAppliesTo
			.OrderBy(x => x.AppliesTo is null ? 1 : 0) // Unparseable items last
			.ThenBy(x => x.AppliesTo, comparer)
			.ToList();

		// Remove all items from the block
		foreach (var item in items)
			_ = Remove(item);

		// Re-add items in sorted order
		foreach (var (item, _) in sorted)
			Add(item);

		// Update indices after sorting
		foreach (var item in items)
			item.UpdateIndex();
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

	// Called after sorting to update the index
	internal void UpdateIndex() => Index = Parent!.IndexOf(this);

	public static string GenerateSyncKey(string appliesToDefinition, ProductsConfiguration productsConfiguration)
	{
		var applicableTo = YamlSerialization.Deserialize<ApplicableTo>(appliesToDefinition, productsConfiguration);
		// Use ShortId.Create for a stable, deterministic hash based on the normalized ToString()
		// ToString() normalizes different YAML representations into a canonical form,
		// ensuring semantically equivalent definitions get the same sync key
		return $"applies-{ShortId.Create(applicableTo.ToString())}";
	}
}
