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

	[Test, DisplayName("renders the heading and intro")]
	public async Task RendersHeadingAndIntro() =>
		await Docs.ConvertsToContainingHtml("""<h2 class="hub-wn-title">What's new in docs-builder</h2>""");

	[Test, DisplayName("renders a card per item")]
	public async Task RendersCardPerItem() => await Docs.ConvertsToContainingHtml("""<h3 class="hub-wn-card-title">Hub pages</h3>""");

	[Test, DisplayName("spans the featured card across two columns")]
	public async Task SpansFeaturedCard() => await Docs.ConvertsToContainingRawHtml("""<li class="hub-wn-card hub-wn-card-featured">""");

	[Test, DisplayName("renders the date and tag")]
	public async Task RendersDateAndTag() => await Docs.ConvertsToContainingHtml("""<span class="hub-wn-card-tag">Syntax</span>""");

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

	[Test, DisplayName("renders the release link")]
	public async Task RendersReleaseLink() =>
		await Docs.ConvertsToContainingHtml("""<a class="hub-wn-rn-link" href="/">View release notes</a>""");

	[Test, DisplayName("renders the upgrade prompt")]
	public async Task RendersUpgradePrompt() =>
		await Docs.ConvertsToContainingHtml("""<span class="hub-wn-footer-text">Ready to move to the latest version?</span>""");

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

	[Test, DisplayName("errors and names the file it looked in")]
	public async Task ErrorsAndNamesFile() => await Docs.HasError("hub-whats-new.yml");
}

public class WhatsNewWithNeitherProductNorBody : MarkdownTest
{
	protected override string Markdown => """
		:::{whats-new}
		:::
		""";

	[Test, DisplayName("errors")]
	public async Task Errors() => await Docs.HasError("requires either a `:product:` option or a YAML body");
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

	[Test, DisplayName("rejects a relative path")]
	public async Task RejectsRelativePath() => await Docs.HasError("must be an absolute path starting with `/`");
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

	[Test, DisplayName("renders the release notes label")]
	public async Task RendersReleaseNotesLabel() =>
		await Docs.ConvertsToContainingHtml("""<span class="hub-wn-rn-label">Go to latest release notes:</span>""");

	[Test, DisplayName("resolves a substitution in a release link label")]
	public async Task ResolvesSubstitutionInLabel() => await Docs.DoesNotConvertToContainingHtml("{{version.stack}}");

	[Test, DisplayName("shows the availability list in place of the date")]
	public async Task ShowsAvailabilityInPlaceOfDate() =>
		await Docs.ConvertsToContainingHtml("""<span class="hub-wn-card-date">9.5 · Serverless</span>""");

	[Test, DisplayName("falls back to the date when there is no availability")]
	public async Task FallsBackToDate() => await Docs.ConvertsToContainingHtml("""<span class="hub-wn-card-date">Aug 2026</span>""");

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

	[Test, DisplayName("resolves the substitution on the first page")]
	public async Task FirstPage() => await Docs.Converts("index.md").ContainsRawHtml("1.0 &#xB7; Serverless");

	[Test, DisplayName("resolves the substitution again on the second page")]
	public async Task SecondPage() => await Docs.Converts("second.md").ContainsRawHtml("2.0 &#xB7; Serverless");
}
