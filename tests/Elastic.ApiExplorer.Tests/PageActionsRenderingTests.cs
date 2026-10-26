// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer._Partials;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.Documentation.Site;
using RazorSlices;
using DesignSystemPageActions = Elastic.Documentation.Site.Components.Navigation.PageActions;

namespace Elastic.ApiExplorer.Tests;

public class PageActionsRenderingTests
{
	[Test]
	public async Task Render_SplitButton_CopiesPageAndOpensMarkdown()
	{
		var html = await _PageActions.Create(
			new ApiPageActionsModel("/api/doc/elasticsearch/operation/operation-search.md", null)
		).RenderAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);

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

	[Test]
	public async Task Render_WithSpec_ListsJsonAndYamlDownloads()
	{
		var spec = new ApiSpecDownload("/api/doc/elasticsearch.json", "/api/doc/elasticsearch.yaml");
		var html = await _PageActions.Create(new ApiPageActionsModel("/api/doc/elasticsearch.md", spec)).RenderAsync(
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		html.Should().Contain("Download OpenAPI spec (JSON)").And.Contain("href=\"/api/doc/elasticsearch.json\"");
		html.Should().Contain("Download OpenAPI spec (YAML)").And.Contain("href=\"/api/doc/elasticsearch.yaml\"");
		html.Should().Contain("download");
	}

	[Test]
	public async Task Render_WithoutSpec_HasNoDownloads()
	{
		var html = await _PageActions.Create(new ApiPageActionsModel("/api/doc/elasticsearch.md", null)).RenderAsync(
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		html.Should().NotContain("Download OpenAPI spec");
	}

	[Test]
	public async Task Render_WithoutMarkdown_OffersTheDownloadsAlone()
	{
		var spec = new ApiSpecDownload("/api/doc/elasticsearch.json", "/api/doc/elasticsearch.yaml");
		var html = await _PageActions.Create(new ApiPageActionsModel(null, spec)).RenderAsync(
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		html
			.Should()
			.Contain("Download spec")
			.And
			.Contain("href=\"/api/doc/elasticsearch.json\"")
			.And
			.Contain("href=\"/api/doc/elasticsearch.yaml\"");
		html.Should().NotContain("Copy page").And.NotContain("data-copy-page").And.NotContain("View as Markdown");
		html.Should().NotContain("role=\"separator\"");
	}

	[Test]
	public async Task DesignSystemRail_ListsTheDownloadsAsDownloadLinks()
	{
		var model = new DesignSystemPageActions.PageActionsModel
		{
			MarkdownUrl = "/api/doc/elasticsearch.md",
			Downloads = [new("Download OpenAPI spec (JSON)", "/api/doc/elasticsearch.json")]
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

		html.Should().Contain("View as Markdown").And.Contain("Download OpenAPI spec (JSON)");
		html.Should().Contain("href=\"/api/doc/elasticsearch.json\"").And.Contain("download");
	}
}
