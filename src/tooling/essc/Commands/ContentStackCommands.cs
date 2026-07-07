// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using Elastic.Documentation.Indexing;
using Elastic.SiteSearch.Cli.Elasticsearch;
using Microsoft.Extensions.Logging;
using Nullean.Argh;
using Spectre.Console;

namespace Elastic.SiteSearch.Cli.Commands;

/// <summary>
/// Contentstack sourcing: sync site content into Elasticsearch, survey content types, and dump sample payloads.
/// </summary>
/// <remarks>
/// These commands call the Contentstack Delivery/Preview APIs (per environment configuration) and target
/// <c>site-*</c> search indices. They are separate from <c>labs</c> commands, which crawl elastic.co labs
/// properties into <c>labs-*</c> indices.
/// </remarks>
internal sealed class ContentStackCommands(
	SyncCommand sync,
	ContentTypesCommand types,
	DumpSamplesCommand samples,
	FindUrlCommand findUrl,
	SourcingConfiguration config,
	ILoggerFactory loggerFactory
)
{
	/// <summary>
	/// Incremental sync: pull published entries per content type and index documents into Elasticsearch.
	/// </summary>
	/// <remarks>
	/// Parallel lanes per content type; cursors persist under the cache folder so runs are resumable.
	/// Use <c>--force</c> to clear saved cursors and re-bootstrap indices for a clean run.
	/// Unless <c>--no-ai</c> is passed, after finalize this command runs a bounded generative AI enrichment pass on the
	/// semantic <c>site-*</c> index: by default up to <c>100</c> documents that are candidates for enrichment (still missing
	/// or due for enrichment). Pass <c>--max-ai-docs</c> to change that cap, or <c>--no-ai</c> to skip the post-sync batch entirely.
	/// </remarks>
	/// <param name="cacheFolder">Disk folder for sync cursors and bootstrap metadata (default from configuration).</param>
	/// <param name="esApiKey">Override Elasticsearch API key when not using configured credentials.</param>
	/// <param name="esUrl">Override Elasticsearch base URL (absolute URI).</param>
	/// <param name="force">Remove persisted state and re-run from a clean slate.</param>
	/// <param name="noAi">When <see langword="true"/>, skip ingest-time AI wiring and the post-sync generative enrichment batch.</param>
	/// <param name="maxAiDocs">Maximum enrichment candidates processed in the post-sync AI batch per run; omit for <c>100</c>. Must be at least 1 when specified.</param>
	/// <param name="maxAiTime">Optional wall-clock limit for the post-sync AI phase (minimum 1 minute when set).</param>
	/// <param name="noIndex">Process Contentstack only; skip writing to Elasticsearch.</param>
	/// <param name="pagePer">Max Contentstack API pages to fetch per content type; <c>0</c> means no cap.</param>
	/// <param name="ct">Cancellation token.</param>
	public Task Sync(
		[StringLength(4096)] string? cacheFolder = null,
		string? esApiKey = null,
		[Url] Uri? esUrl = null,
		bool force = false,
		bool noAi = false,
		[Range(1, int.MaxValue)] int? maxAiDocs = null,
		TimeSpan? maxAiTime = null,
		bool noIndex = false,
		[Range(0, int.MaxValue)] int pagePer = 0,
		Cancel ct = default) =>
		sync.Sync(cacheFolder, esApiKey, esUrl, force, noAi, maxAiDocs, maxAiTime, noIndex, pagePer, ct);

	/// <summary>
	/// Discover all content types and whether each exposes a root URL field (sitemap and routing inputs).
	/// </summary>
	/// <remarks>Survey state is saved incrementally. Use <c>--force</c> to discard it and start over.</remarks>
	/// <param name="cacheFolder">Disk folder for the survey state file.</param>
	/// <param name="force">Delete saved survey progress and re-run discovery.</param>
	/// <param name="ct">Cancellation token.</param>
	public Task Types(
		[StringLength(4096)] string? cacheFolder = null,
		bool force = false,
		Cancel ct = default) =>
		types.Types(cacheFolder, force, ct);

	/// <summary>
	/// Fetch one sync page per content type and write the first item’s JSON to disk (fixtures and debugging).
	/// </summary>
	/// <remarks>
	/// When <paramref name="outputDir"/> is omitted, a default directory under the machine temp path is used.
	/// </remarks>
	/// <param name="outputDir">Directory for <c>*.json</c> files; created if it does not exist.</param>
	/// <param name="ct">Cancellation token.</param>
	public Task Samples(
		[StringLength(4096)] string? outputDir = null,
		Cancel ct = default) =>
		samples.Samples(outputDir, ct);

	/// <summary>
	/// Diagnostic: scan Contentstack's sync stream (the same paginated, cursor-based API
	/// <c>sync</c> uses) for every item whose resolved path contains a fragment, flagging any path
	/// delivered more than once within a single pass. Read-only — no Elasticsearch writes.
	/// </summary>
	/// <remarks>
	/// Useful for tracking down <c>version_conflict_engine_exception</c> errors during sync — a
	/// path delivered twice in one pass races itself across concurrent bulk batches.
	/// </remarks>
	/// <param name="pathPrefix">Path fragment to search for, e.g. <c>/elasticon/archive/2020</c>.</param>
	/// <param name="contentType">Restrict the scan to one content type uid; omit to scan all types.</param>
	/// <param name="ct">Cancellation token.</param>
	public Task FindUrl(
		[Argument] string pathPrefix,
		string? contentType = null,
		Cancel ct = default) =>
		findUrl.FindUrl(pathPrefix, contentType, ct);

	/// <summary>
	/// Run generative AI enrichment on existing <c>site-*</c> semantic indices (no Contentstack fetch).
	/// </summary>
	/// <remarks>
	/// <paramref name="maxRunDocs"/> is an optional cap; <c>0</c> means no document limit.
	/// Omit <paramref name="maxRunTime"/> for no wall-clock limit, or set a duration of at least one minute (for example <c>1h</c>, <c>90m</c>).
	/// </remarks>
	[CommandName("ai-enrich")]
	public async Task AiEnrich(
		string? esApiKey = null,
		[Url] Uri? esUrl = null,
		[Range(0, int.MaxValue)] int maxRunDocs = 0,
		TimeSpan? maxRunTime = null,
		Cancel ct = default
	)
	{
		if (!AiEnrichmentBudget.TryValidateMaxTime(maxRunTime, out var maxRunTimeError))
		{
			await Console.Error.WriteLineAsync($"Error: --max-run-time {maxRunTimeError}");
			await Console.Error.WriteLineAsync("Run 'essc contentstack ai-enrich --help' for usage.");
			Environment.Exit(2);
		}

		AnsiConsole.MarkupLine("[aqua bold]Contentstack site AI enrichment[/] [dim](site-* indices)[/]");
		AnsiConsole.WriteLine();

		var endpoint = config.Elasticsearch;
		if (esUrl is not null)
			endpoint.Uri = esUrl;
		if (esApiKey is not null)
		{
			endpoint.ApiKey = esApiKey;
			endpoint.Username = null;
			endpoint.Password = null;
		}

		AnsiConsole.MarkupLine($"[dim]Elasticsearch: {Markup.Escape(endpoint.Uri.ToString())}[/]");

		var transport = ElasticsearchTransportFactory.Create(endpoint);

		using var deadline = AiEnrichmentDeadline.Create(maxRunTime, ct);
		var effectiveToken = deadline.Token;

		if (maxRunTime is { } limit)
			AnsiConsole.MarkupLine($"[dim]Time limit: [yellow]{Markup.Escape(limit.ToString())}[/][/]");
		if (maxRunDocs > 0)
			AnsiConsole.MarkupLine($"[dim]Document limit: [yellow]{maxRunDocs:N0}[/] documents[/]");

		AnsiConsole.WriteLine();

		try
		{
			using var exporter = new SiteDocumentExporter(
				loggerFactory,
				endpoint,
				transport,
				config.BuildType,
				config.ElasticsearchEnvironment,
				enableAiEnrichment: true
			);

			await AnsiConsole.Status()
				.AutoRefresh(true)
				.Spinner(Spinner.Known.Dots)
				.StartAsync("[aqua]Bootstrapping Elasticsearch indices...[/]", async _ =>
				{
					await exporter.StartAsync(effectiveToken);
				});

			AnsiConsole.MarkupLine($"[green]✓[/] Elasticsearch indices ready [dim]({exporter.Strategy})[/]");
			AnsiConsole.WriteLine();

			var aiResult = await AiEnrichmentConsole.RunInteractiveAsync(
				exporter.AiEnrichmentEnabled,
				(max, token) => exporter.RunAiEnrichmentAsync(max, token),
				maxRunDocs,
				effectiveToken);
			AiEnrichmentConsole.DisplaySummary(aiResult, maxRunTime, maxRunDocs);
		}
		catch (OperationCanceledException) when (deadline.TimedOut)
		{
			AnsiConsole.MarkupLine("[yellow]AI enrichment stopped — time limit reached[/]");
		}
	}
}
