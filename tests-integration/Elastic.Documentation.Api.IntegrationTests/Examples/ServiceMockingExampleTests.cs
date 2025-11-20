// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using Elastic.Documentation.Api.Core.Search;
using Elastic.Documentation.Api.Core.Telemetry;
using Elastic.Documentation.Api.IntegrationTests.Fixtures;
using FakeItEasy;
using FluentAssertions;
using Xunit;

namespace Elastic.Documentation.Api.IntegrationTests.Examples;

/// <summary>
/// Example test demonstrating how to mock multiple services in integration tests.
/// This serves as documentation for the service mocking pattern.
/// </summary>
public class ServiceMockingExampleTests
{
	[Fact]
	public async Task ExampleWithMultipleServiceMocks()
	{
		// Arrange - Create multiple mocks
		var mockOtlpGateway = A.Fake<IOtlpGateway>();
		var mockSearchGateway = A.Fake<ISearchGateway>();

		// Configure mock behaviors
		A.CallTo(() => mockOtlpGateway.ForwardOtlp(
			A<string>._,
			A<Stream>._,
			A<string>._,
			A<Cancel>._))
			.Returns(Task.FromResult<(int StatusCode, string? Content)>((200, "{}}")));

		A.CallTo(() => mockSearchGateway.SearchAsync(
			A<string>._,
			A<int>._,
			A<int>._,
			A<Cancel>._))
			.Returns(Task.FromResult((TotalHits: 1, Results: new List<SearchResultItem>
			{
				new()
				{
					Type = "page",
					Url = "/docs/test",
					Title = "Test Result",
					Description = "A test result",
					Parents = []
				}
			})));

		// Create factory with multiple mocked services using fluent API
		using var factory = ApiWebApplicationFactory.WithMockedServices(services =>
			services
				.Replace(mockOtlpGateway)      // Replace IOtlpGateway
				.Replace(mockSearchGateway));  // Replace ISearchGateway

		using var client = factory.CreateClient();

		// Act - Make a search request that uses the mocked search gateway
		var searchResponse = await client.GetAsync(
			"/docs/_api/v1/search?q=test&page=1&pageSize=5",
			TestContext.Current.CancellationToken);

		// Assert
		searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		// Verify the search gateway was called with correct parameters
		A.CallTo(() => mockSearchGateway.SearchAsync(
			"test",    // query
			1,         // page
			5,         // pageSize (default in API)
			A<Cancel>._))
			.MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task ExampleWithSingletonMock()
	{
		// Arrange
		var mockGateway = A.Fake<IOtlpGateway>();
		A.CallTo(() => mockGateway.ForwardOtlp(A<string>._, A<Stream>._, A<string>._, A<Cancel>._))
			.Returns(Task.FromResult<(int, string?)>((503, "Unavailable")));

		// Use ReplaceSingleton for services registered as singletons
		using var factory = ApiWebApplicationFactory.WithMockedServices(services =>
			services.ReplaceSingleton(mockGateway));

		using var client = factory.CreateClient();

		// Act & Assert
		// Your test logic here...
	}
}
