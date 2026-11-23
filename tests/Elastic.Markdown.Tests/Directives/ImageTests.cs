// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Testing;
using Elastic.Markdown.Myst.Directives.Image;
using Nullean.Snapshots;
using Nullean.Snapshots.Html;

namespace Elastic.Markdown.Tests.Directives;

[InheritsTests]
public class ImageBlockTests() : DirectiveTest<ImageBlock>(
	"""
:::{image} img/observability.png
:alt: Elasticsearch
:width: 250px
:screenshot:
:::
"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) => fileSystem.AddFile(@"docs/img/observability.png", "");

	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void ParsesBreakPoint()
	{
		Block!.Alt.Should().Be("Elasticsearch");
		Block!.Width.Should().Be("250px");
		Block!.ImageUrl.Should().Be("/img/observability.png");
		Block!.Screenshot.Should().Be("screenshot");
	}

	[Test]
	public void ImageIsFoundSoNoErrorIsEmitted()
	{
		Block!.Found.Should().BeTrue();
		Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
	}
}

[InheritsTests]
public class ImageAltWithQuoteTests() : DirectiveTest<ImageBlock>("""
:::{image} img/observability.png
:alt: Say "hi"
:::
""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) => fileSystem.AddFile(@"docs/img/observability.png", "");

	[Test]
	public void QuoteInAltStaysInsideTheAttributes() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<a class="reference internal image-reference" href="javascript:void(0)" data-modal-id="modal-D9161EE2">
			<img loading="lazy" title="Say &quot;hi&quot;" alt="Say &quot;hi&quot;" src="/img/observability.png" style="">
		</a>
		<dialog id="modal-D9161EE2" class="modal" aria-label="Say &quot;hi&quot;">
			<div class="modal-content">
				<form method="dialog" class="modal-close">
					<button type="submit" class="close-button" aria-label="Close">×</button>
				</form>
				<a class="reference internal image-reference" href="/img/observability.png" target="_blank">
					<img loading="lazy" title="Say &quot;hi&quot;" alt="Say &quot;hi&quot;" src="/img/observability.png">
				</a>
			</div>
		</dialog>
		"""
		);
}

[InheritsTests]
public class ImageScreenshotInsideListInAppliesSwitchTests() : DirectiveTest<ImageBlock>(
	"""
	:::::{applies-switch}
	::::{applies-item} serverless: ga

	1. Customize the lifecycle.

	   :::{image} img/observability.png
	   :alt: Elasticsearch
	   :width: 250px
	   :screenshot:
	   :::

	::::
	:::::
	"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) => fileSystem.AddFile(@"docs/img/observability.png", "");

	[Test]
	public void ScreenshotReachesTheImage()
	{
		Block!.Alt.Should().Be("Elasticsearch");
		Block!.Width.Should().Be("250px");
		Block!.Screenshot.Should().Be("screenshot");
	}
}

[InheritsTests]
public class AllowedExternalHostTests() : DirectiveTest<ImageBlock>(
	"""
:::{image} https://images.contentstack.io/v3/assets/bltefdd0b53724fa2ce/blt/example.gif
:alt: An animated screenshot hosted on the Elastic Contentstack CDN
:::
"""
)
{
	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void AllowedHostDoesNotWarn()
	{
		Block!.Found.Should().BeTrue();
		Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
	}
}

[InheritsTests]
public class FigureTests() : DirectiveTest<ImageBlock>(
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
	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void WarnsOnExternalUri()
	{
		Block!.Found.Should().BeTrue();

		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"warning {repo}/docs/index.md:3 image is using an external URI: https://github.com/rowanc1/pics/blob/main/sunset.png?raw=true "
			);
	}
}
