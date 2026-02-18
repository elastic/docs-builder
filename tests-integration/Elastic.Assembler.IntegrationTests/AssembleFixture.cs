// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Elastic.Documentation.ServiceDefaults;
using InMemLogger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static Elastic.Documentation.Aspire.ResourceNames;

[assembly: CaptureConsole, AssemblyFixture(typeof(Elastic.Assembler.IntegrationTests.DocumentationFixture))]

namespace Elastic.Assembler.IntegrationTests;

public static class DistributedApplicationExtensions
{
	/// <summary>
	/// Ensures all parameters in the application configuration have values set.
	/// </summary>
	public static TBuilder WithEmptyParameters<TBuilder>(this TBuilder builder)
		where TBuilder : IDistributedApplicationTestingBuilder
	{
		var parameters = builder.Resources.OfType<ParameterResource>().Where(p => !p.IsConnectionString).ToList();
		foreach (var parameter in parameters)
			builder.Configuration[$"Parameters:{parameter.Name}"] = string.Empty;

		var configBuilder = new ConfigurationBuilder();
		_ = configBuilder.AddUserSecrets("72f50f33-6fb9-4d08-bff3-39568fe370b3");
		var config = configBuilder.Build();

		builder.Configuration[$"Parameters:DocumentationElasticUrl"] = config["Parameters:DocumentationElasticUrl"] ?? "http://localhost.example:9200";
		builder.Configuration[$"Parameters:DocumentationElasticApiKey"] = config["Parameters:DocumentationElasticApiKey"] ?? "not-configured";
		return builder;
	}
}


public class DocumentationFixture : IAsyncLifetime
{
	public DistributedApplication DistributedApplication { get; private set; } = null!;

	public InMemoryLogger InMemoryLogger { get; private set; } = null!;

	/// <inheritdoc />
	public async ValueTask InitializeAsync()
	{
		// --assume-build is not allowed on CI (blocks stale content), so only use it locally
		var isCI = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
		string[] args = isCI
			? ["--skip-private-repositories", "--assume-cloned"]
			: ["--skip-private-repositories", "--assume-cloned", "--assume-build"];

		var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.aspire>(
			args,
			(options, _) =>
			{
				options.DisableDashboard = true;
				options.AllowUnsecuredTransport = true;
				options.EnableResourceLogging = true;
			}
		);
		_ = builder.WithEmptyParameters();
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

		try
		{
			_ = await DistributedApplication.ResourceNotifications
				.WaitForResourceHealthyAsync(AssemblerServe, cancellationToken: TestContext.Current.CancellationToken)
				.WaitAsync(TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);
		}
		catch (Exception e)
		{
			await DistributedApplication.StopAsync();
			await DistributedApplication.DisposeAsync();
			throw new Exception($"{e.Message}: {string.Join(Environment.NewLine, InMemoryLogger.RecordedLogs.Reverse().Take(30).Reverse())}", e);
		}
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
			throw new Exception(
				$"Exit code should be 0 for {resourceName}: {string.Join(Environment.NewLine, InMemoryLogger.RecordedLogs.Reverse().Take(30).Reverse())}");
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
