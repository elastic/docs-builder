// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.Documentation.Mcp.Remote.Responses;
using FluentAssertions;

namespace Mcp.Remote.IntegrationTests;

/// <summary>
/// Integration tests for DocumentTools MCP tools.
/// </summary>
public class DocumentToolsIntegrationTests(ITestOutputHelper output) : McpToolsIntegrationTestsBase(output)
{
	[Fact]
	public async Task GetDocumentByUrl_ReturnsDocument()
	{
		// Arrange
		var (documentTools, clientAccessor) = CreateDocumentTools();
		Assert.SkipUnless(documentTools is not null, "Elasticsearch is not configured");
		var canConnect = await clientAccessor!.CanConnect(TestContext.Current.CancellationToken);
		Assert.SkipUnless(canConnect, "Elasticsearch is not connected");

		// Act - use a URL that is likely to exist
		var resultJson = await documentTools.GetDocumentByUrl(
			"/docs/reference/elasticsearch",
			cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		Output.WriteLine($"Result: {resultJson}");

		// Check if it's an error response (document not found)
		if (resultJson.Contains("\"error\""))
		{
			var errorResponse = JsonSerializer.Deserialize(resultJson, McpJsonContext.Default.ErrorResponse);
			Output.WriteLine($"Document not found: {errorResponse?.Error}");
			// Skip the test if the document doesn't exist in the index
			Assert.Skip($"Test document not found: {errorResponse?.Error}");
		}

		var response = JsonSerializer.Deserialize(resultJson, McpJsonContext.Default.DocumentResponse);
		response.Should().NotBeNull();
		response!.Url.Should().NotBeNullOrEmpty();
		response.Title.Should().NotBeNullOrEmpty();
		Output.WriteLine($"Document title: {response.Title}");
		Output.WriteLine($"Document URL: {response.Url}");
	}

	[Fact]
	public async Task GetDocumentByUrl_NotFound_ReturnsError()
	{
		// Arrange
		var (documentTools, clientAccessor) = CreateDocumentTools();
		Assert.SkipUnless(documentTools is not null, "Elasticsearch is not configured");
		var canConnect = await clientAccessor!.CanConnect(TestContext.Current.CancellationToken);
		Assert.SkipUnless(canConnect, "Elasticsearch is not connected");

		// Act - use a URL that should not exist
		var resultJson = await documentTools.GetDocumentByUrl(
			"/docs/this-document-definitely-does-not-exist-12345",
			cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		Output.WriteLine($"Result: {resultJson}");
		var errorResponse = JsonSerializer.Deserialize(resultJson, McpJsonContext.Default.ErrorResponse);

		errorResponse.Should().NotBeNull();
		errorResponse!.Error.Should().Contain("not found");
		Output.WriteLine($"Error message: {errorResponse.Error}");
	}

	[Fact]
	public async Task AnalyzeDocumentStructure_ReturnsStructure()
	{
		// Arrange
		var (documentTools, clientAccessor) = CreateDocumentTools();
		Assert.SkipUnless(documentTools is not null, "Elasticsearch is not configured");
		var canConnect = await clientAccessor!.CanConnect(TestContext.Current.CancellationToken);
		Assert.SkipUnless(canConnect, "Elasticsearch is not connected");

		// Act - use a URL that is likely to exist
		var resultJson = await documentTools.AnalyzeDocumentStructure(
			"/docs/reference/elasticsearch",
			cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		Output.WriteLine($"Result: {resultJson}");

		// Check if it's an error response (document not found)
		if (resultJson.Contains("\"error\""))
		{
			var errorResponse = JsonSerializer.Deserialize(resultJson, McpJsonContext.Default.ErrorResponse);
			Output.WriteLine($"Document not found: {errorResponse?.Error}");
			Assert.Skip($"Test document not found: {errorResponse?.Error}");
		}

		var response = JsonSerializer.Deserialize(resultJson, McpJsonContext.Default.DocumentStructureResponse);
		response.Should().NotBeNull();
		response!.Url.Should().NotBeNullOrEmpty();
		response.Title.Should().NotBeNullOrEmpty();
		Output.WriteLine($"Document title: {response.Title}");
		Output.WriteLine($"Heading count: {response.HeadingCount}");
		Output.WriteLine($"Link count: {response.LinkCount}");
		Output.WriteLine($"Has AI Summary: {response.AiEnrichment?.HasSummary}");
	}
}
