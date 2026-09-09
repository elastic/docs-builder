// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Markdown.Myst.Directives.Changelog;

namespace Elastic.Markdown.Tests.Directives;

public class ChangelogFeatureAndEnhancementSectionsTests(ITestOutputHelper output) : DirectiveTest<ChangelogBlock>(
	output,
	"""
	:::{changelog}
	:::
	"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Brand new capability
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			- title: Improved existing capability
			  type: enhancement
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			- title: Bug fix
			  type: bug-fix
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			"""
			)
		);

	[Fact]
	public void RendersSeparateFeaturesHeading() => Html.Should().Contain(">Features<");

	[Fact]
	public void RendersSeparateEnhancementsHeading() => Html.Should().Contain(">Enhancements<");

	[Fact]
	public void OmitsCombinedHeading() => Html.Should().NotContain("Features and enhancements");

	[Fact]
	public void FeaturesAppearBeforeEnhancements()
	{
		var featuresIdx = Html.IndexOf(">Features<", StringComparison.Ordinal);
		var enhancementsIdx = Html.IndexOf(">Enhancements<", StringComparison.Ordinal);
		featuresIdx.Should().BeLessThan(enhancementsIdx);
	}

	[Fact]
	public void TocIncludesBothSections()
	{
		var toc = Block!.GeneratedTableOfContent.ToList();
		toc.Should().Contain(t => t.Heading == "Features" && t.Slug.EndsWith("-features"));
		toc.Should().Contain(t => t.Heading == "Enhancements" && t.Slug.EndsWith("-enhancements"));
	}
}

public class ChangelogEnhancementsOnlyOmitsFeaturesSectionTests(ITestOutputHelper output) : DirectiveTest<ChangelogBlock>(
	output,
	"""
	:::{changelog}
	:::
	"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile(
			"docs/changelog/bundles/9.3.6.yaml",
			new MockFileData(
				"""
			products:
			- product: elasticsearch
			  target: 9.3.6
			entries:
			- title: Faster existing query
			  type: enhancement
			  products:
			  - product: elasticsearch
			    target: 9.3.6
			"""
			)
		);

	[Fact]
	public void RendersEnhancements() => Html.Should().Contain(">Enhancements<");

	[Fact]
	public void OmitsEmptyFeaturesSection() => Html.Should().NotContain(">Features<");

	[Fact]
	public void TocOmitsFeatures()
	{
		var toc = Block!.GeneratedTableOfContent.ToList();
		toc.Should().NotContain(t => t.Heading == "Features");
		toc.Should().Contain(t => t.Heading == "Enhancements");
	}
}

public class ChangelogFeaturesOnlyOmitsEnhancementsSectionTests(ITestOutputHelper output) : DirectiveTest<ChangelogBlock>(
	output,
	"""
	:::{changelog}
	:::
	"""
)
{
	protected override void AddToFileSystem(MockFileSystem fileSystem) =>
		fileSystem.AddFile(
			"docs/changelog/bundles/9.4.0.yaml",
			new MockFileData(
				"""
			products:
			- product: elasticsearch
			  target: 9.4.0
			entries:
			- title: Entirely new API
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.4.0
			"""
			)
		);

	[Fact]
	public void RendersFeatures() => Html.Should().Contain(">Features<");

	[Fact]
	public void OmitsEmptyEnhancementsSection() => Html.Should().NotContain(">Enhancements<");
}
