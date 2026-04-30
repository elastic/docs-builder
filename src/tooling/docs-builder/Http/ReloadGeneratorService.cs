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
	private ReloadableGeneratorState ReloadableGenerator { get; } = reloadableGenerator;
	private InMemoryBuildState InMemoryBuildState { get; } = inMemoryBuildState;
	private ILogger Logger { get; } = logger;

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
		foreach (var ext in AssetExtensions)
			watcher.Filters.Add($"*{ext}");
		watcher.IncludeSubdirectories = true;
		watcher.EnableRaisingEvents = true;
		_watcher = watcher;
	}

	private void Reload(bool reloadConfiguration = false)
	{
		var token = _serviceCts?.Token ?? Cancel.None;
		_ = _debouncer.ExecuteAsync(async ctx =>
		{
			await ReloadableGenerator.ReloadAsync(ctx, reloadConfiguration);
			Logger.LogInformation("Reload complete!");
			_ = LiveReloadMiddleware.RefreshWebSocketRequest();

			// Only run the full validation build for structural changes (config/toc edits, file add/delete).
			// Content-only .md edits are picked up on the next request via ParseFullAsync.
			if (reloadConfiguration)
			{
				var sourcePath = ReloadableGenerator.Generator.Context.DocumentationSourceDirectory.FullName;
				await InMemoryBuildState.StartBuildAsync(sourcePath, ctx);
			}
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
