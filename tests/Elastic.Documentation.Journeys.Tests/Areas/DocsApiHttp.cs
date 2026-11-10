// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

extern alias DocsApi;

using AwesomeAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
		// The page-feedback bootstrap hosted service would call Elasticsearch with whatever credentials the environment or user secrets hold.
		using var factory = new WebApplicationFactory<DocsApi::Program>().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
		{
			var bootstrap = services.Where(
				d => d.ServiceType == typeof(IHostedService) && d.ImplementationType?.Name == "PageFeedbackBootstrapService"
			).ToList();
			foreach (var descriptor in bootstrap)
				_ = services.Remove(descriptor);
		}));
		using var client = factory.CreateClient();

		var transcript = await HttpFileSnapshot.RunAsync(
			file.Path,
			new HttpFileSnapshotOptions { Client = client },
			TestContext.Current!.Execution.CancellationToken
		);
		transcript.Should().MatchHttpFileSnapshot();
	}
}
