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
		// decisionStats kept for API compatibility
		_ = decisionStats;

		AnsiConsole.WriteLine();

		var rows = new List<IRenderable>();

		// Crawling section grid
		var expectedCrawl = crawlStats.UrlsDiscovered;
		var actualCrawled = crawlStats.UrlsCrawled;
		var crawlDiff = expectedCrawl - actualCrawled;

		var crawlGrid = new Grid()
			.AddColumn(new GridColumn().NoWrap().PadRight(2))
			.AddColumn(new GridColumn().NoWrap());

		_ = crawlGrid.AddRow(
			new Markup("[aqua]🔍 Expected Crawl[/]"),
			new Markup($"[white]{expectedCrawl:N0}[/]")
		);
		_ = crawlGrid.AddRow(
			new Markup("[green]   Actually Crawled[/]"),
			new Markup($"[white]{actualCrawled:N0}[/]")
		);

		if (crawlDiff > 0)
		{
			if (crawlStats.UrlsSkipped > 0)
			{
				_ = crawlGrid.AddRow(
					new Markup("[grey]      ⊘ Skipped[/]"),
					new Markup($"[white]{crawlStats.UrlsSkipped:N0}[/]")
				);
			}
			if (crawlStats.UrlsUnavailable > 0)
			{
				_ = crawlGrid.AddRow(
					new Markup("[yellow]      ⚠ Unavailable[/]"),
					new Markup($"[white]{crawlStats.UrlsUnavailable:N0}[/]")
				);
			}
			if (crawlStats.UrlsFailed > 0)
			{
				_ = crawlGrid.AddRow(
					new Markup("[red]      ✗ Failed[/]"),
					new Markup($"[white]{crawlStats.UrlsFailed:N0}[/]")
				);
			}
		}

		rows.Add(crawlGrid);
		rows.Add(new Text(""));

		// Indexing section grid
		var expectedIndex = actualCrawled;
		var actualIndexed = crawlStats.UrlsIndexed;

		var indexGrid = new Grid()
			.AddColumn(new GridColumn().NoWrap().PadRight(2))
			.AddColumn(new GridColumn().NoWrap());

		_ = indexGrid.AddRow(
			new Markup("[yellow]📦 Expected Index[/]"),
			new Markup($"[white]{expectedIndex:N0}[/]")
		);
		_ = indexGrid.AddRow(
			new Markup("[green]   Actually Indexed[/]"),
			new Markup($"[white]{actualIndexed:N0}[/]")
		);

		if (crawlStats.IndexingErrors > 0)
		{
			_ = indexGrid.AddRow(
				new Markup("[red]      ✗ Failures[/]"),
				new Markup($"[white]{crawlStats.IndexingErrors:N0}[/]")
			);
		}

		rows.Add(indexGrid);
		rows.Add(new Rule { Style = Style.Parse("grey") });

		// Metadata grid
		var metaGrid = new Grid()
			.AddColumn(new GridColumn().NoWrap().PadRight(2))
			.AddColumn(new GridColumn().NoWrap());

		_ = metaGrid.AddRow(
			new Markup("[cyan]📦 Downloaded[/]"),
			new Markup($"[white]{FormatBytes(crawlStats.BytesDownloaded)}[/]")
		);
		_ = metaGrid.AddRow(
			new Markup("[magenta]⏱  Duration[/]"),
			new Markup($"[white]{crawlStats.Elapsed:hh\\:mm\\:ss}[/]")
		);

		rows.Add(metaGrid);

		// Show search aliases
		var channels = channelInfo?.ToList();
		if (channels is { Count: > 0 })
		{
			rows.Add(new Rule { Style = Style.Parse("grey") });
			var aliases = string.Join(", ", channels.Select(c => c.Alias));

			var aliasGrid = new Grid()
				.AddColumn(new GridColumn().NoWrap().PadRight(2))
				.AddColumn(new GridColumn().NoWrap());

			_ = aliasGrid.AddRow(
				new Markup("[yellow]🔎 Search aliases[/]"),
				new Markup($"[white]{aliases}[/]")
			);

			rows.Add(aliasGrid);
		}

		var panel = new Panel(new Rows(rows))
		{
			Header = new PanelHeader("[aqua bold]✨ Crawl Complete ✨[/]"),
			Border = BoxBorder.Double,
			BorderStyle = Style.Parse("aqua"),
			Padding = new Padding(2, 1)
		};

		AnsiConsole.Write(panel);

		var hasErrors = crawlStats.UrlsFailed > 0 || crawlStats.IndexingErrors > 0;
		if (!hasErrors && crawlStats.UrlsCrawled > 0)
		{
			AnsiConsole.WriteLine();
			AnsiConsole.Write(
				new FigletText("SUCCESS!")
					.Color(Color.Green)
					.Centered()
			);
		}
	}

	public static void DisplayDryRunWithCacheStats(
		CrawlDecisionStats stats,
		int staleUrls,
		int translationsDiscovered = 0,
		IReadOnlyDictionary<string, int>? translationsByLanguage = null,
		HashSet<string>? languageFilter = null
	)
	{
		SpectreConsoleTheme.WriteSection("Dry Run Results");

		var savingsPercent = stats.TotalUrls > 0
			? 100.0 * stats.UnchangedUrls / stats.TotalUrls
			: 0;

		var rows = new List<IRenderable>
		{
			new Markup($"[green]🆕 New URLs:[/] [white]{stats.NewUrls:N0}[/]"),
			new Markup($"[grey]✓ Unchanged (cached):[/] [white]{stats.UnchangedUrls:N0}[/]"),
			new Markup($"[yellow]🔄 To verify (HTTP):[/] [white]{stats.PossiblyChangedUrls:N0}[/]")
		};

		if (translationsDiscovered > 0)
		{
			rows.Add(new Rule { Style = Style.Parse("grey") });
			rows.Add(new Markup($"[cyan]🌐 Translations discovered:[/] [white]{translationsDiscovered:N0}[/]"));

			if (translationsByLanguage is { Count: > 0 })
			{
				var langBreakdown = string.Join(", ", translationsByLanguage
					.OrderByDescending(kv => kv.Value)
					.Select(kv => $"{GetLanguageFlag(kv.Key)} {kv.Value:N0}"));
				rows.Add(new Markup($"[dim]  {langBreakdown}[/]"));
			}
		}

		rows.Add(new Rule { Style = Style.Parse("grey") });
		rows.Add(new Markup($"[aqua]Total URLs:[/] [white]{stats.TotalUrls + translationsDiscovered:N0}[/]"));
		rows.Add(new Markup($"[cyan]URLs to crawl:[/] [white]{stats.UrlsToCrawl + translationsDiscovered:N0}[/]"));
		rows.Add(staleUrls > 0
			? new Markup($"[red]🗑 Stale (to delete):[/] [white]{staleUrls:N0}[/]")
			: new Markup("[dim]No stale URLs[/]"));

		if (languageFilter is { Count: > 0 })
		{
			var langs = string.Join(", ", languageFilter.Select(l => $"{GetLanguageFlag(l)} {l}"));
			rows.Add(new Markup($"[yellow]🔤 Language filter:[/] [white]{langs}[/]"));
		}

		rows.Add(new Rule { Style = Style.Parse("grey") });
		rows.Add(new Markup("[dim]Estimated HTTP savings:[/]"));
		rows.Add(new Markup($"[dim]  • Skipped requests: {stats.UnchangedUrls:N0} ({savingsPercent:F0}%)[/]"));

		var panel = new Panel(new Rows(rows))
		{
			Header = new PanelHeader("[aqua bold]📊 Crawl Analysis[/]"),
			Border = BoxBorder.Rounded,
			BorderStyle = Style.Parse("aqua"),
			Padding = new Padding(2, 1)
		};

		AnsiConsole.Write(panel);
	}

	/// <summary>
	/// Displays translation discovery summary with a BreakdownChart.
	/// </summary>
	public static void DisplayTranslationDiscoverySummary(int found, int fromCache, IReadOnlyDictionary<string, int> byLanguage)
	{
		if (found == 0)
		{
			SpectreConsoleTheme.WriteInfo("No translations discovered");
			return;
		}

		// Language colors for the chart
		var colors = new Dictionary<string, Color>
		{
			["de"] = Color.Yellow,
			["fr"] = Color.Blue,
			["es"] = Color.Orange1,
			["jp"] = Color.Red,
			["ja"] = Color.Red,
			["kr"] = Color.Cyan1,
			["ko"] = Color.Cyan1,
			["cn"] = Color.Green,
			["zh"] = Color.Green,
			["pt"] = Color.Magenta1
		};

		var chart = new BreakdownChart()
			.Width(60);

		foreach (var (lang, count) in byLanguage.OrderByDescending(kv => kv.Value))
		{
			var label = $"{GetLanguageFlag(lang)} {lang.ToUpper()}";
			_ = chart.AddItem(label, count, colors.GetValueOrDefault(lang, Color.Grey));
		}

		var cacheInfo = fromCache > 0 ? $" [dim]({fromCache:N0} from cache)[/]" : "";
		SpectreConsoleTheme.WriteSuccess($"Discovered [yellow]{found:N0}[/] translations across [cyan]{byLanguage.Count}[/] languages{cacheInfo}");
		AnsiConsole.Write(chart);
		AnsiConsole.WriteLine();
	}

	private static string GetLanguageFlag(string lang) =>
		lang.ToLowerInvariant() switch
		{
			"de" => "🇩🇪",
			"fr" => "🇫🇷",
			"es" => "🇪🇸",
			"jp" or "ja" => "🇯🇵",
			"kr" or "ko" => "🇰🇷",
			"cn" or "zh" => "🇨🇳",
			"pt" => "🇧🇷",
			"en" => "🇬🇧",
			_ => "🌐"
		};

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
