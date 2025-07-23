// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Http;

public sealed class ReloadGeneratorService(
	ReloadableGeneratorState reloadableGenerator,
	ILogger<ReloadGeneratorService> logger) : IHostedService,
	IDisposable
{
	private FileSystemWatcher? _watcher;
	private ReloadableGeneratorState ReloadableGenerator { get; } = reloadableGenerator;
	private ILogger Logger { get; } = logger;

	//debounce reload requests due to many file changes
	private readonly Debouncer _debouncer = new(TimeSpan.FromMilliseconds(200));

	public async Task StartAsync(Cancel cancellationToken)
	{
		await ReloadableGenerator.ReloadAsync(cancellationToken);

		var watcher = new FileSystemWatcher(ReloadableGenerator.Generator.DocumentationSet.SourceDirectory.FullName)
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

		watcher.Filters.Add("*.md");
		watcher.Filters.Add("docset.yml");
		watcher.IncludeSubdirectories = true;
		watcher.EnableRaisingEvents = true;
		_watcher = watcher;
	}

	private void Reload() =>
		_ = _debouncer.ExecuteAsync(async ctx =>
		{
			Logger.LogInformation("Reload due to changes!");
			await ReloadableGenerator.ReloadAsync(ctx);
			Logger.LogInformation("Reload complete!");
		}, default);

	public Task StopAsync(Cancel cancellationToken)
	{
		_watcher?.Dispose();
		return Task.CompletedTask;
	}

	private void OnChanged(object sender, FileSystemEventArgs e)
	{
		if (e.ChangeType != WatcherChangeTypes.Changed)
			return;

		if (e.FullPath.EndsWith("docset.yml"))
			Reload();
		if (e.FullPath.EndsWith(".md"))
			Reload();

		Logger.LogInformation("Changed: {FullPath}", e.FullPath);
	}

	private void OnCreated(object sender, FileSystemEventArgs e)
	{
		if (e.FullPath.EndsWith(".md"))
			Reload();
		Logger.LogInformation("Created: {FullPath}", e.FullPath);
	}

	private void OnDeleted(object sender, FileSystemEventArgs e)
	{
		if (e.FullPath.EndsWith(".md"))
			Reload();
		Logger.LogInformation("Deleted: {FullPath}", e.FullPath);
	}

	private void OnRenamed(object sender, RenamedEventArgs e)
	{
		Logger.LogInformation("Renamed:");
		Logger.LogInformation("    Old: {OldFullPath}", e.OldFullPath);
		Logger.LogInformation("    New: {NewFullPath}", e.FullPath);
		if (e.FullPath.EndsWith(".md"))
			Reload();
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
