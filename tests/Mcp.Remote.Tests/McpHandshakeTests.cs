// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Elastic.Documentation.Mcp.Remote;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Mcp.Remote.Tests;

/// <summary>
/// End-to-end tests for the MCP initialize handshake. Verifies that the serverInfo.name,
/// serverInfo.version, and protocolVersion fields set in Program.cs are actually returned
/// by the server's initialize response — a regression in the wiring would produce wrong
/// metadata while all unit tests still pass.
/// </summary>
public class McpHandshakeTests
{
	[Test]
	public async Task Initialize_PublicProfile_ReturnsCorrectServerInfoAndProtocolVersion()
	{
		// Program.cs reads MCP_SERVER_PROFILE via Environment.GetEnvironmentVariable before
		// any DI override can run, so we set and restore the actual process variable to keep
		// the test deterministic when a developer's environment has the variable set differently.
		var previousProfile = Environment.GetEnvironmentVariable("MCP_SERVER_PROFILE");
		Environment.SetEnvironmentVariable("MCP_SERVER_PROFILE", "public");
		try
		{
			using var factory = new WebApplicationFactory<Program>();
			using var client = factory.CreateClient();

			// The client deliberately sends an older protocol version ("2024-11-05") rather than
			// the one the server pins to ("2025-11-25"). If options.ProtocolVersion regresses to
			// null the SDK echoes the client value, so this assertion would fail — proving the
			// pinning is actually enforced.
			const string initRequest = /*lang=json,strict*/
				"""
				{
				  "jsonrpc": "2.0",
				  "id": 1,
				  "method": "initialize",
				  "params": {
				    "protocolVersion": "2024-11-05",
				    "capabilities": {},
				    "clientInfo": { "name": "test-client", "version": "1.0.0" }
				  }
				}
				""";

			using var content = new StringContent(initRequest, Encoding.UTF8, "application/json");
			// MCP HTTP transport requires both JSON and SSE in the Accept header; the server
			// responds with SSE framing (text/event-stream). Extract the JSON from the data: line.
			using var request = new HttpRequestMessage(HttpMethod.Post, "/docs/_mcp") { Content = content };
			request.Headers.TryAddWithoutValidation("Accept", "application/json, text/event-stream");
			using var response = await client.SendAsync(request, TestContext.Current!.Execution.CancellationToken);

			response.StatusCode.Should().Be(HttpStatusCode.OK);

			var rawBody = await response.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken);
			// SSE format: lines starting with "data: " carry the JSON payload.
			var jsonLine = rawBody.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(
				l => l.StartsWith("data:", StringComparison.Ordinal)
			);
			jsonLine.Should().NotBeNull("response body should contain an SSE data line");
			var json = (jsonLine ?? "data:")["data:".Length..].Trim();
			using var doc = JsonDocument.Parse(json);

			var result = doc.RootElement.GetProperty("result");
			result.GetProperty("protocolVersion").GetString().Should().Be("2025-11-25");

			// Compute the expected version the same way Program.cs does so a regression to the
			// default assembly version (1.0.0.0) or the fallback ("0.0.0") fails this assertion.
			var expectedVersion = typeof(Program)
				.Assembly
				.GetCustomAttributes<AssemblyInformationalVersionAttribute>()
				.FirstOrDefault()?.InformationalVersion
				?? "0.0.0";

			var serverInfo = result.GetProperty("serverInfo");
			serverInfo.GetProperty("name").GetString().Should().Be(McpServerProfile.Public.ServiceName);
			serverInfo.GetProperty("version").GetString().Should().Be(expectedVersion);
		}
		finally
		{
			Environment.SetEnvironmentVariable("MCP_SERVER_PROFILE", previousProfile);
		}
	}
}
