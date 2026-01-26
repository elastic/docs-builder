// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Documentation.Builder.Diagnostics;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Isolated;
using Microsoft.Extensions.Logging;
using Actions.Core;
using Actions.Core.Services;
using Actions.Core.Summaries;

namespace Documentation.Builder.Http;

public enum BuildStatus
{
	Idle,
	Building,
	Complete
}

public record BuildEvent(
	[property: JsonPropertyName("type")] string Type,
	[property: JsonPropertyName("timestamp")] long Timestamp,
	[property: JsonPropertyName("diagnostic")] DiagnosticDto? Diagnostic = null,
	[property: JsonPropertyName("errors")] int? Errors = null,
	[property: JsonPropertyName("warnings")] int? Warnings = null,
	[property: JsonPropertyName("hints")] int? Hints = null,
	[property: JsonPropertyName("status")] string? Status = null,
	[property: JsonPropertyName("diagnostics")] DiagnosticDto[]? Diagnostics = null
);

public record DiagnosticDto(
	[property: JsonPropertyName("severity")] string Severity,
	[property: JsonPropertyName("file")] string File,
	[property: JsonPropertyName("message")] string Message,
	[property: JsonPropertyName("line")] int? Line = null,
	[property: JsonPropertyName("column")] int? Column = null
);

public class InMemoryBuildState(ILoggerFactory loggerFactory, IConfigurationContext configurationContext) : IDisposable
{
	private readonly ILoggerFactory _loggerFactory = loggerFactory;
	private readonly IConfigurationContext _configurationContext = configurationContext;
	private readonly ILogger<InMemoryBuildState> _logger = loggerFactory.CreateLogger<InMemoryBuildState>();
	private readonly SemaphoreSlim _buildSemaphore = new(1, 1);
	private readonly Lock _diagnosticsLock = new();
	private readonly List<DiagnosticDto> _diagnostics = [];

	// Reuse MockFileSystem across builds to benefit from caching
	private readonly MockFileSystem _writeFs = new();

	// Broadcast: maintain list of connected client channels
	private readonly Lock _clientsLock = new();
	private readonly List<Channel<BuildEvent>> _clientChannels = [];

	private CancellationTokenSource? _currentBuildCts;
	private Task? _currentBuildTask;

	private int _errorCount;
	private int _warningCount;
	private int _hintCount;

	public BuildStatus Status { get; private set; } = BuildStatus.Idle;
	public int ErrorCount => _errorCount;
	public int WarningCount => _warningCount;
	public int HintCount => _hintCount;

	/// <summary>
	/// Subscribe a new client to receive build events. Returns a ChannelReader for the client.
	/// </summary>
	public ChannelReader<BuildEvent> Subscribe()
	{
		var channel = Channel.CreateUnbounded<BuildEvent>(new UnboundedChannelOptions
		{
			SingleReader = true,
			SingleWriter = true
		});

		lock (_clientsLock)
		{
			_clientChannels.Add(channel);
		}

		_logger.LogDebug("Client subscribed to diagnostics stream. Total clients: {Count}", _clientChannels.Count);
		return channel.Reader;
	}

	/// <summary>
	/// Unsubscribe a client from build events.
	/// </summary>
	public void Unsubscribe(ChannelReader<BuildEvent> reader)
	{
		lock (_clientsLock)
		{
			var channel = _clientChannels.FirstOrDefault(c => c.Reader == reader);
			if (channel != null)
			{
				_ = _clientChannels.Remove(channel);
				_ = channel.Writer.TryComplete();
			}
		}

		_logger.LogDebug("Client unsubscribed from diagnostics stream. Total clients: {Count}", _clientChannels.Count);
	}

	public async Task StartBuildAsync(string sourcePath, Cancel externalCt)
	{
		// Cancel any existing build
		if (_currentBuildCts != null)
		{
			_logger.LogDebug("Cancelling previous in-memory build");
			await _currentBuildCts.CancelAsync();

			// Wait for the previous build to complete (with timeout)
			if (_currentBuildTask != null)
			{
				try
				{
					await _currentBuildTask.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
				}
				catch (TimeoutException)
				{
					_logger.LogWarning("Previous build did not complete within timeout");
				}
				catch (OperationCanceledException)
				{
					// Expected
				}
			}
		}

		// Create a new CTS linked to the external token
		_currentBuildCts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
		var buildCt = _currentBuildCts.Token;

		// Start the new build
		_currentBuildTask = ExecuteBuildAsync(sourcePath, buildCt);
		await _currentBuildTask;
	}

	private async Task ExecuteBuildAsync(string sourcePath, Cancel ct)
	{
		await _buildSemaphore.WaitAsync(ct);
		try
		{
			Status = BuildStatus.Building;
			_ = Interlocked.Exchange(ref _errorCount, 0);
			_ = Interlocked.Exchange(ref _warningCount, 0);
			_ = Interlocked.Exchange(ref _hintCount, 0);

			// Clear stored diagnostics
			lock (_diagnosticsLock)
			{
				_diagnostics.Clear();
			}

			// Emit build_start event
			await BroadcastEventAsync(new BuildEvent("build_start", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));

			ct.ThrowIfCancellationRequested();

			// Create a diagnostics collector that streams to our channel
			var streamingCollector = new StreamingDiagnosticsCollector(_loggerFactory, this);

			var readFs = new FileSystem();
			var service = new IsolatedBuildService(_loggerFactory, _configurationContext, new NullCoreService());

			_logger.LogInformation("Starting in-memory validation build for {Path}", sourcePath);

			_ = await service.Build(
				streamingCollector,
				readFs,
				sourcePath,
				null,  // output
				null,  // pathPrefix
				true,  // force - always rebuild for validation
				false, // strict
				false, // allowIndexing
				false, // metadataOnly
				ExportOptions.Default,
				null,  // canonicalBaseUrl
				_writeFs, // reuse MockFileSystem across builds for caching
				true,  // skipOpenApi - skip for faster validation builds
				false, // skipCrossLinks - enable cross-links (cached in MockFileSystem)
				ct
			);

			// Stop the collector to complete the channel
			await streamingCollector.StopAsync(ct);

			Status = BuildStatus.Complete;

			// Emit build_complete event
			await BroadcastEventAsync(new BuildEvent(
				"build_complete",
				DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
				Errors: ErrorCount,
				Warnings: WarningCount,
				Hints: HintCount
			));

			_logger.LogInformation("In-memory build complete: {Errors} errors, {Warnings} warnings, {Hints} hints",
				ErrorCount, WarningCount, HintCount);
		}
		catch (OperationCanceledException)
		{
			_logger.LogDebug("In-memory build was cancelled");
			Status = BuildStatus.Idle;

			// Emit build_cancelled event
			await BroadcastEventAsync(new BuildEvent("build_cancelled", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error during in-memory build: {Message}", ex.Message);
			Status = BuildStatus.Complete;

			// Emit build_complete with current counts even on error
			await BroadcastEventAsync(new BuildEvent(
				"build_complete",
				DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
				Errors: ErrorCount,
				Warnings: WarningCount,
				Hints: HintCount
			));
		}
		finally
		{
			_ = _buildSemaphore.Release();
		}
	}

	/// <summary>
	/// Broadcast an event to all connected clients
	/// </summary>
	internal async Task BroadcastEventAsync(BuildEvent buildEvent)
	{
		List<Channel<BuildEvent>> deadChannels = [];

		lock (_clientsLock)
		{
			foreach (var channel in _clientChannels)
			{
				if (!channel.Writer.TryWrite(buildEvent))
				{
					// Channel is full or closed, mark for removal
					deadChannels.Add(channel);
				}
			}

			// Remove dead channels
			foreach (var dead in deadChannels)
			{
				_ = _clientChannels.Remove(dead);
				_ = dead.Writer.TryComplete();
			}
		}

		await Task.CompletedTask; // Keep async signature for consistency
	}

	internal void IncrementCount(Severity severity)
	{
		switch (severity)
		{
			case Severity.Error:
				_ = Interlocked.Increment(ref _errorCount);
				break;
			case Severity.Warning:
				_ = Interlocked.Increment(ref _warningCount);
				break;
			case Severity.Hint:
				_ = Interlocked.Increment(ref _hintCount);
				break;
		}
	}

	internal void StoreDiagnostic(DiagnosticDto diagnostic)
	{
		lock (_diagnosticsLock)
		{
			_diagnostics.Add(diagnostic);
		}
	}

	public DiagnosticDto[] GetStoredDiagnostics()
	{
		lock (_diagnosticsLock)
		{
			return [.. _diagnostics];
		}
	}

	public BuildEvent GetCurrentState() => new(
		"state",
		DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
		Errors: ErrorCount,
		Warnings: WarningCount,
		Hints: HintCount,
		Status: Status.ToString().ToLowerInvariant(),
		Diagnostics: GetStoredDiagnostics()
	);

	public void Dispose()
	{
		_currentBuildCts?.Cancel();
		_currentBuildCts?.Dispose();
		_buildSemaphore.Dispose();

		// Close all client channels
		lock (_clientsLock)
		{
			foreach (var channel in _clientChannels)
			{
				_ = channel.Writer.TryComplete();
			}
			_clientChannels.Clear();
		}

		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// A diagnostics collector that streams diagnostics to the InMemoryBuildState
	/// </summary>
	private sealed class StreamingDiagnosticsCollector(ILoggerFactory logFactory, InMemoryBuildState buildState)
		: DiagnosticsCollector([new Log(logFactory.CreateLogger<Log>())])
	{
		public override void Write(Diagnostic diagnostic)
		{
			base.Write(diagnostic);

			// Increment counters
			buildState.IncrementCount(diagnostic.Severity);

			// Create DTO and store it
			var dto = new DiagnosticDto(
				diagnostic.Severity.ToString().ToLowerInvariant(),
				diagnostic.File,
				diagnostic.Message,
				diagnostic.Line,
				diagnostic.Column
			);

			// Store for later retrieval by new clients
			buildState.StoreDiagnostic(dto);

			// Emit diagnostic event to all connected clients
			var buildEvent = new BuildEvent(
				"diagnostic",
				DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
				Diagnostic: dto
			);

			_ = buildState.BroadcastEventAsync(buildEvent);
		}

		protected override void HandleItem(Diagnostic diagnostic) { }

		public override Task StopAsync(Cancel cancellationToken)
		{
			// Don't call base.StopAsync() as we don't use the background reader loop
			// Just complete the channel
			Channel.TryComplete();
			return Task.CompletedTask;
		}
	}

	/// <summary>
	/// A null implementation of ICoreService for in-memory builds
	/// </summary>
#pragma warning disable IDE0060 // Remove unused parameter - required by interface
	private sealed class NullCoreService : ICoreService
	{
		public string GetInput(string name) => string.Empty;
		public string GetInput(string name, InputOptions? options) => string.Empty;
		public string[] GetMultilineInput(string name, InputOptions? options = null) => [];
		public bool GetBoolInput(string name, InputOptions? options = null) => false;
		public Task SetOutputAsync(string name, string value) => Task.CompletedTask;
		public ValueTask SetOutputAsync<T>(string name, T value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>? jsonTypeInfo = null) => ValueTask.CompletedTask;
		public ValueTask ExportVariableAsync(string name, string value) => ValueTask.CompletedTask;
		public void SetSecret(string secret) { }
		public ValueTask AddPathAsync(string inputPath) => ValueTask.CompletedTask;
		public void SetFailed(string message) { }
		public void SetCommandEcho(bool enabled) { }
		public void WriteDebug(string message) { }
		public void WriteError(string message, AnnotationProperties? properties = null) { }
		public void WriteWarning(string message, AnnotationProperties? properties = null) { }
		public void WriteNotice(string message, AnnotationProperties? properties = null) { }
		public void WriteInfo(string message) { }
		public void StartGroup(string name) { }
		public void EndGroup() { }
		public ValueTask<T> GroupAsync<T>(string name, Func<ValueTask<T>> action) => action();
		public ValueTask SaveStateAsync<T>(string name, T value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>? jsonTypeInfo = null) => ValueTask.CompletedTask;
		public string GetState(string name) => string.Empty;
		public Summary Summary { get; } = new();
		public bool IsDebug => false;
	}
#pragma warning restore IDE0060
}
