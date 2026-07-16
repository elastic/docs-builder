// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.SiteSearch.Cli.ContentStack;
using Spectre.Console;

namespace Elastic.SiteSearch.Cli.Commands;

internal sealed class ContentTypesCommand(
	ContentStackClient client
)
{
	private const string StateFile = "content-types-state.json";

	/// <summary>
	/// Discover all content types and whether each defines a root URL field (for sitemaps and routing).
	/// </summary>
	/// <remarks>
	/// Progress is persisted incrementally. User-facing CLI documentation: <see cref="ContentStackCommands.Types"/>.
	/// </remarks>
	/// <param name="cacheFolder">On-disk folder for the survey cache.</param>
	/// <param name="force">Discard saved survey state and restart.</param>
	/// <param name="ct">Cancellation token.</param>
	public async Task Types(
		string? cacheFolder = null,
		bool force = false,
		Cancel ct = default
	)
	{
		var store = new StateManager(cacheFolder);

		if (force)
			store.Delete(StateFile);

		var state = store.Load(StateFile, StateJsonContext.Default.ContentTypesState)
			?? new ContentTypesState();

		AnsiConsole.MarkupLine("[aqua bold]Contentstack Content Type Survey[/]");
		AnsiConsole.MarkupLine($"[dim]Cache: {Markup.Escape(store.CacheFolder)}[/]");
		AnsiConsole.WriteLine();

		if (state.ContentTypes.Count > 0)
		{
			AnsiConsole.MarkupLine(
				$"[dim]Loaded [white]{state.ContentTypes.Count}[/] previously discovered content types " +
				$"([white]{state.TotalItemsSeen:N0}[/] items seen)[/]");

			if (state.Completed)
			{
				AnsiConsole.MarkupLine("[green]Survey already complete.[/] Use [yellow]--force[/] to re-run.");
				AnsiConsole.WriteLine();
				DisplayResults(state);
				ExitIfUnregistered(state);
				return;
			}

			if (state.PaginationToken != null)
				AnsiConsole.MarkupLine("[yellow]Resuming from previous interrupted run...[/]");

			AnsiConsole.WriteLine();
			DisplayResults(state);
			AnsiConsole.WriteLine();
		}

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
				var task = ctx.AddTask("[aqua]Syncing content[/]", maxValue: 100);
				task.IsIndeterminate = true;

				_ = await client.InitialSyncAsync(
					resumePaginationToken: state.PaginationToken,
					progress: new Progress<SyncProgress>(p =>
					{
						if (p.TotalCount > 0)
						{
							task.IsIndeterminate = false;
							task.MaxValue = p.TotalCount;
							task.Value = p.ItemsSoFar;
						}
						task.Description = $"[aqua]Page {p.PagesCompleted}[/] — [white]{p.ItemsSoFar:N0}[/] items " +
							$"([white]{state.ContentTypes.Count}[/] types)";
					}),
					onPage: response =>
					{
						ProcessPage(state, response);
						state.PaginationToken = response.PaginationToken;
						store.Save(StateFile, state, StateJsonContext.Default.ContentTypesState);
						return Task.CompletedTask;
					},
					ct: ct
				);

				state.Completed = true;
				state.PaginationToken = null;
				store.Save(StateFile, state, StateJsonContext.Default.ContentTypesState);

				task.Value = task.MaxValue;
				task.Description = "[green]✓ Sync complete[/]";
			});

		AnsiConsole.WriteLine();
		DisplayResults(state);
		ExitIfUnregistered(state);
	}

	/// <summary>
	/// Every discovered content type must be explicitly registered as synced (<see cref="PageContentTypes.All"/>),
	/// intentionally ignored for now (<see cref="PageContentTypes.Blocked"/>), or a non-page component/taxonomy
	/// type that will never be synced (<see cref="PageContentTypes.KnownNonPages"/>). Fail the run so newly-added
	/// Contentstack content types can't silently go unclassified.
	/// </summary>
	private static void ExitIfUnregistered(ContentTypesState state)
	{
		var unregistered = state.ContentTypes.Keys
			.Where(uid => !PageContentTypes.All.Contains(uid)
				&& !PageContentTypes.Blocked.Contains(uid)
				&& !PageContentTypes.KnownNonPages.Contains(uid))
			.OrderBy(uid => uid, StringComparer.Ordinal)
			.ToList();

		if (unregistered.Count == 0)
			return;

		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine(
			$"[red bold]{unregistered.Count} unregistered content type(s) found:[/] {Markup.Escape(string.Join(", ", unregistered))}");
		AnsiConsole.MarkupLine(
			"[red]Add each to PageContentTypes.All (to sync it), PageContentTypes.Blocked (to ignore it for now), " +
			"or PageContentTypes.KnownNonPages (if it's a component/taxonomy type, not a page).[/]");
		Environment.Exit(1);
	}

	private static void ProcessPage(ContentTypesState state, SyncResponse response)
	{
		foreach (var item in response.Items)
		{
			if (string.IsNullOrEmpty(item.ContentTypeUid))
				continue;

			state.TotalItemsSeen++;

			if (!state.ContentTypes.TryGetValue(item.ContentTypeUid, out var entry))
			{
				entry = new ContentTypeEntry { Uid = item.ContentTypeUid };
				state.ContentTypes[item.ContentTypeUid] = entry;
			}

			entry.Ingest(item);
		}
	}

	private static void DisplayResults(ContentTypesState state)
	{
		if (state.ContentTypes.Count == 0)
		{
			AnsiConsole.MarkupLine("[grey]No content types discovered yet.[/]");
			return;
		}

		var groups = state.ContentTypes.Values
			.OrderByDescending(e => e.Total)
			.ToList();

		var table = new Table()
			.Border(TableBorder.Rounded)
			.BorderColor(Color.Grey)
			.Title("[aqua]Content Types[/]")
			.AddColumn("[aqua]Content Type UID[/]")
			.AddColumn(new TableColumn("[aqua]Entries[/]").RightAligned())
			.AddColumn(new TableColumn("[aqua]Has URL[/]").Centered())
			.AddColumn(new TableColumn("[aqua]Status[/]").Centered())
			.AddColumn("[aqua]Sample URLs[/]");

		foreach (var info in groups)
		{
			var hasUrlDisplay = info.WithUrl switch
			{
				0 => "[grey]—[/]",
				_ when info.WithUrl == info.Total => "[green]all[/]",
				_ => $"[yellow]{info.WithUrl}[/]/{info.Total}"
			};

			var statusDisplay = info.Uid switch
			{
				_ when PageContentTypes.Blocked.Contains(info.Uid) => "[grey]ignored[/]",
				_ when PageContentTypes.KnownNonPages.Contains(info.Uid) => "[grey]skipped[/]",
				_ when PageContentTypes.All.Contains(info.Uid) => "[green]synced[/]",
				_ => "[red bold]unregistered[/]"
			};

			var samples = info.SampleUrls.Count > 0
				? string.Join("\n", info.SampleUrls.Select(u => $"[dim]{Markup.Escape(u)}[/]"))
				: "[grey]—[/]";

			_ = table.AddRow(
				new Markup(Markup.Escape(info.Uid)),
				new Markup($"[white]{info.Total:N0}[/]"),
				new Markup(hasUrlDisplay),
				new Markup(statusDisplay),
				new Markup(samples)
			);
		}

		AnsiConsole.Write(table);
		AnsiConsole.WriteLine();

		var pagesCount = groups.Count(g => g.WithUrl > 0);
		var nonPagesCount = groups.Count(g => g.WithUrl == 0);

		var summary = new Panel(
			new Rows(
				new Markup($"[green]Content types with URLs:[/] [white]{pagesCount}[/]"),
				new Markup($"[grey]Content types without URLs:[/] [white]{nonPagesCount}[/]"),
				new Markup($"[aqua]Total entries surveyed:[/] [white]{state.TotalItemsSeen:N0}[/]")
			)
		)
		{
			Header = new PanelHeader("[aqua]Summary[/]"),
			Border = BoxBorder.Rounded,
			BorderStyle = Style.Parse("aqua"),
			Padding = new Padding(2, 1)
		};

		AnsiConsole.Write(summary);
	}
}
