// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.Directives.VectorSizing;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

public class VectorSizingBlockTests(ITestOutputHelper output) : DirectiveTest<VectorSizingBlock>(output,
"""
:::{vector-sizing-calculator}
:::
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void SetsCorrectDirectiveType() => Block!.Directive.Should().Be("vector-sizing-calculator");

	[Fact]
	public void RendersCustomElement() => Html.Should().Contain("<vector-sizing-calculator>");
}
