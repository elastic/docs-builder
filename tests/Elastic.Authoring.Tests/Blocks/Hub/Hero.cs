// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Authoring.Tests.Blocks.Hub.Hero;

public class HeroWithTitleOnly : MarkdownTest
{
	protected override string Markdown => """
		:::{hero}
		:title: Elasticsearch documentation hub
		:::
		""";

	[Fact(DisplayName = "renders the title as an h1")]
	public async Task RendersTitleAsH1() => await Docs.ConvertsToContainingHtml("""<h1>Elasticsearch documentation hub</h1>""");

	[Fact(DisplayName = "renders the fixed eyebrow")]
	public async Task RendersFixedEyebrow() => await Docs.ConvertsToContainingHtml("""<span>Browse all Elastic docs</span>""");

	[Fact(DisplayName = "has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class HeroWithoutATitle : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{hero}
		:description: Missing the required title option.
		:::
		""";

	[Fact(DisplayName = "errors")]
	public async Task Errors() => await Docs.HasError("{hero} requires a `:title:` option.");
}

public class HeroWithADescription : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{hero}
		:title: Kibana documentation hub
		:description: The UI for the **Elasticsearch** platform.
		:::
		""";

	[Fact(DisplayName = "renders inline markup in the description")]
	public async Task RendersInlineMarkup() => await Docs.ConvertsToContainingHtml("""<strong>Elasticsearch</strong>""");

	[Fact(DisplayName = "has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class HeroWithAnUnknownIconKey : MarkdownTest
{
	protected override string Markdown => """
		:::{hero}
		:icon: notaproduct
		:title: Something else
		:::
		""";

	[Fact(DisplayName = "falls back to a letter chip")]
	public async Task FallsBackToLetterChip() =>
		await Docs.ConvertsToContainingHtml("""<span class="hub-hero-icon" aria-hidden="true">N</span>""");

	[Fact(DisplayName = "has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class HeroWithAnchorActions : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{hero}
		:title: Elasticsearch documentation hub
		:primary-action: [Get started](#get-started)
		:secondary-action: [What's new](#whats-new)
		:::
		""";

	// Actions render as buttons, and no button on the site carries an arrow, not even
	// an anchor action that jumps within the page. The assertion starts at the section,
	// because the pretty-printer only matches from the outermost element of the output.
	[Fact(DisplayName = "renders both actions as buttons without an arrow")]
	public async Task RendersBothActionsAsButtons() =>
		await Docs.ConvertsToContainingHtml(
			"""
		<section class="hub-hero">
			<div class="hub-hero-inner">
				<div class="hub-hero-eyebrow">
					<a class="hub-hero-eyebrow-link" href="/">
						<span>Browse all Elastic docs</span>
						<svg class="hub-arrow" viewBox="0 0 24 24" fill="none" stroke-width="1.5" stroke="currentColor" aria-hidden="true">
							<path stroke-linecap="round" stroke-linejoin="round" d="M17.25 8.25 21 12m0 0-3.75 3.75M21 12H3"></path>
						</svg>
					</a>
				</div>
				<div class="hub-hero-top">
					<h1>Elasticsearch documentation hub</h1>
				</div>
				<div class="hub-hero-actions doc-button-group">
					<span class="doc-button-item doc-button-neutral">
						<a class="hub-hero-action" href="#get-started">
							Get started
						</a>
					</span>
					<span class="doc-button-item doc-button-neutral">
						<a class="hub-hero-action" href="#whats-new">
							What's new
						</a>
					</span>
				</div>
			</div>
		</section>
		"""
		);

	[Fact(DisplayName = "has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class HeroWithAnExternalAction : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{hero}
		:title: Elasticsearch documentation hub
		:primary-action: [Install Elasticsearch](https://www.elastic.co/downloads/elasticsearch)
		:::
		""";

	// External links follow the same rules as inline links: they open in a new tab. The
	// assertion starts at the section, because the pretty-printer only matches from the
	// outermost element of the directive output. It also strips `preload`, so the absence
	// of preloading on an external action cannot be asserted here.
	[Fact(DisplayName = "opens in a new tab")]
	public async Task OpensInANewTab() =>
		await Docs.ConvertsToContainingHtml(
			"""
		<section class="hub-hero">
			<div class="hub-hero-inner">
				<div class="hub-hero-eyebrow">
					<a class="hub-hero-eyebrow-link" href="/">
						<span>Browse all Elastic docs</span>
						<svg class="hub-arrow" viewBox="0 0 24 24" fill="none" stroke-width="1.5" stroke="currentColor" aria-hidden="true">
							<path stroke-linecap="round" stroke-linejoin="round" d="M17.25 8.25 21 12m0 0-3.75 3.75M21 12H3"></path>
						</svg>
					</a>
				</div>
				<div class="hub-hero-top">
					<h1>Elasticsearch documentation hub</h1>
				</div>
				<div class="hub-hero-actions doc-button-group">
					<span class="doc-button-item doc-button-neutral">
						<a class="hub-hero-action" href="https://www.elastic.co/downloads/elasticsearch" target="_blank" rel="noopener noreferrer">
							Install Elasticsearch
						</a>
					</span>
				</div>
			</div>
		</section>
		"""
		);

	[Fact(DisplayName = "has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class HeroWithAnInternalAction : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{hero}
		:title: Elasticsearch documentation hub
		:primary-action: [Syntax reference](/index.md)
		:::
		""";

	[Fact(DisplayName = "strips the markdown extension and does not open a new tab")]
	public async Task StripsExtensionAndNoNewTab() =>
		await Docs.ConvertsToContainingHtml(
			"""
		<section class="hub-hero">
			<div class="hub-hero-inner">
				<div class="hub-hero-eyebrow">
					<a class="hub-hero-eyebrow-link" href="/">
						<span>Browse all Elastic docs</span>
						<svg class="hub-arrow" viewBox="0 0 24 24" fill="none" stroke-width="1.5" stroke="currentColor" aria-hidden="true">
							<path stroke-linecap="round" stroke-linejoin="round" d="M17.25 8.25 21 12m0 0-3.75 3.75M21 12H3"></path>
						</svg>
					</a>
				</div>
				<div class="hub-hero-top">
					<h1>Elasticsearch documentation hub</h1>
				</div>
				<div class="hub-hero-actions doc-button-group">
					<span class="doc-button-item doc-button-neutral">
						<a class="hub-hero-action" href="/">
							Syntax reference
						</a>
					</span>
				</div>
			</div>
		</section>
		"""
		);

	[Fact(DisplayName = "has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class HeroWithARelativeActionUrl : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{hero}
		:title: Elasticsearch documentation hub
		:primary-action: [Get started](get-started.md)
		:::
		""";

	[Fact(DisplayName = "rejects a relative path")]
	public async Task RejectsRelativePath() => await Docs.HasError("must be an absolute path starting with `/`");
}

public class HeroWithAMalformedAction : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{hero}
		:title: Elasticsearch documentation hub
		:primary-action: Get started
		:::
		""";

	[Fact(DisplayName = "errors")]
	public async Task Errors() => await Docs.HasError("must be a markdown link");
}

public class HeroWithACrossLinkAction : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{hero}
		:title: docs-builder documentation hub
		:primary-action: [Elastic documentation](docs-content://get-started/index.md)
		:::
		""";

	// A cross-link resolves to a full URL but still points at documentation this site serves,
	// so it must not open in a new tab. Inline links make the same distinction. The assertion
	// snapshots the section rather than looking for target="_blank" anywhere on the page,
	// because site chrome carries that attribute too.
	[Fact(DisplayName = "does not open in a new tab")]
	public async Task DoesNotOpenInNewTab() =>
		await Docs.ConvertsToContainingHtml(
			"""
		<section class="hub-hero">
			<div class="hub-hero-inner">
				<div class="hub-hero-eyebrow">
					<a class="hub-hero-eyebrow-link" href="/">
						<span>Browse all Elastic docs</span>
						<svg class="hub-arrow" viewBox="0 0 24 24" fill="none" stroke-width="1.5" stroke="currentColor" aria-hidden="true">
							<path stroke-linecap="round" stroke-linejoin="round" d="M17.25 8.25 21 12m0 0-3.75 3.75M21 12H3"></path>
						</svg>
					</a>
				</div>
				<div class="hub-hero-top">
					<h1>docs-builder documentation hub</h1>
				</div>
				<div class="hub-hero-actions doc-button-group">
					<span class="doc-button-item doc-button-neutral">
						<a class="hub-hero-action" href="https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main/get-started">
							Elastic documentation
						</a>
					</span>
				</div>
			</div>
		</section>
		"""
		);

	[Fact(DisplayName = "has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

// These two set frontmatter, so they use DocumentTest. Setup.Markdown prepends an H1,
// which would push the frontmatter into the body where it never parses.
public class HubLayoutWithoutAHero : DocumentTest
{
	protected override string Document =>
		"""
		---
		layout: hub
		---

		Body content with no hero directive.
		""";

	// The hub layout removes the page H1, so {hero} is the only thing that can title the page.
	[Fact(DisplayName = "errors")]
	public async Task Errors() => await Docs.HasError("A page with `layout: hub` requires a {hero} directive.");
}

public class HubLayoutWithAHero : DocumentTest
{
	protected override string Document =>
		"""
		---
		layout: hub
		---

		:::{hero}
		:title: Elasticsearch documentation hub
		:::
		""";

	[Fact(DisplayName = "has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}
