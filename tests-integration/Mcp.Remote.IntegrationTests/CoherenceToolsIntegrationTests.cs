// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using Elastic.Documentation.Mcp.Remote.Responses;

namespace Mcp.Remote.IntegrationTests;

/// <summary>
/// Integration tests for CoherenceTools MCP tools.
/// </summary>
public class CoherenceToolsIntegrationTests : McpToolsIntegrationTestsBase
{
	[Test]
	public async Task CheckCoherence_ReturnsAnalysis()
	{
		// Arrange
		var (coherenceTools, clientAccessor) = CreateCoherenceTools();
		Skip.Unless(coherenceTools is not null, "Elasticsearch is not configured");
		LogDiagnostics(clientAccessor);
		var canConnect = await clientAccessor!.CanConnect(TestContext.Current!.Execution.CancellationToken);
		Skip.Unless(canConnect, "Elasticsearch is not connected");

		// Act
		var resultJson = await coherenceTools.CheckCoherence(
			"elasticsearch security",
			limit: 10,
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		// Assert
		TestContext.Current?.Output.WriteLine($"Result: {resultJson}");
		var response = JsonSerializer.Deserialize(resultJson, McpJsonContext.Default.CoherenceCheckResponse);

		response.Should().NotBeNull();
		response!.Topic.Should().Be("elasticsearch security");
		response.TotalDocuments.Should().BeGreaterThan(0);
		response.CoverageScore.Should().BeGreaterThanOrEqualTo(0);
		TestContext.Current?.Output.WriteLine($"Total documents: {response.TotalDocuments}");
		TestContext.Current?.Output.WriteLine($"Analyzed documents: {response.AnalyzedDocuments}");
		TestContext.Current?.Output.WriteLine($"Coverage score: {response.CoverageScore}");
		TestContext.Current?.Output.WriteLine(
			$"Section coverage: {string.Join(", ", response.SectionCoverage.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}"
		);
		TestContext.Current?.Output.WriteLine(
			$"Product coverage: {string.Join(", ", response.ProductCoverage.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}"
		);
	}

	[Test]
	public async Task FindInconsistencies_ReturnsResults()
	{
		// Arrange
		var (coherenceTools, clientAccessor) = CreateCoherenceTools();
		Skip.Unless(coherenceTools is not null, "Elasticsearch is not configured");
		LogDiagnostics(clientAccessor);
		var canConnect = await clientAccessor!.CanConnect(TestContext.Current!.Execution.CancellationToken);
		Skip.Unless(canConnect, "Elasticsearch is not connected");

		// Act
		var resultJson = await coherenceTools.FindInconsistencies(
			"authentication",
			focusArea: "configuration",
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		// Assert
		TestContext.Current?.Output.WriteLine($"Result: {resultJson}");
		var response = JsonSerializer.Deserialize(resultJson, McpJsonContext.Default.InconsistenciesResponse);

		response.Should().NotBeNull();
		response!.Topic.Should().Be("authentication");
		response.FocusArea.Should().Be("configuration");
		response.TotalDocuments.Should().BeGreaterThan(0);
		TestContext.Current?.Output.WriteLine($"Total documents: {response.TotalDocuments}");
		TestContext.Current?.Output.WriteLine($"Potential inconsistencies: {response.PotentialInconsistencies.Count}");
		TestContext.Current?.Output.WriteLine(
			$"Product breakdown: {string.Join(", ", response.ProductBreakdown.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}"
		);
	}
}
