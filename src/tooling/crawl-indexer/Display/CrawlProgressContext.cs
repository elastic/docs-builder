// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Spectre.Console;

namespace CrawlIndexer.Display;

/// <summary>
/// Manages crawl progress display with Spectre.Console live rendering.
/// </summary>
public sealed class CrawlProgressContext : IDisposable
{
	private int _urlsDiscovered;
	private int _urlsCrawled;
	private int _urlsSkipped;
	private int _urlsFailed;
	private long _bytesDownloaded;
	private readonly DateTime _startTime;
	private readonly List<string> _recentUrls = [];
	private readonly Lock _lock = new();

	public CrawlProgressContext()
	{
		IsInteractive = !IsRunningOnCi() && !System.Console.IsOutputRedirected;
		_startTime = DateTime.UtcNow;
	}

	private static bool IsRunningOnCi() =>
		!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) ||
		!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")) ||
		!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD")) ||
		!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JENKINS_URL"));

	public bool IsInteractive { get; }

	public void ReportUrlDiscovered(int count)
	{
		lock (_lock)
		{
			_urlsDiscovered += count;
		}
	}

	public void ReportUrlCrawled(string url, int bytes)
	{
		lock (_lock)
		{
			_urlsCrawled++;
			_bytesDownloaded += bytes;
			_recentUrls.Insert(0, url);
			if (_recentUrls.Count > 5)
				_recentUrls.RemoveAt(5);
		}
	}

	public void ReportUrlSkipped(string url, string reason)
	{
		lock (_lock)
		{
			_urlsSkipped++;
		}

		if (!IsInteractive)
			AnsiConsole.MarkupLine($"[grey]SKIP[/] {Markup.Escape(url)} [dim]({reason})[/]");
	}

	public void ReportUrlFailed(string url, string error)
	{
		lock (_lock)
		{
			_urlsFailed++;
		}

		if (!IsInteractive)
			AnsiConsole.MarkupLine($"[red]FAIL[/] {Markup.Escape(url)} [dim]({error})[/]");
	}

	public CrawlStats GetStats()
	{
		lock (_lock)
		{
			return new CrawlStats(
				_urlsDiscovered,
				_urlsCrawled,
				_urlsSkipped,
				_urlsFailed,
				_bytesDownloaded,
				DateTime.UtcNow - _startTime,
				[.. _recentUrls]
			);
		}
	}

	public async Task RunWithLiveAsync(string title, Func<CrawlProgressContext, CrawlLiveContext?, Task> action)
	{
		if (!IsInteractive)
		{
			AnsiConsole.MarkupLine($"[aqua]{title}[/]");
			await action(this, null);
			// Final summary is displayed by IndexingDisplay.DisplayFinalSummary in commands
			return;
		}

		// Suppress info logs during live display to prevent interference
		LiveDisplayState.SuppressInfoLogs = true;
		try
		{
			await AnsiConsole.Progress()
				.AutoRefresh(true)
				.AutoClear(false)
				.HideCompleted(false)
				.Columns(
					new SpinnerColumn(),
					new TaskDescriptionColumn(),
					new ProgressBarColumn(),
					new PercentageColumn(),
					new DownloadedColumn(),
					new TransferSpeedColumn(),
					new RemainingTimeColumn()
				)
				.StartAsync(async ctx =>
				{
					var crawlTask = ctx.AddTask("[aqua]🔍 Crawling pages[/]", maxValue: _urlsDiscovered);
					var indexTask = ctx.AddTask("[yellow]📦 Indexing documents[/]", maxValue: _urlsDiscovered);

					var displayContext = new CrawlLiveContext(crawlTask, indexTask);

					// Start refresh task to update progress bars
					using var cts = new CancellationTokenSource();
					var refreshTask = RefreshProgressAsync(crawlTask, indexTask, cts.Token);

					try
					{
						await action(this, displayContext);
					}
					finally
					{
						await cts.CancelAsync();
						try
						{
							await refreshTask;
						}
						catch (OperationCanceledException)
						{
							// Expected
						}
					}

					// Ensure 100% completion
					crawlTask.Value = crawlTask.MaxValue;
					indexTask.Value = indexTask.MaxValue;
					crawlTask.Description = "[green]✓ Crawling complete[/]";
					indexTask.Description = "[green]✓ Indexing complete[/]";
				});
		}
		finally
		{
			LiveDisplayState.SuppressInfoLogs = false;
		}

		// Final summary is displayed by IndexingDisplay.DisplayFinalSummary in commands
	}

	private async Task RefreshProgressAsync(ProgressTask crawlTask, ProgressTask indexTask, CancellationToken ct)
	{
		while (!ct.IsCancellationRequested)
		{
			try
			{
				await Task.Delay(100, ct);
				lock (_lock)
				{
					crawlTask.Value = _urlsCrawled + _urlsSkipped + _urlsFailed;
					indexTask.Value = _urlsCrawled;
					crawlTask.MaxValue = _urlsDiscovered;
					indexTask.MaxValue = _urlsDiscovered;

					if (_urlsCrawled > 0)
					{
						var rate = _urlsCrawled / (DateTime.UtcNow - _startTime).TotalSeconds;
						crawlTask.Description = $"[aqua]🔍 Crawling[/] [dim]({rate:F1} pages/s)[/]";
						indexTask.Description = $"[yellow]📦 Indexing[/] [dim]({_urlsFailed} errors)[/]";
					}
				}
			}
			catch (OperationCanceledException)
			{
				break;
			}
		}
	}

	private static string FormatBytes(long bytes)
	{
		string[] sizes = ["B", "KB", "MB", "GB"];
		var order = 0;
		var size = (double)bytes;
		while (size >= 1024 && order < sizes.Length - 1)
		{
			order++;
			size /= 1024;
		}
		return $"{size:0.##} {sizes[order]}";
	}

	public void Dispose()
	{
		// Nothing to dispose
	}
}

public readonly record struct CrawlStats(
	int UrlsDiscovered,
	int UrlsCrawled,
	int UrlsSkipped,
	int UrlsFailed,
	long BytesDownloaded,
	TimeSpan Elapsed,
	IReadOnlyList<string> RecentUrls
);

public sealed class CrawlLiveContext(ProgressTask crawlTask, ProgressTask indexTask)
{
	public ProgressTask CrawlTask => crawlTask;
	public ProgressTask IndexTask => indexTask;
}
