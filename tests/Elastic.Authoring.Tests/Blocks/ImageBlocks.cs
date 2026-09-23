// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using static Elastic.Authoring.Tests.Framework.TestFile;

namespace Elastic.Authoring.Tests.Blocks.ImageBlocks;

public class StaticPathToImage : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{image} img/observability.png
		:alt: Elasticsearch
		:width: 250px
		:screenshot:
		:::
		""";

	[Fact(DisplayName = "validate src is anchored")]
	public async Task ValidateSrcIsAnchored() =>
		await Docs.ConvertsToContainingHtml(
			"""
		<img loading="lazy" title="Elasticsearch" alt="Elasticsearch" src="/img/observability.png" style="width: 250px;" class="screenshot">
		"""
		);
}

public class SupportsUrlPathPrefix : GeneratorTest
{
	protected override SetupOptions Options => new() { UrlPathPrefix = "/docs" };

	protected override IReadOnlyCollection<TestFile> Files =>
		[
			Static("img/observability.png"),
			Index(
				"""
			# Testing nested inline anchors
			:::{image} img/observability.png
			:alt: Elasticsearch
			:width: 250px
			:screenshot:
			:::
			"""
			),
			Page(
				"folder/relative.md",
				"""
			:::{image} ../img/observability.png
			:alt: Elasticsearch
			:width: 250px
			:screenshot:
			:::
			"""
			)
		];

	[Fact(DisplayName = "validate image src contains prefix")]
	public async Task ValidateImageSrcContainsPrefix() =>
		await Docs.ConvertsToContainingHtml(
			"""
		<img loading="lazy" title="Elasticsearch" alt="Elasticsearch" src="/docs/img/observability.png" style="width: 250px;" class="screenshot">
		"""
		);

	[Fact(DisplayName = "validate image src contains prefix when referenced relatively")]
	public async Task ValidateRelativeReference() =>
		await Docs.Converts("folder/relative.md").ContainsHtml(
			"""
			<img loading="lazy" title="Elasticsearch" alt="Elasticsearch" src="/docs/img/observability.png" style="width: 250px;" class="screenshot">
			"""
		);

	[Fact(DisplayName = "has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class ImageRefOutOfScope : GeneratorTest
{
	protected override SetupOptions Options => new() { UrlPathPrefix = "/docs" };

	protected override IReadOnlyCollection<TestFile> Files =>
		[
			Static("img/observability.png"),
			Index(
				"""
			# Testing nested inline anchors
			:::{image} ../img/observability.png
			:alt: Elasticsearch
			:width: 250px
			:screenshot:
			:::
			"""
			)
		];

	[Fact(DisplayName = "validate image src contains prefix and is anchored to documentation scope root")]
	public async Task ValidateSrcAnchored() =>
		await Docs.ConvertsToContainingHtml(
			"""
		<img loading="lazy" title="Elasticsearch" alt="Elasticsearch" src="/docs/img/observability.png" style="width: 250px;" class="screenshot">
		"""
		);

	[Fact(DisplayName = "emits an error image reference is outside of documentation scope")]
	public async Task EmitsError() => await Docs.HasError("./img/observability.png` does not exist. resolved to");
}

public class EmptyAltAttribute : MarkdownTest
{
	protected override string Markdown => """
		:::{image} img/some-image.png
		:alt:
		:width: 250px
		:::
		""";

	[Fact(DisplayName = "validate empty alt attribute")]
	public async Task ValidateEmptyAlt() =>
		await Docs.ConvertsToContainingHtml("""
		<img loading="lazy" alt src="/img/some-image.png" style="width: 250px;">
		""");
}
