// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst.Directives.Changelog;
using Markdig.Syntax;

namespace Elastic.Markdown.Tests.Directives;

/// <summary>
/// Mirrors the elastic-cloud-serverless index page: changelog directive at the top, manual release
/// sections below. Separated-type changelog TOC entries must survive page-level TOC merging.
/// </summary>
public class ChangelogIndexPageTocTests(ITestOutputHelper output) : DirectiveTest(output,
	// language=markdown
	"""
	# Serverless changelog [elastic-cloud-serverless-changelog]

	:::{changelog} /changelog/bundles
	:type: all
	:subsections:
	:::

	## April 30, 2026 [serverless-changelog-04302026]

	### Features and enhancements [serverless-changelog-04302026-features-enhancements]

	* Manual feature entry
	""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		fileSystem.AddFile("docs/changelog/bundles/2026-05-19.yaml", new MockFileData(
			// language=yaml
			"""
			products:
			- product: cloud-serverless
			  target: 2026-05-19
			  repo: kibana
			entries:
			- title: New feature
			  type: feature
			  products:
			  - product: cloud-serverless
			    target: 2026-05-19
			  prs:
			  - "111111"
			- title: Deprecated API
			  type: deprecation
			  products:
			  - product: cloud-serverless
			    target: 2026-05-19
			  description: Deprecated.
			  impact: Impact.
			  action: Action.
			  prs:
			  - "222222"
			"""));
	}

	[Fact]
	public void ChangelogBlockLoadsBundles()
	{
		var block = Document.Descendants<ChangelogBlock>().Single();
		block.Found.Should().BeTrue();
		block.LoadedBundles.Should().NotBeEmpty();
	}

	[Fact]
	public void PageTableOfContentsIncludesChangelogDeprecations()
	{
		var toc = File.PageTableOfContent.Values.ToList();

		toc.Should().Contain(t => t.Heading == "May 19, 2026" && t.Level == 2);
		toc.Should().Contain(t => t.Heading == "Deprecations" && t.Level == 3);
		toc.Should().Contain(t => t.Slug == "kibana-2026-05-19-deprecations");
	}

	[Fact]
	public void PageTableOfContentsRetainsManualSections()
	{
		var toc = File.PageTableOfContent.Values.ToList();

		toc.Should().Contain(t => t.Heading == "April 30, 2026");
		toc.Should().Contain(t => t.Slug == "serverless-changelog-04302026-features-enhancements");
	}
}
