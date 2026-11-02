// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Myst.Directives.S3CostCalculator;

namespace Elastic.Markdown.Tests.Directives;

[InheritsTests]
public class S3CostCalculatorBlockTests() : DirectiveTest<S3CostCalculatorBlock>("""
:::{s3-cost-calculator}
:::
""")
{
	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void SetsCorrectDirectiveType() => Block!.Directive.Should().Be("s3-cost-calculator");

	[Test]
	public void RendersCustomElement() => Html.Should().Contain("<s3-cost-calculator>");
}
