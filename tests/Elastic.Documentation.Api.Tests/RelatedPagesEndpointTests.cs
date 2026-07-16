// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using AwesomeAssertions;
using Elastic.Documentation.Api.Tests.Fixtures;
using Elastic.Documentation.Search;
using FakeItEasy;

namespace Elastic.Documentation.Api.Tests;

public class RelatedPagesEndpointTests
{
	[Fact]
	public async Task RelatedPages_ValidPath_ReturnsSuggestions()
	{
		var service = A.Fake<IRelatedPagesService>();
		A.CallTo(() => service.GetRelatedPagesAsync("/docs/old-page", A<CancellationToken>._))
			.Returns(new RelatedPagesResponse
			{
				Query = "old page",
				Results =
				[
					new RelatedPage
					{
						Url = "/docs/new-page",
						Title = "New page",
						Description = "The replacement page.",
						Parents = []
					}
				]
			});
		using var factory = ApiWebApplicationFactory.WithMockedServices(
			services => services.Replace(service));
		using var client = factory.CreateClient();

		using var response = await client.GetAsync(
			"/docs/_api/v1/related-pages?path=%2Fdocs%2Fold-page", TestContext.Current.CancellationToken);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
		json.Should().Contain("\"query\":\"old page\"");
		json.Should().Contain("\"url\":\"/docs/new-page\"");
	}

	[Fact]
	public async Task RelatedPages_OversizedPath_ReturnsBadRequest()
	{
		var service = A.Fake<IRelatedPagesService>();
		using var factory = ApiWebApplicationFactory.WithMockedServices(
			services => services.Replace(service));
		using var client = factory.CreateClient();
		var path = new string('a', RelatedPagesQuery.MaximumPathLength + 1);

		using var response = await client.GetAsync(
			$"/docs/_api/v1/related-pages?path={path}", TestContext.Current.CancellationToken);

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		A.CallTo(() => service.GetRelatedPagesAsync(A<string>._, A<CancellationToken>._)).MustNotHaveHappened();
	}
}
