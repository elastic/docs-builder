// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Authoring.Tests.Blocks.Hub.WhatsNew;

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

	[Fact(DisplayName = "renders the heading and intro")]
	public async Task RendersHeadingAndIntro() =>
		await Docs.ConvertsToContainingHtml("""<h2 class="hub-wn-title">What's new in docs-builder</h2>""");

	[Fact(DisplayName = "renders a card per item")]
	public async Task RendersCardPerItem() => await Docs.ConvertsToContainingHtml("""<h3 class="hub-wn-card-title">Hub pages</h3>""");

	[Fact(DisplayName = "spans the featured card across two columns")]
	public async Task SpansFeaturedCard() => await Docs.ConvertsToContainingRawHtml("""<li class="hub-wn-card hub-wn-card-featured">""");

	[Fact(DisplayName = "renders the date and tag")]
	public async Task RendersDateAndTag() => await Docs.ConvertsToContainingHtml("""<span class="hub-wn-card-tag">Syntax</span>""");

	[Fact(DisplayName = "has no errors")]
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

	[Fact(DisplayName = "renders the release link")]
	public async Task RendersReleaseLink() =>
		await Docs.ConvertsToContainingHtml("""<a class="hub-wn-rn-link" href="/">View release notes</a>""");

	[Fact(DisplayName = "renders the upgrade prompt")]
	public async Task RendersUpgradePrompt() =>
		await Docs.ConvertsToContainingHtml("""<span class="hub-wn-footer-text">Ready to move to the latest version?</span>""");

	[Fact(DisplayName = "has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class WhatsNewWithAnUnresolvableProduct : MarkdownTest
{
	protected override string Markdown => """
		:::{whats-new}
		:product: not-a-product
		:::
		""";

	[Fact(DisplayName = "errors and names the file it looked in")]
	public async Task ErrorsAndNamesFile() => await Docs.HasError("hub-whats-new.yml");
}

public class WhatsNewWithNeitherProductNorBody : MarkdownTest
{
	protected override string Markdown => """
		:::{whats-new}
		:::
		""";

	[Fact(DisplayName = "errors")]
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

	[Fact(DisplayName = "rejects a relative path")]
	public async Task RejectsRelativePath() => await Docs.HasError("must be an absolute path starting with `/`");
}
