// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Elastic.Documentation.ServiceDefaults;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: CaptureConsole, AssemblyFixture(typeof(Elastic.Assembler.IntegrationTests.AssembleFixture))]

namespace Elastic.Assembler.IntegrationTests;


public class AssembleFixture : IAsyncLifetime
{
	public DistributedApplication DistributedApplication { get; private set; } = null!;

	/// <inheritdoc />
	public async ValueTask InitializeAsync()
	{
		var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Elastic_Documentation_Aspire>();
		_ = builder.Services.AddAppLogging(LogLevel.Information);
		DistributedApplication = await builder.BuildAsync();
		await DistributedApplication.StartAsync();
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		await DistributedApplication.StopAsync();
		await DistributedApplication.DisposeAsync();
		GC.SuppressFinalize(this);
	}


}

public class DatabaseTestClass1(AssembleFixture fixture, ITestOutputHelper output)
{
	[Fact]
	public async Task X()
	{
		_ = await fixture.DistributedApplication.ResourceNotifications
			.WaitForResourceHealthyAsync("DocsBuilderServeStatic", cancellationToken: TestContext.Current.CancellationToken);
		var client = fixture.DistributedApplication.CreateHttpClient("DocsBuilderServeStatic", "http");
		var root = await client.GetStringAsync("/", TestContext.Current.CancellationToken);
		output.WriteLine(root);
		_ = root.Should().NotBeNullOrEmpty();
	}


	// ...
}
