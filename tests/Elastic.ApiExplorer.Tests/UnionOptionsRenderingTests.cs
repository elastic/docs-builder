// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Components.PropertyTree._Partials;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

/// <summary>How a list of union variants renders: one after the other, or behind a row of chips from three variants.</summary>
public class UnionOptionsRenderingTests
{
	[Test]
	public async Task Render_OneObjectAndAPrimitive_ShowChips()
	{
		var html = await Render(2, objects: 1);

		html
			.Should()
			.Contain("data-chip-row")
			.And
			.Contain("""id="v-1" hidden="until-found">""", "TotalHits or number reads like any other union");
		html.Should().NotContain(">Variant1</span></code>", "the primitive's chip already names its type");
	}

	[Test]
	public async Task Render_OneVariant_ShowsItWithoutChips()
	{
		var html = await Render(1);

		html.Should().NotContain("data-chip-row").And.NotContain("union-variant-chips").And.NotContain("hidden=");
	}

	[Test]
	public async Task Render_TwoObjects_ShowChips()
	{
		var html = await Render(2);

		html.Should().Contain("data-chip-row").And.Contain("""id="v-1" hidden="until-found">""");
	}

	[Test]
	public async Task Render_PrimitiveVariantBehindChips_LeavesItsPanelWithoutTheTypeName()
	{
		// Variant2 is an array, which keeps its heading; Variant3 is a plain primitive.
		var html = await Render(4, objects: 3);

		html.Should().NotContain(">Variant3</span></code>", "a primitive's type is on its chip");
		html.Should().Contain(
			">Variant2</span><span class=\"type-wrapper array-icon\">[]</span></code>",
			"an array variant keeps its heading"
		);
		html.Should().NotContain(">Variant0</span></code>", "an object variant's name is on its chip");
	}

	[Test]
	public async Task Render_ThreeVariants_ShowsChipsAndOnlyTheFirstVariant()
	{
		var html = await Render(3);

		html.Should().Contain("data-chip-row").And.NotContain("union-separator");
		html.Should().Contain(
			"""<button type="button" role="tab" class="api-example-chip is-active" data-chip="v-0" aria-controls="v-0" aria-selected="true">"""
		);
		html.Should().Contain("""id="v-1" hidden="until-found">""", "other variants stay findable");
		html.Should().Contain(
			"""data-chip="v-1" aria-controls="v-1" aria-selected="false" tabindex="-1">""",
			"only the picked chip is in the tab order"
		);
		html.Should().NotContain("data-chip-filter", "three variants need no search box");
		html.Should().Contain("Variant2[] &#xB7; kind: two", "a menu entry names the array form and the discriminator value");
	}

	[Test]
	public async Task Render_ThreeVariants_ShowsTheFieldsAtOnceWithAToggleAndWithoutRepeatingTheName()
	{
		var html = await Render(3, foldable: true);

		html.Should().Contain("hide properties", "the fields show at once, and a toggle offers a shorter view");
		html.Should().NotContain("show properties", "nothing starts folded");
		html.Should().Contain("""<div class="nested-properties" id="v-0--children">""", "the fields are not folded");
		html.Should().NotContain(">Variant0</span></code>", "the chip already names the variant");
		html.Should().Contain(
			"""<div class="union-variant-label"> <code class="discriminator-value">kind: two</code> </div>""".Replace(
				" <code class=\"discriminator",
				" <span class=\"type-wrapper object-icon\">{}</span> <code class=\"schema-type\"><span class=\"type-object\">Variant2</span><span class=\"type-wrapper array-icon\">[]</span></code> <code class=\"discriminator"
			),
			"an array variant keeps its X[] heading, so its fields read as each item's"
		);
		html.Should().NotContain(">Variant1</span></code>", "a plain variant's name is on its chip");

		var alone = await Render(1, foldable: true);
		alone.Should().Contain("show properties", "a lone variant, with no chip to pick it, still folds its fields");
		alone.Should().Contain(">Variant0</span></code>");
	}

	[Test]
	public async Task Render_ElevenVariants_AddsASearchBoxToTheChipRow()
	{
		var html = await Render(11);

		html.Should().Contain("""<input type="search" class="union-variant-search" data-chip-filter placeholder="Search variants" """);
		html
			.IndexOf("data-chip-filter", StringComparison.Ordinal)
			.Should()
			.BeLessThan(
				html.IndexOf("data-chip-menu", StringComparison.Ordinal),
				"the search box sits in the row, outside the popover menu"
			);
		html.Should().Contain(
			"""id="opts-menu" class="api-example-menu simple-scrollbar" popover="manual" data-chip-menu>""",
			"the search box sits outside the popover, so the menu is closed by script and not by light dismiss"
		);
	}

	[Test]
	public async Task Render_SharedNameSuffix_IsTrimmedFromChipsAndMenuEntries()
	{
		string[] names = ["bedrock_config", "email_config", "jira_config"];
		var variants = new ApiUnionVariants
		{
			Variants = names.Select(
				(name, i) => Variant(i, foldable: false) with { DisplayName = name, IsArrayVariant = false, DiscriminatorLabel = null }
			).ToArray(),
			ContainerId = "opts"
		};
		var html = await _UnionOptions.Create(variants).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);
		html = string.Join(' ', html.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

		html.Should().Contain("""title="bedrock_config"> <span class="api-example-chip-title" data-chip-title>bedrock</span>""");
		html.Should().Contain(
			"""title="bedrock_config" hidden> <span class="api-example-menu-item-title" data-chip-item-title>bedrock</span>""",
			"the menu reads like the chips"
		);
		html.Should().NotContain(">bedrock_config<", "the full name stays only as the tooltip");
	}

	/// <summary>Renders <paramref name="count"/> variants; the first <paramref name="objects"/> are objects, the rest primitives.</summary>
	private static async Task<string> Render(int count, bool foldable = false, int? objects = null)
	{
		var variants = new ApiUnionVariants
		{
			Variants = Enumerable
				.Range(0, count)
				.Select(i => Variant(i, foldable) with { IsObjectType = i < (objects ?? count) })
				.ToArray(),
			ContainerId = "opts"
		};
		var slice = _UnionOptions.Create(variants);
		var html = await slice.RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);
		// The chip buttons spread their attributes over several lines.
		return string.Join(' ', html.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
	}

	private static ApiUnionVariant Variant(int i, bool foldable) =>
		new()
		{
			DisplayName = $"Variant{i}",
			IsArrayVariant = i == 2,
			IsObjectType = true,
			AnchorId = $"v-{i}",
			ShowProperties = foldable,
			IsCollapsible = foldable,
			DefaultExpanded = false,
			NestedCount = foldable ? 3 : 0,
			UseHidden = foldable,
			Properties = foldable ? new ApiPropertyList([]) : null,
			DiscriminatorLabel = i == 2 ? "kind: two" : null
		};
}
