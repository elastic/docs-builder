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
		html.Should().Contain(">Variant1</span></code>", "the primitive keeps its type name");
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
	public async Task Render_PrimitiveVariantBehindChips_KeepsItsTypeName()
	{
		var html = await Render(3, objects: 2);

		html.Should().Contain(">Variant2</span></code>", "a primitive's type is all its panel has to show");
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
		html.Should().NotContain("data-chip-filter", "three variants fit the menu without a filter");
		html.Should().Contain("Variant2[] &#xB7; kind: two", "a menu entry names the array form and the discriminator value");
	}

	[Test]
	public async Task Render_ThreeVariants_ShowsTheFieldsAtOnceWithoutRepeatingTheName()
	{
		var html = await Render(3, foldable: true);

		html.Should().NotContain("show properties", "picking a chip is the choice to read that variant");
		html.Should().Contain("""<div class="nested-properties" id="v-0-children">""", "the fields are not folded");
		html.Should().NotContain(">Variant0</span></code>", "the chip already names the variant");
		html.Should().Contain(
			"""<div class="union-variant-label"> <code class="discriminator-value">kind: two</code> </div>""".Replace(
				" <code class=\"discriminator",
				" <span class=\"type-wrapper array-icon\">[]</span> <span class=\"type-wrapper object-icon\">{}</span> <code class=\"schema-type\"><span class=\"type-object\">Variant2</span></code> <code class=\"discriminator"
			),
			"an array variant keeps its [] heading, so its fields read as each item's"
		);
		html.Should().NotContain(">Variant1</span></code>", "a plain variant's name is on its chip");

		var alone = await Render(1, foldable: true);
		alone.Should().Contain("show properties", "a lone variant, with no chip to pick it, still folds its fields");
		alone.Should().Contain(">Variant0</span></code>");
	}

	[Test]
	public async Task Render_ElevenVariants_AddsAFilterToTheMenu()
	{
		var html = await Render(11);

		html.Should().Contain("""<input type="search" class="api-example-menu-filter" data-chip-filter placeholder="Filter variants" """);
		html.Should().Contain("""id="opts-menu" class="api-example-menu simple-scrollbar" popover="auto" data-chip-menu>""");
	}

	[Test]
	public async Task Render_SharedNameSuffix_IsTrimmedFromChipsAndMenuEntries()
	{
		var variants = new ApiUnionVariants
		{
			Variants = new[] { "bedrock_config", "email_config", "jira_config" }.Select(
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
