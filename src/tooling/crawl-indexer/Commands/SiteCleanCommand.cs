// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using ConsoleAppFramework;
using CrawlIndexer.Display;
using CrawlIndexer.Indexing;
using Elastic.Documentation.Configuration;
using Elastic.Markdown.Exporters.Elasticsearch;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace CrawlIndexer.Commands;

/// <summary>
/// Clean up old site indices and related resources from Elasticsearch.
/// </summary>
public class SiteCleanCommand(
	ILoggerFactory loggerFactory,
	IConfigurationContext configurationContext
)
{
	private static readonly string[] SiteIndexPrefixes = ["site-"];

	/// <summary>
	/// Remove old site indices, keeping the most recent ones.
	/// </summary>
	/// <param name="keepLast">Number of recent index sets to keep per pattern (default: 3)</param>
	/// <param name="dryRun">List indices without deleting</param>
	/// <param name="ctx">Cancellation token</param>
	[Command("")]
	public async Task Clean(int keepLast = 3, bool dryRun = false, Cancel ctx = default)
	{
		SpectreConsoleTheme.WriteHeader("site clean");

		var endpoints = configurationContext.Endpoints;
		var transport = ElasticsearchTransportFactory.Create(endpoints.Elasticsearch);
		var logger = loggerFactory.CreateLogger<IndexCleanupService>();
		var service = new IndexCleanupService(transport, logger);

		var buildType = endpoints.BuildType;
		var environment = endpoints.Environment;

		var prefixes = SiteIndexPrefixes
			.SelectMany(p => new[]
			{
				$"{p}{buildType}.lexical-{environment}-",
				$"{p}{buildType}.semantic-{environment}-",
				$"{p}{buildType}.semantic-{environment}-ai-cache"
			})
			.ToList();

		if (dryRun)
		{
			await DisplayDryRunAsync(service, prefixes, keepLast, ctx);
			return;
		}

		await RunCleanupWithProgressAsync(service, prefixes, keepLast, ctx);
	}

	private static async Task DisplayDryRunAsync(
		IndexCleanupService service,
		List<string> prefixes,
		int keepLast,
		CancellationToken ct
	)
	{
		SpectreConsoleTheme.WriteDryRunBanner();
		SpectreConsoleTheme.WriteSection("Index Discovery");

		var totalKept = 0;
		var totalToDelete = 0;

		foreach (var prefix in prefixes)
		{
			var indices = await service.ListIndicesAsync($"{prefix}*", ct);
			if (indices.Count == 0)
				continue;

			var kept = indices.Take(keepLast).ToList();
			var toDelete = indices.Skip(keepLast).ToList();
			totalKept += kept.Count;
			totalToDelete += toDelete.Count;

			var table = new Table()
				.Border(TableBorder.Rounded)
				.BorderColor(Color.Grey)
				.AddColumn("[aqua]Index[/]")
				.AddColumn(new TableColumn("[aqua]Docs[/]").RightAligned())
				.AddColumn(new TableColumn("[aqua]Size[/]").RightAligned())
				.AddColumn("[aqua]Created[/]")
				.AddColumn("[aqua]Action[/]");

			foreach (var idx in kept)
			{
				_ = table.AddRow(
					$"[green]{Markup.Escape(idx.Name)}[/]",
					$"[white]{idx.DocsCount:N0}[/]",
					$"[white]{idx.StoreSize}[/]",
					$"[dim]{idx.CreationDate:yyyy-MM-dd HH:mm}[/]",
					"[green]keep[/]"
				);
			}

			foreach (var idx in toDelete)
			{
				_ = table.AddRow(
					$"[red]{Markup.Escape(idx.Name)}[/]",
					$"[white]{idx.DocsCount:N0}[/]",
					$"[white]{idx.StoreSize}[/]",
					$"[dim]{idx.CreationDate:yyyy-MM-dd HH:mm}[/]",
					"[red]delete[/]"
				);
			}

			AnsiConsole.Write(table);
			AnsiConsole.WriteLine();
		}

		var panel = new Panel(
			new Rows(
				new Markup($"[green]✓ Keeping:[/] [white]{totalKept:N0}[/] indices"),
				new Markup($"[red]✗ Would delete:[/] [white]{totalToDelete:N0}[/] indices")
			)
		)
		{
			Header = new PanelHeader("[aqua bold]📊 Dry Run Summary[/]"),
			Border = BoxBorder.Rounded,
			BorderStyle = Style.Parse("aqua"),
			Padding = new Padding(2, 1)
		};
		AnsiConsole.Write(panel);
	}

	private static async Task RunCleanupWithProgressAsync(
		IndexCleanupService service,
		List<string> prefixes,
		int keepLast,
		CancellationToken ct
	)
	{
		SpectreConsoleTheme.WriteSection("Cleaning Up Old Indices");

		var totalKept = 0;
		var totalDeleted = 0;
		var totalAliases = 0;

		await AnsiConsole.Progress()
			.AutoRefresh(true)
			.AutoClear(false)
			.HideCompleted(false)
			.Columns(
				new SpinnerColumn(),
				new TaskDescriptionColumn { Alignment = Justify.Left },
				new ProgressBarColumn(),
				new PercentageColumn()
			)
			.StartAsync(async progressCtx =>
			{
				var mainTask = progressCtx.AddTask("[aqua]🧹 Discovering indices...[/]", maxValue: prefixes.Count);

				foreach (var prefix in prefixes)
				{
					var shortPrefix = prefix.Length > 50 ? "..." + prefix[^47..] : prefix;
					mainTask.Description = $"[aqua]🧹 Cleaning[/] [dim]{Markup.Escape(shortPrefix)}[/]";

					var result = await service.CleanupAsync(
						prefix,
						keepLast,
						new Progress<(string phase, int current, int total)>(p =>
						{
							mainTask.Description =
								$"[yellow]🗑 {p.phase}[/] [dim]{p.current}/{p.total}[/] [dim]{Markup.Escape(shortPrefix)}[/]";
						}),
						ct
					);

					totalKept += result.Kept.Count;
					totalDeleted += result.Deleted.Count;
					totalAliases += result.DeletedAliases.Count;

					mainTask.Increment(1);
				}

				mainTask.Description = "[green]✓ Cleanup complete[/]";
			});

		AnsiConsole.WriteLine();

		var rows = new List<Spectre.Console.Rendering.IRenderable>
		{
			new Markup($"[green]✓ Kept:[/] [white]{totalKept:N0}[/] indices"),
			new Markup($"[red]🗑 Deleted:[/] [white]{totalDeleted:N0}[/] indices"),
		};

		if (totalAliases > 0)
			rows.Add(new Markup($"[yellow]🔗 Removed aliases:[/] [white]{totalAliases:N0}[/]"));

		var panel = new Panel(new Rows(rows))
		{
			Header = new PanelHeader("[aqua bold]✨ Cleanup Complete ✨[/]"),
			Border = BoxBorder.Double,
			BorderStyle = Style.Parse("aqua"),
			Padding = new Padding(2, 1)
		};
		AnsiConsole.Write(panel);
	}
}
