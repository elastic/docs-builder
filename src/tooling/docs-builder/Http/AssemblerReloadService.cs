// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.Assembler.Navigation;
using Elastic.Documentation.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Westwind.AspNetCore.LiveReload;

namespace Documentation.Builder.Http;

public sealed class AssemblerReloadService(
	IReadOnlyList<AssemblerDocumentationSet> assemblerSets,
	bool watchMarkdown,
	ILogger<AssemblerReloadService> logger
) : IHostedService, IDisposable
{
	private readonly List<FileSystemWatcher> _watchers = [];
	private CancellationTokenSource? _serviceCts;
	private readonly Debouncer _debouncer = new(TimeSpan.FromMilliseconds(200));

	public Task StartAsync(Cancel cancellationToken)
	{
		_serviceCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

		if (watchMarkdown)
			StartMarkdownWatchers();

		StartStaticAssetsWatcher();

		return Task.CompletedTask;
	}

	private void StartMarkdownWatchers()
	{
		foreach (var set in assemblerSets)
		{
			var checkoutDir = set.Checkout.Directory.FullName;
			if (!Directory.Exists(checkoutDir))
				continue;

			logger.LogInformation("Start markdown watch on checkout: {Directory}", checkoutDir);
			var watcher = new FileSystemWatcher(checkoutDir)
			{
				NotifyFilter = NotifyFilters.Attributes
								| NotifyFilters.CreationTime
								| NotifyFilters.DirectoryName
								| NotifyFilters.FileName
								| NotifyFilters.LastWrite
								| NotifyFilters.Security
								| NotifyFilters.Size
			};
			watcher.Filters.Add("*.md");
			watcher.Filters.Add("docset.yml");
			watcher.Filters.Add("_docset.yml");
			watcher.Filters.Add("toc.yml");
			watcher.IncludeSubdirectories = true;
			watcher.EnableRaisingEvents = true;

			watcher.Changed += (_, e) => OnMarkdownChanged(e.FullPath, set);
			watcher.Created += (_, e) => OnMarkdownChanged(e.FullPath, set);
			watcher.Deleted += (_, e) => OnMarkdownChanged(e.FullPath, set);
			watcher.Renamed += (_, e) => OnMarkdownChanged(e.FullPath, set);
			watcher.Error += (_, e) => logger.LogError(e.GetException(), "File watcher error in {Directory}", checkoutDir);

			_watchers.Add(watcher);
		}
	}

	private void StartStaticAssetsWatcher()
	{
		var solutionRoot = Paths.GetSolutionDirectory();
		if (solutionRoot is null)
			return;

		var staticDir = Path.Join(solutionRoot.FullName, "src", "Elastic.Documentation.Site", "_static");
		if (!Directory.Exists(staticDir))
			return;

		logger.LogInformation("Start static assets watch on: {Directory}", staticDir);
		var watcher = new FileSystemWatcher(staticDir)
		{
			NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
			IncludeSubdirectories = true,
			EnableRaisingEvents = true
		};

		watcher.Changed += (_, e) => OnStaticAssetChanged(e.FullPath);
		watcher.Created += (_, e) => OnStaticAssetChanged(e.FullPath);
		watcher.Renamed += (_, e) => OnStaticAssetChanged(e.FullPath);
		watcher.Error += (_, e) => logger.LogError(e.GetException(), "Static assets watcher error in {Directory}", staticDir);

		_watchers.Add(watcher);
	}

	private static bool ShouldIgnorePath(string path) =>
		path.Contains("/.artifacts/") || path.Contains("\\.artifacts\\") ||
		path.Contains("/.git/") || path.Contains("\\.git\\") ||
		path.Contains("/node_modules/") || path.Contains("\\node_modules\\");

	private void OnMarkdownChanged(string fullPath, AssemblerDocumentationSet set)
	{
		if (ShouldIgnorePath(fullPath))
			return;

		logger.LogInformation("Markdown changed: {FullPath}", fullPath);

		var token = _serviceCts?.Token ?? Cancel.None;
		_ = _debouncer.ExecuteAsync(async _ =>
		{
			set.DocumentationSet.InvalidateResolved();
			logger.LogInformation("Invalidated {RepositoryName}, triggering live reload", set.Checkout.Repository.Name);
			await Task.Run(() => LiveReloadMiddleware.RefreshWebSocketRequest(), CancellationToken.None);
		}, token);
	}

	private void OnStaticAssetChanged(string fullPath)
	{
		logger.LogInformation("Static asset changed: {FullPath}", fullPath);

		var token = _serviceCts?.Token ?? Cancel.None;
		_ = _debouncer.ExecuteAsync(async _ =>
		{
			logger.LogInformation("Triggering live reload for static asset change");
			await Task.Run(() => LiveReloadMiddleware.RefreshWebSocketRequest(), CancellationToken.None);
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
		foreach (var watcher in _watchers)
			watcher.Dispose();
		_watchers.Clear();
	}

	public void Dispose()
	{
		_serviceCts?.Dispose();
		foreach (var watcher in _watchers)
			watcher.Dispose();
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
