// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Myst.Directives.Math;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

public class MathBlockTests(ITestOutputHelper output) : DirectiveTest<MathBlock>(output,
"""
:::{math}
E = mc^2
:::
"""
)
{
	[Fact]
	public void ParsesMathBlock() => Block.Should().NotBeNull();

	[Fact]
	public void SetsCorrectDirectiveType() => Block!.Directive.Should().Be("math");

	[Fact]
	public void ExtractsContent() => Block!.Content.Should().Be("E = mc^2");

	[Fact]
	public void DeterminesInlineMath() => Block!.IsDisplayMath.Should().BeFalse();

	[Fact]
	public void RendersMathSpan() => Html.Should().Contain("<span class=\"math\">E = mc^2</span>");
}

public class MathBlockDisplayMathTests(ITestOutputHelper output) : DirectiveTest<MathBlock>(output,
"""
:::{math}
\[
\int_{-\infty}^{\infty} e^{-x^2} dx = \sqrt{\pi}
\]
:::
"""
)
{
	[Fact]
	public void ParsesDisplayMathBlock() => Block.Should().NotBeNull();

	[Fact]
	public void ExtractsDisplayMathContent() => Block!.Content.Should().Contain("\\int_{-\\infty}^{\\infty}");

	[Fact]
	public void DeterminesDisplayMath() => Block!.IsDisplayMath.Should().BeTrue();

	[Fact]
	public void RendersDisplayMathDiv() => Html.Should().Contain("<div class=\"math\">");
}

public class MathBlockWithLabelTests(ITestOutputHelper output) : DirectiveTest<MathBlock>(output,
"""
:::{math}
:label: einstein-mass-energy
E = mc^2
:::
"""
)
{
	[Fact]
	public void ParsesMathBlockWithLabel() => Block.Should().NotBeNull();

	[Fact]
	public void ExtractsLabel() => Block!.Label.Should().Be("einstein-mass-energy");

	[Fact]
	public void RendersWithId() => Html.Should().Contain("id=\"einstein-mass-energy\"");
}

public class MathBlockEmptyTests(ITestOutputHelper output) : DirectiveTest<MathBlock>(output,
"""
:::{math}
:::
"""
)
{
	[Fact]
	public void EmptyContentGeneratesError() =>
		Collector.Errors.Should().Be(1);

	[Fact]
	public void EmitsErrorForEmptyContent()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty().And.HaveCount(1);
		Collector.Diagnostics.Should().OnlyContain(d => d.Severity == Severity.Error);
		Collector.Diagnostics.Should()
			.OnlyContain(d => d.Message.StartsWith("Math directive requires content."));
	}
}

public class MathBlockComplexExpressionTests(ITestOutputHelper output) : DirectiveTest<MathBlock>(output,
"""
:::{math}
\begin{align}
\frac{\partial f}{\partial x} &= \lim_{h \to 0} \frac{f(x+h) - f(x)}{h} \\
\nabla \cdot \vec{E} &= \frac{\rho}{\epsilon_0}
\end{align}
:::
"""
)
{
	[Fact]
	public void ParsesComplexMathBlock() => Block.Should().NotBeNull();

	[Fact]
	public void ExtractsComplexContent() => Block!.Content.Should().Contain("\\begin{align}");

	[Fact]
	public void DeterminesDisplayMathFromBegin() => Block!.IsDisplayMath.Should().BeTrue();

	[Fact]
	public void RendersComplexMathDiv() => Html.Should().Contain("<div class=\"math\">");
}
