// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;
using TUnit.Core.Interfaces;

namespace Elastic.Documentation.IntegrationTests.Smoke;

[ClassDataSource<DocumentationFixture>(Shared = SharedType.PerAssembly)]
public class McpSmokeTests(DocumentationFixture fixture) : IAsyncInitializer, IAsyncDisposable
{
	public Task InitializeAsync() => Task.CompletedTask;

	public ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		if (TestContext.Current!.Execution.Result?.State is not TestState.Failed)
			return default;
		foreach (var resource in fixture.InMemoryLogger.RecordedLogs.ToList())
			TestContext.Current?.Output.WriteLine(resource.Message);
		return default;
	}

	[Test]
	public async Task AliveEndpoint_Returns200()
	{
		using var client = fixture.CreateMcpClient();
		var response = await client.GetAsync("/docs/_mcp/alive", TestContext.Current!.Execution.CancellationToken);
		_ = response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Test]
	public async Task ListTools_ReturnsAtLeastOneTool()
	{
		using var httpClient = fixture.CreateMcpClient();
		var mcpEndpoint = new Uri(httpClient.BaseAddress!, "/docs/_mcp");
		var transport = new HttpClientTransport(
			new HttpClientTransportOptions { Endpoint = mcpEndpoint },
			httpClient,
			NullLoggerFactory.Instance,
			ownsHttpClient: false
		);
		await using var mcpClient = await McpClient.CreateAsync(
			transport,
			cancellationToken: TestContext.Current!.Execution.CancellationToken
		);
		var tools = await mcpClient.ListToolsAsync(cancellationToken: TestContext.Current!.Execution.CancellationToken);
		_ = tools.Should().NotBeEmpty("the MCP server should expose at least one tool");
	}
}
