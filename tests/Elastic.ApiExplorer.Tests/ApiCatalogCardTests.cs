// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Landing;
using Nullean.Snapshots.Html;
using RazorSlices;

namespace Elastic.ApiExplorer.Tests;

public class ApiCatalogCardTests
{
	private static ApiCatalogItem Item(string key = "elasticsearch") =>
		new(key, "Elasticsearch API", "/docs/api/elasticsearch", "<svg></svg>", "Elasticsearch provides REST APIs.");

	private static async Task<string> Render(
		ApiCatalogItem item,
		ApiCatalogCardKind kind = ApiCatalogCardKind.Standard,
		bool hasVariant = false,
		int headingLevel = 3
	) =>
		await _ApiCatalogCard.Create(new ApiCatalogCard(item, kind, hasVariant, headingLevel)).RenderAsync(
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

	[Test]
	[Arguments(ApiCatalogCardKind.Standard)]
	[Arguments(ApiCatalogCardKind.Featured)]
	[Arguments(ApiCatalogCardKind.Attached)]
	public async Task Render_Card_IsOneLinkThatWrapsTheWholeCardAndNothingElseIsALink(ApiCatalogCardKind kind)
	{
		var html = await Render(Item(), kind);

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
		html.Should().Contain("href=\"/docs/api/elasticsearch\"");
	}

	[Test]
	public async Task Render_FeaturedCard_IsParentOnlyWithAVariant()
	{
		var html = await Render(Item(), ApiCatalogCardKind.Featured, hasVariant: true);

		html.Should().MatchHtmlSnapshot();
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
	public async Task Render_AttachedCard_IsTheSmallTagOnUnderItsParent()
	{
		var html = await Render(Item(), ApiCatalogCardKind.Attached);

		html.Should().MatchHtmlSnapshot();
	}

	[Test]
	[Arguments(ApiCatalogCardKind.Standard)]
	[Arguments(ApiCatalogCardKind.Featured)]
	public async Task Render_CardOfAnotherKind_IsNotAttached(ApiCatalogCardKind kind)
	{
		var html = await Render(Item(), kind);

		html.Should().NotContain("api-catalog-card-attached");
		html.Should().NotContain("<li");
	}

	[Test]
	[Arguments(ApiCatalogCardKind.Standard)]
	[Arguments(ApiCatalogCardKind.Attached)]
	public async Task Render_Card_TopAlignsTheIconWithTheTitle(ApiCatalogCardKind kind) =>
		(await Render(Item(), kind)).Should().Contain("items-start");

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
