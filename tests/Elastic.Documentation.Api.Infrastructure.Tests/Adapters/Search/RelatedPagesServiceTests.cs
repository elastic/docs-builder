// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Search;
using FakeItEasy;

namespace Elastic.Documentation.Api.Infrastructure.Tests.Adapters.Search;

public class RelatedPagesServiceTests
{
	[Fact]
	public async Task GetRelatedPagesAsync_MissingPath_ForcesSemanticSearchAndExcludesSamePath()
	{
		var search = A.Fake<IFullSearchService>();
		A.CallTo(() => search.SearchAsync(A<FullSearchRequest>._, A<CancellationToken>._))
			.Returns(new FullSearchResponse
			{
				Results =
				[
					Result("/docs/deploy-manage/index-lifecycle-management", "Missing page", 12),
					Result("/docs/manage-data/lifecycle/index-lifecycle-management", "Manage index lifecycle", 10)
				],
				TotalResults = 2,
				PageNumber = 1,
				PageSize = 6
			});
		var service = new RelatedPagesService(search);

		var response = await service.GetRelatedPagesAsync(
			"/docs/deploy-manage/index-lifecycle-management", TestContext.Current.CancellationToken);

		A.CallTo(() => search.SearchAsync(
			A<FullSearchRequest>.That.Matches(request =>
				request.ForceSemantic && !request.IncludeHighlighting && request.PageSize == 6),
			A<CancellationToken>._)).MustHaveHappenedOnceExactly();
		response.Results.Should().ContainSingle().Which.Title.Should().Be("Manage index lifecycle");
	}

	[Fact]
	public async Task GetRelatedPagesAsync_UnusablePath_DoesNotSearch()
	{
		var search = A.Fake<IFullSearchService>();
		var service = new RelatedPagesService(search);

		var response = await service.GetRelatedPagesAsync("/docs/", TestContext.Current.CancellationToken);

		response.Results.Should().BeEmpty();
		A.CallTo(() => search.SearchAsync(A<FullSearchRequest>._, A<CancellationToken>._)).MustNotHaveHappened();
	}

	private static FullSearchResultItem Result(string url, string title, float score) => new()
	{
		Type = "doc",
		Url = url,
		Title = title,
		Description = $"Description for {title}",
		Parents = [],
		Score = score
	};
}
