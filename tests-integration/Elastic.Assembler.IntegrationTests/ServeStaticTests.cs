// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Aspire.Hosting.Testing;
using Elastic.Documentation.Aspire;
using FluentAssertions;

namespace Elastic.Assembler.IntegrationTests;

public class ServeStaticTests(DocumentationFixture fixture, ITestOutputHelper output) : IAsyncLifetime
{
	[Fact]
	public async Task AssertRequestToRootReturnsData()
	{
		var client = fixture.DistributedApplication.CreateHttpClient(ResourceNames.AssemblerServe, "http");
		var root = await client.GetStringAsync("/", TestContext.Current.CancellationToken);
		_ = root.Should().NotBeNullOrEmpty();
	}


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

	/// <inheritdoc />
	public ValueTask InitializeAsync() => default;
}
