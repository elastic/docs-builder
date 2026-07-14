// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Markdown.Myst.Directives.Changelog;

namespace Elastic.Markdown.Tests.Directives;

/// <summary>
/// Regression tests for PR/issue link formatting in flattened separated-type changelog output
/// (breaking changes, deprecations, known issues, highlights without <c>:dropdowns:</c>).
/// </summary>
public class ChangelogFlattenedLinksTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogFlattenedLinksTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: deprecation
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Update GET /api/status endpoint details
		  type: deprecation
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "268942"
		  - "202446"
		  issues:
		  - "199001"
		"""));

	[Fact]
	public void FlattenedDeprecationRendersMultipleLinksWithoutOuterBrackets()
	{
		var markdown = ChangelogInlineRenderer.RenderChangelogMarkdown(Block!);

		markdown.Should().Contain("[#268942](https://github.com/elastic/elasticsearch/pull/268942)");
		markdown.Should().Contain("[#202446](https://github.com/elastic/elasticsearch/pull/202446)");
		markdown.Should().Contain("[#199001](https://github.com/elastic/elasticsearch/issues/199001)");
		markdown.Should().NotContain("[ [#268942]");
		markdown.Should().NotContain("[#268942, #202446]");
	}

	[Fact]
	public void FlattenedDeprecationRendersClickableLinkHtml()
	{
		Html.Should().Contain("href=\"https://github.com/elastic/elasticsearch/pull/268942\"");
		Html.Should().Contain("href=\"https://github.com/elastic/elasticsearch/pull/202446\"");
		Html.Should().Contain("href=\"https://github.com/elastic/elasticsearch/issues/199001\"");
		Html.Should().NotContain("[#268942, #202446]");
	}
}
