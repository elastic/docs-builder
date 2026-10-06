// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer._Partials;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public class ApiCodeBlockRenderingTests
{
	private static CancellationToken Ct => TestContext.Current!.Execution.CancellationToken;

	[Test]
	public async Task Render_CodeBlock_FollowsSharedMarkupContract()
	{
		var html = await _ApiCodeBlock.Create(new ApiCodeBlockModel("language-json", "{}")).RenderAsync(cancellationToken: Ct);

		html.Should().Contain("class=\"highlight-json notranslate\"");
		html.Should().Contain("<div class=\"highlight\">");
		html.Should().Contain("<pre><code class=\"language-json\">");
		html.Should().NotContain("data-line-numbers");
	}

	[Test]
	public async Task Render_CodeBlock_OptsIntoLineNumbers()
	{
		var html = await _ApiCodeBlock.Create(new ApiCodeBlockModel("language-json", "{}", LineNumbers: true)).RenderAsync(
			cancellationToken: Ct
		);

		html.Should().Contain("data-line-numbers");
	}

	[Test]
	public async Task Render_CodeSample_ExposesCardActionsAndPanels()
	{
		var samples = new[]
		{
			new CodeSample("Console", "GET /", "language-console"),
			new CodeSample("Python", "client.get()", "language-python")
		};

		var html = await _ApiCodeSample.Create(new ApiCodeSampleModel("rail-x", samples)).RenderAsync(cancellationToken: Ct);

		html.Should().Contain("data-code-card");
		html.Should().Contain("data-code-actions");
		html.Should().Contain("data-code-panel=\"Console\"");
		html.Should().Contain("data-code-panel=\"Python\"");
		html.Should().Contain("data-line-numbers");
	}

	[Test]
	public async Task Render_CodeSample_GivesEachPanelItsOwnHighlightLanguage()
	{
		var samples = new[]
		{
			new CodeSample("Console", "GET /", CodeSample.GetHighlightClass("Console")),
			new CodeSample("Python", "client.get()", CodeSample.GetHighlightClass("Python")),
			new CodeSample("curl", "curl localhost", CodeSample.GetHighlightClass("curl"))
		};

		var html = await _ApiCodeSample.Create(new ApiCodeSampleModel("rail-x", samples)).RenderAsync(cancellationToken: Ct);

		html.Should().Contain("<code class=\"language-console\">");
		html.Should().Contain("<code class=\"language-python\">");
		html.Should().Contain("<code class=\"language-curl\">");
		html.Should().Contain("class=\"highlight-python notranslate\"");
	}
}
