// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer._Partials.Layout;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.Documentation;
using Nullean.Snapshots.Html;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public class ApiTocRenderingTests
{
	[Test]
	public async Task Render_EmptyToc_OmitsViewAsMarkdownLink()
	{
		var html = await Render([]);

		html.Should().MatchInlineHtmlSnapshot("");
	}

	[Test]
	public async Task Render_WithTocItems_IncludesHeadings()
	{
		var html = await Render([new TocItem("Paths", "paths", 2)]);

		html.Should().MatchHtmlSnapshot();
	}

	private static async Task<string> Render(IReadOnlyList<TocItem> tocItems) =>
		await _ApiToc.Create(tocItems).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);
}
