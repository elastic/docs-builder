// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using Elastic.Markdown.Myst.Directives;
using FluentAssertions;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.Directives;

public class CardTests(ITestOutputHelper output) : DirectiveTest<CardBlock>(output,
"""
```{card} Card title
Card content
```
"""
)
{
	[Fact]
	public void ParsesBlock () => Block.Should().NotBeNull();

}

public class LinkCardTests(ITestOutputHelper output) : DirectiveTest<CardBlock>(output,
"""
```{card} Clickable Card
:link: https://elastic.co/docs

The entire card can be clicked to navigate to `Elastic Docs`.
```
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void ExposesLink() => Block!.Link.Should().Be("https://elastic.co/docs");

}
