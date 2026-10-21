// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer._Partials.Layout;
using Elastic.ApiExplorer.Infrastructure;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public class ApiTocRenderingTests
{
	[Test]
	public async Task Render_EmptyToc_OmitsViewAsMarkdownLink()
	{
		var html = await Render([]);

		html.Should().NotContain("View as Markdown");
		html.Should().NotContain("view-as-markdown");
		html.Should().NotContain("/api/doc/elasticsearch/v9.md");
		html.Should().NotContain("On this page");
	}

	[Test]
	public async Task Render_WithTocItems_IncludesHeadings()
	{
		var html = await Render([new ApiTocItem("Paths", "paths")]);

		html.Should().Contain("On this page");
		html.Should().Contain("href=\"#paths\"");
		html.Should().Contain("Paths");
	}

	private static async Task<string> Render(IReadOnlyList<ApiTocItem> tocItems) =>
		await _ApiToc.Create(tocItems).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);
}
