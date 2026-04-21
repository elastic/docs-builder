// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Westwind.AspNetCore.LiveReload;

[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(Documentation.Builder.Http.HotReloadManager))]

namespace Documentation.Builder.Http;

public static class HotReloadManager
{
	public static void ClearCache(Type[]? _) => LiveReloadMiddleware.RefreshWebSocketRequest();

	public static void UpdateApplication(Type[]? _) => Task.Run(async () =>
	{
		await Task.Delay(1000);
		var __ = LiveReloadMiddleware.RefreshWebSocketRequest();
		Console.WriteLine("UpdateApplication");
	});
}

public sealed class ReloadGeneratorService(
	ReloadableGeneratorState reloadableGenerator,
	InMemoryBuildState inMemoryBuildState,
	ILogger<ReloadGeneratorService> logger
) : IHostedService, IDisposable
{
	private FileSystemWatcher? _watcher;
	private CancellationTokenSource? _serviceCts;
	private ReloadableGeneratorState ReloadableGenerator { get; } = reloadableGenerator;
	private InMemoryBuildState InMemoryBuildState { get; } = inMemoryBuildState;
	private ILogger Logger { get; } = logger;

	//debounce reload requests due to many file changes
	private readonly Debouncer _debouncer = new(TimeSpan.FromMilliseconds(200));

	public async Task StartAsync(Cancel cancellationToken)
	{
		_serviceCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

		// Run live reload and in-memory validation build in parallel
		var sourcePath = ReloadableGenerator.Generator.Context.DocumentationSourceDirectory.FullName;
		await Task.WhenAll(
			ReloadableGenerator.ReloadAsync(cancellationToken),
			InMemoryBuildState.StartBuildAsync(sourcePath, cancellationToken)
		);

		// ReSharper disable once RedundantAssignment
		var directory = ReloadableGenerator.Generator.DocumentationSet.SourceDirectory.FullName;
#if DEBUG
		directory = ReloadableGenerator.Generator.Context.DocumentationCheckoutDirectory?.FullName ?? throw new InvalidOperationException("No checkout directory");
#endif
		Logger.LogInformation("Start file watch on: {Directory}", directory);
		var watcher = new FileSystemWatcher(directory)
		{
			NotifyFilter = NotifyFilters.Attributes
							| NotifyFilters.CreationTime
							| NotifyFilters.DirectoryName
							| NotifyFilters.FileName
							| NotifyFilters.LastWrite
							| NotifyFilters.Security
							| NotifyFilters.Size
		};

		watcher.Changed += OnChanged;
		watcher.Created += OnCreated;
		watcher.Deleted += OnDeleted;
		watcher.Renamed += OnRenamed;
		watcher.Error += OnError;

#if DEBUG
		watcher.Filters.Add("*.cshtml");
#endif
		watcher.Filters.Add("*.md");
		watcher.Filters.Add("docset.yml");
		watcher.Filters.Add("_docset.yml");
		watcher.Filters.Add("toc.yml");
		watcher.IncludeSubdirectories = true;
		watcher.EnableRaisingEvents = true;
		_watcher = watcher;
	}

	private void Reload(bool reloadConfiguration = false)
	{
		var token = _serviceCts?.Token ?? Cancel.None;
		_ = _debouncer.ExecuteAsync(async ctx =>
		{
			var sourcePath = ReloadableGenerator.Generator.Context.DocumentationSourceDirectory.FullName;

			// Start in-memory validation build (runs in parallel)
			var validationTask = InMemoryBuildState.StartBuildAsync(sourcePath, ctx);

			// Wait for live reload to complete, then refresh the browser immediately
			await ReloadableGenerator.ReloadAsync(ctx, reloadConfiguration);
			Logger.LogInformation("Reload complete!");
			_ = LiveReloadMiddleware.RefreshWebSocketRequest();

			// Wait for validation build to complete
			await validationTask;
		}, token);
	}

	public async Task StopAsync(Cancel cancellationToken)
	{
		if (_serviceCts is not null)
		{
			await _serviceCts.CancelAsync();
			_serviceCts.Dispose();
			_serviceCts = null;
		}
		_watcher?.Dispose();
	}

	// Check if a path should be ignored (output directories, hidden folders, etc.)
	private static bool ShouldIgnorePath(string path) =>
		path.Contains("/.artifacts/") || path.Contains("\\.artifacts\\") ||
		path.Contains("/_site/") || path.Contains("\\_site\\") ||
		path.Contains("/node_modules/") || path.Contains("\\node_modules\\") ||
		path.Contains("/.git/") || path.Contains("\\.git\\");

	private static bool IsConfigFile(string path) =>
		path.EndsWith("docset.yml") || path.EndsWith("toc.yml");

	private void OnChanged(object sender, FileSystemEventArgs e)
	{
		if (e.ChangeType != WatcherChangeTypes.Changed)
			return;

		if (ShouldIgnorePath(e.FullPath))
			return;

		Logger.LogInformation("Changed: {FullPath}", e.FullPath);

		if (IsConfigFile(e.FullPath))
			Reload(reloadConfiguration: true);
		else if (e.FullPath.EndsWith(".md"))
			Reload();
#if DEBUG
		if (e.FullPath.EndsWith(".cshtml"))
			_ = LiveReloadMiddleware.RefreshWebSocketRequest();
#endif
	}

	private void OnCreated(object sender, FileSystemEventArgs e)
	{
		if (ShouldIgnorePath(e.FullPath))
			return;

		Logger.LogInformation("Created: {FullPath}", e.FullPath);
		if (e.FullPath.EndsWith(".md") || IsConfigFile(e.FullPath))
			Reload(reloadConfiguration: true);
	}

	private void OnDeleted(object sender, FileSystemEventArgs e)
	{
		if (ShouldIgnorePath(e.FullPath))
			return;

		Logger.LogInformation("Deleted: {FullPath}", e.FullPath);
		if (e.FullPath.EndsWith(".md") || IsConfigFile(e.FullPath))
			Reload(reloadConfiguration: true);
	}

	private void OnRenamed(object sender, RenamedEventArgs e)
	{
		if (ShouldIgnorePath(e.FullPath))
			return;

		Logger.LogInformation("Renamed:");
		Logger.LogInformation("    Old: {OldFullPath}", e.OldFullPath);
		Logger.LogInformation("    New: {NewFullPath}", e.FullPath);
		if (e.FullPath.EndsWith(".md") || e.OldFullPath.EndsWith(".md") || IsConfigFile(e.FullPath) || IsConfigFile(e.OldFullPath))
			Reload(reloadConfiguration: true);
#if DEBUG
		if (e.FullPath.EndsWith(".cshtml"))
			_ = LiveReloadMiddleware.RefreshWebSocketRequest();
#endif
	}

	private void OnError(object sender, ErrorEventArgs e) =>
		PrintException(e.GetException());

	private void PrintException(Exception? ex)
	{
		if (ex == null)
			return;
		Logger.LogError("Message: {Message}", ex.Message);
		Logger.LogError("Stacktrace:");
		Logger.LogError("{StackTrace}", ex.StackTrace ?? "No stack trace available");
		PrintException(ex.InnerException);
	}

	public void Dispose()
	{
		_serviceCts?.Dispose();
		_watcher?.Dispose();
		_debouncer.Dispose();
	}

	private sealed class Debouncer(TimeSpan window) : IDisposable
	{
		private readonly SemaphoreSlim _semaphore = new(1, 1);
		private readonly long _windowInTicks = window.Ticks;
		private long _nextRun;

		public async Task ExecuteAsync(Func<Cancel, Task> innerAction, Cancel cancellationToken)
		{
			var requestStart = DateTime.UtcNow.Ticks;

			try
			{
				await _semaphore.WaitAsync(cancellationToken);

				if (requestStart <= _nextRun)
					return;

				await innerAction(cancellationToken);

				_nextRun = requestStart + _windowInTicks;
			}
			finally
			{
				_ = _semaphore.Release();
			}
		}

		public void Dispose() => _semaphore.Dispose();
	}
}
