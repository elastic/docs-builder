// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Spectre.Console;

namespace CrawlIndexer.Display;

/// <summary>
/// Defines the Spectre Console theme and styling for crawl-indexer.
/// </summary>
public static class SpectreConsoleTheme
{
	public static Style Primary { get; } = new(Color.Aqua);
	public static Style Secondary { get; } = new(Color.Yellow);
	public static Style Success { get; } = new(Color.Green);
	public static Style Warning { get; } = new(Color.Orange1);
	public static Style Error { get; } = new(Color.Red);
	public static Style Muted { get; } = new(Color.Grey);
	public static Style Highlight { get; } = new(Color.Magenta1);
	public static Style Version { get; } = new(Color.Cyan1);
	public static Style Url { get; } = new(Color.Blue, decoration: Decoration.Underline);

	public static string ElasticLogo => """
		[aqua]
		    ███████╗██╗      █████╗ ███████╗████████╗██╗ ██████╗
		    ██╔════╝██║     ██╔══██╗██╔════╝╚══██╔══╝██║██╔════╝
		    █████╗  ██║     ███████║███████╗   ██║   ██║██║
		    ██╔══╝  ██║     ██╔══██║╚════██║   ██║   ██║██║
		    ███████╗███████╗██║  ██║███████║   ██║   ██║╚██████╗
		    ╚══════╝╚══════╝╚═╝  ╚═╝╚══════╝   ╚═╝   ╚═╝ ╚═════╝
		[/]
		""";

	public static string CrawlerBanner => """
		[yellow]
		     ██████╗██████╗  █████╗ ██╗    ██╗██╗     ███████╗██████╗
		    ██╔════╝██╔══██╗██╔══██╗██║    ██║██║     ██╔════╝██╔══██╗
		    ██║     ██████╔╝███████║██║ █╗ ██║██║     █████╗  ██████╔╝
		    ██║     ██╔══██╗██╔══██║██║███╗██║██║     ██╔══╝  ██╔══██╗
		    ╚██████╗██║  ██║██║  ██║╚███╔███╔╝███████╗███████╗██║  ██║
		     ╚═════╝╚═╝  ╚═╝╚═╝  ╚═╝ ╚══╝╚══╝ ╚══════╝╚══════╝╚═╝  ╚═╝
		[/]
		""";

	public static void WriteHeader(string mode)
	{
		AnsiConsole.WriteLine();
		AnsiConsole.Write(new FigletText("crawl-indexer").Color(Color.Aqua));
		AnsiConsole.MarkupLine($"[grey]Elastic Documentation Crawler & Indexer[/] [dim]•[/] [yellow]{mode}[/] [dim]mode[/]");
		AnsiConsole.WriteLine();
	}

	public static void WriteDryRunBanner()
	{
		var rule = new Rule("[yellow]🔍 DRY RUN MODE[/]")
		{
			Justification = Justify.Center,
			Style = Style.Parse("yellow")
		};
		AnsiConsole.Write(rule);
		AnsiConsole.MarkupLine("[dim]No actual crawling or indexing will be performed[/]");
		AnsiConsole.WriteLine();
	}

	public static void WriteSection(string title)
	{
		AnsiConsole.WriteLine();
		var rule = new Rule($"[aqua]{title}[/]")
		{
			Justification = Justify.Left,
			Style = Style.Parse("grey")
		};
		AnsiConsole.Write(rule);
	}

	public static void WriteSuccess(string message) =>
		AnsiConsole.MarkupLine($"[green]✓[/] {message}");

	public static void WriteWarning(string message) =>
		AnsiConsole.MarkupLine($"[orange1]⚠[/] {message}");

	public static void WriteError(string message) =>
		AnsiConsole.MarkupLine($"[red]✗[/] {message}");

	public static void WriteInfo(string message) =>
		AnsiConsole.MarkupLine($"[aqua]ℹ[/] {message}");

	public static void WriteBullet(string message) =>
		AnsiConsole.MarkupLine($"  [grey]•[/] {message}");
}
