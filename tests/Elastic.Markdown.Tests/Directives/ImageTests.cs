// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using Elastic.Markdown.Myst.Directives;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.Directives;

public class ImageBlockTests(ITestOutputHelper output) : DirectiveTest<ImageBlock>(output,
"""
```{image} /_static/img/observability.png
:alt: Elasticsearch
:width: 250px
```
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void ParsesBreakPoint()
	{
		Block!.Alt.Should().Be("Elasticsearch");
		Block!.Width.Should().Be("250px");
		Block!.ImageUrl.Should().Be("/_static/img/observability.png");
	}
}

public class FigureTests(ITestOutputHelper output) : DirectiveTest<ImageBlock>(output,
"""
```{figure} https://github.com/rowanc1/pics/blob/main/sunset.png?raw=true
:label: myFigure
:alt: Sunset at the beach
:align: center

Relaxing at the beach 🏝 🌊 😎
```
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void ParsesBreakPoint()
	{
	}
}
