// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Elastic.Documentation.Search;

namespace Elastic.Documentation.IntegrationTests.Smoke;

public class ApiSmokeTests(DocumentationFixture fixture, ITestOutputHelper output) : IAsyncLifetime
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
		using var client = fixture.CreateApiClient();
		var response = await client.GetAsync("/docs/_api/health", TestContext.Current.CancellationToken);
		_ = response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task AliveEndpoint_Returns200()
	{
		using var client = fixture.CreateApiClient();
		var response = await client.GetAsync("/docs/_api/alive", TestContext.Current.CancellationToken);
		_ = response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task SearchEndpoint_ReturnsResults()
	{
		using var client = fixture.CreateApiClient();
		var response = await client.GetAsync("/docs/_api/v1/search?q=elasticsearch", TestContext.Current.CancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			var diagnostics = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
			Assert.Fail($"Search endpoint returned {(int)response.StatusCode} {response.StatusCode}:\n{diagnostics}");
		}

		var body = await response.Content.ReadFromJsonAsync<FullSearchResponse>(TestContext.Current.CancellationToken);
		Assert.NotNull(body);
		Assert.SkipUnless(body.TotalResults > 0 && body.Results.Count > 0, "search index has no data, skipping result assertions");
		_ = body.Results.Should().NotBeEmpty("search for 'elasticsearch' should return results when the index is populated");
		body.Results.Should().AllSatisfy(r =>
		{
			_ = r.Url.Should().NotBeNullOrEmpty();
			_ = r.Title.Should().NotBeNullOrEmpty();
		});
	}

	[Fact]
	public async Task ChangesEndpoint_ReturnsResponse()
	{
		using var client = fixture.CreateApiClient();
		var since = Uri.EscapeDataString("2020-01-01T00:00:00Z");
		// The changes endpoint requires open_point_in_time privilege. CI uses a read-only API key that lacks it.
		Assert.SkipUnless(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")),
			"Skipping: CI read-only API key lacks open_point_in_time privilege required by the changes endpoint");

		var response = await client.GetAsync($"/docs/_api/v1/changes?since={since}", TestContext.Current.CancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			var diagnostics = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
			Assert.Fail($"Changes endpoint returned {(int)response.StatusCode} {response.StatusCode}:\n{diagnostics}");
		}

		var body = await response.Content.ReadFromJsonAsync<ChangesResponse>(TestContext.Current.CancellationToken);
		_ = body.Should().NotBeNull();
		body.Pages.Should().NotBeNull("pages collection should always be present even when empty");
	}
}
