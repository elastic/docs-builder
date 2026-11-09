// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using Elastic.Documentation.Mcp.Remote.Gateways;
using Elastic.Documentation.Mcp.Remote.Responses;
using Elastic.Documentation.Mcp.Remote.Tools;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mcp.Remote.Tests;

public class DocumentToolsTests
{
	[Test]
	public async Task GetDocumentByUrl_MapsSourceUrlIntoResponse()
	{
		const string expectedSourceUrl = "https://github.com/elastic/docs-content/blob/main/docs/some-page.md";
		var gateway = new StubDocumentGateway(new DocumentResult
		{
			Url = "/docs/some-page",
			Title = "Some Page",
			Type = "doc",
			SourceUrl = expectedSourceUrl
		});
		var tools = new DocumentTools(gateway, NullLogger<DocumentTools>.Instance);

		var json = await tools.GetDocumentByUrl("/docs/some-page", cancellationToken: TestContext.Current!.Execution.CancellationToken);

		// MCP responses use camelCase (McpJsonContext); assert the wire key, not only the CLR property.
		json.Should().Contain("\"sourceUrl\"");
		var response = JsonSerializer.Deserialize(json, McpJsonContext.Default.DocumentResponse);
		response.Should().NotBeNull();
		response.SourceUrl.Should().Be(expectedSourceUrl);
	}

	[Test]
	public async Task GetDocumentByUrl_OmitsSourceUrlWhenNull()
	{
		var gateway = new StubDocumentGateway(new DocumentResult
		{
			Url = "/docs/some-page",
			Title = "Some Page",
			Type = "doc",
			SourceUrl = null
		});
		var tools = new DocumentTools(gateway, NullLogger<DocumentTools>.Instance);

		var json = await tools.GetDocumentByUrl("/docs/some-page", cancellationToken: TestContext.Current!.Execution.CancellationToken);

		using var doc = JsonDocument.Parse(json);
		var hasNonNullSourceUrl = doc.RootElement.TryGetProperty("sourceUrl", out var sourceUrl)
			&& sourceUrl.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined;
		hasNonNullSourceUrl.Should().BeFalse();
		var response = JsonSerializer.Deserialize(json, McpJsonContext.Default.DocumentResponse);
		response.Should().NotBeNull();
		response.SourceUrl.Should().BeNull();
	}

	[Test]
	public async Task GetDocumentByUrl_ApiPage_ReturnsApiTypeAndOperation()
	{
		var gateway = new StubDocumentGateway(new DocumentResult
		{
			Url = "/docs/api/doc/logstash/operation/operation-nodestats",
			Title = "Get node statistics",
			Type = "api",
			Api = new DocumentApi
			{
				Method = "GET",
				Path = "/_node/stats",
				Endpoints = ["GET /_node/stats"],
				OperationId = "nodestats",
				Deprecated = false
			}
		});
		var tools = new DocumentTools(gateway, NullLogger<DocumentTools>.Instance);

		var json = await tools.GetDocumentByUrl(
			"/docs/api/doc/logstash/operation/operation-nodestats",
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		var response = JsonSerializer.Deserialize(json, McpJsonContext.Default.DocumentResponse);
		response.Should().NotBeNull();
		response.Type.Should().Be("api");
		response.Api.Should().NotBeNull();
		response.Api.Method.Should().Be("GET");
		response.Api.Endpoints.Should().Equal("GET /_node/stats");
		response.Api.OperationId.Should().Be("nodestats");
	}

	[Test]
	public async Task GetDocumentByUrl_NarrativePage_HasNoApiBlock()
	{
		var gateway = new StubDocumentGateway(new DocumentResult { Url = "/docs/some-page", Title = "Some Page", Type = "docs" });
		var tools = new DocumentTools(gateway, NullLogger<DocumentTools>.Instance);

		var json = await tools.GetDocumentByUrl("/docs/some-page", cancellationToken: TestContext.Current!.Execution.CancellationToken);

		var response = JsonSerializer.Deserialize(json, McpJsonContext.Default.DocumentResponse);
		response.Should().NotBeNull();
		response.Api.Should().BeNull();
	}

	[Test]
	public async Task GetDocumentByUrl_BodyOmitted_ExplainsHowToRequestIt()
	{
		var tools = new DocumentTools(StubWithBody(), NullLogger<DocumentTools>.Instance);

		var json = await tools.GetDocumentByUrl("/docs/some-page", cancellationToken: TestContext.Current!.Execution.CancellationToken);

		var response = JsonSerializer.Deserialize(json, McpJsonContext.Default.DocumentResponse);
		response.Should().NotBeNull();
		response.Body.Should().BeNull();
		response.BodyLength.Should().Be(9);
		response.BodyNote.Should().Contain("includeBody=true");
	}

	[Test]
	public async Task GetDocumentByUrl_IncludeBody_ReturnsBodyWithoutNote()
	{
		var tools = new DocumentTools(StubWithBody(), NullLogger<DocumentTools>.Instance);

		var json = await tools.GetDocumentByUrl(
			"/docs/some-page",
			includeBody: true,
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);

		var response = JsonSerializer.Deserialize(json, McpJsonContext.Default.DocumentResponse);
		response.Should().NotBeNull();
		response.Body.Should().Be("Body text");
		response.BodyNote.Should().BeNull();
	}

	private static StubDocumentGateway StubWithBody() =>
		new(new DocumentResult { Url = "/docs/some-page", Title = "Some Page", Type = "doc", Body = "Body text" });

	private sealed class StubDocumentGateway(DocumentResult? result) : IDocumentGateway
	{
		public Task<DocumentResult?> GetByUrlAsync(string url, CancellationToken ct = default) => Task.FromResult(result);

		public Task<DocumentStructure?> GetStructureAsync(string url, CancellationToken ct = default) =>
			Task.FromResult<DocumentStructure?>(null);
	}
}
