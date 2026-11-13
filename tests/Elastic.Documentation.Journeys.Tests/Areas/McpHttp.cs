// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

extern alias McpRemote;

using System.Reflection;
using AwesomeAssertions;
using Elastic.Documentation.Journeys.Tests.Targets;
using Microsoft.AspNetCore.Mvc.Testing;
using Nullean.Snapshots;
using Nullean.Snapshots.Http;
using Nullean.Snapshots.TUnit;

namespace Elastic.Documentation.Journeys.Tests.Areas;

/// <summary>
/// The remote MCP server, in memory. A deployed server answers the same requests, but its tools need Elasticsearch, so
/// the files stay with the ones that need no credentials.
/// </summary>
public class McpHttp
{
	[Before(Class)]
	public static void UsePublicProfile() =>
		// Program.cs reads the profile from the process environment before any service override can run.
		Environment.SetEnvironmentVariable("MCP_SERVER_PROFILE", "public");

	[Test]
	[HttpFiles("http/mcp")]
	public async Task Run(SnapshotFile file)
	{
		using var factory = new WebApplicationFactory<McpRemote::Program>();
		using var client = factory.CreateClient();

		var transcript = await HttpFileSnapshot.RunAsync(
			file.Path,
			new HttpFileSnapshotOptions { Client = client },
			TestContext.Current!.Execution.CancellationToken
		);

		var version = typeof(McpRemote::Program)
			.Assembly
			.GetCustomAttributes<AssemblyInformationalVersionAttribute>()
			.FirstOrDefault()?.InformationalVersion
			?? "0.0.0";

		// The wording of instructions and tool descriptions is covered by Mcp.Remote.Tests; this area keeps names, schemas and hints.
		transcript.Should().MatchHttpFileSnapshot(
			new SnapshotOptions().Scrub(BodyScrubbers.WithoutProse).Scrub(
				text => text.Replace(version, "{server-version}", StringComparison.Ordinal)
			)
		);
	}
}
