// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using AwesomeAssertions;
using Elastic.Documentation.Mcp.Remote;
using Elastic.Documentation.Mcp.Remote.Gateways;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
		// Program.cs reads MCP_SERVER_PROFILE from configuration, so pin it per host instead of mutating the process
		// environment. That keeps the test deterministic whatever the developer's environment says, and safe in parallel.
		using var baseFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseSetting("MCP_SERVER_PROFILE", "public"));
		// Tool calls in the .http files must not reach Elasticsearch: a developer's credentials would query a real
		// cluster and an unreachable one would stall the test until the transport times out.
		using var factory = baseFactory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
		{
			_ = services.RemoveAll<IDocumentGateway>();
			_ = services.AddScoped<IDocumentGateway, EchoDocumentGateway>();
		}));
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

	/// <summary>Answers every URL with a document at that URL, so a snapshot shows the tool's argument arrived.</summary>
	private sealed class EchoDocumentGateway : IDocumentGateway
	{
		public Task<DocumentResult?> GetByUrlAsync(string url, CancellationToken ct = default) =>
			Task.FromResult<DocumentResult?>(new DocumentResult { Url = url, Title = "Stub page", Type = "doc" });

		public Task<DocumentStructure?> GetStructureAsync(string url, CancellationToken ct = default) =>
			Task.FromResult<DocumentStructure?>(null);
	}
}
