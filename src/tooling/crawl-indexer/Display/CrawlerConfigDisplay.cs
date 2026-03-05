// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using CrawlIndexer.Crawling;
using Spectre.Console;

namespace CrawlIndexer.Display;

public static class CrawlerConfigDisplay
{
	public static void DisplayConfiguration(CrawlerSettings settings)
	{
		var grid = new Grid()
			.AddColumn(new GridColumn().NoWrap().PadRight(2))
			.AddColumn(new GridColumn().NoWrap());

		if (settings.RateLimitingEnabled)
		{
			// Show actual configuration when rate limiting is enabled
			_ = grid.AddRow(
				new Markup("[aqua]Semaphore[/]"),
				new Markup($"[white]{settings.Concurrency:N0}[/] [dim]max concurrent requests[/]")
			);

			_ = grid.AddRow(
				new Markup("[aqua]Rate Limiter[/]"),
				new Markup($"[green]Enabled[/] [dim]TokenBucket({settings.Rps}/sec)[/]")
			);
		}
		else
		{
			// Show unlimited configuration
			_ = grid.AddRow(
				new Markup("[aqua]Semaphore[/]"),
				new Markup($"[white]{settings.Concurrency:N0}[/] [dim]max concurrent requests[/]")
			);

			_ = grid.AddRow(
				new Markup("[aqua]Rate Limiter[/]"),
				new Markup("[yellow]Disabled[/] [dim](no throttling)[/]")
			);
		}

		var panel = new Panel(grid)
		{
			Header = new PanelHeader("[aqua bold]Crawler Configuration[/]"),
			Border = BoxBorder.Rounded,
			BorderStyle = Style.Parse("aqua"),
			Padding = new Padding(2, 1)
		};

		AnsiConsole.Write(panel);
		AnsiConsole.WriteLine();
	}
}
