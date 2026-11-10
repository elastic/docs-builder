// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

extern alias DocsApi;

using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Nullean.Snapshots;
using Nullean.Snapshots.Http;
using Nullean.Snapshots.TUnit;

namespace Elastic.Documentation.Journeys.Tests.Areas;

/// <summary>The docs API (search, AI answers, page feedback), in memory, for the endpoints that need no credentials.</summary>
public class DocsApiHttp
{
	[Test]
	[HttpFiles("http/docs-api")]
	public async Task Run(SnapshotFile file)
	{
		using var factory = new WebApplicationFactory<DocsApi::Program>();
		using var client = factory.CreateClient();

		var transcript = await HttpFileSnapshot.RunAsync(
			file.Path,
			new HttpFileSnapshotOptions { Client = client },
			TestContext.Current!.Execution.CancellationToken
		);
		transcript.Should().MatchHttpFileSnapshot();
	}
}
