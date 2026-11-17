// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Contract;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Api.Tests;

public class FullSearchServiceTests
{
	[Test]
	[Arguments(0, 0, 1, 1)]
	[Arguments(-3, -5, 1, 1)]
	[Arguments(2, 20, 2, 20)]
	[Arguments(1, 100000, 1, FullSearchService.MaxPageSize)]
	public async Task SearchAsync_ClampsPageAndSizeBeforeQueryingTheIndex(int page, int size, int expectedPage, int expectedSize)
	{
		SearchRequest? received = null;
		var inner = A.Fake<ISearchService<DocumentationDocument>>();
		_ = A
			.CallTo(() => inner.SearchAsync(A<SearchRequest>._, A<CancellationToken>._))
			.Invokes((SearchRequest request, CancellationToken _) => received = request)
			.Throws(new OperationCanceledException("stop after the request is captured"));
		using var service = new FullSearchService(inner, A.Fake<IProductNameLookup>(), NullLogger<FullSearchService>.Instance);

		var search =
			() => service.SearchAsync(
				new FullSearchRequest { Query = "health", PageNumber = page, PageSize = size },
				TestContext.Current!.Execution.CancellationToken
			);

		_ = await search.Should().ThrowAsync<OperationCanceledException>();
		received.Should().NotBeNull();
		received.PageNumber.Should().Be(expectedPage);
		received.PageSize.Should().Be(expectedSize);
	}
}
