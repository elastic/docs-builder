// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Aspire.Hosting.Testing;
using AwesomeAssertions;
using Elastic.Documentation.Aspire;
using TUnit.Core.Interfaces;

namespace Elastic.Documentation.IntegrationTests;

[ClassDataSource<DocumentationFixture>(Shared = SharedType.PerAssembly)]
public class ServeStaticTests(DocumentationFixture fixture) : IAsyncInitializer, IAsyncDisposable
{
	[Test]
	public async Task AssertRequestToRootReturnsData()
	{
		var client = fixture.DistributedApplication.CreateHttpClient(ResourceNames.AssemblerServe, "http");
		var root = await client.GetStringAsync("/", TestContext.Current!.Execution.CancellationToken);
		_ = root.Should().NotBeNullOrEmpty();
	}

	public ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		if (TestContext.Current!.Execution.Result?.State is not TestState.Failed)
			return default;
		foreach (var resource in fixture.InMemoryLogger.RecordedLogs)
			TestContext.Current?.Output.WriteLine(resource.Message);
		return default;
	}

	public Task InitializeAsync() => Task.CompletedTask;
}
