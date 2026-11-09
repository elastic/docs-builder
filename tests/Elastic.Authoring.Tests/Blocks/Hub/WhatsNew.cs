// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Authoring.Tests.Blocks.Hub;

// The :product: path reads hub-whats-new.yml from the documentation set root. The authoring
// harness builds from a temporary set with no such file, so these cover the inline-body path
// and the error raised when a product key cannot be resolved.

public class WhatsNewWithAnInlineBody : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{whats-new}
		title: What's new in docs-builder
		id: whats-new
		intro: Recent additions to the toolchain.
		items:
		  - title: Hub pages
		    description: A product-scoped landing page.
		    link: /index.md
		    date: AUG 2026
		    tag: Syntax
		    featured: true
		  - title: Explore sections
		    description: Collapse a long link list.
		    link: /index.md
		    date: AUG 2026
		    tag: Syntax
		:::
		""";

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() => await Docs.ConvertsToHtmlSnapshot();

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class WhatsNewWithReleaseAndUpgradeLinks : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{whats-new}
		title: What's new
		release-links:
		  - label: View release notes
		    url: /index.md
		upgrade-link:
		  label: Upgrade
		  url: /index.md
		:::
		""";

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<section class="hub-whats-new">
			<header class="hub-wn-header">
				<div class="hub-wn-heading">
					<h2 class="hub-wn-title">What's new</h2>
				</div>
				<div class="hub-wn-rn">
					<span class="hub-wn-rn-label">Go to latest release notes:</span>
					<ul class="hub-wn-rn-list">
						<li>
							<a class="hub-wn-rn-link" href="/">View release notes</a>
						</li>
					</ul>
				</div>
			</header>
			<div class="hub-wn-footer">
				<span class="hub-wn-footer-text">Ready to move to the latest version?</span>
				<a class="hub-wn-upgrade" href="/">
					<span>Upgrade</span>
					<svg class="hub-arrow" viewBox="0 0 24 24" fill="none" stroke-width="1.5" stroke="currentColor" aria-hidden="true">
						<path stroke-linecap="round" stroke-linejoin="round" d="M17.25 8.25 21 12m0 0-3.75 3.75M21 12H3"></path>
					</svg>
				</a>
			</div>
		</section>
		"""
		);

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class WhatsNewWithAnUnresolvableProduct : MarkdownTest
{
	protected override string Markdown => """
		:::{whats-new}
		:product: not-a-product
		:::
		""";

	[Test, DisplayName("reports diagnostics")]
	public async Task ReportsDiagnostics() =>
		await Docs.HasDiagnostics(
			"error {repo}/docs/index.md:2 {whats-new} :product: 'not-a-product' was not found in hub-whats-new.yml at the root of this documentation set."
		);
}

public class WhatsNewWithNeitherProductNorBody : MarkdownTest
{
	protected override string Markdown => """
		:::{whats-new}
		:::
		""";

	[Test, DisplayName("reports diagnostics")]
	public async Task ReportsDiagnostics() =>
		await Docs.HasDiagnostics("error {repo}/docs/index.md:2 {whats-new} requires either a `:product:` option or a YAML body.");
}

public class WhatsNewWithARelativeItemLink : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{whats-new}
		title: What's new
		items:
		  - title: Broken
		    link: nope.md
		:::
		""";

	[Test, DisplayName("reports diagnostics")]
	public async Task ReportsDiagnostics() =>
		await Docs.HasDiagnostics(
			"error {repo}/docs/index.md:2 Directive link `nope.md` must be an absolute path starting with `/`, a cross-link scheme (for example `kibana://`), or an external URL."
		);
}

public class WhatsNewWithAvailabilityAndSubstitutedLabel : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{whats-new}
		title: What's new
		release-links:
		  - label: "{{version.stack}}"
		    url: /index.md
		  - label: Serverless
		    url: /index.md
		items:
		  - title: Both
		    description: Ships in both.
		    availability:
		      - "9.5"
		      - Serverless
		    date: Aug 2026
		  - title: Dated
		    description: Only a date.
		    date: Aug 2026
		:::
		""";

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<section class="hub-whats-new">
			<header class="hub-wn-header">
				<div class="hub-wn-heading">
					<h2 class="hub-wn-title">What's new</h2>
				</div>
				<div class="hub-wn-rn">
					<span class="hub-wn-rn-label">Go to latest release notes:</span>
					<ul class="hub-wn-rn-list">
						<li>
							<a class="hub-wn-rn-link" href="/">8.0.0</a>
						</li>
						<li>
							<a class="hub-wn-rn-link" href="/">Serverless</a>
						</li>
					</ul>
				</div>
			</header>
			<ul class="hub-wn-grid">
				<li class="hub-wn-card">
					<div class="hub-wn-card-link">
						<div class="hub-wn-card-meta">
							<span class="hub-wn-card-meta-left">
								<span class="hub-wn-card-date">9.5 · Serverless</span>
							</span>
						</div>
						<h3 class="hub-wn-card-title">Both</h3>
						<p class="hub-wn-card-desc">Ships in both.</p>
					</div>
				</li>
				<li class="hub-wn-card">
					<div class="hub-wn-card-link">
						<div class="hub-wn-card-meta">
							<span class="hub-wn-card-meta-left">
								<span class="hub-wn-card-date">Aug 2026</span>
							</span>
						</div>
						<h3 class="hub-wn-card-title">Dated</h3>
						<p class="hub-wn-card-desc">Only a date.</p>
					</div>
				</li>
			</ul>
		</section>
		"""
		);

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

// A :product: panel is read once and shared by every page that shows it, so each page has to
// resolve its own substitutions from the original values.
public class WhatsNewFromCentralConfigOnTwoPages : GeneratorTest
{
	protected override IReadOnlyCollection<TestFile> Files =>
		[
			new TestFile(
				"hub-whats-new.yml",
				"""
				products:
				  demo:
				    title: What's new in Demo
				    release-links:
				      - label: "{{release}}"
				        url: /index.md
				    items:
				      - title: Card
				        description: A card.
				        availability:
				          - "{{release}}"
				          - Serverless
				"""
			),
			Index(
				"""
				---
				sub:
				  release: "1.0"
				---
				# First

				:::{whats-new}
				:product: demo
				:::
				"""
			),
			Page(
				"second.md",
				"""
				---
				sub:
				  release: "2.0"
				---
				# Second

				:::{whats-new}
				:product: demo
				:::
				"""
			)
		];

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<section class="hub-whats-new">
			<header class="hub-wn-header">
				<div class="hub-wn-heading">
					<h2 class="hub-wn-title">What's new in Demo</h2>
				</div>
				<div class="hub-wn-rn">
					<span class="hub-wn-rn-label">Go to latest release notes:</span>
					<ul class="hub-wn-rn-list">
						<li>
							<a class="hub-wn-rn-link" href="/">1.0</a>
						</li>
					</ul>
				</div>
			</header>
			<ul class="hub-wn-grid">
				<li class="hub-wn-card">
					<div class="hub-wn-card-link">
						<div class="hub-wn-card-meta">
							<span class="hub-wn-card-meta-left">
								<span class="hub-wn-card-date">1.0 · Serverless</span>
							</span>
						</div>
						<h3 class="hub-wn-card-title">Card</h3>
						<p class="hub-wn-card-desc">A card.</p>
					</div>
				</li>
			</ul>
		</section>
		"""
		);

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtmlOfSecond() =>
		await Docs.Converts("second.md").ToHtml(
			"""
		<section class="hub-whats-new">
			<header class="hub-wn-header">
				<div class="hub-wn-heading">
					<h2 class="hub-wn-title">What's new in Demo</h2>
				</div>
				<div class="hub-wn-rn">
					<span class="hub-wn-rn-label">Go to latest release notes:</span>
					<ul class="hub-wn-rn-list">
						<li>
							<a class="hub-wn-rn-link" href="/">2.0</a>
						</li>
					</ul>
				</div>
			</header>
			<ul class="hub-wn-grid">
				<li class="hub-wn-card">
					<div class="hub-wn-card-link">
						<div class="hub-wn-card-meta">
							<span class="hub-wn-card-meta-left">
								<span class="hub-wn-card-date">2.0 · Serverless</span>
							</span>
						</div>
						<h3 class="hub-wn-card-title">Card</h3>
						<p class="hub-wn-card-desc">A card.</p>
					</div>
				</li>
			</ul>
		</section>
		"""
		);
}

// Every documentation set in these tests reads hub-whats-new.yml from the same path, and the parse is
// cached per path across builds (e.g. --watch). The cache is keyed by the file's content, so a file
// that changed, here one that differs from the file in the test above, is parsed again instead of
// replaying the earlier result.
public class WhatsNewFromCentralConfigThatChanged : GeneratorTest
{
	protected override IReadOnlyCollection<TestFile> Files =>
		[
			new TestFile(
				"hub-whats-new.yml",
				"""
				products:
				  demo:
				    title: Changed panel
				    items:
				      - title: Card
				        description: A card.
				"""
			),
			Index("""
				# First

				:::{whats-new}
				:product: demo
				:::
				""")
		];

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<section class="hub-whats-new">
			<header class="hub-wn-header">
				<div class="hub-wn-heading">
					<h2 class="hub-wn-title">Changed panel</h2>
				</div>
			</header>
			<ul class="hub-wn-grid">
				<li class="hub-wn-card">
					<div class="hub-wn-card-link">
						<div class="hub-wn-card-meta">
							<span class="hub-wn-card-meta-left">
							</span>
						</div>
						<h3 class="hub-wn-card-title">Card</h3>
						<p class="hub-wn-card-desc">A card.</p>
					</div>
				</li>
			</ul>
		</section>
		"""
		);

	[Test]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}
