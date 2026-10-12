// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Landing;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public class ApiCatalogCardTests
{
	private static ApiCatalogItem Item(string key = "elasticsearch", params string[] pills) =>
		new(
			key,
			"Elasticsearch API",
			"/docs/api/doc/elasticsearch",
			"<svg></svg>",
			"Elasticsearch provides REST APIs.",
			[.. pills.Select(p => new ApiCatalogDeployment(p, p))]
		);

	private static async Task<string> Render(
		ApiCatalogItem item,
		ApiCatalogCardKind kind = ApiCatalogCardKind.Standard,
		bool hasVariant = false,
		int? column = null,
		int headingLevel = 3
	) =>
		await _ApiCatalogCard.Create(new ApiCatalogCard(item, kind, hasVariant, column, headingLevel)).RenderAsync(
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

	[Test]
	[Arguments(ApiCatalogCardKind.Standard)]
	[Arguments(ApiCatalogCardKind.Featured)]
	[Arguments(ApiCatalogCardKind.Compact)]
	public async Task Render_Card_IsOneLinkThatWrapsTheWholeCardAndNothingElseIsALink(ApiCatalogCardKind kind)
	{
		var html = await Render(Item(pills: ["Self-managed", "Serverless"]), kind);

		html.Split("<a ").Length.Should().Be(2, "there is exactly one link in a card");
		html.Split("</a>").Length.Should().Be(2);
		html
			.IndexOf("<a ", StringComparison.Ordinal)
			.Should()
			.BeLessThan(html.IndexOf("<h3", StringComparison.Ordinal), "the heading is inside the link");
		html
			.IndexOf("</a>", StringComparison.Ordinal)
			.Should()
			.BeGreaterThan(html.IndexOf("Elasticsearch provides REST APIs.", StringComparison.Ordinal), "so is the description");
		html.Should().Contain("href=\"/docs/api/doc/elasticsearch\"");
	}

	[Test]
	public async Task Render_RegularCard_ShowsTheDeploymentPills() =>
		(await Render(Item(pills: ["Self-managed", "Serverless"]))).Should().Contain("api-catalog-tag").And.Contain("Serverless");

	[Test]
	[Arguments(ApiCatalogCardKind.Featured)]
	[Arguments(ApiCatalogCardKind.Compact)]
	public async Task Render_FeaturedOrCompactCard_LeavesOutTheDeploymentPills(ApiCatalogCardKind kind)
	{
		var html = await Render(Item(pills: ["Self-managed", "Serverless"]), kind);

		html.Should().NotContain("api-catalog-tag");
		html.Should().Contain("Elasticsearch provides REST APIs.");
	}

	[Test]
	[Arguments(true)]
	[Arguments(false)]
	public async Task Render_FeaturedCard_IsMarkedAsParentOnlyWhenATagOnIsAttached(bool hasVariant) =>
		(await Render(Item(), ApiCatalogCardKind.Featured, hasVariant))
			.Contains("api-catalog-card-parent", StringComparison.Ordinal)
			.Should()
			.Be(hasVariant);

	[Test]
	public async Task Render_CardWithAColumn_IsAttachedAndPlacedInThatColumn()
	{
		var html = await Render(Item(), ApiCatalogCardKind.Compact, column: 2);

		html.Should().Contain("api-catalog-card-attached");
		html.Should().Contain("--api-catalog-column: 2");
	}

	[Test]
	public async Task Render_CardWithoutAColumn_IsNotAttached()
	{
		var html = await Render(Item(), ApiCatalogCardKind.Compact);

		html.Should().NotContain("api-catalog-card-attached");
		html.Should().NotContain("--api-catalog-column");
	}

	[Test]
	[Arguments(ApiCatalogCardKind.Standard)]
	[Arguments(ApiCatalogCardKind.Compact)]
	public async Task Render_Card_TopAlignsTheIconWithTheTitle(ApiCatalogCardKind kind) =>
		(await Render(Item(), kind)).Should().Contain("flex h-full items-start");

	[Test]
	[Arguments(2)]
	[Arguments(3)]
	public async Task Render_Card_TitleUsesTheGivenHeadingLevel(int level)
	{
		var html = await Render(Item(), headingLevel: level);

		html.Should().Contain($"<h{level} id=\"api-catalog-title-elasticsearch\"");
		html.Should().Contain($"Elasticsearch API</h{level}>");
	}

	[Test]
	[Arguments("elasticsearch", "elasticsearch")]
	[Arguments("my api/v2", "my-api-v2")]
	public async Task Render_Card_LinkIsNamedByItsTitleWithAnIdThatIsValid(string key, string id)
	{
		var html = await Render(Item(key));

		html.Should().Contain($"aria-labelledby=\"api-catalog-title-{id}\"");
		html.Should().Contain($"<h3 id=\"api-catalog-title-{id}\"");
	}
}
