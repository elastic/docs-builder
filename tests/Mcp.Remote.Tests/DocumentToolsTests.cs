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
	[Fact]
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

		var json = await tools.GetDocumentByUrl("/docs/some-page");

		var response = JsonSerializer.Deserialize(json, McpJsonContext.Default.DocumentResponse);
		response.Should().NotBeNull();
		response!.SourceUrl.Should().Be(expectedSourceUrl);
	}

	[Fact]
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

		var json = await tools.GetDocumentByUrl("/docs/some-page");

		var response = JsonSerializer.Deserialize(json, McpJsonContext.Default.DocumentResponse);
		response.Should().NotBeNull();
		response!.SourceUrl.Should().BeNull();
	}

	private sealed class StubDocumentGateway(DocumentResult? result) : IDocumentGateway
	{
		public Task<DocumentResult?> GetByUrlAsync(string url, CancellationToken ct = default) =>
			Task.FromResult(result);

		public Task<DocumentStructure?> GetStructureAsync(string url, CancellationToken ct = default) =>
			Task.FromResult<DocumentStructure?>(null);
	}
}
