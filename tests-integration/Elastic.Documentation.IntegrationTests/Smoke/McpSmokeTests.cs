// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;

namespace Elastic.Documentation.IntegrationTests.Smoke;

public class McpSmokeTests(DocumentationFixture fixture, ITestOutputHelper output) : IAsyncLifetime
{
	/// <inheritdoc />
	public ValueTask InitializeAsync() => default;

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		if (TestContext.Current.TestState?.Result is TestResult.Passed)
			return default;
		foreach (var resource in fixture.InMemoryLogger.RecordedLogs)
			output.WriteLine(resource.Message);
		return default;
	}

	[Fact]
	public async Task HealthEndpoint_Returns200()
	{
		using var client = fixture.CreateMcpClient();
		var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);
		_ = response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task ListTools_ReturnsAtLeastOneTool()
	{
		using var httpClient = fixture.CreateMcpClient();
		var mcpEndpoint = new Uri(httpClient.BaseAddress!, "/docs/_mcp");
		var transport = new HttpClientTransport(
			new HttpClientTransportOptions { Endpoint = mcpEndpoint },
			httpClient,
			NullLoggerFactory.Instance,
			ownsHttpClient: false);
		await using var mcpClient = await McpClient.CreateAsync(
			transport,
			cancellationToken: TestContext.Current.CancellationToken);
		var tools = await mcpClient.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
		_ = tools.Should().NotBeEmpty("the MCP server should expose at least one tool");
	}
}
