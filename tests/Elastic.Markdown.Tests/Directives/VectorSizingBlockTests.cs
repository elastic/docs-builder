// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Myst.Directives.VectorSizing;

namespace Elastic.Markdown.Tests.Directives;

public class VectorSizingBlockTests() : DirectiveTest<VectorSizingBlock>("""
:::{vector-sizing-calculator}
:::
""")
{
	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void SetsCorrectDirectiveType() => Block!.Directive.Should().Be("vector-sizing-calculator");

	[Test]
	public void RendersCustomElement() => Html.Should().Contain("<vector-sizing-calculator>");
}
