// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using CrawlIndexer.Caching;
using CrawlIndexer.Indexing;
using Elastic.Documentation.Diagnostics;
using Spectre.Console;
using Spectre.Console.Rendering;
using Color = Spectre.Console.Color;

namespace CrawlIndexer.Display;

/// <summary>
/// Displays indexing progress and results in a rich format.
/// </summary>
public static class IndexingDisplay
{
	public static void DisplayDryRunResults(IReadOnlyList<string> urls)
	{
		SpectreConsoleTheme.WriteSection("Dry Run Results");

		if (urls.Count == 0)
		{
			AnsiConsole.MarkupLine("[yellow]No URLs matched the specified filters.[/]");
			return;
		}

		// Show breakdown chart by path
		var pathGroups = urls
			.Select(u => new Uri(u))
			.GroupBy(u =>
			{
				var segments = u.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
				return segments.Length > 0 ? segments[0] : "root";
			})
			.OrderByDescending(g => g.Count())
			.Take(10)
			.ToList();

		var breakdown = new BreakdownChart()
			.Width(60);

		var colors = new[] { Color.Green, Color.Yellow, Color.Aqua, Color.Blue, Color.Magenta1, Color.Orange1, Color.Cyan1, Color.Purple, Color.Teal, Color.Grey };
		var colorIndex = 0;

		foreach (var group in pathGroups)
			_ = breakdown.AddItem(group.Key, group.Count(), colors[colorIndex++ % colors.Length]);

		AnsiConsole.MarkupLine($"[aqua]📊 URL Distribution[/] [dim]({urls.Count:N0} total URLs)[/]");
		AnsiConsole.Write(breakdown);
		AnsiConsole.WriteLine();

		var panel = new Panel(
			new Markup($"[green bold]{urls.Count:N0}[/] [dim]URLs ready for crawling[/]")
		)
		{
			Header = new PanelHeader("[aqua]✓ Dry Run Complete[/]"),
			Border = BoxBorder.Rounded,
			BorderStyle = Style.Parse("green"),
			Padding = new Padding(2, 1)
		};

		AnsiConsole.Write(panel);
	}

	public static void DisplayFinalSummary(
		CrawlStats crawlStats,
		CrawlDecisionStats? decisionStats = null,
		IEnumerable<IndexChannelInfo>? channelInfo = null
	)
	{
		AnsiConsole.WriteLine();

		// Order: Total → Cached → Crawled → Skipped → Failed
		var rows = new List<IRenderable>();

		// Total discovered (from sitemap filtering)
		var totalUrls = decisionStats?.TotalUrls ?? crawlStats.UrlsDiscovered;
		rows.Add(new Markup($"[aqua]🔍 Total URLs:[/] [white]{totalUrls:N0}[/]"));

		// Cached (unchanged)
		if (decisionStats is not null && decisionStats.UnchangedUrls > 0)
			rows.Add(new Markup($"[blue]📋 Cached (unchanged):[/] [white]{decisionStats.UnchangedUrls:N0}[/]"));

		// Crawled
		rows.Add(new Markup($"[green]✓ Crawled:[/] [white]{crawlStats.UrlsCrawled:N0}[/] pages"));

		// Skipped
		rows.Add(new Markup($"[grey]⊘ Skipped:[/] [white]{crawlStats.UrlsSkipped:N0}[/]"));

		// Failed
		rows.Add(new Markup($"[red]✗ Failed:[/] [white]{crawlStats.UrlsFailed:N0}[/]"));

		rows.Add(new Rule { Style = Style.Parse("grey") });
		rows.Add(new Markup($"[cyan]📦 Downloaded:[/] [white]{FormatBytes(crawlStats.BytesDownloaded)}[/]"));
		rows.Add(new Markup($"[magenta]⏱ Duration:[/] [white]{crawlStats.Elapsed:hh\\:mm\\:ss}[/]"));

		// Show search aliases
		var channels = channelInfo?.ToList();
		if (channels is { Count: > 0 })
		{
			rows.Add(new Rule { Style = Style.Parse("grey") });
			var aliases = string.Join(", ", channels.Select(c => c.Alias));
			rows.Add(new Markup($"[yellow]🔎 Search aliases:[/] [white]{aliases}[/]"));
		}

		var panel = new Panel(new Rows(rows))
		{
			Header = new PanelHeader("[aqua bold]✨ Crawl Complete ✨[/]"),
			Border = BoxBorder.Double,
			BorderStyle = Style.Parse("aqua"),
			Padding = new Padding(2, 1)
		};

		AnsiConsole.Write(panel);

		if (crawlStats.UrlsFailed == 0 && crawlStats.UrlsCrawled > 0)
		{
			AnsiConsole.WriteLine();
			AnsiConsole.Write(
				new FigletText("SUCCESS!")
					.Color(Color.Green)
					.Centered()
			);
		}
	}

	public static void DisplayDryRunWithCacheStats(CrawlDecisionStats stats, int staleUrls)
	{
		SpectreConsoleTheme.WriteSection("Dry Run Results");

		var savingsPercent = stats.TotalUrls > 0
			? 100.0 * stats.UnchangedUrls / stats.TotalUrls
			: 0;

		var panel = new Panel(
			new Rows(
				new Markup($"[green]🆕 New URLs:[/] [white]{stats.NewUrls:N0}[/]"),
				new Markup($"[grey]✓ Unchanged (cached):[/] [white]{stats.UnchangedUrls:N0}[/]"),
				new Markup($"[yellow]🔄 To verify (HTTP):[/] [white]{stats.PossiblyChangedUrls:N0}[/]"),
				new Rule { Style = Style.Parse("grey") },
				new Markup($"[aqua]Total URLs:[/] [white]{stats.TotalUrls:N0}[/]"),
				new Markup($"[cyan]URLs to crawl:[/] [white]{stats.UrlsToCrawl:N0}[/]"),
				staleUrls > 0
					? new Markup($"[red]🗑 Stale (to delete):[/] [white]{staleUrls:N0}[/]")
					: new Markup("[dim]No stale URLs[/]"),
				new Rule { Style = Style.Parse("grey") },
				new Markup("[dim]Estimated HTTP savings:[/]"),
				new Markup($"[dim]  • Skipped requests: {stats.UnchangedUrls:N0} ({savingsPercent:F0}%)[/]")
			)
		)
		{
			Header = new PanelHeader("[aqua bold]📊 Crawl Analysis[/]"),
			Border = BoxBorder.Rounded,
			BorderStyle = Style.Parse("aqua"),
			Padding = new Padding(2, 1)
		};

		AnsiConsole.Write(panel);
	}

	public static void DisplayCacheLoadProgress(int loaded, string? currentUrl)
	{
		var shortUrl = currentUrl?.Length > 60 ? "..." + currentUrl[^57..] : currentUrl;
		AnsiConsole.MarkupLine($"[dim]Loaded {loaded:N0} docs[/] [grey]{Markup.Escape(shortUrl ?? "")}[/]");
	}

	/// <summary>
	/// Displays error summary using Errata-style formatting.
	/// </summary>
	public static void DisplayErrorSummary(IDiagnosticsCollector diagnostics)
	{
		if (diagnostics is CrawlIndexerDiagnosticsCollector crawlDiagnostics)
			crawlDiagnostics.WriteErrorsToConsole();
	}

	/// <summary>
	/// Displays index bootstrap status with hash comparison details.
	/// </summary>
	public static void DisplayBootstrapStatus(IEnumerable<IndexChannelInfo> channelInfo)
	{
		SpectreConsoleTheme.WriteSection("Index Bootstrap");

		var table = new Table()
			.Border(TableBorder.Rounded)
			.BorderColor(Color.Aqua)
			.AddColumn(new TableColumn("[aqua]Alias[/]").LeftAligned())
			.AddColumn(new TableColumn("[aqua]Index[/]").LeftAligned())
			.AddColumn(new TableColumn("[aqua]Status[/]").Centered())
			.AddColumn(new TableColumn("[aqua]Hash Match[/]").Centered());

		foreach (var channel in channelInfo)
		{
			var status = channel.IsReusing
				? "[green]Reusing existing[/]"
				: string.IsNullOrEmpty(channel.ServerHash)
					? "[yellow]New index[/]"
					: "[yellow]Recreating[/]";

			var hashMatch = string.IsNullOrEmpty(channel.ServerHash)
				? "[dim]N/A[/]"
				: channel.IsReusing
					? $"[green]✓[/] [dim]{channel.ServerHash[..Math.Min(8, channel.ServerHash.Length)]}[/]"
					: $"[red]✗[/] [dim]{channel.ServerHash[..Math.Min(8, channel.ServerHash.Length)]} → {channel.ChannelHash[..Math.Min(8, channel.ChannelHash.Length)]}[/]";

			_ = table.AddRow(
				new Markup(Markup.Escape(channel.Alias)),
				new Markup(Markup.Escape(channel.IndexName)),
				new Markup(status),
				new Markup(hashMatch)
			);
		}

		AnsiConsole.Write(table);
		AnsiConsole.WriteLine();
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
}
