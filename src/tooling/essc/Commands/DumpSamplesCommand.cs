// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.SiteSearch.Cli.ContentStack;
using Spectre.Console;

namespace Elastic.SiteSearch.Cli.Commands;

internal sealed class DumpSamplesCommand(
	ContentStackClient client
)
{
	private const string DefaultOutputDir = "/tmp/contentstack-samples";

	/// <summary>
	/// Fetch one page of sync data per content type and save the first item’s JSON payload to disk.
	/// </summary>
	/// <remarks>
	/// Useful for inspecting Contentstack schemas and generating test fixtures.
	/// See <see cref="ContentStackCommands.Samples"/> for the generated CLI surface.
	/// </remarks>
	/// <param name="outputDir">Output directory for JSON files.</param>
	/// <param name="ct">Cancellation token.</param>
	public async Task Samples(
		string? outputDir = null,
		Cancel ct = default
	)
	{
		var dir = outputDir ?? DefaultOutputDir;
		_ = Directory.CreateDirectory(dir);

		AnsiConsole.MarkupLine("[aqua bold]Contentstack Sample Dumper[/]");
		AnsiConsole.MarkupLine($"[dim]Output: {Markup.Escape(dir)}[/]");
		AnsiConsole.MarkupLine($"[dim]Content types: {PageContentTypes.All.Length}[/]");
		AnsiConsole.WriteLine();

		var results = new List<(string ContentType, int ItemCount, string? FilePath)>();

		await AnsiConsole.Progress()
			.AutoRefresh(true)
			.AutoClear(false)
			.HideCompleted(false)
			.Columns(
				new SpinnerColumn(),
				new TaskDescriptionColumn(),
				new ProgressBarColumn(),
				new PercentageColumn()
			)
			.StartAsync(async ctx =>
			{
				var task = ctx.AddTask("[aqua]Fetching samples[/]", maxValue: PageContentTypes.All.Length);

				foreach (var contentType in PageContentTypes.All)
				{
					ct.ThrowIfCancellationRequested();
					task.Description = $"[aqua]Fetching:[/] {Markup.Escape(contentType)}";

					try
					{
						var result = await client.InitialSyncAsync(
							contentTypeUid: contentType,
							maxPages: 1,
							ct: ct
						);

						if (result.Items.Count > 0 && result.Items[0].Data is { } data)
						{
							var filePath = Path.Combine(dir, $"{contentType}.json");
							await using var stream = File.Create(filePath);
							await using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
							data.WriteTo(writer);
							await writer.FlushAsync(ct);
							results.Add((contentType, result.Items.Count, filePath));
						}
						else
							results.Add((contentType, 0, null));
					}
					catch (Exception ex)
					{
						AnsiConsole.MarkupLine($"[red]Error fetching {Markup.Escape(contentType)}:[/] {Markup.Escape(ex.Message)}");
						results.Add((contentType, -1, null));
					}

					task.Increment(1);
				}

				task.Description = "[green]✓ All samples fetched[/]";
			});

		AnsiConsole.WriteLine();

		var table = new Table()
			.Border(TableBorder.Rounded)
			.BorderColor(Color.Grey)
			.Title("[aqua]Samples[/]")
			.AddColumn("[aqua]Content Type[/]")
			.AddColumn(new TableColumn("[aqua]Items[/]").RightAligned())
			.AddColumn(new TableColumn("[aqua]Status[/]").Centered());

		foreach (var (contentType, itemCount, filePath) in results)
		{
			var status = filePath != null
				? "[green]✓[/]"
				: itemCount == 0
					? "[grey]empty[/]"
					: "[red]error[/]";

			_ = table.AddRow(
				new Markup(Markup.Escape(contentType)),
				new Markup($"[white]{itemCount}[/]"),
				new Markup(status)
			);
		}

		AnsiConsole.Write(table);

		var saved = results.Count(r => r.FilePath != null);
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine($"[green]Saved {saved} samples to {Markup.Escape(dir)}[/]");
	}
}
