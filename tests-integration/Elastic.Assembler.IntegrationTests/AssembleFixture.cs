// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Elastic.Documentation.ServiceDefaults;
using FluentAssertions;
using InMemLogger;
using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static Elastic.Documentation.Aspire.ResourceNames;

[assembly: CaptureConsole, AssemblyFixture(typeof(Elastic.Assembler.IntegrationTests.DocumentationFixture))]

namespace Elastic.Assembler.IntegrationTests;

public class DocumentationFixture : IAsyncLifetime
{
	private bool _failedBootstrap;
	public DistributedApplication DistributedApplication { get; private set; } = null!;

	public InMemoryLogger InMemoryLogger { get; private set; } = null!;

	/// <inheritdoc />
	public async ValueTask InitializeAsync()
	{
		var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire>(
			["--skip-private-repositories", "--assume-cloned"],
			(options, settings) =>
			{
				options.DisableDashboard = true;
				options.AllowUnsecuredTransport = true;
				options.EnableResourceLogging = true;
			}
		);
		_ = builder.Services.AddElasticDocumentationLogging(LogLevel.Information);
		_ = builder.Services.AddLogging(c => c.AddXUnit());
		_ = builder.Services.AddLogging(c => c.AddInMemory());
		DistributedApplication = await builder.BuildAsync();
		InMemoryLogger = DistributedApplication.Services.GetService<InMemoryLogger>()!;
		_ = DistributedApplication.StartAsync().WaitAsync(TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken);

		_ = await DistributedApplication.ResourceNotifications
			.WaitForResourceAsync(AssemblerClone, KnownResourceStates.TerminalStates, cancellationToken: TestContext.Current.CancellationToken)
			.WaitAsync(TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken);

		await ValidateExitCode(AssemblerClone);

		_ = await DistributedApplication.ResourceNotifications
			.WaitForResourceAsync(AssemblerBuild, KnownResourceStates.TerminalStates, cancellationToken: TestContext.Current.CancellationToken)
			.WaitAsync(TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken);

		await ValidateExitCode(AssemblerBuild);

		_ = await DistributedApplication.ResourceNotifications
			.WaitForResourceHealthyAsync(AssemblerServe, cancellationToken: TestContext.Current.CancellationToken)
			.WaitAsync(TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);
	}

	private async ValueTask ValidateExitCode(string resourceName)
	{
		var eventResource = await DistributedApplication.ResourceNotifications.WaitForResourceAsync(resourceName, _ => true);
		var id = eventResource.ResourceId;
		if (!DistributedApplication.ResourceNotifications.TryGetCurrentState(id, out var e))
			throw new Exception($"Could not find {resourceName} in the current state");
		if (e.Snapshot.ExitCode is not 0)
		{
			await DistributedApplication.StopAsync();
			await DistributedApplication.DisposeAsync();
			throw new Exception($"Exit code should be 0 for {resourceName}: {string.Join(Environment.NewLine, InMemoryLogger.RecordedLogs.Reverse().Take(30).Reverse())}");
		}
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		await DistributedApplication.StopAsync();
		await DistributedApplication.DisposeAsync();
		GC.SuppressFinalize(this);
	}
}

public class ServeStaticTests(DocumentationFixture fixture, ITestOutputHelper output) : IAsyncLifetime
{
	[Fact]
	public async Task AssertRequestToRootReturnsData()
	{
		var client = fixture.DistributedApplication.CreateHttpClient(AssemblerServe, "http");
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
