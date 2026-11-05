// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using AwesomeAssertions;
using Elastic.Documentation.Mcp.Remote;
using Microsoft.AspNetCore.Mvc.Testing;
using Nullean.Snapshots;
using Nullean.Snapshots.Http;
using Nullean.Snapshots.TUnit;

namespace Mcp.Remote.Tests;

/// <summary>
/// End-to-end tests for the MCP initialize handshake. Verifies that the serverInfo.name,
/// serverInfo.version, and protocolVersion fields set in Program.cs are actually returned
/// by the server's initialize response — a regression in the wiring would produce wrong
/// metadata while all unit tests still pass. Each <c>http/*.http</c> file is one test; its responses
/// are in <c>.snapshots/http/*.verified.responses</c>.
/// </summary>
public class McpHandshakeTests
{
	[Test]
	[HttpFiles("http")]
	public async Task Run(SnapshotFile file)
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

			var transcript = await HttpFileSnapshot.RunAsync(
				file.Path,
				new HttpFileSnapshotOptions { Client = client },
				TestContext.Current!.Execution.CancellationToken
			);

			// Computed the same way Program.cs does, so a regression to the default assembly version (1.0.0.0)
			// or the fallback ("0.0.0") leaves a different value in the snapshot instead of the placeholder.
			var expectedVersion = typeof(Program)
				.Assembly
				.GetCustomAttributes<AssemblyInformationalVersionAttribute>()
				.FirstOrDefault()?.InformationalVersion
				?? "0.0.0";

			transcript.Should().MatchHttpFileSnapshot(
				new SnapshotOptions().Scrub(SseScrubbers.PrettyPrintDataLines).Scrub(
					text => text.Replace(expectedVersion, "{server-version}", StringComparison.Ordinal)
				)
			);
		}
		finally
		{
			Environment.SetEnvironmentVariable("MCP_SERVER_PROFILE", previousProfile);
		}
	}
}
