// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.SiteSearch.Cli.ContentStack;
using Nullean.Argh;
using Spectre.Console;

namespace Elastic.SiteSearch.Cli.Commands;

internal sealed class FindUrlCommand(ContentStackClient client)
{
	private sealed record Sighting(string ContentType, string Uid, string EventType, string? EventAt, int Page, JsonElement? Data);

	private static string ToIndentedJson(JsonElement element)
	{
		using var stream = new MemoryStream();
		using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
			element.WriteTo(writer);
		return System.Text.Encoding.UTF8.GetString(stream.ToArray());
	}

	/// <summary>
	/// Diagnostic: scan Contentstack's sync stream (the same paginated, cursor-based API the real
	/// <c>sync</c> command uses) for every item whose resolved path contains a given fragment, and
	/// flag any path seen more than once <em>within a single pass</em>. Read-only — no
	/// Elasticsearch writes, no sync cursors persisted.
	/// </summary>
	/// <remarks>
	/// Unlike a Content Delivery API query (server-side filtered but a different code path from
	/// production sync), this replicates the exact mechanism <c>contentstack sync</c> uses —
	/// <see cref="ContentStackClient.InitialSyncAsync"/> with <c>onPage</c> callbacks — so a
	/// duplicate delivery here (the same path/uid surfacing on two different pages of the same
	/// pass) is real evidence of the same condition that causes
	/// <c>version_conflict_engine_exception</c> during sync, not an artifact of a different query
	/// mechanism.
	/// </remarks>
	/// <param name="pathPrefix">Path fragment to search for (substring match against the resolved path, so locale-prefixed variants like <c>/pt/...</c> still match).</param>
	/// <param name="contentType">Restrict the scan to one Contentstack content type uid; omit to scan all types (slower — pages through full content).</param>
	/// <param name="ct">Cancellation token.</param>
	public async Task FindUrl(
		[Argument] string pathPrefix,
		string? contentType = null,
		Cancel ct = default)
	{
		var contentTypes = contentType is not null ? [contentType] : PageContentTypes.All;

		AnsiConsole.MarkupLine("[aqua bold]Contentstack Sync-Stream Duplicate Scan[/]");
		AnsiConsole.MarkupLine($"[dim]Path fragment: {Markup.Escape(pathPrefix)}[/]");
		AnsiConsole.MarkupLine($"[dim]Content types: {contentTypes.Length}{(contentType is null ? " (all)" : "")}[/]");
		AnsiConsole.WriteLine();

		// Keyed by resolved path — every sighting within this single pass, in delivery order.
		// More than one sighting for the same path means Contentstack's sync stream delivered the
		// same document more than once within one pass, exactly as the real sync command would see it.
		var sightings = new Dictionary<string, List<Sighting>>();
		var totalItems = 0;
		var matchedItems = 0;

		await AnsiConsole.Status()
			.AutoRefresh(true)
			.Spinner(Spinner.Known.Dots)
			.StartAsync("[aqua]Scanning...[/]", async statusCtx =>
			{
				foreach (var type in contentTypes)
				{
					var page = 0;

					_ = await client.InitialSyncAsync(
						contentTypeUid: type,
						onPage: page2 =>
						{
							page++;
							totalItems += page2.Items.Count;

							foreach (var item in page2.Items)
							{
								var doc = ContentStackMapper.ToSiteDocument(item);
								if (doc is null)
									continue;
								if (!doc.Path.Contains(pathPrefix, StringComparison.OrdinalIgnoreCase))
									continue;

								matchedItems++;
								var uid = item.Data?.TryGetProperty("uid", out var uidEl) == true
									? uidEl.GetString() ?? "?"
									: "?";

								var list = sightings.TryGetValue(doc.Path, out var existing) ? existing : [];
								list.Add(new Sighting(type, uid, item.Type, item.EventAt, page, item.Data));
								sightings[doc.Path] = list;
							}

							_ = statusCtx.Status(
								$"[aqua]Scanning:[/] {Markup.Escape(type)} [dim](page {page}, {totalItems:N0} items seen, {matchedItems:N0} matched)[/]");
							return Task.CompletedTask;
						},
						ct: ct
					);
				}
			});

		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine($"[dim]{totalItems:N0} items scanned, {matchedItems:N0} matched the path fragment.[/]");
		AnsiConsole.WriteLine();

		if (sightings.Count == 0)
		{
			AnsiConsole.MarkupLine("[yellow]No matching paths found.[/]");
			return;
		}

		var duplicates = sightings.Where(kvp => kvp.Value.Count > 1).OrderBy(kvp => kvp.Key).ToList();

		foreach (var (path, list) in sightings.OrderBy(kvp => kvp.Key))
		{
			var isDuplicate = list.Count > 1;
			AnsiConsole.MarkupLine(isDuplicate
				? $"[red bold]DUPLICATE DELIVERY[/] [red]{Markup.Escape(path)}[/] [red]({list.Count} sightings in this pass)[/]"
				: $"[aqua]{Markup.Escape(path)}[/] [dim](1 sighting)[/]");

			foreach (var s in list)
			{
				AnsiConsole.MarkupLine(
					$"  [green]{Markup.Escape(s.ContentType)}[/] uid=[yellow]{Markup.Escape(s.Uid)}[/] " +
					$"event={Markup.Escape(s.EventType)} eventAt=[dim]{Markup.Escape(s.EventAt ?? "?")}[/] page=[white]{s.Page}[/]");
			}
		}

		foreach (var (path, list) in duplicates)
		{
			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine($"[red bold]Raw payloads for[/] [red]{Markup.Escape(path)}[/][red bold]:[/]");

			// Dedupe by exact raw JSON text — sightings sharing the same eventAt tend to share
			// identical content too, so this shows only the genuinely distinct payloads that make
			// up the duplicate group, rather than repeating the same document N times.
			var seenRawText = new HashSet<string>();
			foreach (var s in list)
			{
				if (s.Data is not { } data)
					continue;
				var raw = data.GetRawText();
				if (!seenRawText.Add(raw))
					continue;

				AnsiConsole.MarkupLine(
					$"  [dim]-- page={s.Page} eventAt={Markup.Escape(s.EventAt ?? "?")} event={Markup.Escape(s.EventType)} --[/]");
				AnsiConsole.WriteLine(ToIndentedJson(data));
			}
		}

		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine($"[aqua]{sightings.Count}[/] distinct paths matched.");
		AnsiConsole.MarkupLine(duplicates.Count > 0
			? $"[red bold]{duplicates.Count}[/] [red]path(s) were delivered more than once in this single pass — this is the 409 root cause.[/]"
			: "[green]No duplicate deliveries observed in this pass.[/] [dim](run again — a duplicate may only surface intermittently, e.g. under a retried/slow request.)[/]");
	}
}
