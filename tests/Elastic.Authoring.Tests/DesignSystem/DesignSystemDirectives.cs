// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;

namespace Elastic.Authoring.Tests.DesignSystem;

/// <summary>Renders the document with <c>NAVIGATION_PREVIEW</c> on, so directives use design-system components.</summary>
public abstract class DesignSystemDirectiveTest : GeneratorTest
{
	protected abstract string Markdown { get; }

	protected override IReadOnlyCollection<TestFile> Files => [TestFile.Index($"# Test Document\n{Markdown}\n")];

	protected override SetupOptions Options => new() { NavigationPreview = true };
}

public class AdmonitionKinds : DesignSystemDirectiveTest
{
	protected override string Markdown => """
		:::{warning}
		Back up first.
		:::
		""";

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<div class="admonition admonition--warning" role="note">
			<div class="admonition__header">
				<svg class="icon icon--sm" xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
					<path d="M11.354 5.354 8.707 8l2.646 2.646-.707.707L8 8.707l-2.646 2.646-.708-.707L7.293 8 4.646 5.354l.708-.708L8 7.293l2.646-2.647.707.708Z"></path>
					<path fill-rule="evenodd" d="M10.293 1a1 1 0 0 1 .707.293L14.707 5a1 1 0 0 1 .293.707v4.586a1 1 0 0 1-.293.707L11 14.707a1 1 0 0 1-.707.293H5.707A1 1 0 0 1 5 14.707L1.293 11A1 1 0 0 1 1 10.293V5.707A1 1 0 0 1 1.293 5L5 1.293A1 1 0 0 1 5.707 1h4.586ZM2 5.707v4.586L5.707 14h4.586L14 10.293V5.707L10.293 2H5.707L2 5.707Z" clip-rule="evenodd"></path>
				</svg>
				<span class="admonition__title">Warning</span>
			</div>
			<div class="admonition__body">
				<p>Back up first.</p>
			</div>
		</div>
		"""
		);

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class AdmonitionInListStaysNested : DesignSystemDirectiveTest
{
	protected override string Markdown => """
		- List Item 1
		  :::::{note}
		  Hello, World!
		  :::::
		""";

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<ul>
			<li>List Item 1
				<div class="admonition admonition--note" role="note">
					<div class="admonition__header">
						<svg class="icon icon--sm" xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
							<path fill-rule="evenodd" d="M8 14A6 6 0 1 0 8 2a6 6 0 0 0 0 12Zm0 1A7 7 0 1 0 8 1a7 7 0 0 0 0 14Z" clip-rule="evenodd"></path>
							<path d="M9 5a1 1 0 1 1-2 0 1 1 0 0 1 2 0Z"></path>
							<path fill-rule="evenodd" d="M6.5 7h2v4H10v1H6v-1h1.5V8h-1V7Z" clip-rule="evenodd"></path>
						</svg>
						<span class="admonition__title">Note</span>
					</div>
					<div class="admonition__body">
						<p>Hello, World!</p>
					</div>
				</div>
			</li>
		</ul>
		"""
		);
}

public class DropdownComponent : DesignSystemDirectiveTest
{
	protected override string Markdown =>
		"""
		:::{dropdown} What is a shard?
		:open:
		A unit of storage.
		:::
		""";

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<details class="disclosure dropdown" open="">
			<summary class="disclosure__summary">
				<span class="disclosure__title">
					<span>What is a shard?</span>
				</span>
				<svg class="icon icon--sm" xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
					<path fill-rule="evenodd" d="m8 10.293 5.646-5.647.708.708L8 11.707 1.646 5.354l.708-.708L8 10.293Z" clip-rule="evenodd"></path>
				</svg>
			</summary>
			<div class="disclosure__body">
				<p>A unit of storage.</p>
			</div>
		</details>
		"""
		);
}

public class TabsComponent : DesignSystemDirectiveTest
{
	protected override string Markdown =>
		"""
		::::{tab-set}
		:group: lang

		:::{tab-item} curl
		:sync: curl
		Use curl.
		:::

		:::{tab-item} Python
		:sync: python
		Use Python.
		:::
		::::
		""";

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<div class="tab-set">
			<input class="tab-set__input" type="radio" id="tabs-item-1-0" name="tabs-set-1" checked="">
			<label class="tab-set__tab" for="tabs-item-1-0" data-tab-sync-key="curl" data-tab-sync-group="lang">curl</label>
			<div class="tab-set__panel" tabindex="0">
				<p>Use curl.</p>
			</div>
			<input class="tab-set__input" type="radio" id="tabs-item-1-1" name="tabs-set-1">
			<label class="tab-set__tab" for="tabs-item-1-1" data-tab-sync-key="python" data-tab-sync-group="lang">Python</label>
			<div class="tab-set__panel" tabindex="0">
				<p>Use Python.</p>
			</div>
		</div>
		"""
		);
}

public class StepperComponent : DesignSystemDirectiveTest
{
	protected override string Markdown =>
		"""
		::::{stepper}
		:::{step} Install
		Download it.
		:::

		:::{step} Start
		Run it.
		:::
		::::
		""";

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<div class="stepper">
			<ol class="stepper__list">
				<li class="stepper__step">
					<h2 class="stepper__title" id="install">
						<a href="#install">Install</a>
					</h2>
					<p>Download it.</p>
				</li>
				<li class="stepper__step">
					<h2 class="stepper__title" id="start">
						<a href="#start">Start</a>
					</h2>
					<p>Run it.</p>
				</li>
			</ol>
		</div>
		"""
		);
}

public class ButtonDirectiveComponent : DesignSystemDirectiveTest
{
	protected override string Markdown => """
		:::{button}
		[Get started](https://www.elastic.co)
		:::
		""";

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<div class="button-directive button-directive--primary button-directive--left">
			<p>
				<a href="https://www.elastic.co" target="_blank" rel="noopener noreferrer">Get started</a>
			</p>
		</div>
		"""
		);
}

/// <summary>Flag off must keep producing the legacy markup, whatever the design system adds.</summary>
public class FlagOffKeepsLegacyMarkup : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{warning}
		Back up first.
		:::

		:::{dropdown} Title
		Body
		:::
		""";

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<div class="admonition warning">
			<div class="admonition-header">
				<span class="admonition-title">Warning</span>
			</div>
			<div class="admonition-content">
				<p>Back up first.</p>
			</div>
		</div>
		<details class="dropdown">
			<summary class="dropdown-title">
				<div class="dropdown-title__container">
					<span class="dropdown-title__summary-text">Title</span>
				</div>
				<div class="dropdown-title__icon">
					<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-4 mr-1 shrink -rotate-90 group-has-checked/label:rotate-0 cursor-pointer text-ink">
						<path stroke-linecap="round" stroke-linejoin="round" d="m19.5 8.25-7.5 7.5-7.5-7.5"></path>
					</svg>
				</div>
			</summary>
			<div class="dropdown-content">
				<p>Body</p>
			</div>
		</details>
		"""
		);
}

public class CodeBlockComponent : DesignSystemDirectiveTest
{
	protected override string Markdown =>
		"""
		```json
		{ "size": 10 }
		```

		```yaml
		cluster.name: docs <1>
		```

		1. The cluster name.
		""";

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<figure class="code-block notranslate" data-language="json">
			<figcaption class="code-block__header">
				<span class="code-block__language">JSON</span>
				<button class="code-block__copy" type="button" data-copy="" hidden="">
					<span class="code-block__copy-label">Copy</span>
					<span class="code-block__copied-label" aria-live="polite">Copied</span>
				</button>
			</figcaption>
			<pre class="code-block__pre">
				<code class="language-json">{ "size": 10 }
				</code>
			</pre>
		</figure>
		<figure class="code-block notranslate" data-language="yaml">
			<figcaption class="code-block__header">
				<span class="code-block__language">YAML</span>
				<button class="code-block__copy" type="button" data-copy="" hidden="">
					<span class="code-block__copy-label">Copy</span>
					<span class="code-block__copied-label" aria-live="polite">Copied</span>
				</button>
			</figcaption>
			<pre class="code-block__pre">
				<code class="language-yaml">cluster.name: docs<span style="display: inline-block; width: 1ch"></span>
					<span class="code-callout" data-index="1"></span>
				</code>
			</pre>
		</figure>
		<ol class="code-callouts">
			<li>The cluster name.</li>
		</ol>
		"""
		);
}

public class PageShellComponents : DesignSystemDirectiveTest
{
	protected override string Markdown =>
		"""
		## First section

		Some text.

		### A subsection

		More text.
		""";

	[Test, DisplayName("wraps the page in the design-system shell")]
	public async Task Shell()
	{
		await Docs.ConvertsToContainingRawHtml("class=\"layout layout--three\"");
		await Docs.ConvertsToContainingRawHtml("class=\"layout__nav\"");
		await Docs.ConvertsToContainingRawHtml("class=\"layout__rail\"");
	}

	[Test, DisplayName("renders the page content inside the article, not before the shell")]
	public async Task ContentSitsInsideTheArticle()
	{
		var html = await Docs.IndexHtmlAsync();

		var shell = html.IndexOf("class=\"layout layout--three\"", StringComparison.Ordinal);
		var articleOpen = html.IndexOf("<article id=\"markdown-content\"", StringComparison.Ordinal);
		var heading = html.IndexOf("First section", StringComparison.Ordinal);
		var articleClose = html.IndexOf("</article>", StringComparison.Ordinal);

		shell.Should().BeGreaterThan(-1);
		articleOpen.Should().BeGreaterThan(shell);
		heading.Should().BeGreaterThan(articleOpen);
		heading.Should().BeLessThan(articleClose);
	}

	[Test, DisplayName("styles the article with prose instead of the legacy markdown-content class")]
	public async Task Prose()
	{
		await Docs.ConvertsToContainingRawHtml("<article id=\"markdown-content\" class=\"prose min-w-0\"");
		await Docs.DoesNotConvertToContainingHtml("class=\"markdown-content");
	}

	[Test, DisplayName("renders the outline from the page headings")]
	public async Task Outline()
	{
		await Docs.ConvertsToContainingRawHtml("<nav class=\"toc\"");
		await Docs.ConvertsToContainingRawHtml("href=\"#first-section\"");
		await Docs.ConvertsToContainingRawHtml("toc__item--nested");
	}

	[Test, DisplayName("renders the design-system header and footer")]
	public async Task HeaderAndFooter()
	{
		await Docs.ConvertsToContainingRawHtml("<header class=\"site-header\"");
		await Docs.ConvertsToContainingRawHtml("<footer class=\"site-footer\"");
	}

	[Test, DisplayName("adds a search trigger that stays hidden until script can open the dialog")]
	public async Task SearchTrigger()
	{
		await Docs.ConvertsToContainingRawHtml(
			"<button class=\"search-field search-field--trigger\" type=\"button\" data-search-open hidden"
		);
		await Docs.ConvertsToContainingRawHtml("<dialog class=\"search-dialog\" data-search-dialog");
	}

	[Test, DisplayName("leaves out the legacy React header")]
	public async Task NoLegacyHeader() => await Docs.DoesNotConvertToContainingHtml("<elastic-docs-header");
}

public class ConsoleCodeBlockSegments : DesignSystemDirectiveTest
{
	protected override string Markdown =>
		"""
		```console
		GET /_search
		{ "size": 1 }

		POST /docs/_doc
		{ "title": "one" }
		```
		""";

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<figure class="code-block notranslate" data-language="json">
			<figcaption class="code-block__header">
				<span class="code-block__language">JSON</span>
				<button class="code-block__copy" type="button" data-copy="" hidden="">
					<span class="code-block__copy-label">Copy</span>
					<span class="code-block__copied-label" aria-live="polite">Copied</span>
				</button>
			</figcaption>
			<pre class="code-block__pre">
				<code class="language-apiheader">GET /_search</code>
				<code class="language-json">{ "size": 1 }</code>
				<code class="language-apiheader">POST /docs/_doc</code>
				<code class="language-json">{ "title": "one" }</code>
			</pre>
		</figure>
		"""
		);

	// Inside <pre> a newline between two elements is output, which a pretty-printed snapshot cannot show.
	[Test, DisplayName("starts each request and each body on its own line")]
	public async Task SegmentsAreSeparatedByANewline()
	{
		await Docs.ConvertsToContainingRawHtml("</code>\n<code class=\"language-json\">");
		await Docs.ConvertsToContainingRawHtml("</code>\n<code class=\"language-apiheader\">");
		await Docs.DoesNotConvertToContainingHtml("</code><code");
	}
}

public class AppliesToComponent : DesignSystemDirectiveTest
{
	protected override string Markdown =>
		"""
		:::{note}
		:applies_to: stack: ga 9.1
		This applies to the stack.
		:::

		Inline badge: {applies_to}`stack: preview 9.0`
		""";

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() => await Docs.ConvertsToHtmlSnapshot();
}

public class VectorSizingComponent : DesignSystemDirectiveTest
{
	protected override string Markdown => """
		:::{vector-sizing-calculator}
		:::
		""";

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() => await Docs.ConvertsToHtmlSnapshot();
}

public class ImageComponents : DesignSystemDirectiveTest
{
	protected override string Markdown =>
		"""
		:::{image} /images/logo.png
		:alt: The logo
		:::

		:::{figure} /images/logo.png
		:label: logo-figure
		:alt: The logo
		A caption
		:::
		""";

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<a class="image-reference image__link" href="/images/logo.png" hx-boost="false" data-lightbox="lightbox-C3F4077F">
			<img class="image__img" loading="lazy" title="The logo" alt="The logo" src="/images/logo.png">
		</a>
		<dialog class="lightbox" id="lightbox-C3F4077F" aria-label="The logo">
			<form method="dialog">
				<button class="lightbox__close" type="submit" aria-label="Close">
					<svg class="icon icon--sm" xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
						<path fill-rule="evenodd" d="M7.293 8 2.646 3.354l.708-.708L8 7.293l4.646-4.647.708.708L8.707 8l4.647 4.646-.707.708L8 8.707l-4.646 4.647-.708-.707L7.293 8Z" clip-rule="evenodd"></path>
					</svg>
				</button>
			</form>
			<a href="/images/logo.png" target="_blank" rel="noopener">
				<img class="lightbox__img" loading="lazy" title="The logo" alt="The logo" src="/images/logo.png">
			</a>
		</dialog>
		<figure class="figure" id="logo-figure">
			<a class="image-reference image__link" href="/images/logo.png" hx-boost="false">
				<img class="image__img" alt="The logo" src="/images/logo.png">
			</a>
			<figcaption class="figure__caption">
				<p>A caption</p>
				<a class="headerlink" href="#logo-figure" title="Link to this image">¶</a>
			</figcaption>
		</figure>
		"""
		);
}

public class PageCardComponent : DesignSystemDirectiveTest
{
	protected override string Markdown => """
		:::{page-card} index.md
		A description.
		:::
		""";

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<a class="page-card">
			<span class="page-card__text">
				<span class="page-card__title"></span>
				<span class="page-card__description">
					<p>A description.</p>
				</span>
			</span>
			<svg class="icon icon--sm" xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16">
				<path d="M2 7.5h10.793L9.646 4.354l.707-.708L14.707 8l-4.354 4.354-.707-.707L12.793 8.5H2v-1Z"></path>
			</svg>
		</a>
		"""
		);
}

public abstract class DesignSystemLayoutTest(string layout) : GeneratorTest
{
	protected override IReadOnlyCollection<TestFile> Files => [TestFile.Index($"---\nlayout: {layout}\n---\n# Test Document\n")];

	protected override SetupOptions Options => new() { NavigationPreview = true };
}

public class NotFoundLayout() : DesignSystemLayoutTest("not-found")
{
	[Test, DisplayName("renders the page message without utility classes")]
	public async Task Renders()
	{
		await Docs.ConvertsToContainingRawHtml("class=\"page-message band ");
		await Docs.DoesNotConvertToContainingHtml("items-center justify-center");
	}
}

public class ArchiveLayout() : DesignSystemLayoutTest("archive")
{
	[Test, DisplayName("renders the archive list from the shared link list")]
	public async Task Renders()
	{
		await Docs.ConvertsToContainingRawHtml("class=\"grid grid--3 archive__list\"");
		await Docs.ConvertsToContainingRawHtml("Elasticsearch - The Definitive Guide");
		await Docs.DoesNotConvertToContainingHtml("delorean");
	}
}

public class LandingLayout() : DesignSystemLayoutTest("landing-page")
{
	[Test, DisplayName("renders the landing sections with design-system buttons")]
	public async Task Renders()
	{
		await Docs.ConvertsToContainingRawHtml("class=\"landing layout layout--bands\"");
		await Docs.ConvertsToContainingRawHtml("class=\"grid grid--4 landing__grid\"");
		await Docs.ConvertsToContainingRawHtml("btn btn--primary");
		await Docs.DoesNotConvertToContainingHtml("min-[1024px]");
	}
}

public class FullSearchLayout() : DesignSystemLayoutTest("full-search")
{
	[Test, DisplayName("renders the search page component instead of the React element")]
	public async Task Renders()
	{
		await Docs.ConvertsToContainingRawHtml("data-search-page");
		await Docs.DoesNotConvertToContainingHtml("<full-page-search");
	}
}

public class GetStartedLabeledCommandsComponent : DesignSystemDirectiveTest
{
	protected override string Markdown =>
		"""
		:::{get-started}
		title: Get started
		steps:
		  - title: Run it
		    options:
		      - label: Run locally
		        commands:
		          - label: Server only
		            code: install --server-only
		          - label: Server and UI
		            code: install
		        language: sh
		:::
		""";

	[Test, DisplayName("gives each labeled command its own copy button")]
	public async Task Renders()
	{
		await Docs.ConvertsToContainingRawHtml("""<span class="get-started__command-label">Server only</span>""");
		await Docs.ConvertsToContainingRawHtml("data-copy-text=\"install --server-only\"");
		await Docs.ConvertsToContainingRawHtml("data-copy-text=\"install\"");
	}
}
