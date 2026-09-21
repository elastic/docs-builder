// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Myst.Directives.Stepper;

namespace Elastic.Markdown.Tests.Directives;

public class StepperTocDefaultTests(ITestOutputHelper output) : DirectiveTest<StepperBlock>(
	output,
	"""
:::::{stepper}

::::{step} Install
First install the dependencies.
::::

:::::
"""
)
{
	[Fact]
	public void IncludesStepTitlesInTheTableOfContents()
	{
		var toc = File.PageTableOfContent.Values.ToList();
		toc.Should().ContainSingle();
		toc[0].Heading.Should().Be("Install");
		toc[0].IsStepperStep.Should().BeTrue();
	}

	[Fact]
	public void RendersStepTitlesAsHeadings()
	{
		Html.Should().Contain("<h2");
		Html.Should().Contain("id=\"install\"");
		Html.Should().NotContain("step-title");
	}
}

public class StepperTocFalseTests(ITestOutputHelper output) : DirectiveTest<StepperBlock>(
	output,
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
	[Fact]
	public void OmitsStepTitlesFromTheTableOfContents()
	{
		File.PageTableOfContent.Should().BeEmpty();
		Block!.IncludeInToc.Should().BeFalse();
	}

	[Fact]
	public void RendersStepTitlesWithoutHeadingElements()
	{
		Html.Should().NotContain("<h2");
		Html.Should().Contain("class=\"step-title step-title-2\"");
		Html.Should().Contain("id=\"install\"");
		Html.Should().Contain("Install");
	}
}

public class StepperTocFalseKeepsInternalHeadingsTests(ITestOutputHelper output) : DirectiveTest<StepperBlock>(
	output,
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
	[Fact]
	public void KeepsRealHeadingsAndDropsTheStep()
	{
		var toc = File.PageTableOfContent.Values.Select(item => item.Heading).ToList();
		toc.Should().Equal("Section", "Internal");
	}

	[Fact]
	public void DoesNotPromoteTheInternalHeading()
	{
		Html.Should().Contain("<h3");
		Html.Should().NotContain("<h4");
		Html.Should().Contain("class=\"step-title step-title-3\"");
	}
}
