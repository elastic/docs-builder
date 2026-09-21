// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Myst.Directives.Math;

namespace Elastic.Markdown.Tests.Directives;

public class MathBlockTests() : DirectiveTest<MathBlock>("""
:::{math}
E = mc^2
:::
""")
{
	[Test]
	public void ParsesMathBlock() => Block.Should().NotBeNull();

	[Test]
	public void SetsCorrectDirectiveType() => Block!.Directive.Should().Be("math");

	[Test]
	public void ExtractsContent() => Block!.Content.Should().Be("E = mc^2");

	[Test]
	public void DeterminesInlineMath() => Block!.IsDisplayMath.Should().BeFalse();

	[Test]
	public void RendersMathSpan() => Html.Should().Contain("<span class=\"math\">E = mc^2</span>");
}

public class MathBlockDisplayMathTests() : DirectiveTest<MathBlock>(
	"""
:::{math}
\[
\int_{-\infty}^{\infty} e^{-x^2} dx = \sqrt{\pi}
\]
:::
"""
)
{
	[Test]
	public void ParsesDisplayMathBlock() => Block.Should().NotBeNull();

	[Test]
	public void ExtractsDisplayMathContent() => Block!.Content.Should().Contain("\\int_{-\\infty}^{\\infty}");

	[Test]
	public void DeterminesDisplayMath() => Block!.IsDisplayMath.Should().BeTrue();

	[Test]
	public void RendersDisplayMathDiv() => Html.Should().Contain("<div class=\"math\">");
}

public class MathBlockWithLabelTests() : DirectiveTest<MathBlock>("""
:::{math}
:label: einstein-mass-energy
E = mc^2
:::
""")
{
	[Test]
	public void ParsesMathBlockWithLabel() => Block.Should().NotBeNull();

	[Test]
	public void ExtractsLabel() => Block!.Label.Should().Be("einstein-mass-energy");

	[Test]
	public void RendersWithId() => Html.Should().Contain("id=\"einstein-mass-energy\"");
}

public class MathBlockEmptyTests() : DirectiveTest<MathBlock>("""
:::{math}
:::
""")
{
	[Test]
	public void EmptyContentGeneratesError() => Collector.Errors.Should().Be(1);

	[Test]
	public void EmitsErrorForEmptyContent()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty().And.HaveCount(1);
		Collector.Diagnostics.Should().OnlyContain(d => d.Severity == Severity.Error);
		Collector.Diagnostics.Should().OnlyContain(d => d.Message.StartsWith("Math directive requires content."));
	}
}

public class MathBlockComplexExpressionTests() : DirectiveTest<MathBlock>(
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
	[Test]
	public void ParsesComplexMathBlock() => Block.Should().NotBeNull();

	[Test]
	public void ExtractsComplexContent() => Block!.Content.Should().Contain("\\begin{align}");

	[Test]
	public void DeterminesDisplayMathFromBegin() => Block!.IsDisplayMath.Should().BeTrue();

	[Test]
	public void RendersComplexMathDiv() => Html.Should().Contain("<div class=\"math\">");
}
