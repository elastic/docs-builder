// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Myst.Directives;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

public class ImageBlockTests(ITestOutputHelper output) : DirectiveTest<ImageBlock>(output,
"""
:::{image} img/observability.png
:alt: Elasticsearch
:width: 250px
:screenshot:
:::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile(@"docs/img/observability.png", "");

	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void ParsesBreakPoint()
	{
		Block!.Alt.Should().Be("Elasticsearch");
		Block!.Width.Should().Be("250px");
		Block!.ImageUrl.Should().Be("img/observability.png");
		Block!.Screenshot.Should().Be("screenshot");
	}

	[Fact]
	public void ImageIsFoundSoNoErrorIsEmitted()
	{
		Block!.Found.Should().BeTrue();
		Collector.Diagnostics.Count.Should().Be(0);
	}
}

public class FigureTests(ITestOutputHelper output) : DirectiveTest<ImageBlock>(output,
"""
:::{figure} https://github.com/rowanc1/pics/blob/main/sunset.png?raw=true
:label: myFigure
:alt: Sunset at the beach
:align: center

Relaxing at the beach 🏝 🌊 😎
:::
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void WarnsOnExternalUri()
	{
		Block!.Found.Should().BeTrue();

		Collector.Diagnostics.Should().HaveCount(1)
			.And.OnlyContain(d => d.Severity == Severity.Warning);
	}
}
