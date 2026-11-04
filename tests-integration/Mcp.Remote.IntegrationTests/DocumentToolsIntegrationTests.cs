// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using Elastic.Documentation.Mcp.Remote.Responses;

namespace Mcp.Remote.IntegrationTests;

/// <summary>
/// Integration tests for DocumentTools MCP tools.
/// </summary>
public class DocumentToolsIntegrationTests : McpToolsIntegrationTestsBase
{
	[Test]
	public async Task GetDocumentByUrl_ReturnsDocument()
	{
		// Arrange
		var (documentTools, clientAccessor) = CreateDocumentTools();
		Skip.Unless(documentTools is not null, "Elasticsearch is not configured");
		LogDiagnostics(clientAccessor);
		var canConnect = await clientAccessor!.CanConnect(TestContext.Current!.Execution.CancellationToken);
		Skip.Unless(canConnect, "Elasticsearch is not connected");

		// Act - use a URL that is likely to exist
		var resultJson = await documentTools.GetDocumentByUrl(
			"/docs/reference/elasticsearch",
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		// Assert
		TestContext.Current?.Output.WriteLine($"Result: {resultJson}");

		// Check if it's an error response (document not found)
		if (resultJson.Contains("\"error\""))
		{
			var errorResponse = JsonSerializer.Deserialize(resultJson, McpJsonContext.Default.ErrorResponse);
			TestContext.Current?.Output.WriteLine($"Document not found: {errorResponse?.Error}");
			Skip.Test($"Test document not found: {errorResponse?.Error}");
		}

		var response = JsonSerializer.Deserialize(resultJson, McpJsonContext.Default.DocumentResponse);
		response.Should().NotBeNull();
		response!.Url.Should().NotBeNullOrEmpty();
		response.Title.Should().NotBeNullOrEmpty();
		TestContext.Current?.Output.WriteLine($"Document title: {response.Title}");
		TestContext.Current?.Output.WriteLine($"Document URL: {response.Url}");
	}

	[Test]
	public async Task GetDocumentByUrl_NotFound_ReturnsError()
	{
		// Arrange
		var (documentTools, clientAccessor) = CreateDocumentTools();
		Skip.Unless(documentTools is not null, "Elasticsearch is not configured");
		LogDiagnostics(clientAccessor);
		var canConnect = await clientAccessor!.CanConnect(TestContext.Current!.Execution.CancellationToken);
		Skip.Unless(canConnect, "Elasticsearch is not connected");

		// Act - use a URL that should not exist
		var resultJson = await documentTools.GetDocumentByUrl(
			"/docs/this-document-definitely-does-not-exist-12345",
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		// Assert
		TestContext.Current?.Output.WriteLine($"Result: {resultJson}");
		var errorResponse = JsonSerializer.Deserialize(resultJson, McpJsonContext.Default.ErrorResponse);

		errorResponse.Should().NotBeNull();
		errorResponse!.Error.Should().Contain("not found");
		TestContext.Current?.Output.WriteLine($"Error message: {errorResponse.Error}");
	}

	[Test]
	public async Task GetDocumentByUrl_SourceUrlIsGitHubBlobUrlWhenPresent()
	{
		// Arrange
		var (documentTools, clientAccessor) = CreateDocumentTools();
		Skip.Unless(documentTools is not null, "Elasticsearch is not configured");
		LogDiagnostics(clientAccessor);
		var canConnect = await clientAccessor!.CanConnect(TestContext.Current!.Execution.CancellationToken);
		Skip.Unless(canConnect, "Elasticsearch is not connected");

		// Act
		var resultJson = await documentTools.GetDocumentByUrl(
			"/docs/reference/elasticsearch",
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		if (resultJson.Contains("\"error\""))
			Skip.Test("Test document not found in index");

		var response = JsonSerializer.Deserialize(resultJson, McpJsonContext.Default.DocumentResponse);
		response.Should().NotBeNull();
		TestContext.Current?.Output.WriteLine($"sourceUrl: {response!.SourceUrl ?? "(null — document predates indexing of this field)"}");

		// sourceUrl is null for docs indexed before this field was added; when present it must be a GitHub blob URL
		response.SourceUrl?.Should().StartWith("https://github.com/").And.Contain("/blob/");
	}

	[Test]
	public async Task AnalyzeDocumentStructure_ReturnsStructure()
	{
		// Arrange
		var (documentTools, clientAccessor) = CreateDocumentTools();
		Skip.Unless(documentTools is not null, "Elasticsearch is not configured");
		LogDiagnostics(clientAccessor);
		var canConnect = await clientAccessor!.CanConnect(TestContext.Current!.Execution.CancellationToken);
		Skip.Unless(canConnect, "Elasticsearch is not connected");

		// Act - use a URL that is likely to exist
		var resultJson = await documentTools.AnalyzeDocumentStructure(
			"/docs/reference/elasticsearch",
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		// Assert
		TestContext.Current?.Output.WriteLine($"Result: {resultJson}");

		// Check if it's an error response (document not found)
		if (resultJson.Contains("\"error\""))
		{
			var errorResponse = JsonSerializer.Deserialize(resultJson, McpJsonContext.Default.ErrorResponse);
			TestContext.Current?.Output.WriteLine($"Document not found: {errorResponse?.Error}");
			Skip.Test($"Test document not found: {errorResponse?.Error}");
		}

		var response = JsonSerializer.Deserialize(resultJson, McpJsonContext.Default.DocumentStructureResponse);
		response.Should().NotBeNull();
		response!.Url.Should().NotBeNullOrEmpty();
		response.Title.Should().NotBeNullOrEmpty();
		TestContext.Current?.Output.WriteLine($"Document title: {response.Title}");
		TestContext.Current?.Output.WriteLine($"Heading count: {response.HeadingCount}");
		TestContext.Current?.Output.WriteLine($"Link count: {response.LinkCount}");
		TestContext.Current?.Output.WriteLine($"Has AI Summary: {response.AiEnrichment?.HasSummary}");
	}
}
