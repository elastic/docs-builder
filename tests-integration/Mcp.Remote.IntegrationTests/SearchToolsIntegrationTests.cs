// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.Documentation.Mcp.Remote.Responses;
using FluentAssertions;

namespace Mcp.Remote.IntegrationTests;

/// <summary>
/// Integration tests for SearchTools MCP tools.
/// </summary>
public class SearchToolsIntegrationTests(ITestOutputHelper output) : McpToolsIntegrationTestsBase(output)
{
	[Fact]
	public async Task SemanticSearch_ReturnsResults()
	{
		// Arrange
		var (searchTools, clientAccessor) = CreateSearchTools();
		Assert.SkipUnless(searchTools is not null, "Elasticsearch is not configured");
		var canConnect = await clientAccessor!.CanConnect(TestContext.Current.CancellationToken);
		Assert.SkipUnless(canConnect, "Elasticsearch is not connected");

		// Act
		var resultJson = await searchTools.SemanticSearch(
			"elasticsearch getting started",
			cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		Output.WriteLine($"Result: {resultJson}");
		var response = JsonSerializer.Deserialize(resultJson, McpJsonContext.Default.SemanticSearchResponse);

		response.Should().NotBeNull();
		response.Results.Should().NotBeEmpty("Search for 'elasticsearch getting started' should return results");
		response.TotalHits.Should().BeGreaterThan(0);
		Output.WriteLine($"Total hits: {response.TotalHits}");
		Output.WriteLine($"Results returned: {response.Results.Count}");
	}

	[Fact]
	public async Task SemanticSearch_WithProductFilter()
	{
		// Arrange
		var (searchTools, clientAccessor) = CreateSearchTools();
		Assert.SkipUnless(searchTools is not null, "Elasticsearch is not configured");
		var canConnect = await clientAccessor!.CanConnect(TestContext.Current.CancellationToken);
		Assert.SkipUnless(canConnect, "Elasticsearch is not connected");

		// Act
		var resultJson = await searchTools.SemanticSearch(
			"getting started",
			productFilter: "elasticsearch",
			cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		Output.WriteLine($"Result: {resultJson}");
		var response = JsonSerializer.Deserialize(resultJson, McpJsonContext.Default.SemanticSearchResponse);

		response.Should().NotBeNull();
		response!.Results.Should().NotBeEmpty("Search with product filter should return results");
		Output.WriteLine($"Total hits: {response.TotalHits}");
	}

	[Fact]
	public async Task FindRelatedDocs_ReturnsRelated()
	{
		// Arrange
		var (searchTools, clientAccessor) = CreateSearchTools();
		Assert.SkipUnless(searchTools is not null, "Elasticsearch is not configured");
		var canConnect = await clientAccessor!.CanConnect(TestContext.Current.CancellationToken);
		Assert.SkipUnless(canConnect, "Elasticsearch is not connected");

		// Act
		var resultJson = await searchTools.FindRelatedDocs(
			"data streams",
			limit: 5,
			cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		Output.WriteLine($"Result: {resultJson}");
		var response = JsonSerializer.Deserialize(resultJson, McpJsonContext.Default.RelatedDocsResponse);

		response.Should().NotBeNull();
		response!.RelatedDocs.Should().NotBeEmpty("Finding related docs for 'data streams' should return results");
		response.Count.Should().BeGreaterThan(0);
		Output.WriteLine($"Related docs count: {response.Count}");
	}
}
