// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
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
	private static readonly FrozenSet<string> AssetExtensions = new[]
	{
		".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp",
		".yml", ".yaml", ".toml"
	}.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

	private FileSystemWatcher? _watcher;
	private CancellationTokenSource? _serviceCts;
	private Task? _backgroundBuildTask;
	private ReloadableGeneratorState ReloadableGenerator { get; } = reloadableGenerator;
	private InMemoryBuildState InMemoryBuildState { get; } = inMemoryBuildState;
	private ILogger Logger { get; } = logger;

	private readonly Debouncer _debouncer = new(TimeSpan.FromMilliseconds(500));

	public async Task StartAsync(Cancel cancellationToken)
	{
		_serviceCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

		// Await the live-reload generator so the server can serve pages immediately.
		var sourcePath = ReloadableGenerator.Generator.Context.DocumentationCheckoutDirectory?.FullName
			?? ReloadableGenerator.Generator.Context.DocumentationSourceDirectory.FullName;
		await ReloadableGenerator.ReloadAsync(cancellationToken);

		// Start the build loop; only shutdownCt (Ctrl+C / app exit) can cancel a running build.
		// File-edit triggers enqueue via ScheduleBuild and never interrupt the current build.
		_backgroundBuildTask = InMemoryBuildState.RunAsync(_serviceCts.Token);
		InMemoryBuildState.ScheduleBuild(sourcePath);

		// ReSharper disable once RedundantAssignment
		var directory = ReloadableGenerator.Generator.DocumentationSet.SourceDirectory.FullName;
#if DEBUG
		// Fall back to source directory when there is no separate checkout directory (e.g. when serving the project's own docs from a worktree)
		directory = ReloadableGenerator.Generator.Context.DocumentationCheckoutDirectory?.FullName
			?? ReloadableGenerator.Generator.DocumentationSet.SourceDirectory.FullName;
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
		foreach (var ext in AssetExtensions)
			watcher.Filters.Add($"*{ext}");
		watcher.IncludeSubdirectories = true;
		watcher.EnableRaisingEvents = true;
		_watcher = watcher;
	}

	private void Reload(bool reloadConfiguration = false)
	{
		var token = _serviceCts?.Token ?? Cancel.None;
		_debouncer.Schedule(async ctx =>
		{
			await ReloadableGenerator.ReloadAsync(ctx, reloadConfiguration);
			Logger.LogInformation("Reload complete!");
			_ = LiveReloadMiddleware.RefreshWebSocketRequest();

			// Schedule a validation build after every reload — both content edits and structural changes.
			// The build loop coalesces rapid triggers: a new request while a build runs queues one more.
			var sourcePath = ReloadableGenerator.Generator.Context.DocumentationCheckoutDirectory?.FullName
				?? ReloadableGenerator.Generator.Context.DocumentationSourceDirectory.FullName;
			InMemoryBuildState.ScheduleBuild(sourcePath);
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

		// Wait briefly for the build loop to exit cleanly.
		if (_backgroundBuildTask is not null)
		{
			try
			{
				await _backgroundBuildTask.WaitAsync(TimeSpan.FromSeconds(2), CancellationToken.None);
			}
			catch (TimeoutException)
			{
				Logger.LogDebug("Background build loop did not stop within timeout during shutdown");
			}
			catch (OperationCanceledException)
			{
				// Expected
			}
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

	private static bool IsAssetFile(string path) =>
		AssetExtensions.Contains(Path.GetExtension(path));

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
		else if (IsAssetFile(e.FullPath))
			_ = LiveReloadMiddleware.RefreshWebSocketRequest();
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
		else if (IsAssetFile(e.FullPath))
			_ = LiveReloadMiddleware.RefreshWebSocketRequest();
	}

	private void OnDeleted(object sender, FileSystemEventArgs e)
	{
		if (ShouldIgnorePath(e.FullPath))
			return;

		Logger.LogInformation("Deleted: {FullPath}", e.FullPath);
		if (e.FullPath.EndsWith(".md") || IsConfigFile(e.FullPath))
			Reload(reloadConfiguration: true);
		else if (IsAssetFile(e.FullPath))
			_ = LiveReloadMiddleware.RefreshWebSocketRequest();
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
		else if (IsAssetFile(e.FullPath) || IsAssetFile(e.OldFullPath))
			_ = LiveReloadMiddleware.RefreshWebSocketRequest();
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

	/// <summary>
	/// True debounce: each call to <see cref="Schedule"/> resets the timer. The action fires only
	/// after the window elapses without another call. Pending but not-yet-fired actions are cancelled
	/// when a newer one arrives, which also cancels any in-progress <see cref="ReloadAsync"/> —
	/// the generator falls back to its previous state until the next debounced action completes.
	/// </summary>
	private sealed class Debouncer(TimeSpan window) : IDisposable
	{
		private readonly Lock _lock = new();
		private CancellationTokenSource? _pendingCts;

		public void Schedule(Func<Cancel, Task> action, Cancel cancellationToken)
		{
			CancellationTokenSource newCts;
			lock (_lock)
			{
				_pendingCts?.Cancel();
				_pendingCts?.Dispose();
				newCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				_pendingCts = newCts;
			}
			_ = Task.Run(async () =>
			{
				try
				{
					await Task.Delay(window, newCts.Token);
					await action(newCts.Token);
				}
				catch (OperationCanceledException) { }
			}, newCts.Token);
		}

		public void Dispose()
		{
			lock (_lock)
			{
				_pendingCts?.Cancel();
				_pendingCts?.Dispose();
				_pendingCts = null;
			}
		}
	}
}
