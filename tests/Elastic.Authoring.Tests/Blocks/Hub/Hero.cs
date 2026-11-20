// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Authoring.Tests.Blocks.Hub;

public class HeroWithTitleOnly : MarkdownTest
{
	protected override string Markdown => """
		:::{hero}
		:title: Elasticsearch documentation hub
		:::
		""";

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<section class="hub-hero">
			<div class="hub-hero-inner">
				<div class="hub-hero-top">
					<h1>Elasticsearch documentation hub</h1>
				</div>
			</div>
		</section>
		"""
		);

	[Test, DisplayName("has no errors")]
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

	[Test, DisplayName("reports diagnostics")]
	public async Task ReportsDiagnostics() => await Docs.HasDiagnostics("error {repo}/docs/index.md:2 {hero} requires a `:title:` option.");
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

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<section class="hub-hero">
			<div class="hub-hero-inner">
				<div class="hub-hero-top">
					<h1>Kibana documentation hub</h1>
				</div>
				<p class="hub-hero-desc">The UI for the
					<strong>Elasticsearch</strong>
					platform.</p>
			</div>
		</section>
		"""
		);

	[Test, DisplayName("has no errors")]
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

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<section class="hub-hero">
			<div class="hub-hero-inner">
				<div class="hub-hero-top">
					<span class="hub-hero-icon" aria-hidden="true">N</span>
					<h1>Something else</h1>
				</div>
			</div>
		</section>
		"""
		);

	[Test, DisplayName("has no errors")]
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
	// an anchor action that jumps within the page.
	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<section class="hub-hero">
			<div class="hub-hero-inner">
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

	[Test, DisplayName("has no errors")]
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

	// External links follow the same rules as inline links: they open in a new tab. The snapshot
	// strips `preload`, so the absence of preloading on an external action cannot be asserted here.
	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<section class="hub-hero">
			<div class="hub-hero-inner">
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

	[Test, DisplayName("has no errors")]
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

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<section class="hub-hero">
			<div class="hub-hero-inner">
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

	[Test, DisplayName("has no errors")]
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

	[Test, DisplayName("reports diagnostics")]
	public async Task ReportsDiagnostics() =>
		await Docs.HasDiagnostics(
			"error {repo}/docs/index.md:2 Directive link `get-started.md` must be an absolute path starting with `/`, a cross-link scheme (for example `kibana://`), or an external URL."
		);
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

	[Test, DisplayName("reports diagnostics")]
	public async Task ReportsDiagnostics() =>
		await Docs.HasDiagnostics(
			"error {repo}/docs/index.md:2 {hero} action `Get started` must be a markdown link, for example `[Get started](#get-started)`."
		);
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
	// so it must not open in a new tab. Inline links make the same distinction. The snapshot covers
	// the section only, because site chrome carries target="_blank" too.
	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<section class="hub-hero">
			<div class="hub-hero-inner">
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

	[Test, DisplayName("has no errors")]
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
	[Test, DisplayName("reports diagnostics")]
	public async Task ReportsDiagnostics() =>
		await Docs.HasDiagnostics(
			"""
		error {repo}/docs/index.md A page with `layout: hub` requires a {hero} directive. Without it the page renders without a title.
		warning {repo}/docs/index.md Document has no title, using file name as title.
		"""
		);
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

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}
