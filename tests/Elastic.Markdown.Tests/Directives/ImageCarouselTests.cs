// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Myst.Directives;
using Elastic.Markdown.Myst.Directives.Image;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

public class ImageCarouselBlockTests(ITestOutputHelper output) : DirectiveTest<ImageCarouselBlock>(output,
"""
:::{carousel}
:max-height: medium

```{image} img/image1.png
:alt: First image
```

```{image} img/image2.png
:alt: Second image
```
:::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		fileSystem.AddFile(@"docs/img/image1.png", "");
		fileSystem.AddFile(@"docs/img/image2.png", "");
	}

	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void ParsesCarouselProperties()
	{
		Block!.MaxHeight.Should().Be("medium");
	}

	[Fact]
	public void ProcessesNestedImages()
	{
		Block!.Images.Should().HaveCount(2);
		Block!.Images[0].Alt.Should().Be("First image");
		Block!.Images[0].ImageUrl.Should().Be("/img/image1.png");
		Block!.Images[1].Alt.Should().Be("Second image");
		Block!.Images[1].ImageUrl.Should().Be("/img/image2.png");
	}

	[Fact]
	public void AllImagesFoundSoNoErrorIsEmitted()
	{
		Block!.Images.Should().AllSatisfy(img => img.Found.Should().BeTrue());
		Collector.Diagnostics.Count.Should().Be(0);
	}
}

public class ImageCarouselWithSmallHeightTests(ITestOutputHelper output) : DirectiveTest<ImageCarouselBlock>(output,
"""
:::{carousel}
:max-height: small

```{image} img/small.png
:alt: Small image
```
:::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile(@"docs/img/small.png", "");

	[Fact]
	public void ParsesSmallMaxHeight()
	{
		Block!.MaxHeight.Should().Be("small");
		Collector.Diagnostics.Count.Should().Be(0);
	}
}

public class ImageCarouselWithAutoHeightTests(ITestOutputHelper output) : DirectiveTest<ImageCarouselBlock>(output,
"""
:::{carousel}
:max-height: none

```{image} img/auto.png
:alt: Auto height image
```
:::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile(@"docs/img/auto.png", "");

	[Fact]
	public void ParsesNoneMaxHeight()
	{
		Block!.MaxHeight.Should().Be("none");
		Collector.Diagnostics.Count.Should().Be(0);
	}
}

public class ImageCarouselWithInvalidHeightTests(ITestOutputHelper output) : DirectiveTest<ImageCarouselBlock>(output,
"""
:::{carousel}
:max-height: large

```{image} img/invalid.png
:alt: Invalid height image
```
:::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile(@"docs/img/invalid.png", "");

	[Fact]
	public void WarnsOnInvalidMaxHeight()
	{
		Block!.MaxHeight.Should().Be("large");

		Collector.Diagnostics.Should().HaveCount(1)
			.And.OnlyContain(d => d.Severity == Severity.Warning);

		var warning = Collector.Diagnostics.First();
		warning.Message.Should().Contain("Invalid max-height value 'large'");
		warning.Message.Should().Contain("Valid options are: none, small, medium");
	}
}

public class ImageCarouselWithoutImagesTests(ITestOutputHelper output) : DirectiveTest<ImageCarouselBlock>(output,
"""
:::{carousel}
:::
"""
)
{
	[Fact]
	public void EmitsErrorForEmptyCarousel()
	{
		Block!.Images.Should().BeEmpty();

		Collector.Diagnostics.Should().HaveCount(1)
			.And.OnlyContain(d => d.Severity == Severity.Error);

		var error = Collector.Diagnostics.First();
		error.Message.Should().Be("carousel directive requires nested image directives");
	}
}

public class ImageCarouselMinimalTests(ITestOutputHelper output) : DirectiveTest<ImageCarouselBlock>(output,
"""
:::{carousel}

```{image} img/minimal.png
:alt: Minimal carousel
```
:::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile(@"docs/img/minimal.png", "");

	[Fact]
	public void ParsesMinimalCarousel()
	{
		Block!.MaxHeight.Should().BeNull();
		Block!.Images.Should().HaveCount(1);
		Block!.Images[0].Alt.Should().Be("Minimal carousel");
		Collector.Diagnostics.Count.Should().Be(0);
	}
}

public class ImageCarouselWithMissingImageTests(ITestOutputHelper output) : DirectiveTest<ImageCarouselBlock>(output,
"""
:::{carousel}

```{image} img/missing.png
:alt: Missing image
```

```{image} img/exists.png
:alt: Existing image
```
:::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile(@"docs/img/exists.png", "");

	[Fact]
	public void HandlesPartiallyMissingImages()
	{
		Block!.Images.Should().HaveCount(2);
		Block!.Images[0].Found.Should().BeFalse(); // missing.png
		Block!.Images[1].Found.Should().BeTrue();  // exists.png

		// Should have diagnostics for the missing image
		Collector.Diagnostics.Should().HaveCount(1)
			.And.OnlyContain(d => d.Severity == Severity.Error);
	}
}
