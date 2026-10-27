// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using Elastic.Documentation.Mcp.Remote.Responses;

namespace Mcp.Remote.IntegrationTests;

/// <summary>
/// Integration tests for SearchTools MCP tools.
/// </summary>
public class SearchToolsIntegrationTests : McpToolsIntegrationTestsBase
{
	[Test]
	public async Task SemanticSearch_ReturnsResults()
	{
		// Arrange
		var (searchTools, clientAccessor) = CreateSearchTools();
		Skip.Unless(searchTools is not null, "Elasticsearch is not configured");
		LogDiagnostics(clientAccessor);
		var canConnect = await clientAccessor!.CanConnect(TestContext.Current!.Execution.CancellationToken);
		Skip.Unless(canConnect, "Elasticsearch is not connected");

		// Act
		var resultJson = await searchTools.SemanticSearch(
			"elasticsearch getting started",
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		// Assert
		TestContext.Current?.Output.WriteLine($"Result: {resultJson}");
		var response = JsonSerializer.Deserialize(resultJson, McpJsonContext.Default.SemanticSearchResponse);

		response.Should().NotBeNull();
		if (response!.Results.Count == 0)
			await LogIndexCount(clientAccessor, TestContext.Current!.Execution.CancellationToken);

		response
			.Results
			.Should()
			.NotBeEmpty($"Search for 'elasticsearch getting started' should return results (index: {clientAccessor.SearchIndex})");
		response.TotalHits.Should().BeGreaterThan(0);
		TestContext.Current?.Output.WriteLine($"Total hits: {response.TotalHits}");
		TestContext.Current?.Output.WriteLine($"Results returned: {response.Results.Count}");
	}

	[Test]
	public async Task SemanticSearch_WithProductFilter()
	{
		// Arrange
		var (searchTools, clientAccessor) = CreateSearchTools();
		Skip.Unless(searchTools is not null, "Elasticsearch is not configured");
		LogDiagnostics(clientAccessor);
		var canConnect = await clientAccessor!.CanConnect(TestContext.Current!.Execution.CancellationToken);
		Skip.Unless(canConnect, "Elasticsearch is not connected");

		// Act
		var resultJson = await searchTools.SemanticSearch(
			"getting started",
			productFilter: "elasticsearch",
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		// Assert
		TestContext.Current?.Output.WriteLine($"Result: {resultJson}");
		var response = JsonSerializer.Deserialize(resultJson, McpJsonContext.Default.SemanticSearchResponse);

		response.Should().NotBeNull();
		if (response!.Results.Count == 0)
			await LogIndexCount(clientAccessor!, TestContext.Current!.Execution.CancellationToken);

		response.Results.Should().NotBeEmpty($"Search with product filter should return results (index: {clientAccessor!.SearchIndex})");
		TestContext.Current?.Output.WriteLine($"Total hits: {response.TotalHits}");
	}

	[Test]
	public async Task FindRelatedDocs_ReturnsRelated()
	{
		// Arrange
		var (searchTools, clientAccessor) = CreateSearchTools();
		Skip.Unless(searchTools is not null, "Elasticsearch is not configured");
		LogDiagnostics(clientAccessor);
		var canConnect = await clientAccessor!.CanConnect(TestContext.Current!.Execution.CancellationToken);
		Skip.Unless(canConnect, "Elasticsearch is not connected");

		// Act
		var resultJson = await searchTools.FindRelatedDocs(
			"data streams",
			limit: 5,
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		// Assert
		TestContext.Current?.Output.WriteLine($"Result: {resultJson}");
		var response = JsonSerializer.Deserialize(resultJson, McpJsonContext.Default.RelatedDocsResponse);

		response.Should().NotBeNull();
		if (response!.RelatedDocs.Count == 0)
			await LogIndexCount(clientAccessor!, TestContext.Current!.Execution.CancellationToken);

		response
			.RelatedDocs
			.Should()
			.NotBeEmpty($"Finding related docs for 'data streams' should return results (index: {clientAccessor!.SearchIndex})");
		response.Count.Should().BeGreaterThan(0);
		TestContext.Current?.Output.WriteLine($"Related docs count: {response.Count}");
	}
}
