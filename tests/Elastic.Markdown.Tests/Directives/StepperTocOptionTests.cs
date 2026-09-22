// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Myst.Directives.Stepper;

namespace Elastic.Markdown.Tests.Directives;

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
	public void Render_WhenDefault_UsesHeadingElement()
	{
		Html.Should().Contain("<h2");
		Html.Should().Contain("id=\"install\"");
		Html.Should().NotContain("step-title");
		Html.Should().NotContain("role=\"heading\"");
	}
}

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
	public void Render_WhenTocFalse_UsesDiv()
	{
		Html.Should().NotContain("<h2");
		Html.Should().Contain("class=\"step-title step-title-2\"");
		Html.Should().Contain("role=\"heading\"");
		Html.Should().Contain("aria-level=\"2\"");
		Html.Should().Contain("id=\"install\"");
		Html.Should().Contain("Install");
	}
}

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
	public void Render_WhenTocFalse_KeepsInternalHeadingLevel()
	{
		Html.Should().Contain("<h3");
		Html.Should().NotContain("<h4");
		Html.Should().Contain("class=\"step-title step-title-3\"");
	}
}

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
	public void Render_WhenNoPrecedingHeading_AdjustsInternalHeading()
	{
		Html.Should().Contain("<h2");
		Html.Should().NotContain("<h1");
		Html.Should().Contain("class=\"step-title step-title-2\"");
	}
}
