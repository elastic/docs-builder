// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.Documentation.Site.FileProviders;

namespace Elastic.ApiExplorer.Tests;

[ClassDataSource<ApiExplorerFixture>(Shared = SharedType.PerClass)]
public class ApiCatalogViewRenderingTests(ApiExplorerFixture fixture)
{
	private static ApiCatalogEntry Entry(string key, string title) => new(key, title, $"/api/{key}/");

	private async Task<string> RenderAsync(params ApiCatalogEntry[] entries)
	{
		var context = new ApiRenderContext(
			fixture.Context,
			fixture.Document,
			new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(fixture.Context))
		)
		{ NavigationHtml = string.Empty, CurrentNavigation = fixture.Navigation, MarkdownRenderer = PassthroughMarkdownRenderer.Instance };

		var fs = new MockFileSystem();
		await using (var stream = fs.FileStream.New("/out.html", FileMode.Create, FileAccess.Write))
			await new ApiCatalog { Entries = entries }.RenderAsync(stream, context, TestContext.Current!.Execution.CancellationToken);

		return fs.File.ReadAllText("/out.html");
	}

	private static int Count(string html, string value) => html.Split(value).Length - 1;

	private static int PositionOf(string html, string key) => html.IndexOf($"href=\"/api/{key}/\"", StringComparison.Ordinal);

	[Test]
	public async Task Render_Catalog_PlacesEachServerlessCardRightAfterItsParentExactlyOnce()
	{
		var html = await RenderAsync(
			Entry("logstash", "Logstash APIs"),
			Entry("kibana-serverless", "Kibana Serverless APIs"),
			Entry("elasticsearch", "Elasticsearch API"),
			Entry("kibana", "Kibana APIs"),
			Entry("elasticsearch-serverless", "Elasticsearch Serverless API")
		);

		var keys = new[] { "elasticsearch", "elasticsearch-serverless", "kibana", "kibana-serverless", "logstash" };
		foreach (var key in keys)
			Count(html, $"href=\"/api/{key}/\"").Should().Be(1, $"{key} renders once");

		var positions = keys.Select(k => PositionOf(html, k)).ToArray();
		positions.Should().BeInAscendingOrder("each tag-on follows its parent, so the order holds when the cards stack");
	}

	[Test]
	public async Task Render_Catalog_OrchestrationCardsUseTheFullSizeCard()
	{
		var html = await RenderAsync(
			Entry("elasticsearch", "Elasticsearch API"),
			Entry("kibana", "Kibana APIs"),
			Entry("logstash", "Logstash APIs"),
			Entry("elasticsearch-serverless", "Elasticsearch Serverless API"),
			Entry("cloud", "Elastic Cloud API"),
			Entry("cloud-enterprise", "Elastic Cloud Enterprise API")
		);

		Count(html, "api-catalog-card-attached").Should().Be(1, "only the Serverless tag-on is attached");
		Count(html, "text-sm").Should().Be(1, "every other card takes its size from the catalog stylesheet, and only the tag-on is small");
	}

	[Test]
	public async Task Render_Catalog_TwoFeaturedCards_KeepEveryGridAtTwoColumns()
	{
		var html = await RenderAsync(
			Entry("elasticsearch", "Elasticsearch API"),
			Entry("kibana", "Kibana APIs"),
			Entry("cloud", "Elastic Cloud API")
		);

		Count(html, "sm:grid-cols-2").Should().Be(2, "the featured row and the Orchestration row both step to two");
		html.Should().NotContain("min-[1024px]:grid-cols-3");
	}

	[Test]
	public async Task Render_Catalog_TagOnSharesTheCellOfItsParent()
	{
		var html = await RenderAsync(
			Entry("elasticsearch", "Elasticsearch API"),
			Entry("kibana", "Kibana APIs"),
			Entry("logstash", "Logstash APIs"),
			Entry("elasticsearch-serverless", "Elasticsearch Serverless API")
		);

		Count(html, "api-catalog-cell-tagged").Should().Be(1, "only the cell with a tag-on spans two rows");
		var between = html[PositionOf(html, "elasticsearch")..PositionOf(html, "elasticsearch-serverless")];
		between.Should().NotContain("<li", "the tag-on is in the same list item as its parent, so the grid cannot separate them");
	}

	[Test]
	public async Task Render_Catalog_KeepsOrphanedAndUnrelatedCardsVisible()
	{
		var html = await RenderAsync(
			Entry("elasticsearch", "Elasticsearch API"),
			Entry("kibana", "Kibana APIs"),
			Entry("logstash", "Logstash APIs"),
			Entry("elasticsearch-serverless", "Elasticsearch Serverless API"),
			Entry("cloud", "Elastic Cloud API")
		);

		Count(html, "href=\"/api/cloud/\"").Should().Be(1);
		Count(html, "api-catalog-card-attached").Should().Be(1, "only the Elasticsearch tag-on is attached");
		PositionOf(html, "elasticsearch-serverless").Should().BeLessThan(PositionOf(html, "kibana"));
	}
}
