// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.Documentation.Mcp.Lambda.Responses;
using FluentAssertions;

namespace Mcp.Lambda.IntegrationTests;

/// <summary>
/// Integration tests for CoherenceTools MCP tools.
/// </summary>
public class CoherenceToolsIntegrationTests(ITestOutputHelper output) : McpToolsIntegrationTestsBase(output)
{
	[Fact]
	public async Task CheckCoherence_ReturnsAnalysis()
	{
		// Arrange
		var (coherenceTools, clientAccessor) = CreateCoherenceTools();
		Assert.SkipUnless(coherenceTools is not null, "Elasticsearch is not configured");
		var canConnect = await clientAccessor!.CanConnect(TestContext.Current.CancellationToken);
		Assert.SkipUnless(canConnect, "Elasticsearch is not connected");

		// Act
		var resultJson = await coherenceTools.CheckCoherence(
			"elasticsearch security",
			limit: 10,
			cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		Output.WriteLine($"Result: {resultJson}");
		var response = JsonSerializer.Deserialize(resultJson, McpJsonContext.Default.CoherenceCheckResponse);

		response.Should().NotBeNull();
		response!.Topic.Should().Be("elasticsearch security");
		response.TotalDocuments.Should().BeGreaterThan(0);
		response.CoverageScore.Should().BeGreaterOrEqualTo(0);
		Output.WriteLine($"Total documents: {response.TotalDocuments}");
		Output.WriteLine($"Analyzed documents: {response.AnalyzedDocuments}");
		Output.WriteLine($"Coverage score: {response.CoverageScore}");
		Output.WriteLine($"Section coverage: {string.Join(", ", response.SectionCoverage.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}");
		Output.WriteLine($"Product coverage: {string.Join(", ", response.ProductCoverage.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}");
	}

	[Fact]
	public async Task FindInconsistencies_ReturnsResults()
	{
		// Arrange
		var (coherenceTools, clientAccessor) = CreateCoherenceTools();
		Assert.SkipUnless(coherenceTools is not null, "Elasticsearch is not configured");
		var canConnect = await clientAccessor!.CanConnect(TestContext.Current.CancellationToken);
		Assert.SkipUnless(canConnect, "Elasticsearch is not connected");

		// Act
		var resultJson = await coherenceTools.FindInconsistencies(
			"authentication",
			focusArea: "configuration",
			cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		Output.WriteLine($"Result: {resultJson}");
		var response = JsonSerializer.Deserialize(resultJson, McpJsonContext.Default.InconsistenciesResponse);

		response.Should().NotBeNull();
		response!.Topic.Should().Be("authentication");
		response.FocusArea.Should().Be("configuration");
		response.TotalDocuments.Should().BeGreaterThan(0);
		Output.WriteLine($"Total documents: {response.TotalDocuments}");
		Output.WriteLine($"Potential inconsistencies: {response.PotentialInconsistencies.Count}");
		Output.WriteLine($"Product breakdown: {string.Join(", ", response.ProductBreakdown.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}");
	}
}
