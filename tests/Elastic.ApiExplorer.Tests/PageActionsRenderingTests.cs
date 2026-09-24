// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer._Partials;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public class PageActionsRenderingTests
{
	[Test]
	public async Task Render_SplitButton_CopiesPageAndOpensMarkdown()
	{
		var html = await _PageActions.Create("/api/doc/elasticsearch/operation/operation-search.md").RenderAsync(
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		html.Should().Contain("class=\"api-page-actions\"");
		html.Should().Contain("class=\"api-page-actions-split\"");
		html.Should().Contain("class=\"api-page-actions-button\"");
		html.Should().Contain("class=\"api-page-actions-chevron\"");
		html.Should().Contain("class=\"api-page-actions-icon\"");
		html.Should().Contain("class=\"api-page-actions-label\"");
		html.Should().Contain("Copy page");
		html.Should().Contain("View as Markdown");
		html.Should().Contain("data-copy-page=\"/api/doc/elasticsearch/operation/operation-search.md\"");
		html.Should().Contain("href=\"/api/doc/elasticsearch/operation/operation-search.md\"");
		html.Should().Contain("target=\"_blank\"");
		html.Should().Contain("hx-boost=\"false\"");
		html.Should().Contain("nav-select-dropdown");
		html.Should().NotContain("<select");
		html.Should().NotContain("<option");
		html.Should().NotContain("view-as-markdown");
	}
}
