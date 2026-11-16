// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Authoring.Tests.Blocks.Hub;

public class GetStartedWithATitleAndIntro : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{get-started}
		title: Get started in 3 steps
		intro: Install, write, preview.
		steps:
		  - title: Install
		    description: Install the CLI.
		:::
		""";

	// Nothing renders between the intro and the numbered list. The section is the steps.
	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<section class="hub-get-started" id="get-started">
			<div class="hub-get-started-lead">
				<h2 class="hub-get-started-title">Get started in 3 steps</h2>
				<p class="hub-get-started-intro">Install, write, preview.</p>
			</div>
			<div class="hub-get-started-track" style="--hub-step-columns: 1">
				<ol class="hub-get-started-first">
					<li class="hub-get-started-box">
						<div class="hub-get-started-head">
							<span class="hub-get-started-step-num" aria-hidden="true">1</span>
							<h3 class="hub-get-started-step-title">Install</h3>
						</div>
						<p class="hub-get-started-step-desc hub-get-started-indent">Install the CLI.</p>
					</li>
				</ol>
			</div>
		</section>
		"""
		);

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class GetStartedWithALinkStep : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{get-started}
		title: Get started
		steps:
		  - title: Write your first page
		    description: Author markdown.
		    link: /index.md
		    link-label: Start writing
		:::
		""";

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<section class="hub-get-started" id="get-started">
			<div class="hub-get-started-lead">
				<h2 class="hub-get-started-title">Get started</h2>
			</div>
			<div class="hub-get-started-track" style="--hub-step-columns: 1">
				<ol class="hub-get-started-first">
					<li class="hub-get-started-box">
						<div class="hub-get-started-head">
							<span class="hub-get-started-step-num" aria-hidden="true">1</span>
							<h3 class="hub-get-started-step-title">Write your first page</h3>
						</div>
						<p class="hub-get-started-step-desc hub-get-started-indent">Author markdown.</p>
						<div class="hub-get-started-indent">
							<a class="hub-get-started-step-arrow" href="/">Start
								<span class="hub-get-started-nowrap">writing<svg class="hub-arrow" viewBox="0 0 24 24" fill="none" stroke-width="1.5" stroke="currentColor" aria-hidden="true">
										<path stroke-linecap="round" stroke-linejoin="round" d="M17.25 8.25 21 12m0 0-3.75 3.75M21 12H3"></path>
									</svg>
								</span>
							</a>
						</div>
					</li>
				</ol>
			</div>
		</section>
		"""
		);

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class GetStartedWithOptionSteps : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{get-started}
		title: Get started
		steps:
		  - title: Preview and publish
		    options:
		      - label: Preview locally
		        description: Serve with live reload.
		        code: docs-builder serve
		        language: sh
		      - label: Publish
		        description: Build and publish.
		        url: /index.md
		        url-label: How to publish
		:::
		""";

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
			<section class="hub-get-started" id="get-started">
				<div class="hub-get-started-lead">
					<h2 class="hub-get-started-title">Get started</h2>
				</div>
				<div class="hub-get-started-track" style="--hub-step-columns: 1">
					<ol class="hub-get-started-first">
						<li class="hub-get-started-box is-fork">
							<div class="hub-get-started-head">
								<span class="hub-get-started-step-num" aria-hidden="true">1</span>
								<h3 class="hub-get-started-step-title">Preview and publish</h3>
							</div>
							<div class="hub-get-started-fork">
								<div class="hub-get-started-option">
									<span class="hub-get-started-option-label">Preview locally</span>
									<p class="hub-get-started-option-desc">Serve with live reload.</p>
									<div class="hub-get-started-install highlight">
										<pre><code class="language-sh">docs-builder serve</code></pre>
									</div>
								</div>
								<div class="hub-get-started-or" aria-hidden="true">
									<span>or</span>
								</div>
								<div class="hub-get-started-option">
									<span class="hub-get-started-option-label">Publish</span>
									<p class="hub-get-started-option-desc">Build and publish.</p>
									<a class="hub-get-started-option-btn" href="/">How to publish</a>
								</div>
							</div>
						</li>
					</ol>
				</div>
			</section>
			"""
		);

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class GetStartedWithARelativeStepLink : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{get-started}
		title: Get started
		steps:
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

public class GetStartedWithoutABody : MarkdownTest
{
	protected override string Markdown => """
		:::{get-started}
		:::
		""";

	[Test, DisplayName("reports diagnostics")]
	public async Task ReportsDiagnostics() =>
		await Docs.HasDiagnostics("error {repo}/docs/index.md:2 {get-started} requires a YAML body. See the get-started directive docs.");
}

// A body that is only a YAML comment is not null, so it reaches the deserializer rather than the
// "requires a YAML body" check above. SharpYaml deserializes it to null, which must still fall back
// to GetStartedData.Empty and report the usual missing-title error, not throw.
public class GetStartedWithACommentOnlyBody : MarkdownTest
{
	protected override string Markdown => """
		:::{get-started}
		# title: Get started
		:::
		""";

	[Test, DisplayName("reports diagnostics")]
	public async Task ReportsDiagnostics() =>
		await Docs.HasDiagnostics("error {repo}/docs/index.md:2 {get-started} requires a `title` field in its YAML body.");
}

// The section is not fixed at three steps. The first step sits in its own box. The column
// count follows the steps after it, so the last row is never short.
public class GetStartedWithFourSteps : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{get-started}
		title: Get started
		steps:
		  - title: Install
		    options:
		      - label: Source
		        code: dotnet build
		      - label: Container
		        url: /index.md
		  - title: Write
		  - title: Preview
		  - title: Validate
		:::
		""";

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() => await Docs.ConvertsToHtmlSnapshot();

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class GetStartedWithThreeOptions : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{get-started}
		title: Get started
		steps:
		  - title: Install
		    options:
		      - label: Source
		        code: dotnet build
		      - label: Container
		        url: /index.md
		      - label: Package
		        url: /index.md
		:::
		""";

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
			<section class="hub-get-started" id="get-started">
				<div class="hub-get-started-lead">
					<h2 class="hub-get-started-title">Get started</h2>
				</div>
				<div class="hub-get-started-track" style="--hub-step-columns: 1">
					<ol class="hub-get-started-first">
						<li class="hub-get-started-box">
							<div class="hub-get-started-head">
								<span class="hub-get-started-step-num" aria-hidden="true">1</span>
								<h3 class="hub-get-started-step-title">Install</h3>
							</div>
							<div class="hub-get-started-options">
								<div class="hub-get-started-option">
									<span class="hub-get-started-option-label">Source</span>
									<div class="hub-get-started-install highlight">
										<pre><code class="language-text">dotnet build</code></pre>
									</div>
								</div>
								<div class="hub-get-started-option">
									<span class="hub-get-started-option-label">Container</span>
									<a class="hub-get-started-option-btn" href="/">Get started</a>
								</div>
								<div class="hub-get-started-option">
									<span class="hub-get-started-option-label">Package</span>
									<a class="hub-get-started-option-btn" href="/">Get started</a>
								</div>
							</div>
						</li>
					</ol>
				</div>
			</section>
			"""
		);
}

public class GetStartedWithFiveSteps : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{get-started}
		title: Get started
		steps:
		  - title: One
		  - title: Two
		  - title: Three
		  - title: Four
		:::
		""";

	// The first of four steps sits in the box, and the three after it take three columns.
	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<section class="hub-get-started" id="get-started">
			<div class="hub-get-started-lead">
				<h2 class="hub-get-started-title">Get started</h2>
			</div>
			<div class="hub-get-started-track" style="--hub-step-columns: 3">
				<ol class="hub-get-started-first">
					<li class="hub-get-started-box">
						<div class="hub-get-started-head">
							<span class="hub-get-started-step-num" aria-hidden="true">1</span>
							<h3 class="hub-get-started-step-title">One</h3>
						</div>
					</li>
				</ol>
				<ol class="hub-get-started-grid has-three-columns" start="2">
					<li class="hub-get-started-step">
						<div class="hub-get-started-head">
							<span class="hub-get-started-step-num" aria-hidden="true">2</span>
							<h3 class="hub-get-started-step-title">Two</h3>
						</div>
						<div class="hub-get-started-step-foot">
						</div>
					</li>
					<li class="hub-get-started-step">
						<div class="hub-get-started-head">
							<span class="hub-get-started-step-num" aria-hidden="true">3</span>
							<h3 class="hub-get-started-step-title">Three</h3>
						</div>
						<div class="hub-get-started-step-foot">
						</div>
					</li>
					<li class="hub-get-started-step">
						<div class="hub-get-started-head">
							<span class="hub-get-started-step-num" aria-hidden="true">4</span>
							<h3 class="hub-get-started-step-title">Four</h3>
						</div>
						<div class="hub-get-started-step-foot">
						</div>
					</li>
				</ol>
			</div>
		</section>
		"""
		);

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class GetStartedWithFiveStepsAfterTheBox : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{get-started}
		title: Get started
		steps:
		  - title: One
		  - title: Two
		  - title: Three
		  - title: Four
		  - title: Five
		:::
		""";

	// Four steps after the first divide evenly into two rows of two, so they take two columns
	// rather than three with a single step stranded on the last row.
	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() => await Docs.ConvertsToHtmlSnapshot();

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class GetStartedWithAFirstStepThatHasNoOptions : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{get-started}
		title: Get started
		steps:
		  - title: Install
		    description: Download the binary.
		    link: /index.md
		    link-label: See the install guide
		  - title: Configure
		    description: Write a config.
		:::
		""";

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<section class="hub-get-started" id="get-started">
			<div class="hub-get-started-lead">
				<h2 class="hub-get-started-title">Get started</h2>
			</div>
			<div class="hub-get-started-track" style="--hub-step-columns: 1">
				<ol class="hub-get-started-first">
					<li class="hub-get-started-box">
						<div class="hub-get-started-head">
							<span class="hub-get-started-step-num" aria-hidden="true">1</span>
							<h3 class="hub-get-started-step-title">Install</h3>
						</div>
						<p class="hub-get-started-step-desc hub-get-started-indent">Download the binary.</p>
						<div class="hub-get-started-indent">
							<a class="hub-get-started-step-arrow" href="/">See the install
								<span class="hub-get-started-nowrap">guide<svg class="hub-arrow" viewBox="0 0 24 24" fill="none" stroke-width="1.5" stroke="currentColor" aria-hidden="true">
										<path stroke-linecap="round" stroke-linejoin="round" d="M17.25 8.25 21 12m0 0-3.75 3.75M21 12H3"></path>
									</svg>
								</span>
							</a>
						</div>
					</li>
				</ol>
				<ol class="hub-get-started-grid" start="2">
					<li class="hub-get-started-step">
						<div class="hub-get-started-head">
							<span class="hub-get-started-step-num" aria-hidden="true">2</span>
							<h3 class="hub-get-started-step-title">Configure</h3>
						</div>
						<p class="hub-get-started-step-desc">Write a config.</p>
						<div class="hub-get-started-step-foot">
						</div>
					</li>
				</ol>
			</div>
		</section>
		"""
		);

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}
