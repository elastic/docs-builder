// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer._Partials;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.Documentation.Site;
using Nullean.Snapshots.Html;
using RazorSlices;
using DesignSystemPageActions = Elastic.Documentation.Site.Components.Navigation.PageActions;

namespace Elastic.ApiExplorer.Tests;

public class PageActionsRenderingTests
{
	[Test]
	public async Task Render_SplitButton_CopiesPageAndOpensMarkdown()
	{
		var html = await _PageActions.Create(new ApiPageActionsModel("/api/elasticsearch/operation/operation-search.md", null)).RenderAsync(
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		html.Should().MatchHtmlSnapshot();
	}

	[Test]
	public async Task Render_WithSpec_ListsJsonAndYamlDownloads()
	{
		var spec = new ApiSpecDownload("/api/elasticsearch.json", "/api/elasticsearch.yaml");
		var html = await _PageActions.Create(new ApiPageActionsModel("/api/elasticsearch.md", spec)).RenderAsync(
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		html.Should().MatchHtmlSnapshot();
	}

	[Test]
	public async Task Render_WithoutSpec_HasNoDownloads()
	{
		var html = await _PageActions.Create(new ApiPageActionsModel("/api/elasticsearch.md", null)).RenderAsync(
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		html.Should().MatchHtmlSnapshot();
	}

	[Test]
	public async Task DesignSystemRail_ListsTheDownloadsAsDownloadLinks()
	{
		var model = new DesignSystemPageActions.PageActionsModel
		{
			MarkdownUrl = "/api/elasticsearch.md",
			Downloads = [new("Download OpenAPI spec (JSON)", "/api/elasticsearch.json")]
		};

		var html = await Elastic
			.Documentation
			.Site
			.Components
			.Navigation
			.PageActions
			._PageActions
			.Create(model)
			.RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

		html.Should().MatchHtmlSnapshot();
	}
}
