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
	private static ApiCatalogEntry Entry(string key, string title, params string[] categories) =>
		new(key, title, $"/api/doc/{key}/") { CatalogCategories = categories };

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

	private static int PositionOf(string html, string key) => html.IndexOf($"href=\"/api/doc/{key}/\"", StringComparison.Ordinal);

	[Test]
	public async Task Render_Catalog_PlacesEachServerlessCardRightAfterItsParentExactlyOnce()
	{
		var html = await RenderAsync(
			Entry("logstash", "Logstash APIs"),
			Entry("kibana-serverless", "Kibana Serverless APIs", "serverless"),
			Entry("elasticsearch", "Elasticsearch API"),
			Entry("kibana", "Kibana APIs"),
			Entry("elasticsearch-serverless", "Elasticsearch Serverless API", "serverless")
		);

		var keys = new[] { "elasticsearch", "elasticsearch-serverless", "kibana", "kibana-serverless", "logstash" };
		foreach (var key in keys)
			Count(html, $"href=\"/api/doc/{key}/\"").Should().Be(1, $"{key} renders once");

		var positions = keys.Select(k => PositionOf(html, k)).ToArray();
		positions.Should().BeInAscendingOrder("each tag-on follows its parent, so the order holds when the cards stack");
	}

	[Test]
	public async Task Render_Catalog_KeepsOrphanedAndUnrelatedCardsVisible()
	{
		var html = await RenderAsync(
			Entry("elasticsearch", "Elasticsearch API"),
			Entry("kibana", "Kibana APIs"),
			Entry("logstash", "Logstash APIs"),
			Entry("elasticsearch-serverless", "Elasticsearch Serverless API", "serverless"),
			Entry("cloud", "Elastic Cloud API")
		);

		Count(html, "href=\"/api/doc/cloud/\"").Should().Be(1);
		Count(html, "api-catalog-card-attached").Should().Be(1, "only the Elasticsearch tag-on is attached");
		PositionOf(html, "elasticsearch-serverless").Should().BeLessThan(PositionOf(html, "kibana"));
	}
}
