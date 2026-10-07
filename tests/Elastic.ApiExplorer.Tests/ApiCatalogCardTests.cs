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

	private static async Task<string> Render(ApiCatalogItem item, bool featured = false) =>
		await _ApiCatalogCard.Create(new ApiCatalogCard(item, featured)).RenderAsync(
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

	[Test]
	[Arguments(false)]
	[Arguments(true)]
	public async Task Render_Card_IsOneLinkThatWrapsTheWholeCardAndNothingElseIsALink(bool featured)
	{
		var html = await Render(Item(pills: ["Self-managed", "Serverless"]), featured);

		html.Split("<a ").Length.Should().Be(2, "there is exactly one link in a card");
		html.Split("</a>").Length.Should().Be(2);
		html
			.IndexOf("<a ", StringComparison.Ordinal)
			.Should()
			.BeLessThan(html.IndexOf("<h2", StringComparison.Ordinal), "the heading is inside the link");
		html
			.IndexOf("</a>", StringComparison.Ordinal)
			.Should()
			.BeGreaterThan(html.IndexOf("Self-managed", StringComparison.Ordinal), "so are the description and the pills");
		html.Should().Contain("href=\"/docs/api/doc/elasticsearch\"");
	}

	[Test]
	[Arguments("elasticsearch", "elasticsearch")]
	[Arguments("my api/v2", "my-api-v2")]
	public async Task Render_Card_LinkIsNamedByItsTitleWithAnIdThatIsValid(string key, string id)
	{
		var html = await Render(Item(key));

		html.Should().Contain($"aria-labelledby=\"api-catalog-title-{id}\"");
		html.Should().Contain($"<h2 id=\"api-catalog-title-{id}\"");
	}
}
