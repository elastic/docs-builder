// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Documentation.Builder.Diagnostics.Console;
using Elastic.Documentation.Aspire;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Search;
using Elastic.Documentation.ServiceDefaults;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.Indices;
using Elastic.Mapping;
using Elastic.Markdown.Exporters.Elasticsearch;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elastic.Assembler.IntegrationTests.Search;

[CollectionDefinition(Collection)]
public class SearchBootstrapFixture(DocumentationFixture fixture) : IAsyncLifetime
{
	public const string Collection = "Search";
	public HttpClient HttpClient { get; private set; } = null!;
	public bool Connected { get; private set; }

	/// <summary>
	/// Initializes the test by ensuring AssemblerServe (which hosts the API) is healthy and Elasticsearch is indexed.
	/// Checks if the remote Elasticsearch already has up-to-date data to avoid unnecessary indexing.
	/// </summary>
	public async ValueTask InitializeAsync()
	{
		try
		{
			// Wait for AssemblerServe to be ready (it hosts the embedded Lambda API)
			Console.WriteLine("Waiting for AssemblerServe (with embedded API) to become healthy...");
			await fixture.DistributedApplication.ResourceNotifications
				.WaitForResourceHealthyAsync(ResourceNames.AssemblerServe, cancellationToken: TestContext.Current.CancellationToken)
				.WaitAsync(TimeSpan.FromMinutes(2), TestContext.Current.CancellationToken);

			Console.WriteLine("AssemblerServe is healthy. Creating HTTP client...");

			// Get the HTTP client for AssemblerServe which includes the API endpoints
			HttpClient = fixture.DistributedApplication.CreateHttpClient(ResourceNames.AssemblerServe, "http");
			HttpClient.Should().NotBeNull("Should be able to create HTTP client for AssemblerServe");

			// Check if Elasticsearch already has up-to-date data
			var indexingNeeded = await IsIndexingNeeded();

			if (!Connected)
			{
				Console.WriteLine("Can not connect to Elasticsearch. Skipping indexing.");
				return;
			}

			if (!indexingNeeded)
			{
				Console.WriteLine("Elasticsearch already has up-to-date data. Skipping indexing.");
				return;
			}

			Console.WriteLine("Elasticsearch needs indexing. Manually starting indexer...");

			// The indexer always has WithExplicitStart(), so we must manually start it
			// Get the ResourceLoggerService to send the start command
			fixture.DistributedApplication.Services
				.GetRequiredService<ResourceLoggerService>();

			// Get the resource notification service to find the resource
			fixture.DistributedApplication.Services
				.GetRequiredService<ResourceNotificationService>();

			// Wait for the resource to be available
			var resourceEvent = await fixture.DistributedApplication.ResourceNotifications
				.WaitForResourceAsync(ResourceNames.ElasticsearchIngest, _ => true, TestContext.Current.CancellationToken)
				.WaitAsync(TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);

			// Get the resource instance
			var resource = resourceEvent.Resource;

			// Execute the start command using ResourceCommandAnnotation
			var startCommand = resource.Annotations.OfType<ResourceCommandAnnotation>()
				.FirstOrDefault(a => a.Name == "resource-start");

			if (startCommand != null)
			{
				Console.WriteLine($"Executing start command for {ResourceNames.ElasticsearchIngest}...");

				// Create ExecuteCommandContext for the start command
				var commandContext = new ExecuteCommandContext
				{
					ResourceName = resourceEvent.ResourceId,
					ServiceProvider = fixture.DistributedApplication.Services,
					CancellationToken = TestContext.Current.CancellationToken
				};

				await startCommand.ExecuteCommand(commandContext);
				Console.WriteLine($"Start command executed for {ResourceNames.ElasticsearchIngest}");
			}
			else
			{
				throw new Exception($"Could not find start command for {ResourceNames.ElasticsearchIngest}");
			}

			Console.WriteLine("Waiting for indexer to complete...");

			// Wait for the indexer to complete
			_ = await fixture.DistributedApplication.ResourceNotifications
				.WaitForResourceAsync(ResourceNames.ElasticsearchIngest, KnownResourceStates.TerminalStates,
					cancellationToken: TestContext.Current.CancellationToken)
				.WaitAsync(TimeSpan.FromMinutes(10), TestContext.Current.CancellationToken);

			Console.WriteLine("Elasticsearch indexer reached terminal state. Validating exit code...");

			// Validate the indexer completed successfully
			await ValidateResourceExitCode(ResourceNames.ElasticsearchIngest);

			Console.WriteLine("Elasticsearch indexing completed successfully. Tests can now run.");
		}
		catch (Exception e)
		{
			Console.WriteLine($"Failed to initialize test: {e.Message}");
			Console.WriteLine(string.Join(Environment.NewLine,
				fixture.InMemoryLogger.RecordedLogs.Reverse().Take(50).Reverse()));
			throw;
		}
	}

	/// <summary>
	/// Checks if indexing is needed by comparing the channel hash in Elasticsearch
	/// with the current semantic exporter channel hash.
	/// Uses the same pattern as ElasticsearchMarkdownExporter.
	/// </summary>
	private async ValueTask<bool> IsIndexingNeeded()
	{
		try
		{
			var endpoints = ElasticsearchEndpointFactory.Create();

			var endpoint = endpoints.Elasticsearch;
			Console.WriteLine($"Checking remote Elasticsearch at {endpoint.Uri} for existing data...");

			var transport = ElasticsearchTransportFactory.Create(endpoint);
			Connected = (await transport.HeadAsync("/", TestContext.Current.CancellationToken)).ApiCallDetails.HasSuccessfulStatusCode;

			// Create a logger factory and diagnostics collector
			var loggerFactory = fixture.DistributedApplication.Services.GetRequiredService<ILoggerFactory>();
			var collector = new ConsoleDiagnosticsCollector(loggerFactory);

			// Create semantic type context to check channel hash (index namespace is 'dev' for tests)
			var semanticTypeContext = DocumentationMappingContext.DocumentationDocumentSemantic.CreateContext(type: "assembler") with
			{
				ConfigureAnalysis = a => DocumentationAnalysisFactory.BuildAnalysis(a, "docs-assembler", [])
			};

			var options = new IngestChannelOptions<DocumentationDocument>(transport, semanticTypeContext);
			using var channel = new IngestChannel<DocumentationDocument>(options);

			// Get the current hash from Elasticsearch index template
			var currentSemanticHash = await channel.GetIndexTemplateHashAsync(TestContext.Current.CancellationToken) ?? string.Empty;

			// Get the expected channel hash
			_ = await channel.BootstrapElasticsearchAsync(BootstrapMethod.Silent, TestContext.Current.CancellationToken);
			var expectedSemanticHash = channel.ChannelHash;

			Console.WriteLine($"Elasticsearch semantic hash: '{currentSemanticHash}'");
			Console.WriteLine($"Expected semantic hash: '{expectedSemanticHash}'");

			// If hashes match, no indexing needed
			if (!string.IsNullOrEmpty(currentSemanticHash) && currentSemanticHash == expectedSemanticHash)
			{
				Console.WriteLine("Semantic channel hashes match. Skipping indexing.");
				return false;
			}

			Console.WriteLine("Semantic channel hashes do not match or remote hash is empty. Indexing needed.");
			return true;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error checking Elasticsearch state: {ex.Message}. Will proceed with indexing.");
			return true; // If we can't check, safer to index
		}
	}

	private async ValueTask ValidateResourceExitCode(string resourceName)
	{
		var eventResource = await fixture.DistributedApplication.ResourceNotifications
			.WaitForResourceAsync(resourceName, _ => true);
		var id = eventResource.ResourceId;

		if (!fixture.DistributedApplication.ResourceNotifications.TryGetCurrentState(id, out var state))
			throw new Exception($"Could not find {resourceName} in the current state");

		if (state.Snapshot.ExitCode is not 0)
		{
			var recentLogs = string.Join(Environment.NewLine,
				fixture.InMemoryLogger.RecordedLogs.Reverse().Take(100).Reverse());
			throw new Exception(
				$"Exit code should be 0 for {resourceName}, but was {state.Snapshot.ExitCode}. Recent logs:{Environment.NewLine}{recentLogs}");
		}

		Console.WriteLine($"{resourceName} completed with exit code 0");
	}

	public ValueTask DisposeAsync()
	{
		HttpClient?.Dispose();

		// Only dump logs if test failed
		if (TestContext.Current.TestState?.Result is not TestResult.Passed)
		{
			foreach (var log in fixture.InMemoryLogger.RecordedLogs.Reverse().Take(50).Reverse())
				Console.WriteLine(log.Message);
		}

		GC.SuppressFinalize(this);
		return default;
	}
}
