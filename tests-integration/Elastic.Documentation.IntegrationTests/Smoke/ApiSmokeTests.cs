// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Elastic.Documentation.Search;
using TUnit.Core.Interfaces;

namespace Elastic.Documentation.IntegrationTests.Smoke;

[ClassDataSource<DocumentationFixture>(Shared = SharedType.PerAssembly)]
public class ApiSmokeTests(DocumentationFixture fixture) : IAsyncInitializer, IAsyncDisposable
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
	public async Task HealthEndpoint_Returns200()
	{
		using var client = fixture.CreateApiClient();
		var response = await client.GetAsync("/docs/_api/health", TestContext.Current!.Execution.CancellationToken);
		_ = response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Test]
	public async Task AliveEndpoint_Returns200()
	{
		using var client = fixture.CreateApiClient();
		var response = await client.GetAsync("/docs/_api/alive", TestContext.Current!.Execution.CancellationToken);
		_ = response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Test]
	public async Task SearchEndpoint_ReturnsResults()
	{
		using var client = fixture.CreateApiClient();
		var response = await client.GetAsync("/docs/_api/v1/search?q=elasticsearch", TestContext.Current!.Execution.CancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			var diagnostics = await response.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken);
			throw new Exception($"Search endpoint returned {(int)response.StatusCode} {response.StatusCode}:\n{diagnostics}");
		}

		var body = await response.Content.ReadFromJsonAsync<FullSearchResponse>(TestContext.Current!.Execution.CancellationToken);
		body.Should().NotBeNull();
		Skip.Unless(body!.TotalResults > 0 && body.Results.Count > 0, "search index has no data, skipping result assertions");
		_ = body.Results.Should().NotBeEmpty("search for 'elasticsearch' should return results when the index is populated");
		body
			.Results
			.Should()
			.AllSatisfy(result =>
			{
				result.Title.Should().NotBeNullOrEmpty("each result should have a title");
				result.Url.Should().NotBeNullOrEmpty("each result should have a URL");
			});
	}

	[Test]
	public async Task ChangesEndpoint_ReturnsResults()
	{
		var since = DateTimeOffset.UtcNow.AddDays(-30).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
		using var client = fixture.CreateApiClient();
		Skip.Unless(fixture.DistributedApplication is not null, "Distributed application is not running");
		var response = await client.GetAsync($"/docs/_api/v1/changes?since={since}", TestContext.Current!.Execution.CancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			var diagnostics = await response.Content.ReadAsStringAsync(TestContext.Current!.Execution.CancellationToken);
			throw new Exception($"Changes endpoint returned {(int)response.StatusCode} {response.StatusCode}:\n{diagnostics}");
		}

		var body = await response.Content.ReadFromJsonAsync<ChangesResponse>(TestContext.Current!.Execution.CancellationToken);
		body.Should().NotBeNull();
	}
}
