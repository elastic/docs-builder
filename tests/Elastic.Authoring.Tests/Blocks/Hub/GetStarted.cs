// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Authoring.Tests.Blocks.Hub.GetStarted;

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

	[Fact(DisplayName = "renders the heading")]
	public async Task RendersHeading() =>
		await Docs.ConvertsToContainingHtml("""<h2 class="hub-get-started-title">Get started in 3 steps</h2>""");

	[Fact(DisplayName = "numbers steps from one, zero padded")]
	public async Task NumbersStepsFromOne() =>
		await Docs.ConvertsToContainingRawHtml("""<span class="hub-get-started-step-num" aria-hidden="true">01</span>""");

	// Nothing renders between the intro and the numbered list. The section is the steps.
	[Fact(DisplayName = "renders nothing above the steps")]
	public async Task RendersNothingAboveSteps() => await Docs.DoesNotConvertToContainingHtml("hub-get-started-actions");

	[Fact(DisplayName = "has no errors")]
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

	[Fact(DisplayName = "makes the whole step clickable")]
	public async Task MakesStepClickable() => await Docs.ConvertsToContainingHtml("""<span>Start writing</span>""");

	[Fact(DisplayName = "has no errors")]
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

	[Fact(DisplayName = "renders both options")]
	public async Task RendersBothOptions() =>
		await Docs.ConvertsToContainingHtml("""<span class="hub-get-started-option-label">Preview locally</span>""");

	[Fact(DisplayName = "renders the option command")]
	public async Task RendersOptionCommand() =>
		await Docs.ConvertsToContainingHtml("""<code class="language-sh">docs-builder serve</code>""");

	[Fact(DisplayName = "has no errors")]
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

	[Fact(DisplayName = "rejects a relative path")]
	public async Task RejectsRelativePath() => await Docs.HasError("must be an absolute path starting with `/`");
}

public class GetStartedWithoutABody : MarkdownTest
{
	protected override string Markdown => """
		:::{get-started}
		:::
		""";

	[Fact(DisplayName = "errors")]
	public async Task Errors() => await Docs.HasError("{get-started}");
}

// The section is not fixed at three steps. The track count follows the steps that flow in
// columns, so the last row is never short. A step carrying options spans the full row and
// takes no track.
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

	[Fact(DisplayName = "lays the three remaining steps across three tracks")]
	public async Task LaysRemainingStepsAcrossThreeTracks() =>
		await Docs.ConvertsToContainingRawHtml("""<ol class="hub-get-started-steps" style="--hub-step-columns: 3">""");

	[Fact(DisplayName = "has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
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

	// Four steps divide evenly into two rows of two, so they take two tracks rather than
	// three with a single step stranded on the last row.
	[Fact(DisplayName = "pairs four steps into two tracks")]
	public async Task PairsFourStepsIntoTwoTracks() =>
		await Docs.ConvertsToContainingRawHtml("""<ol class="hub-get-started-steps" style="--hub-step-columns: 2">""");

	[Fact(DisplayName = "has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}
