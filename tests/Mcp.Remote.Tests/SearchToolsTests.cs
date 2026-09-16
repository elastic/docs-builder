// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Mcp.Remote.Tools;
using Elastic.Documentation.Search;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mcp.Remote.Tests;

public class SearchToolsTests
{
	private static readonly FullSearchResponse EmptyResponse = new() { Results = [], TotalResults = 0, PageNumber = 1, PageSize = 10 };

	[Fact]
	public async Task SemanticSearch_PassesVersionFilter_ToSearchRequest()
	{
		var searchService = A.Fake<IFullSearchService>();
		A.CallTo(() => searchService.SearchAsync(A<FullSearchRequest>._, A<CancellationToken>._)).Returns(EmptyResponse);

		var tools = new SearchTools(searchService, NullLogger<SearchTools>.Instance);
		_ = await tools.SemanticSearch("cluster setup", versionFilter: "9.0", cancellationToken: TestContext.Current.CancellationToken);

		A.CallTo(
			() => searchService.SearchAsync(A<FullSearchRequest>.That.Matches(r => r.VersionFilter == "9.0"), A<CancellationToken>._)
		).MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task SemanticSearch_WithNullVersionFilter_SendsNullToSearchRequest()
	{
		var searchService = A.Fake<IFullSearchService>();
		A.CallTo(() => searchService.SearchAsync(A<FullSearchRequest>._, A<CancellationToken>._)).Returns(EmptyResponse);

		var tools = new SearchTools(searchService, NullLogger<SearchTools>.Instance);
		_ = await tools.SemanticSearch("cluster setup", cancellationToken: TestContext.Current.CancellationToken);

		A.CallTo(
			() => searchService.SearchAsync(A<FullSearchRequest>.That.Matches(r => r.VersionFilter == null), A<CancellationToken>._)
		).MustHaveHappenedOnceExactly();
	}
}
