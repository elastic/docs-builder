// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Authoring.Tests.Blocks.NestedDirectiveOptions;

// DirectiveBlockParser.TryContinue stops an ancestor directive consuming an option line once
// it has opened a nested directive child. Without the guard the ancestor also takes every
// descendant's options, and the last one wins.
//
// No existing directive pair shares an option name, so the collision is latent for them:
// {tab-set} reads group while {tab-item} reads sync and selected, and {applies-switch} and
// {applies-item} split the same way. These tests pin that each option still reaches the block
// that declared it, which is what the guard must not break.

public class TabSetWithItsOwnGroupAndPerItemSync : MarkdownTest
{
	protected override string Markdown =>
		"""
		::::{tab-set}
		:group: install-method

		:::{tab-item} Local
		:sync: local
		local body
		:::

		:::{tab-item} Container
		:sync: container
		container body
		:::
		::::
		""";

	// The group is declared on the tab-set before any child, so it still reaches the tab-set.
	[Fact(DisplayName = "the tab set keeps its own group")]
	public async Task TabSetKeepsGroup() => await Docs.ConvertsToContainingRawHtml("data-sync-group=\"install-method\"");

	// Each sync reaches the item that declared it, rather than all landing on the last one.
	[Fact(DisplayName = "the first item keeps its own sync")]
	public async Task FirstItemKeepsSync() => await Docs.ConvertsToContainingRawHtml("data-sync-id=\"local\"");

	[Fact(DisplayName = "the second item keeps its own sync")]
	public async Task SecondItemKeepsSync() => await Docs.ConvertsToContainingRawHtml("data-sync-id=\"container\"");

	[Fact(DisplayName = "has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class AppliesSwitchWithItsOwnGroupAndPerItemSync : MarkdownTest
{
	protected override string Markdown =>
		"""
		::::{applies-switch}
		:group: deployment

		:::{applies-item} serverless: ga
		:sync: serverless
		serverless body
		:::

		:::{applies-item} stack: ga 9.0+
		:sync: self-managed
		self-managed body
		:::
		::::
		""";

	[Fact(DisplayName = "the switch keeps its own group")]
	public async Task SwitchKeepsGroup() => await Docs.ConvertsToContainingRawHtml("data-sync-group=\"deployment\"");

	[Fact(DisplayName = "the first item keeps its own sync")]
	public async Task FirstItemKeepsSync() => await Docs.ConvertsToContainingRawHtml("data-sync-id=\"serverless\"");

	[Fact(DisplayName = "the second item keeps its own sync")]
	public async Task SecondItemKeepsSync() => await Docs.ConvertsToContainingRawHtml("data-sync-id=\"self-managed\"");

	[Fact(DisplayName = "has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class StepperWithPerStepAnchors : MarkdownTest
{
	protected override string Markdown =>
		"""
		::::{stepper}
		:::{step} Install
		:anchor: install-step
		Install the thing.
		:::
		:::{step} Configure
		:anchor: configure-step
		Configure the thing.
		:::
		::::
		""";

	[Fact(DisplayName = "each step keeps its own anchor")]
	public async Task EachStepKeepsAnchor() => await Docs.ConvertsToContainingRawHtml("install-step");

	[Fact(DisplayName = "the second step keeps its own anchor")]
	public async Task SecondStepKeepsAnchor() => await Docs.ConvertsToContainingRawHtml("configure-step");

	[Fact(DisplayName = "has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class DropdownWrappingAnAdmonitionWithItsOwnName : MarkdownTest
{
	protected override string Markdown =>
		"""
		::::{dropdown} Outer summary
		:open:
		:::{note}
		:name: inner-note
		Inner content.
		:::
		::::
		""";

	[Fact(DisplayName = "the dropdown keeps its own open state")]
	public async Task DropdownKeepsOpenState() => await Docs.ConvertsToContainingRawHtml("Outer summary");

	[Fact(DisplayName = "the nested admonition keeps its own name")]
	public async Task NestedAdmonitionKeepsName() => await Docs.ConvertsToContainingRawHtml("inner-note");

	[Fact(DisplayName = "has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}
