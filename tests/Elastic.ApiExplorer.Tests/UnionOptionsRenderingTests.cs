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
	public async Task Render_TwoVariants_ListsThemOneAfterTheOther()
	{
		var html = await Render(2);

		html.Should().NotContain("data-chip-row").And.NotContain("union-variant-chips").And.Contain("union-separator");
		html.Should().NotContain("hidden=", "both variants stay visible");
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
	public async Task Render_ElevenVariants_AddsAFilterToTheMenu()
	{
		var html = await Render(11);

		html.Should().Contain("""<input type="search" class="api-example-menu-filter" data-chip-filter placeholder="Filter variants" """);
		html.Should().Contain("""id="opts-menu" class="api-example-menu simple-scrollbar" popover="auto" data-chip-menu>""");
	}

	private static async Task<string> Render(int count)
	{
		var variants = new ApiUnionVariants { Variants = Enumerable.Range(0, count).Select(Variant).ToArray(), ContainerId = "opts" };
		var slice = _UnionOptions.Create(variants);
		var html = await slice.RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);
		// The chip buttons spread their attributes over several lines.
		return System.Text.RegularExpressions.Regex.Replace(html, @"\s+", " ");
	}

	private static ApiUnionVariant Variant(int i) =>
		new()
		{
			DisplayName = $"Variant{i}",
			IsArrayVariant = i == 2,
			IsObjectType = true,
			AnchorId = $"v-{i}",
			ShowProperties = false,
			IsCollapsible = false,
			DefaultExpanded = false,
			NestedCount = 0,
			UseHidden = false,
			DiscriminatorLabel = i == 2 ? "kind: two" : null
		};
}
