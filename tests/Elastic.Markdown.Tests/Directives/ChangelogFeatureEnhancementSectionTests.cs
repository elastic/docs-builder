// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Markdown.Myst.Directives.Changelog;

namespace Elastic.Markdown.Tests.Directives;

[InheritsTests]
public class ChangelogFeatureAndEnhancementSectionsTests() : DirectiveTest<ChangelogBlock>("""
	:::{changelog}
	:::
	""")
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

	[Test]
	public void RendersSeparateFeaturesHeading() => Html.Should().Contain(">Features<");

	[Test]
	public void RendersSeparateEnhancementsHeading() => Html.Should().Contain(">Enhancements<");

	[Test]
	public void OmitsCombinedHeading() => Html.Should().NotContain("Features and enhancements");

	[Test]
	public void FeaturesAppearBeforeEnhancements()
	{
		var featuresIdx = Html.IndexOf(">Features<", StringComparison.Ordinal);
		var enhancementsIdx = Html.IndexOf(">Enhancements<", StringComparison.Ordinal);
		featuresIdx.Should().BeLessThan(enhancementsIdx);
	}

	[Test]
	public void TocIncludesBothSections()
	{
		var toc = Block!.GeneratedTableOfContent.ToList();
		toc.Should().Contain(t => t.Heading == "Features" && t.Slug.EndsWith("-features"));
		toc.Should().Contain(t => t.Heading == "Enhancements" && t.Slug.EndsWith("-enhancements"));
	}
}

[InheritsTests]
public class ChangelogEnhancementsOnlyOmitsFeaturesSectionTests() : DirectiveTest<ChangelogBlock>("""
	:::{changelog}
	:::
	""")
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

	[Test]
	public void RendersEnhancements() => Html.Should().Contain(">Enhancements<");

	[Test]
	public void OmitsEmptyFeaturesSection() => Html.Should().NotContain(">Features<");

	[Test]
	public void TocOmitsFeatures()
	{
		var toc = Block!.GeneratedTableOfContent.ToList();
		toc.Should().NotContain(t => t.Heading == "Features");
		toc.Should().Contain(t => t.Heading == "Enhancements");
	}
}

[InheritsTests]
public class ChangelogFeaturesOnlyOmitsEnhancementsSectionTests() : DirectiveTest<ChangelogBlock>("""
	:::{changelog}
	:::
	""")
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

	[Test]
	public void RendersFeatures() => Html.Should().Contain(">Features<");

	[Test]
	public void OmitsEmptyEnhancementsSection() => Html.Should().NotContain(">Enhancements<");
}
