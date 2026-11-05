// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Testing;
using Elastic.Markdown.Myst.Directives.Stepper;
using Nullean.Snapshots.Html;

namespace Elastic.Markdown.Tests.Directives;

[InheritsTests]
public class StepperTocDefaultTests() : DirectiveTest<StepperBlock>(
	"""
:::::{stepper}

::::{step} Install
First install the dependencies.
::::

:::::
"""
)
{
	[Test]
	public void Toc_WhenDefault_IncludesStepTitle()
	{
		var toc = File.PageTableOfContent.Values.ToList();
		toc.Should().ContainSingle();
		toc[0].Heading.Should().Be("Install");
		toc[0].IsStepperStep.Should().BeTrue();
	}

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="stepper">
			<ol>
				<li class="step">
					<h2 id="install">
						<a href="#install">Install</a>
					</h2>
					<p>First install the dependencies.</p>
				</li>
			</ol>
		</div>
		"""
		);
}

[InheritsTests]
public class StepperTocFalseTests() : DirectiveTest<StepperBlock>(
	"""
:::::{stepper}
:toc: false

::::{step} Install
First install the dependencies.
::::

:::::
"""
)
{
	[Test]
	public void Toc_WhenTocFalse_OmitsStepTitle()
	{
		File.PageTableOfContent.Should().BeEmpty();
		Block!.IncludeInToc.Should().BeFalse();
	}

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="stepper">
			<ol>
				<li class="step">
					<div id="install" class="step-title step-title-2" role="heading" aria-level="2">
						<a href="#install">Install</a>
					</div>
					<p>First install the dependencies.</p>
				</li>
			</ol>
		</div>
		"""
		);
}

[InheritsTests]
public class StepperTocFalseKeepsInternalHeadingsTests() : DirectiveTest<StepperBlock>(
	"""
## Section

:::::{stepper}
:toc: false

::::{step} First
### Internal

Some content under the internal heading.
::::

:::::
"""
)
{
	[Test]
	public void Toc_WhenTocFalse_KeepsInternalHeading()
	{
		var toc = File.PageTableOfContent.Values.Select(item => item.Heading).ToList();
		toc.Should().Equal("Section", "Internal");
	}

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

[InheritsTests]
public class StepperTocFalseNoPrecedingHeadingTests() : DirectiveTest<StepperBlock>(
	"""
	---
	title: Outline level
	---

	:::::{stepper}
	:toc: false

	::::{step} First
	# Too high

	Body.
	::::

	:::::
	"""
)
{
	[Test]
	public void Hint_WhenNoPrecedingHeading_NamesOutlineLevel()
	{
		var hint = Collector
			.Diagnostics
			.Should()
			.ContainSingle(d => d.Severity == Severity.Hint && d.Message.Contains("Heading level h1"))
			.Which;
		hint.Message.Should().Contain("outline level for this step (h1)");
		hint.Message.Should().NotContain("preceding heading");
	}

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="stepper">
			<ol>
				<li class="step">
					<div id="first" class="step-title step-title-2" role="heading" aria-level="2">
						<a href="#first">First</a>
					</div>
					<div class="heading-wrapper" id="too-high">
						<h2>
							<a class="headerlink" href="#too-high">Too high</a>
						</h2>
					</div>
					<p>Body.</p>
				</li>
			</ol>
		</div>
		"""
		);
}
