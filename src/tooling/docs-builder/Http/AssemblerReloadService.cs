// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Assembler.Navigation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Westwind.AspNetCore.LiveReload;

namespace Documentation.Builder.Http;

public sealed class AssemblerReloadService(
	IReadOnlyList<AssemblerDocumentationSet> assemblerSets,
	ILogger<AssemblerReloadService> logger
) : IHostedService, IDisposable
{
	private readonly List<FileSystemWatcher> _watchers = [];
	private CancellationTokenSource? _serviceCts;
	private readonly Debouncer _debouncer = new(TimeSpan.FromMilliseconds(200));

	public Task StartAsync(Cancel cancellationToken)
	{
		_serviceCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

		foreach (var set in assemblerSets)
		{
			var checkoutDir = set.Checkout.Directory.FullName;
			if (!Directory.Exists(checkoutDir))
				continue;

			logger.LogInformation("Start file watch on checkout: {Directory}", checkoutDir);
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

			watcher.Changed += (_, e) => OnChanged(e.FullPath, set);
			watcher.Created += (_, e) => OnChanged(e.FullPath, set);
			watcher.Deleted += (_, e) => OnChanged(e.FullPath, set);
			watcher.Renamed += (_, e) => OnChanged(e.FullPath, set);
			watcher.Error += (_, e) => logger.LogError(e.GetException(), "File watcher error in {Directory}", checkoutDir);

			_watchers.Add(watcher);
		}

		return Task.CompletedTask;
	}

	private static bool ShouldIgnorePath(string path) =>
		path.Contains("/.artifacts/") || path.Contains("\\.artifacts\\") ||
		path.Contains("/.git/") || path.Contains("\\.git\\") ||
		path.Contains("/node_modules/") || path.Contains("\\node_modules\\");

	private void OnChanged(string fullPath, AssemblerDocumentationSet set)
	{
		if (ShouldIgnorePath(fullPath))
			return;

		logger.LogInformation("Changed: {FullPath}", fullPath);

		var token = _serviceCts?.Token ?? Cancel.None;
		_ = _debouncer.ExecuteAsync(async _ =>
		{
			// Invalidate so the next RenderLayout call re-parses the changed files
			set.DocumentationSet.InvalidateResolved();
			logger.LogInformation("Invalidated {RepositoryName}, triggering live reload", set.Checkout.Repository.Name);
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
