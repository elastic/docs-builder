// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Authoring.Tests.Blocks;

// DirectiveBlockParser.TryContinue stops an ancestor directive consuming an option line once
// a nested directive is still open (including when a list sits between them). Without the
// guard the ancestor also takes every descendant's options, and the last one wins.
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
	// Each sync reaches the item that declared it, rather than all landing on the last one.
	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<div class="tabs">
			<input class="tabs-input" checked="checked" id="tabs-item-1-0" name="tabs-set-1" type="radio" tabindex="0">
			<label class="tabs-label" data-sync-id="local" data-sync-group="install-method" for="tabs-item-1-0">Local</label>
			<div class="tabs-content" tabindex="0">
				<p>local body</p>
			</div>
			<input class="tabs-input" id="tabs-item-1-1" name="tabs-set-1" type="radio" tabindex="0">
			<label class="tabs-label" data-sync-id="container" data-sync-group="install-method" for="tabs-item-1-1">Container</label>
			<div class="tabs-content" tabindex="0">
				<p>container body</p>
			</div>
		</div>
		"""
		);

	[Test, DisplayName("has no errors")]
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

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<div class="applies-switch">
			<input class="applies-switch-input" checked="checked" id="applies-switch-item-1-0" name="applies-switch-set-1" type="radio" tabindex="0">
			<label class="applies-switch-label" data-sync-id="serverless" data-sync-group="deployment" for="applies-switch-item-1-0">
				<span class="applies applies-item">
					<applies-to-popover badge-key="Serverless" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="" show-popover="false" is-inline="true"></applies-to-popover>
				</span>
			</label>
			<div class="applies-switch-content" tabindex="0">
				<p>serverless body</p>
			</div>
			<input class="applies-switch-input" id="applies-switch-item-1-1" name="applies-switch-set-1" type="radio" tabindex="0">
			<label class="applies-switch-label" data-sync-id="self-managed" data-sync-group="deployment" for="applies-switch-item-1-1">
				<span class="applies applies-item">
					<applies-to-popover badge-key="Stack" badge-lifecycle-text="Planned" lifecycle-class="ga" lifecycle-name="GA" show-lifecycle-name="false" show-version="false" has-multiple-lifecycles="false" popover-data="" show-popover="false" is-inline="true"></applies-to-popover>
				</span>
			</label>
			<div class="applies-switch-content" tabindex="0">
				<p>self-managed body</p>
			</div>
		</div>
		"""
		);

	[Test, DisplayName("has no errors")]
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

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<div class="stepper">
			<ol>
				<li class="step">
					<h2 id="install-step">
						<a href="#install-step">Install</a>
					</h2>
					<p>Install the thing.</p>
				</li>
				<li class="step">
					<h2 id="configure-step">
						<a href="#configure-step">Configure</a>
					</h2>
					<p>Configure the thing.</p>
				</li>
			</ol>
		</div>
		"""
		);

	[Test, DisplayName("has no errors")]
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

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<details class="dropdown dropdown" open="open">
			<summary class="dropdown-title">
				<div class="dropdown-title__container">
					<span class="dropdown-title__summary-text">Outer summary</span>
				</div>
				<div class="dropdown-title__icon">
					<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-4 mr-1 shrink -rotate-90 group-has-checked/label:rotate-0 cursor-pointer text-ink">
						<path stroke-linecap="round" stroke-linejoin="round" d="m19.5 8.25-7.5 7.5-7.5-7.5"></path>
					</svg>
				</div>
			</summary>
			<div class="dropdown-content">
				<div class="admonition note" id="inner-note">
					<div class="admonition-header">
						<span class="admonition-title">Note</span>
					</div>
					<div class="admonition-content">
						<p>Inner content.</p>
					</div>
				</div>
			</div>
		</details>
		"""
		);

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}
