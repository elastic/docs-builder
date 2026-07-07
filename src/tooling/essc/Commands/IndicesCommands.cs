// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Elastic.Channels;
using Elastic.Documentation.Search.Contract;
using Elastic.Documentation.Search.Contract.Mapping;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.Helpers;
using Elastic.Ingest.Elasticsearch.Indices;
using Elastic.Ingest.Elasticsearch.Strategies;
using Elastic.SiteSearch.Cli.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Logging;
using Nullean.Argh;
using Spectre.Console;

namespace Elastic.SiteSearch.Cli.Commands;

/// <summary>
/// Shared connection options for remote-cluster <c>indices sync-remote</c> and
/// <c>indices unify-incremental-sync</c>, expanded from the CLI via Argh <c>[AsParameters]</c>.
/// </summary>
internal sealed class IndicesRemoteSyncOptions
{
	/// <summary>
	/// Source (production) cluster base URL, including scheme and port
	/// (e.g. <c>https://my-prod.es.us-east-1.aws.found.io:443</c>).
	/// Defaults to the configured Elasticsearch URL (<c>DOCUMENTATION_ELASTIC_URL</c> /
	/// <c>ELASTICSEARCH_URL</c>).
	/// </summary>
	[Url]
	public Uri? FromUrl { get; set; }

	/// <summary>
	/// Source (production) cluster encoded API key (base64 <c>id:api_key</c> form).
	/// Defaults to the configured API key (<c>DOCUMENTATION_ELASTIC_APIKEY</c> /
	/// <c>ELASTICSEARCH_API_KEY</c>).
	/// </summary>
	public string? FromApiKey { get; set; }

	/// <summary>
	/// Destination cluster base URL, including scheme and port.
	/// Defaults to <c>DESTINATION_ELASTIC_URL</c>.
	/// </summary>
	[Url]
	public Uri? ToUrl { get; set; }

	/// <summary>
	/// Destination cluster encoded API key.
	/// Defaults to <c>DESTINATION_ELASTIC_APIKEY</c>.
	/// </summary>
	public string? ToApiKey { get; set; }

	/// <summary>
	/// Reindex parallelism — a positive integer. <c>auto</c> is not supported for remote reindex.
	/// Defaults to <c>5</c>.
	/// </summary>
	[Range(1, int.MaxValue)]
	public int Slices { get; set; } = 5;

	/// <summary>Requests-per-second throttle. Omit or leave unset for unlimited.</summary>
	[Range(0, int.MaxValue)]
	public float? Rps { get; set; }

	/// <summary>
	/// Comma-separated list of additional aliases to point at the synced backing index on the
	/// destination cluster after a successful reindex (e.g. <c>ws-content-staging</c>).
	/// Each alias is atomically swapped off any index it currently targets.
	/// </summary>
	public string? Aliases { get; set; }
}

/// <summary>Options for <c>indices cleanup</c>, expanded from the CLI via Argh <c>[AsParameters]</c>.</summary>
internal sealed class IndicesCleanupOptions
{
	/// <summary>Total backing indices to retain per (source, variant) pair. Defaults to 2.</summary>
	[Range(1, int.MaxValue)]
	public int Keep { get; set; } = 2;

	/// <summary>Elasticsearch environment (e.g. prod, staging, dev). Defaults to the ENVIRONMENT env var.</summary>
	public string? Environment { get; set; }

	/// <summary>Print what would be deleted without actually deleting anything.</summary>
	public bool DryRun { get; set; }

	/// <summary>Override Elasticsearch API key (otherwise from configuration).</summary>
	public string? EsApiKey { get; set; }

	/// <summary>Override Elasticsearch base URL (absolute URI).</summary>
	[Url]
	public Uri? EsUrl { get; set; }
}

/// <summary>
/// Operations that work across multiple Elasticsearch indices.
/// </summary>
internal sealed class IndicesCommands(SourcingConfiguration config, ILoggerFactory loggerFactory)
{
	private static bool IsInteractive() =>
		string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) &&
		AnsiConsole.Profile.Capabilities.Interactive;

	/// <summary>
	/// Incrementally reindex <c>docs-assembler.*</c>, <c>site-*</c>, and <c>labs-*</c> semantic
	/// indices into a unified <c>website-search.semantic-{env}</c> index, then update the
	/// <c>ws-content-{env}</c> alias.
	/// </summary>
	/// <remarks>
	/// On the first run (or when <c>WebsiteSearchMappingConfig</c> or any source template changes)
	/// a full reindex runs and all docs trigger inference. On subsequent runs only docs whose
	/// <c>batch_index_date</c> in the source exceeds the per-source cutoff stored in the semantic
	/// index are reindexed — inference runs only for those changed documents.
	///
	/// Rollover is triggered by:
	/// - <c>WebsiteSearchMappingConfig</c> mapping/settings change (detected by orchestrator)
	/// - Any source index template hash change (detected by comparing against stored hashes in lexical)
	///
	/// Delete detection uses a mark-and-sweep: every doc present in the current lexical snapshot is
	/// marked with the current batch timestamp; docs in semantic that were not marked are removed.
	/// The <c>unify_source</c> field on each doc records which source alias it came from.
	/// </remarks>
	/// <param name="esApiKey">Override Elasticsearch API key.</param>
	/// <param name="esUrl">Override Elasticsearch base URL.</param>
	/// <param name="environment">Override target environment (e.g. <c>prod</c>, <c>staging</c>, <c>dev</c>). Defaults to the configured environment.</param>
	/// <param name="slices">Reindex parallelism: <c>auto</c> or a numeric string. Defaults to <c>auto</c>.</param>
	/// <param name="rps">Requests-per-second throttle for reindex operations. <c>-1</c> means unlimited (default).</param>
	/// <param name="aliases">Comma-separated extra aliases to point at the final <c>website-search.semantic-{env}</c> backing index. Each alias is moved atomically (added to the new index, removed from its prior target).</param>
	/// <param name="renameFrom">
	/// Regular expression applied to both the lexical and semantic backing index names
	/// (e.g. <c>website-search\.(lexical|semantic)-prod-</c>).
	/// Must be paired with <c>--rename-to</c>.
	/// When omitted backing index names from the orchestrator are used as-is.
	/// </param>
	/// <param name="renameTo">
	/// Replacement string for <c>--rename-from</c> — supports regex back-references
	/// (e.g. <c>ws-content.$1-staging-</c>).
	/// Applied to both the lexical and semantic backing index names unless <c>--rename-to-lexical</c> overrides lexical.
	/// Must be paired with <c>--rename-from</c>.
	/// </param>
	/// <param name="renameToLexical">
	/// Override replacement applied only to the lexical backing index name.
	/// When set takes precedence over <c>--rename-to</c> for lexical; <c>--rename-to</c> still applies to semantic.
	/// </param>
	/// <param name="ct">Cancellation token.</param>
	public async Task Unify(
		string? esApiKey = null,
		[Url] Uri? esUrl = null,
		string? environment = null,
		string slices = "auto",
		[Range(-1, int.MaxValue)] float rps = -1,
		string? aliases = null,
		string? renameFrom = null,
		string? renameTo = null,
		string? renameToLexical = null,
		Cancel ct = default
	)
	{
		var endpoint = ResolveEndpoint(esUrl, esApiKey);
		var env = environment ?? config.ElasticsearchEnvironment;
		var buildType = config.BuildType;
		var batchTs = DateTimeOffset.UtcNow;
		var batchTsStr = batchTs.ToString("o");
		float? rpsOpt = rps < 0 ? null : rps;

		var extraLabels = (aliases ?? "")
			.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		var docsAssemblerAlias = $"docs-assembler.semantic-{env}-latest";
		var siteAlias = SiteMappingContext.SiteDocumentSemantic.CreateContext(type: buildType, env: env).ResolveWriteAlias();
		var labsAlias = LabsMappingContext.LabsDocumentSemantic.CreateContext(type: buildType, env: env).ResolveWriteAlias();
		var sourceAliases = new[] { docsAssemblerAlias, siteAlias, labsAlias };
		var pageAlias = IndicesCleanupPlanner.PageAliasName(env);

		AnsiConsole.MarkupLine("[aqua bold]Indices unify[/] — [dim]docs-assembler + site + labs → website-search[/]");
		AnsiConsole.MarkupLine($"[dim]Elasticsearch:[/] {Markup.Escape(endpoint.Uri.ToString())}");
		AnsiConsole.MarkupLine($"[dim]Environment:[/]   [white]{Markup.Escape(env)}[/]");
		AnsiConsole.MarkupLine($"[dim]Sources:[/]       [white]{Markup.Escape(string.Join(", ", sourceAliases))}[/]");
		AnsiConsole.MarkupLine($"[dim]Alias:[/]         [white]{Markup.Escape(pageAlias)}[/]");
		AnsiConsole.MarkupLine($"[dim]Slices:[/]        [white]{Markup.Escape(slices)}[/]  [dim]rps:[/] [white]{(rps < 0 ? "unlimited" : rps.ToString("F0"))}[/]");
		if (extraLabels.Length > 0)
			AnsiConsole.MarkupLine($"[dim]Aliases:[/]       [white]{Markup.Escape(string.Join(", ", extraLabels))}[/]");
		AnsiConsole.WriteLine();

		var transport = ElasticsearchTransportFactory.Create(endpoint);
		var logger = loggerFactory.CreateLogger<IndicesCommands>();

		var synonymSetName = $"docs-assembler-{env}";
		var indexTimeSynonyms = IndexTimeSynonyms.Docs;

		var lexicalContext = WebsiteSearchMappingContext.WebsiteSearchDocument
			.CreateContext(env: env) with
		{
			ConfigureAnalysis = a => SharedAnalysisFactory.BuildAnalysis(a, synonymSetName, indexTimeSynonyms)
		};
		var semanticContext = WebsiteSearchMappingContext.WebsiteSearchDocumentSemantic
			.CreateContext(env: env) with
		{
			ConfigureAnalysis = a => SharedAnalysisFactory.BuildAnalysis(a, synonymSetName, indexTimeSynonyms)
		};

		// Bootstrap index templates and determine Multiplex vs Reindex strategy.
		AnsiConsole.MarkupLine("[dim]Bootstrapping index templates...[/]");
		using var orchestrator = new IncrementalSyncOrchestrator<WebsiteSearchDocument>(transport, lexicalContext, semanticContext)
		{
			ConfigurePrimary = o => ConfigureChannelOptions("primary", o, endpoint),
			ConfigureSecondary = o => ConfigureChannelOptions("secondary", o, endpoint, semantic: true),
			OnRolloverDecision = info =>
			{
				var roll = info.RolledOver ? "new backing index" : "reuse";
				logger.LogInformation("[{Label}] rollover={Roll} local={Local} remote={Remote}",
					info.Label, roll, info.LocalHash, info.RemoteHash);
			},
		};

		var context = await orchestrator.StartAsync(BootstrapMethod.Silent, ct);

		var lexicalAlias = context.PrimaryWriteAlias;    // website-search.lexical-{env}-latest
		var semanticAlias = context.SecondaryWriteAlias;  // website-search.semantic-{env}-latest

		// ── Source hash check ─────────────────────────────────────────────────────
		var (currentSourceHash, perSourceHashes) = await FetchCombinedSourceHashAsync(transport, sourceAliases, ct);
		var storedSourceHash = await ReadStoredSourceHashAsync(transport, env, ct);
		var sourceHashChanged = storedSourceHash is null || storedSourceHash != currentSourceHash;

		AnsiConsole.MarkupLine("[dim]Source template hashes:[/]");
		foreach (var (alias, template, hash) in perSourceHashes)
			AnsiConsole.MarkupLine($"[dim]  {Markup.Escape(template)}: [white]{Markup.Escape(hash)}[/][/]");
		AnsiConsole.MarkupLine(
			sourceHashChanged
				? $"[dim]  combined: [yellow]{Markup.Escape(currentSourceHash)}[/] (stored: [yellow]{Markup.Escape(storedSourceHash ?? "none")}[/]) — [yellow]changed[/][/]"
				: $"[dim]  combined: [white]{Markup.Escape(currentSourceHash)}[/] (stored: [white]{Markup.Escape(storedSourceHash ?? "none")}[/]) — unchanged[/]");
		AnsiConsole.WriteLine();

		// ── Resolve backing indices ───────────────────────────────────────────────
		string lexicalIndex, semanticIndex;
		if (orchestrator.Strategy == IngestSyncStrategy.Multiplex)
		{
			lexicalIndex = lexicalContext.ResolveIndexName(orchestrator.BatchTimestamp);
			semanticIndex = semanticContext.ResolveIndexName(orchestrator.BatchTimestamp);
		}
		else if (sourceHashChanged)
		{
			// Source mappings changed — create new date-stamped backing indices.
			lexicalIndex = lexicalContext.ResolveIndexName(batchTs.AddSeconds(1));
			semanticIndex = semanticContext.ResolveIndexName(batchTs.AddSeconds(1));
			_ = await transport.PutAsync<StringResponse>(lexicalIndex, PostData.Empty, ct);
			_ = await transport.PutAsync<StringResponse>(semanticIndex, PostData.Empty, ct);
			await PointAliasAsync(transport, lexicalAlias, lexicalIndex, logger, ct);
			await PointAliasAsync(transport, semanticAlias, semanticIndex, logger, ct);
		}
		else
		{
			lexicalIndex = await ResolveAliasIndexAsync(transport, lexicalAlias, ct)
							?? throw new InvalidOperationException($"Lexical alias '{lexicalAlias}' not found.");
			semanticIndex = await ResolveAliasIndexAsync(transport, semanticAlias, ct)
							?? throw new InvalidOperationException($"Semantic alias '{semanticAlias}' not found.");
		}

		// Apply rename after the orchestrator has set up the canonical template and backing indices.
		// BootstrapSemanticIndexAsync copies the mapping from the canonical index so renamed
		// indices get the same settings without needing to match the template pattern.
		// Lexical is plain-text (no inference), so ES dynamic mapping is sufficient — no bootstrap needed.
		var originalLexicalIndex = lexicalIndex;
		lexicalIndex = ApplyRename(lexicalIndex, renameFrom, renameToLexical ?? renameTo);
		var originalSemanticIndex = semanticIndex;
		semanticIndex = ApplyRename(semanticIndex, renameFrom, renameTo);
		var semanticWasRenamed = semanticIndex != originalSemanticIndex;

		var isFullReindex = orchestrator.Strategy == IngestSyncStrategy.Multiplex || sourceHashChanged;
		var currentPageAlias = await ResolveAliasIndexAsync(transport, pageAlias, ct);

		AnsiConsole.MarkupLine($"[green]✓[/] Strategy: [white]{orchestrator.Strategy}[/]{(sourceHashChanged ? " [yellow]+ source hash rollover[/]" : "")}");
		AnsiConsole.MarkupLine($"[dim]Mode:[/] [white]{(isFullReindex ? "full reindex" : "incremental")}[/]");
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine("[dim]Indices:[/]");
		var lexicalDisplay = lexicalIndex != originalLexicalIndex
			? $"{Markup.Escape(originalLexicalIndex)} [dim](renamed →)[/] {Markup.Escape(lexicalIndex)}"
			: Markup.Escape(lexicalIndex);
		AnsiConsole.MarkupLine($"[dim]  lexical:  [white]{lexicalDisplay}[/][/]");
		var semanticDisplay = semanticWasRenamed
			? $"{Markup.Escape(originalSemanticIndex)} [dim](renamed →)[/] {Markup.Escape(semanticIndex)}"
			: Markup.Escape(semanticIndex);
		AnsiConsole.MarkupLine($"[dim]  semantic: [white]{semanticDisplay}[/][/]");
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine("[dim]Current aliases:[/]");
		AnsiConsole.MarkupLine($"[dim]  {Markup.Escape(lexicalAlias)} → [white]{Markup.Escape(await ResolveAliasIndexAsync(transport, lexicalAlias, ct) ?? "not set")}[/][/]");
		AnsiConsole.MarkupLine($"[dim]  {Markup.Escape(semanticAlias)} → [white]{Markup.Escape(await ResolveAliasIndexAsync(transport, semanticAlias, ct) ?? "not set")}[/][/]");
		AnsiConsole.MarkupLine($"[dim]  {Markup.Escape(pageAlias)} → [white]{Markup.Escape(currentPageAlias ?? "not set")}[/][/]");
		AnsiConsole.WriteLine();

		// Semantic steps target a semantic_text inference endpoint — use a single slice to avoid
		// overwhelming the Jina endpoint with concurrent bulk requests.
		const string semanticSlices = "1";

		var state = new UnifyRunState(
			sourceAliases, lexicalIndex, semanticIndex, originalSemanticIndex, semanticWasRenamed,
			lexicalAlias, semanticAlias, pageAlias, extraLabels, env, isFullReindex, batchTsStr,
			currentSourceHash, semanticSlices, slices, rpsOpt);

		try
		{
			await RunUnifySteps(transport, state, logger, ct);
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
			if (isFullReindex)
			{
				// A full reindex creates a brand-new semantic backing index that no alias points at
				// yet — cancelling mid-run leaves it partially populated. Safe to delete; the next
				// run's Multiplex/source-hash-changed path will recreate it from scratch.
				AnsiConsole.MarkupLine("[dim]Cleaning up partial index...[/]");
				// ct is already cancelled — use a fresh token so the cleanup call itself can run.
				_ = await transport.DeleteAsync<StringResponse>(semanticIndex, cancellationToken: CancellationToken.None);
				AnsiConsole.MarkupLine($"[dim]Deleted partial index {Markup.Escape(semanticIndex)}[/]");
			}
			Environment.Exit(130);
			return;
		}
	}

	/// <summary>Bundles the values <see cref="RunUnifySteps"/> needs — resolved once in <see cref="Unify"/>
	/// before the cancellable work begins, so the cleanup handler there can also read them.</summary>
	private sealed record UnifyRunState(
		string[] SourceAliases,
		string LexicalIndex,
		string SemanticIndex,
		string OriginalSemanticIndex,
		bool SemanticWasRenamed,
		string LexicalAlias,
		string SemanticAlias,
		string PageAlias,
		string[] ExtraLabels,
		string Env,
		bool IsFullReindex,
		string BatchTsStr,
		string CurrentSourceHash,
		string SemanticSlices,
		string Slices,
		float? RpsOpt);

	private async Task RunUnifySteps(DistributedTransport transport, UnifyRunState state, ILogger logger, Cancel ct)
	{
		// ── Step 1: Populate lexical from all 3 sources ──────────────────────────
		// Fast bulk copy, no inference. Stamps batch_index_date and unify_source on each doc.
		foreach (var source in state.SourceAliases)
		{
			var fillBody = BuildLexicalFillBody(source, state.LexicalIndex, state.BatchTsStr);
			if (!await RunServerReindexAsync(transport,
				new ServerReindexOptions { Body = fillBody, Slices = state.Slices, RequestsPerSecond = state.RpsOpt },
				$"fill-lexical ({Markup.Escape(source)})", ct))
			{
				AnsiConsole.MarkupLine("[red]Aborting — lexical fill failed.[/]");
				Environment.Exit(1);
				return;
			}
		}

		// Refresh lexical so all 3 fill reindexes are visible before the semantic reindex opens its PIT.
		// _reindex does not refresh the destination — docs written to un-refreshed segments are invisible.
		_ = await transport.PostAsync<StringResponse>($"{state.LexicalIndex}/_refresh", PostData.Empty, ct);
		AnsiConsole.MarkupLine("[dim]Lexical refreshed[/]");

		// Point -latest alias at the (now populated) lexical backing index.
		await PointAliasAsync(transport, state.LexicalAlias, state.LexicalIndex, logger, ct);
		AnsiConsole.MarkupLine($"[green]✓[/] {Markup.Escape(state.LexicalAlias)} → {Markup.Escape(state.LexicalIndex)}");

		if (state.SemanticWasRenamed)
		{
			// The orchestrator created and template-mapped the original semantic index.
			// Copy its settings+mapping to the renamed index so semantic_text fields and
			// custom analyzers are present before the inference reindex runs.
			await BootstrapSemanticIndexAsync(transport, transport, state.OriginalSemanticIndex, state.SemanticIndex, logger, ct);
		}

		if (state.IsFullReindex)
		{
			// ── Full reindex to semantic (all docs trigger inference) ────────────
			// slices=1: prevents es_rejected_execution_exception from concurrent inference bulk.
			if (!await RunServerReindexAsync(transport,
				new ServerReindexOptions { Source = state.LexicalIndex, Destination = state.SemanticIndex, Slices = state.SemanticSlices, RequestsPerSecond = state.RpsOpt },
				"full-semantic", ct))
			{
				// Reindex failed mid-way. The semantic backing index is partial — delete it so
				// the next run starts clean (Multiplex will recreate it).
				AnsiConsole.MarkupLine("[red]Reindex to semantic failed — cleaning up partial index...[/]");
				_ = await transport.DeleteAsync<StringResponse>(state.SemanticIndex, cancellationToken: ct);
				AnsiConsole.MarkupLine($"[dim]Deleted partial index {Markup.Escape(state.SemanticIndex)}[/]");
				Environment.Exit(1);
				return;
			}

			// Point -latest and ws-content-{env} aliases only after a successful reindex.
			await PointAliasAsync(transport, state.SemanticAlias, state.SemanticIndex, logger, ct);
			await PointAliasAsync(transport, state.PageAlias, state.SemanticIndex, logger, ct);
			AnsiConsole.MarkupLine($"[green]✓[/] {Markup.Escape(state.SemanticAlias)} → {Markup.Escape(state.SemanticIndex)}");
			AnsiConsole.MarkupLine($"[green]✓[/] {Markup.Escape(state.PageAlias)} → {Markup.Escape(state.SemanticIndex)}");
			_ = await PruneOldIndicesAsync(transport, $"ws-catalog.semantic-{state.Env}-*", keepCount: 3, logger, ct);
		}
		else // Reindex mode — incremental
		{
			// Global cutoff: max(last_updated) across all docs currently in semantic.
			// Changed/new source docs have newer last_updated from their sync → caught by inference step.
			var cutoff = await QueryMaxLastUpdatedAsync(transport, state.SemanticIndex, ct);
			AnsiConsole.MarkupLine($"[dim]Cutoff (max last_updated in semantic):[/] [white]{cutoff:o}[/]");
			AnsiConsole.WriteLine();

			// ── Inference step ───────────────────────────────────────────────────
			// Reindex only docs whose last_updated > cutoff from lexical → semantic (slices=1, inference).
			// These are docs that changed in any source since the last unify run.
			var inferenceBody =
				"{\"source\":{\"index\":\"" + state.LexicalIndex +
				"\",\"query\":{\"range\":{\"last_updated\":{\"gt\":\"" + cutoff.ToString("o") + "\"}}}}," +
				"\"dest\":{\"index\":\"" + state.SemanticIndex + "\"}}";
			if (!await RunServerReindexAsync(transport,
				new ServerReindexOptions { Body = inferenceBody, Slices = state.SemanticSlices, RequestsPerSecond = state.RpsOpt },
				"inference-reindex", ct))
			{
				AnsiConsole.MarkupLine("[red]Aborting — inference reindex failed.[/]");
				Environment.Exit(1);
				return;
			}

			// ── Delete step ──────────────────────────────────────────────────────
			// Reindex stale lexical docs (batch_index_date < batchTimestamp = not in current fill)
			// to semantic using a delete script. This removes docs deleted from all sources.
			var deleteBody = BuildDeleteScriptBody(state.LexicalIndex, state.SemanticIndex, state.BatchTsStr);
			if (!await RunServerReindexAsync(transport,
				new ServerReindexOptions { Body = deleteBody, Slices = state.Slices },
				"reindex-deletes", ct))
			{
				AnsiConsole.MarkupLine("[red]Aborting — delete step failed.[/]");
				Environment.Exit(1);
				return;
			}

			// ── Lexical cleanup ──────────────────────────────────────────────────
			// Remove the stale docs from lexical too now that they've been deleted from semantic.
			var staleQuery = "{\"range\":{\"batch_index_date\":{\"lt\":\"" + state.BatchTsStr + "\"}}}";
			await RunDeleteByQueryAsync(transport, state.LexicalIndex, staleQuery, state.Slices, "cleanup-lexical", ct);

			// Reindex mode does not create new backing indices — aliases are already correct.
		}

		// Persist combined source hash into component template for next-run rollover detection.
		await WriteStoredSourceHashAsync(transport, state.Env, state.CurrentSourceHash, ct);
		AnsiConsole.MarkupLine($"[dim]Stored source hash {Markup.Escape(state.CurrentSourceHash)} → {Markup.Escape(UnifyStateTemplateName(state.Env))}[/]");

		foreach (var label in state.ExtraLabels)
		{
			await PointAliasAsync(transport, label, state.SemanticIndex, logger, ct);
			AnsiConsole.MarkupLine($"[green]✓[/] {Markup.Escape(label)} → {Markup.Escape(state.SemanticIndex)}");
		}

		AnsiConsole.MarkupLine("[green]✓[/] Done");
	}

	// ─── Source hash helpers ────────────────────────────────────────────────────
	// Hashes are persisted in the lexical index's _meta settings (not as a document)
	// to avoid polluting the semantic reindex with metadata docs.
	// A single combined hash covers all sources — only "did anything change?" matters.

	private static string ResolveTemplateName(string writeAlias) =>
		(writeAlias.EndsWith("-latest", StringComparison.Ordinal)
			? writeAlias[..^7]       // strip "-latest"
			: writeAlias)
		+ "-template";

	private static async Task<(string Combined, IReadOnlyList<(string Alias, string Template, string Hash)> PerSource)>
		FetchCombinedSourceHashAsync(DistributedTransport transport, IEnumerable<string> sourceAliases, CancellationToken ct)
	{
		var perSource = new List<(string, string, string)>();
		var parts = new List<string>();
		foreach (var alias in sourceAliases)
		{
			var template = ResolveTemplateName(alias);
			var resp = await transport.RequestAsync<JsonResponse>(
				Transport.HttpMethod.GET,
				$"_index_template/{Uri.EscapeDataString(template)}?filter_path=index_templates.index_template._meta.hash",
				cancellationToken: ct);
			var hash = resp.Get<string>("index_templates.0.index_template._meta.hash") ?? "missing";
			perSource.Add((alias, template, hash));
			parts.Add(alias + "=" + hash);
		}
		return (HashedBulkUpdate.CreateHash(parts.ToArray()), perSource);
	}

	private static string UnifyStateTemplateName(string env) =>
		$"website-search-unify-state-{env}";

	private static async Task<string?> ReadStoredSourceHashAsync(
		DistributedTransport transport, string env, CancellationToken ct)
	{
		var name = UnifyStateTemplateName(env);
		var resp = await transport.RequestAsync<JsonResponse>(
			Transport.HttpMethod.GET,
			$"_component_template/{Uri.EscapeDataString(name)}",
			cancellationToken: ct);
		if (!resp.ApiCallDetails.HasSuccessfulStatusCode)
			return null;
		return resp.Get<string>("component_templates.0.component_template._meta.source_hash");
	}

	private static async Task WriteStoredSourceHashAsync(
		DistributedTransport transport, string env, string combinedHash, CancellationToken ct)
	{
		var name = UnifyStateTemplateName(env);
		var body = "{\"_meta\":{\"source_hash\":\"" + combinedHash + "\"},\"template\":{}}";
		_ = await transport.PutAsync<StringResponse>(
			$"_component_template/{Uri.EscapeDataString(name)}",
			PostData.String(body), ct);
	}

	// ─── ServerReindex helpers ──────────────────────────────────────────────────

	private async Task<bool> RunServerReindexAsync(DistributedTransport transport, ServerReindexOptions opts, string label, CancellationToken ct)
	{
		AnsiConsole.MarkupLine($"[aqua]{label}[/]...");
		var reindex = new ServerReindex(transport, opts);
		ReindexProgress? last = null;

		if (IsInteractive())
		{
			await AnsiConsole.Progress()
				.Columns(new SpinnerColumn(), new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn())
				.StartAsync(async pc =>
				{
					var t = pc.AddTask($"[aqua]{label}[/]", maxValue: 1.0);
					await foreach (var p in reindex.MonitorAsync(ct))
					{
						last = p;
						t.Value = p.FractionComplete ?? 0.0;
						t.Description = $"[aqua]{label}[/] {p.Created + p.Updated:N0} docs";
						if (p.IsCompleted)
							t.Value = 1.0;
					}
				});
		}
		else
		{
			await foreach (var p in reindex.MonitorAsync(ct))
			{
				last = p;
				AnsiConsole.MarkupLine($"[dim]{label}[/] {(p.FractionComplete.HasValue ? $"{p.FractionComplete:P0}" : "?")} — created={p.Created:N0} updated={p.Updated:N0} total={p.Total:N0}");
			}
		}

		// Per-document bulk failures (checked before Error — when Failures is non-empty and no
		// task-level error occurred, the library also populates Error with a one-line summary for
		// backward compat, but the grouped breakdown below is far more actionable).
		if (last?.Failures.Count > 0)
		{
			AnsiConsole.MarkupLine($"[yellow]⚠ {label} — {last.Failures.Count:N0} document(s) failed[/]");
			foreach (var group in last.Failures
				.GroupBy(f => (f.CauseType, f.CauseReason))
				.OrderByDescending(g => g.Count())
				.Take(10))
			{
				var sample = group.First();
				AnsiConsole.MarkupLine(
					$"[dim]  ×{group.Count():N0}[/] [red]{Markup.Escape(sample.CauseType ?? "unknown")}[/]: " +
					$"{Markup.Escape(sample.CauseReason ?? "no reason given")} " +
					$"[dim](e.g. {Markup.Escape(sample.Index ?? "?")}/{Markup.Escape(sample.Id ?? "?")})[/]");
			}
			if (last.Failures.Count > 10)
				AnsiConsole.MarkupLine($"[dim]  ... {last.Failures.Count - 10:N0} more failure(s) not shown[/]");
			return false;
		}

		if (last?.Error is { } err)
		{
			AnsiConsole.MarkupLine($"[red]✗ {label} failed:[/] {Markup.Escape(err)}");
			AnsiConsole.MarkupLine($"[dim]  created={last.Created:N0} out of total={last.Total:N0} before failure[/]");
			return false;
		}

		AnsiConsole.MarkupLine($"[green]✓[/] {label} — created={last?.Created ?? 0:N0} updated={last?.Updated ?? 0:N0}");
		return true;
	}

	private async Task RunDeleteByQueryAsync(
		DistributedTransport transport, string index, string query, string slices, string label, CancellationToken ct)
	{
		AnsiConsole.MarkupLine($"[aqua]{label}[/]...");
		var dbq = new DeleteByQuery(transport, new DeleteByQueryOptions
		{
			Index = index,
			QueryBody = query,
			Slices = slices,
		});
		long deleted = 0;
		await foreach (var p in dbq.MonitorAsync(ct))
		{
			deleted = p.Deleted;
			if (!IsInteractive())
				AnsiConsole.MarkupLine($"[dim]{label}[/] deleted={p.Deleted:N0} total={p.Total:N0}");
		}
		AnsiConsole.MarkupLine($"[green]✓[/] {label} — deleted={deleted:N0}");
	}

	// ─── Reindex body builders ──────────────────────────────────────────────────

	private static string BuildLexicalFillBody(string sourceAlias, string lexicalIndex, string batchTs) =>
		"{\"source\":{\"index\":\"" + sourceAlias + "\"}," +
		"\"dest\":{\"index\":\"" + lexicalIndex + "\"}," +
		"\"script\":{\"source\":\"ctx._source.batch_index_date = params.ts;\"," +
		"\"params\":{\"ts\":\"" + batchTs + "\"}}}";

	// Reindex stale lexical docs (batch_index_date < batchTs = not in current fill = deleted from source)
	// to semantic using a delete script. Mirrors orchestrator's ReindexWithDeleteScriptAsync.
	private static string BuildDeleteScriptBody(string lexicalIndex, string semanticIndex, string batchTs) =>
		"{\"source\":{\"index\":\"" + lexicalIndex +
		"\",\"query\":{\"range\":{\"batch_index_date\":{\"lt\":\"" + batchTs + "\"}}}}," +
		"\"dest\":{\"index\":\"" + semanticIndex + "\"}," +
		"\"script\":{\"source\":\"ctx.op = 'delete'\",\"lang\":\"painless\"}}";

	// ─── Cutoff query ───────────────────────────────────────────────────────────

	private static async Task<DateTimeOffset> QueryMaxLastUpdatedAsync(
		DistributedTransport transport, string semanticIndex, CancellationToken ct)
	{
		var body = /*lang=json,strict*/ "{\"size\":0,\"aggs\":{\"max_lu\":{\"max\":{\"field\":\"last_updated\"}}}}";
		var resp = await transport.RequestAsync<JsonResponse>(
			Transport.HttpMethod.POST, $"{semanticIndex}/_search", PostData.String(body), cancellationToken: ct);
		return resp.Get<DateTimeOffset?>("aggregations.max_lu.value_as_string") ?? DateTimeOffset.MinValue;
	}

	// ─── Alias helpers ──────────────────────────────────────────────────────────

	private static async Task<string?> ResolveAliasIndexAsync(
		DistributedTransport transport, string alias, CancellationToken ct)
	{
		// _cat/aliases?h=index returns a plain-text line containing just the index name.
		// Using text/plain avoids JSON array parsing (JsonResponse.Get<T> doesn't traverse arrays).
		// This mirrors IncrementalSyncOrchestrator.ResolveExistingIndexAsync internally.
		var rq = new RequestConfiguration { Accept = "text/plain" };
		var resp = await transport.RequestAsync<StringResponse>(
			Transport.HttpMethod.GET,
			$"_cat/aliases/{Uri.EscapeDataString(alias)}?h=index",
			null, rq, ct);
		var index = resp.Body?.Trim('\n', '\r', ' ');
		return string.IsNullOrEmpty(index) ? null : index;
	}

	/// <summary>
	/// Creates <paramref name="destIndex"/> on the destination cluster with the mapping copied from
	/// <paramref name="sourceAlias"/> on the source cluster. This ensures <c>semantic_text</c> fields
	/// and their inference configuration exist before the inference reindex runs.
	/// </summary>
	private static async Task BootstrapSemanticIndexAsync(
		DistributedTransport fromTransport, DistributedTransport toTransport,
		string sourceAlias, string destIndex, ILogger logger, CancellationToken ct)
	{
		AnsiConsole.MarkupLine($"[dim]  Bootstrapping [white]{Markup.Escape(destIndex)}[/] from source mapping of [white]{Markup.Escape(sourceAlias)}[/]...[/]");

		var mappingResp = await fromTransport.RequestAsync<StringResponse>(
			Transport.HttpMethod.GET, $"{Uri.EscapeDataString(sourceAlias)}/_mapping", cancellationToken: ct);
		if (!mappingResp.ApiCallDetails.HasSuccessfulStatusCode || mappingResp.Body is null)
		{
			logger.LogError("Failed to fetch source mapping from {Alias}: {Info}", sourceAlias, mappingResp.ApiCallDetails.DebugInformation);
			return;
		}

		// Fetch only the analysis settings — tokenizers, analyzers, filters that the mapping references.
		// filter_path trims the response to just the analysis sub-tree.
		var settingsResp = await fromTransport.RequestAsync<StringResponse>(
			Transport.HttpMethod.GET,
			$"{Uri.EscapeDataString(sourceAlias)}/_settings?filter_path=**.index.analysis",
			cancellationToken: ct);

		// Response shapes:
		//   _mapping:  { "<backing>": { "mappings": { ... } } }
		//   _settings: { "<backing>": { "settings": { "index": { "analysis": { ... } } } } }
		var mappingRoot = JsonNode.Parse(mappingResp.Body);
		var mappings = mappingRoot?.AsObject().FirstOrDefault().Value?["mappings"];
		if (mappings is null)
		{
			logger.LogError("No mappings found in source {Alias} — skipping bootstrap", sourceAlias);
			return;
		}

		JsonNode? analysis = null;
		if (settingsResp.ApiCallDetails.HasSuccessfulStatusCode && settingsResp.Body is not null)
		{
			var settingsRoot = JsonNode.Parse(settingsResp.Body);
			analysis = settingsRoot?.AsObject().FirstOrDefault().Value?["settings"]?["index"]?["analysis"];
		}

		var body = analysis is not null
			? $"{{\"settings\":{{\"analysis\":{analysis.ToJsonString()}}},\"mappings\":{mappings.ToJsonString()}}}"
			: $"{{\"mappings\":{mappings.ToJsonString()}}}";

		var createResp = await toTransport.RequestAsync<StringResponse>(
			Transport.HttpMethod.PUT, Uri.EscapeDataString(destIndex), PostData.String(body), cancellationToken: ct);
		if (!createResp.ApiCallDetails.HasSuccessfulStatusCode)
			logger.LogError("Failed to create {Index} with source mapping: {Info}", destIndex, createResp.ApiCallDetails.DebugInformation);
		else
			AnsiConsole.MarkupLine($"[green]✓[/] Created [white]{Markup.Escape(destIndex)}[/] with source mapping");
	}

	/// <summary>
	/// Applies a regex rename to <paramref name="indexName"/>.
	/// <paramref name="from"/> is a regular expression; <paramref name="to"/> is the replacement
	/// (supports back-references). Returns <paramref name="indexName"/> unchanged when either
	/// argument is null or empty.
	/// </summary>
	private static string ApplyRename(string indexName, string? from, string? to) =>
		!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to)
			? Regex.Replace(indexName, from, to)
			: indexName;

	/// <summary>
	/// Derives a date-stamped backing index name from an alias on first run.
	/// Strips a trailing <c>-latest</c> suffix then appends <c>-yyyy.MM.dd.HHmmss</c>.
	/// </summary>
	private static string DeriveBackingIndexName(string alias, DateTimeOffset ts)
	{
		const string latestSuffix = "-latest";
		var prefix = alias.EndsWith(latestSuffix, StringComparison.Ordinal) ? alias[..^latestSuffix.Length] : alias;
		return $"{prefix}-{ts:yyyy.MM.dd.HHmmss}";
	}

	private static async Task PointAliasAsync(
		DistributedTransport transport, string alias, string destIndex, ILogger logger, CancellationToken ct)
	{
		var current = await ResolveAliasIndexAsync(transport, alias, ct);
		var sb = new StringBuilder("{\"actions\":[");
		_ = sb.Append("{\"add\":{\"index\":\"").Append(destIndex).Append("\",\"alias\":\"").Append(alias).Append("\"}}");
		if (current is not null && current != destIndex)
			_ = sb.Append(",{\"remove\":{\"index\":\"").Append(current).Append("\",\"alias\":\"").Append(alias).Append("\"}}");
		_ = sb.Append("]}");
		var resp = await transport.PostAsync<StringResponse>("_aliases", PostData.String(sb.ToString()), ct);
		if (!resp.ApiCallDetails.HasSuccessfulStatusCode)
			logger.LogError("Failed to point alias {Alias} → {Index}: {Info}", alias, destIndex, resp.ApiCallDetails.DebugInformation);
	}

	private static async Task<List<string>> PruneOldIndicesAsync(
		DistributedTransport transport, string pattern, int keepCount, ILogger logger, CancellationToken ct)
	{
		var pruned = new List<string>();
		var resp = await transport.RequestAsync<JsonResponse>(
			Transport.HttpMethod.GET,
			$"_cat/indices/{Uri.EscapeDataString(pattern)}?h=index&s=creation.date.string:desc&format=json",
			cancellationToken: ct);

		if (!resp.ApiCallDetails.HasSuccessfulStatusCode)
			return pruned;

		var i = 0;
		while (true)
		{
			var idx = resp.Get<string>($"{i}.index");
			if (idx is null)
				break;
			if (i >= keepCount)
			{
				var del = await transport.DeleteAsync<StringResponse>(idx, cancellationToken: ct);
				if (del.ApiCallDetails.HasSuccessfulStatusCode)
					pruned.Add(idx);
				else
					logger.LogWarning("Failed to delete old index {Index}", idx);
			}
			i++;
		}

		if (pruned.Count > 0)
			AnsiConsole.MarkupLine($"[dim]Pruned {pruned.Count} old backing {(pruned.Count == 1 ? "index" : "indices")}:[/] {string.Join(", ", pruned.Select(Markup.Escape))}");
		return pruned;
	}

	// ─── Channel options ────────────────────────────────────────────────────────

	private void ConfigureChannelOptions(
		string label, IngestChannelOptions<WebsiteSearchDocument> options, ElasticsearchEndpoint endpoint, bool semantic = false)
	{
		var log = loggerFactory.CreateLogger<IndicesCommands>();
		options.BufferOptions = new BufferOptions
		{
			OutboundBufferMaxSize = semantic ? Math.Max(1, endpoint.BufferSize / 2) : endpoint.BufferSize,
			ExportMaxConcurrency = endpoint.IndexNumThreads,
			ExportMaxRetries = endpoint.MaxRetries,
		};
		options.SerializerContext = SourceGenerationContext.Default;
		options.ExportExceptionCallback = e => log.LogError(e, "[{Label}] channel export exception", label);
		options.ExportMaxRetriesCallback = failed => log.LogError("[{Label}] max retries exceeded for {Count} items", label, failed.Count);
	}

	/// <summary>
	/// Remote-reindex a single alias from a remote (production) cluster into the same alias on
	/// this (destination) cluster. All documents are written regardless of whether they have
	/// changed — inference runs on the destination for every document.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The destination cluster must whitelist the source host:port in
	/// <c>reindex.remote.whitelist</c> (Elastic Cloud: advanced user settings).
	/// </para>
	/// <para>
	/// Use <c>--strip-semantic-fields</c> when the destination index has <c>semantic_text</c>
	/// mappings to avoid "Duplicate field <c>_inference_fields</c>"
	/// (elastic/elasticsearch#150634). This causes the destination to re-run inference even when
	/// embeddings are already present in the source.
	/// </para>
	/// <para>
	/// <c>--slices</c> must be a positive integer — <c>auto</c> is not supported for remote reindex.
	/// </para>
	/// </remarks>
	/// <param name="alias">
	/// Alias (or index) to sync. Used verbatim on both clusters — source and destination must
	/// name the target identically.
	/// </param>
	/// <param name="options">Connection and throttle options.</param>
	/// <param name="stripSemanticFields">
	/// Strip <c>_inference_fields</c> metadata before writing to the destination.
	/// </param>
	/// <param name="ct">Cancellation token.</param>
	public async Task SyncRemote(
		[Argument] string alias,
		[AsParameters] IndicesRemoteSyncOptions options,
		bool stripSemanticFields = false,
		Cancel ct = default)
	{
		var (fromEndpoint, transport, toUri) = ResolveSyncTransport(options);
		var slicesStr = options.Slices.ToString();
		var logger = loggerFactory.CreateLogger<IndicesCommands>();

		// Verify the destination alias/index exists before attempting the reindex.
		var head = await transport.HeadAsync(alias, ct);
		if (head.ApiCallDetails.HttpStatusCode != 200)
		{
			AnsiConsole.MarkupLine($"[red]✗ Destination alias/index [white]{Markup.Escape(alias)}[/] does not exist on {Markup.Escape(toUri)}[/]");
			AnsiConsole.MarkupLine("[dim]Create it first (e.g. run [white]indices unify[/] on the destination cluster).[/]");
			Environment.Exit(1);
			return;
		}

		// ── Copy docs-* search resources (synonym set + query ruleset) ────────────
		// These are cluster-level objects that remote reindex does not carry across.
		// Fail fast before starting the expensive reindex.
		if (!SearchResourceSynchronizer.TryDeriveEnvironment(alias, out var syncEnv))
		{
			AnsiConsole.MarkupLine($"[red]✗ Cannot derive environment from alias '{Markup.Escape(alias)}' — cannot copy search resources.[/]");
			AnsiConsole.MarkupLine("[dim]Alias must follow the pattern <source>.(lexical|semantic)-<env>-latest or ws-content-<env>.[/]");
			Environment.Exit(1);
			return;
		}
		AnsiConsole.MarkupLine($"[dim]Search resources:[/] syncing docs-* resources for env [white]{Markup.Escape(syncEnv)}[/]");
		try
		{
			var fromTransport = ElasticsearchTransportFactory.Create(fromEndpoint);
			await new SearchResourceSynchronizer(fromTransport, transport, logger).CopyAsync(syncEnv, ct);
		}
		catch (InvalidOperationException ex)
		{
			AnsiConsole.MarkupLine($"[red]✗ Failed to copy search resources:[/] {Markup.Escape(ex.Message)}");
			Environment.Exit(1);
			return;
		}

		var destIndex = await ResolveAliasIndexAsync(transport, alias, ct) ?? alias;
		var sourceCount = await CountAsync(transport, alias, /*match_all*/ null, ct);

		AnsiConsole.MarkupLine("[aqua bold]Indices sync-remote[/]");
		AnsiConsole.MarkupLine($"[dim]From (source):[/]   {Markup.Escape(fromEndpoint.Uri.ToString())}");
		AnsiConsole.MarkupLine($"[dim]  alias:[/]         [white]{Markup.Escape(alias)}[/]");
		AnsiConsole.MarkupLine($"[dim]  source docs:[/]   [white]{sourceCount:N0}[/]");
		AnsiConsole.MarkupLine($"[dim]To (dest):[/]       {Markup.Escape(toUri)}");
		AnsiConsole.MarkupLine($"[dim]  alias → index:[/] [white]{Markup.Escape(alias)}[/] → [white]{Markup.Escape(destIndex)}[/]");
		AnsiConsole.MarkupLine($"[dim]Slices:[/]          [white]{Markup.Escape(slicesStr)}[/]  [dim]rps:[/] [white]{(options.Rps.HasValue ? options.Rps.Value.ToString("F0") : "unlimited")}[/]");
		AnsiConsole.MarkupLine($"[dim]Strip sem. fields:[/][white]{stripSemanticFields}[/]");
		AnsiConsole.WriteLine();

		var remoteSource = BuildRemoteSource(fromEndpoint);
		if (!await RunServerReindexAsync(transport,
			new ServerReindexOptions
			{
				Remote = remoteSource,
				Source = alias,
				Destination = alias,
				RequestsPerSecond = options.Rps,
				ExcludeInferenceFields = stripSemanticFields,
			},
			$"sync-remote ({Markup.Escape(alias)})", ct))
		{
			AnsiConsole.MarkupLine("[red]Aborting — remote reindex failed.[/]");
			Environment.Exit(1);
			return;
		}

		await ApplyAliasesAsync(transport, options.Aliases, destIndex, logger, ct);
		AnsiConsole.MarkupLine("[green]✓[/] Done");
	}

	/// <summary>
	/// Two-phase incremental remote sync for a lexical+semantic index pair, avoiding
	/// re-analysis of unchanged documents:
	/// <list type="number">
	///   <item>Remote-reindex the production <em>lexical</em> alias into the destination — fast
	///     bulk copy, no inference, stamps <c>batch_index_date</c> to mark docs seen in this run.</item>
	///   <item>Locally reindex destination lexical → destination semantic, incrementally — only
	///     docs whose <c>last_updated</c> exceeds the current cutoff trigger inference;
	///     mark-and-sweep removes docs no longer in the source.</item>
	/// </list>
	/// </summary>
	/// <remarks>
	/// <para>
	/// Aliases are used verbatim on both clusters — source and destination must share the same
	/// alias names (they differ only by cluster URL). Neither cluster's templates are bootstrapped
	/// by this command; run <c>indices unify</c> first to prepare the destination.
	/// </para>
	/// <para>
	/// Aborts with a non-zero exit code if either destination target is missing or lacks the
	/// required <c>batch_index_date</c> / <c>last_updated</c> fields.
	/// </para>
	/// <para>
	/// The destination cluster must whitelist the source host:port in
	/// <c>reindex.remote.whitelist</c> (Elastic Cloud: advanced user settings).
	/// <c>--slices</c> must be a positive integer.
	/// </para>
	/// </remarks>
	/// <param name="semanticAlias">
	/// Semantic alias to sync into (e.g. <c>website-search.semantic-prod-latest</c>).
	/// Used verbatim on both clusters.
	/// </param>
	/// <param name="lexicalAlias">
	/// Lexical alias to use as the intermediate staging target
	/// (e.g. <c>website-search.lexical-prod-latest</c>).
	/// Used verbatim on both clusters.
	/// </param>
	/// <param name="renameFrom">
	/// Regular expression applied to both the lexical and semantic destination backing index names
	/// (e.g. <c>website-search\.(lexical|semantic)-prod-</c>).
	/// Must be paired with <c>--rename-to</c>.
	/// When omitted backing index names resolved from the aliases are used as-is.
	/// </param>
	/// <param name="renameTo">
	/// Replacement string for <c>--rename-from</c> — supports regex back-references
	/// (e.g. <c>ws-content.$1-staging-</c>).
	/// Applied to both the lexical and semantic destination backing index names unless <c>--rename-to-lexical</c> overrides lexical.
	/// Must be paired with <c>--rename-from</c>.
	/// </param>
	/// <param name="renameToLexical">
	/// Override replacement applied only to the lexical destination backing index name.
	/// When set takes precedence over <c>--rename-to</c> for lexical; <c>--rename-to</c> still applies to semantic.
	/// </param>
	/// <param name="options">Connection and throttle options.</param>
	/// <param name="ct">Cancellation token.</param>
	public async Task UnifyIncrementalSync(
		[Argument] string semanticAlias,
		[Argument] string lexicalAlias,
		string? renameFrom = null,
		string? renameTo = null,
		string? renameToLexical = null,
		[AsParameters] IndicesRemoteSyncOptions options = default!,
		Cancel ct = default)
	{
		var (fromEndpoint, transport, toUri) = ResolveSyncTransport(options);
		var slicesStr = options.Slices.ToString();
		var batchTs = DateTimeOffset.UtcNow;
		var batchTsStr = batchTs.ToString("o");
		var logger = loggerFactory.CreateLogger<IndicesCommands>();

		// ── Validate destination targets ──────────────────────────────────────────
		// Both aliases must exist and expose the tracking fields used for incremental sync.
		if (!await IndexHasFieldsAsync(transport, lexicalAlias, ["batch_index_date", "last_updated"], ct)
			|| !await IndexHasFieldsAsync(transport, semanticAlias, ["last_updated"], ct))
		{
			Environment.Exit(1);
			return;
		}

		// ── Copy docs-* search resources (synonym set + query ruleset) ────────────
		// These are cluster-level objects that remote reindex does not carry across.
		// Fail fast before starting the expensive reindex.
		// Hoist the source transport here; it is also used in the semanticIsNew bootstrap below.
		var fromTransport = ElasticsearchTransportFactory.Create(fromEndpoint);
		if (!SearchResourceSynchronizer.TryDeriveEnvironment(semanticAlias, out var syncEnv))
		{
			AnsiConsole.MarkupLine($"[red]✗ Cannot derive environment from alias '{Markup.Escape(semanticAlias)}' — cannot copy search resources.[/]");
			AnsiConsole.MarkupLine("[dim]Alias must follow the pattern <source>.(lexical|semantic)-<env>-latest.[/]");
			Environment.Exit(1);
			return;
		}
		AnsiConsole.MarkupLine($"[dim]Search resources:[/] syncing docs-* resources for env [white]{Markup.Escape(syncEnv)}[/]");
		try
		{
			await new SearchResourceSynchronizer(fromTransport, transport, logger).CopyAsync(syncEnv, ct);
		}
		catch (InvalidOperationException ex)
		{
			AnsiConsole.MarkupLine($"[red]✗ Failed to copy search resources:[/] {Markup.Escape(ex.Message)}");
			Environment.Exit(1);
			return;
		}

		// ── Resolve concrete write indices behind each alias ──────────────────────
		// On first run the alias won't exist yet — derive a date-stamped backing index name so the
		// alias can be pointed at a real index rather than creating an index named after the alias.
		var resolvedLexicalIndex = await ResolveAliasIndexAsync(transport, lexicalAlias, ct);
		var lexicalIndex = ApplyRename(
			resolvedLexicalIndex ?? DeriveBackingIndexName(lexicalAlias, batchTs),
			renameFrom, renameToLexical ?? renameTo);
		var resolvedSemanticIndex = await ResolveAliasIndexAsync(transport, semanticAlias, ct);
		var semanticIndex = ApplyRename(
			resolvedSemanticIndex ?? DeriveBackingIndexName(semanticAlias, batchTs),
			renameFrom, renameTo);
		// True when the destination index doesn't exist yet and must be created from source mapping.
		// Lexical is plain-text — ES dynamic mapping is sufficient, no bootstrap needed.
		var semanticIsNew = resolvedSemanticIndex is null || semanticIndex != resolvedSemanticIndex;

		// ── Compute cutoff before phase 1 (independent of the remote fill) ────────
		// cutoff = max(last_updated) currently in the semantic index.
		// Docs with last_updated > cutoff were changed/added in prod since the last sync → re-infer.
		var cutoff = await QueryMaxLastUpdatedAsync(transport, semanticIndex, ct);

		AnsiConsole.MarkupLine("[aqua bold]Indices unify-incremental-sync[/]");
		AnsiConsole.MarkupLine($"[dim]From (source):[/]      {Markup.Escape(fromEndpoint.Uri.ToString())}");
		AnsiConsole.MarkupLine($"[dim]  lexical alias:[/]    [white]{Markup.Escape(lexicalAlias)}[/]");
		AnsiConsole.MarkupLine($"[dim]To (dest):[/]          {Markup.Escape(toUri)}");
		var lexicalIndexDisplay = resolvedLexicalIndex is not null && lexicalIndex != resolvedLexicalIndex
			? $"{Markup.Escape(resolvedLexicalIndex)} [dim](renamed →)[/] {Markup.Escape(lexicalIndex)}"
			: Markup.Escape(lexicalIndex);
		AnsiConsole.MarkupLine($"[dim]  lexical  → index:[/] [white]{Markup.Escape(lexicalAlias)}[/] → [white]{lexicalIndexDisplay}[/]");
		var semanticIndexDisplay = resolvedSemanticIndex is not null && semanticIndex != resolvedSemanticIndex
			? $"{Markup.Escape(resolvedSemanticIndex)} [dim](renamed →)[/] {Markup.Escape(semanticIndex)}"
			: Markup.Escape(semanticIndex);
		AnsiConsole.MarkupLine($"[dim]  semantic → index:[/] [white]{Markup.Escape(semanticAlias)}[/] → [white]{semanticIndexDisplay}[/]");
		AnsiConsole.MarkupLine($"[dim]Slices:[/]              [white]{Markup.Escape(slicesStr)}[/]  [dim]rps:[/] [white]{(options.Rps.HasValue ? options.Rps.Value.ToString("F0") : "unlimited")}[/]");
		AnsiConsole.MarkupLine($"[dim]batch_index_date (this run):[/] [white]{batchTs:o}[/]");
		AnsiConsole.MarkupLine($"[dim]cutoff (max last_updated in semantic):[/] [white]{cutoff:o}[/]");
		AnsiConsole.WriteLine();

		// ── Phase 1: Remote-reindex prod lexical → dest lexical ───────────────────
		// Fast bulk copy — lexical has no semantic_text fields → no inference.
		// Stamps batch_index_date = batchTs on every doc so the mark-and-sweep can find deletes.
		// Docs removed from prod are NOT restamped → their batch_index_date < batchTs after this step.
		var stampScript = "{\"lang\":\"painless\",\"source\":\"ctx._source.batch_index_date = params.ts;\",\"params\":{\"ts\":\"" + batchTsStr + "\"}}";
		AnsiConsole.MarkupLine($"[dim]Phase 1:[/] remote-fill [white]{Markup.Escape(lexicalAlias)}[/] → [white]{Markup.Escape(lexicalIndex)}[/]");
		var remoteSource = BuildRemoteSource(fromEndpoint);
		if (!await RunServerReindexAsync(transport,
			new ServerReindexOptions
			{
				Remote = remoteSource,
				Source = lexicalAlias,
				Destination = lexicalIndex,
				Script = stampScript,
				RequestsPerSecond = options.Rps,
			},
			"remote-lexical-fill", ct))
		{
			AnsiConsole.MarkupLine("[red]Aborting — remote lexical fill failed.[/]");
			Environment.Exit(1);
			return;
		}
		_ = await transport.PostAsync<StringResponse>($"{lexicalIndex}/_refresh", PostData.Empty, ct);
		AnsiConsole.MarkupLine("[dim]Lexical refreshed[/]");
		await PointAliasAsync(transport, lexicalAlias, lexicalIndex, logger, ct);
		AnsiConsole.MarkupLine($"[green]✓[/] {Markup.Escape(lexicalAlias)} → {Markup.Escape(lexicalIndex)}");

		// ── Phase 2: Preview counts before any writes to semantic ─────────────────
		// toSync  = docs with last_updated > cutoff  → need (re-)inference
		// toDelete = docs with batch_index_date < batchTs → were not in prod this run → delete
		var toSyncQuery = "{\"range\":{\"last_updated\":{\"gt\":\"" + cutoff.ToString("o") + "\"}}}";
		var toDeleteQuery = "{\"range\":{\"batch_index_date\":{\"lt\":\"" + batchTsStr + "\"}}}";
		var toSync = await CountAsync(transport, lexicalIndex, toSyncQuery, ct);
		var toDelete = await CountAsync(transport, lexicalIndex, toDeleteQuery, ct);

		AnsiConsole.MarkupLine("[dim]Phase 2 preview:[/]");
		AnsiConsole.MarkupLine($"  [green]→[/] [white]{toSync:N0}[/] [dim]docs to (re)index into [white]{Markup.Escape(semanticAlias)}[/]  (last_updated > {cutoff:o})[/]");
		AnsiConsole.MarkupLine($"  [red]←[/] [white]{toDelete:N0}[/] [dim]docs to delete from [white]{Markup.Escape(semanticAlias)}[/] + [white]{Markup.Escape(lexicalAlias)}[/]  (batch_index_date < {batchTs:o})[/]");
		AnsiConsole.WriteLine();

		// ── Phase 2a: Incremental inference reindex ───────────────────────────────
		// Reindex only docs newer than the cutoff — these are changed/added since the last sync.
		// slices=1: prevents concurrent bulk requests from overwhelming the inference endpoint.
		if (semanticIsNew)
		{
			await BootstrapSemanticIndexAsync(fromTransport, transport, semanticAlias, semanticIndex, logger, ct);
		}
		const string semanticSlices = "1";
		AnsiConsole.MarkupLine($"[dim]Phase 2a:[/] inference-reindex [white]{Markup.Escape(lexicalAlias)}[/] → [white]{Markup.Escape(semanticIndex)}[/]");
		var inferenceBody =
			"{\"source\":{\"index\":\"" + lexicalIndex +
			"\",\"query\":" + toSyncQuery + "}," +
			"\"dest\":{\"index\":\"" + semanticIndex + "\"}}";
		if (!await RunServerReindexAsync(transport,
			new ServerReindexOptions { Body = inferenceBody, Slices = semanticSlices, RequestsPerSecond = options.Rps },
			"inference-reindex", ct))
		{
			AnsiConsole.MarkupLine("[red]Aborting — inference reindex failed.[/]");
			Environment.Exit(1);
			return;
		}

		// ── Phase 2b: Delete step — remove docs gone from prod ────────────────────
		// Reindex stale lexical docs (batch_index_date < batchTs) to semantic with a delete script.
		AnsiConsole.MarkupLine($"[dim]Phase 2b:[/] delete-reindex [white]{Markup.Escape(lexicalAlias)}[/] → [white]{Markup.Escape(semanticIndex)}[/]");
		var deleteBody = BuildDeleteScriptBody(lexicalIndex, semanticIndex, batchTsStr);
		if (!await RunServerReindexAsync(transport,
			new ServerReindexOptions { Body = deleteBody, Slices = slicesStr },
			"reindex-deletes", ct))
		{
			AnsiConsole.MarkupLine("[red]Aborting — delete step failed.[/]");
			Environment.Exit(1);
			return;
		}

		// ── Phase 2c: Lexical cleanup — prune stale docs from lexical too ─────────
		AnsiConsole.MarkupLine($"[dim]Phase 2c:[/] cleanup-lexical [white]{Markup.Escape(lexicalAlias)}[/]");
		await RunDeleteByQueryAsync(transport, lexicalIndex, toDeleteQuery, slicesStr, "cleanup-lexical", ct);

		await PointAliasAsync(transport, semanticAlias, semanticIndex, logger, ct);
		AnsiConsole.MarkupLine($"[green]✓[/] {Markup.Escape(semanticAlias)} → {Markup.Escape(semanticIndex)}");
		await ApplyAliasesAsync(transport, options.Aliases, semanticIndex, logger, ct);
		AnsiConsole.MarkupLine("[green]✓[/] Done");
	}

	/// <summary>
	/// Delete old rolled-over backing indices, keeping the N most recent per alias family.
	/// </summary>
	/// <remarks>
	/// Any index that a <c>-latest</c> alias currently points to is always retained, even if that
	/// index falls outside the <c>--keep</c> window. Active indices count toward the keep budget,
	/// so <c>--keep 2</c> retains the active index plus the single most recent non-active index.
	///
	/// Managed alias families (derived from mapping contexts):
	/// <list type="bullet">
	///   <item><c>site-{buildType}.{lexical|semantic}-{env}</c></item>
	///   <item><c>labs-{buildType}.{lexical|semantic}-{env}</c></item>
	///   <item><c>guide-{buildType}.{lexical|semantic}-{env}</c></item>
	///   <item><c>website-search.{lexical|semantic}-{env}</c></item>
	///   <item><c>docs-assembler.{lexical|semantic}-{env}</c> (external — no mapping context)</item>
	/// </list>
	/// The <c>ws-content-{env}</c> alias is also protected unconditionally; a warning is emitted
	/// if it diverges from <c>website-search.semantic-{env}-latest</c>.
	/// </remarks>
	/// <param name="options">Cleanup options.</param>
	/// <param name="ct">Cancellation token.</param>
	public async Task Cleanup([AsParameters] IndicesCleanupOptions options, Cancel ct = default)
	{
		var endpoint = ResolveEndpoint(options.EsUrl, options.EsApiKey);
		var environment = options.Environment ?? config.ElasticsearchEnvironment;
		var buildType = config.BuildType;
		var keep = options.Keep;
		var dryRun = options.DryRun;
		var pageAlias = IndicesCleanupPlanner.PageAliasName(environment);

		AnsiConsole.MarkupLine("[aqua bold]Indices cleanup[/]");
		AnsiConsole.MarkupLine($"[dim]Elasticsearch:[/] {Markup.Escape(endpoint.Uri.ToString())}");
		AnsiConsole.MarkupLine($"[dim]Environment:[/]   [white]{Markup.Escape(environment)}[/]");
		AnsiConsole.MarkupLine($"[dim]Keep:[/]          [white]{keep}[/] per alias family");
		if (dryRun)
			AnsiConsole.MarkupLine("[yellow]Dry run — no indices will be deleted[/]");
		AnsiConsole.WriteLine();

		var knownAliases = IndicesCleanupPlanner.BuildAliasEntries(buildType, environment);

		AnsiConsole.MarkupLine("[dim]Checking alias families:[/]");
		foreach (var a in knownAliases)
			AnsiConsole.MarkupLine($"  [dim]{Markup.Escape(a.LatestAlias)}[/]");
		AnsiConsole.MarkupLine($"[dim]Protected alias:[/]  [white]{Markup.Escape(pageAlias)}[/]");
		AnsiConsole.WriteLine();

		var transport = ElasticsearchTransportFactory.Create(endpoint);

		// Single round-trip: GET all matching index patterns and their aliases
		var patterns = string.Join(",", knownAliases.Select(a => a.IndexPattern));
		var aliasResponse = await transport.GetAsync<JsonResponse>($"{patterns}/_alias", ct);

		if (!aliasResponse.ApiCallDetails.HasSuccessfulStatusCode)
		{
			if (aliasResponse.ApiCallDetails.HttpStatusCode == 404)
			{
				AnsiConsole.MarkupLine("[green]Nothing to clean — no rolled-over indices found.[/]");
				return;
			}

			AnsiConsole.MarkupLine(
				$"[red]Error fetching index aliases:[/] HTTP {aliasResponse.ApiCallDetails.HttpStatusCode}");
			Environment.Exit(1);
			return;
		}

		var indexAliases = ParseAliasResponse(aliasResponse.Body);
		if (indexAliases.Count == 0)
		{
			AnsiConsole.MarkupLine("[green]Nothing to clean — no rolled-over indices found.[/]");
			return;
		}

		var plan = IndicesCleanupPlanner.Plan(indexAliases, knownAliases, keep, pageAlias);

		foreach (var warning in plan.Warnings)
			AnsiConsole.MarkupLine($"[yellow]! {Markup.Escape(warning)}[/]");

		RenderPlanTable(plan);

		if (plan.ToDelete.Count == 0)
		{
			AnsiConsole.MarkupLine("[green]Nothing to delete — all index families are within the keep window.[/]");
			return;
		}

		if (dryRun)
		{
			AnsiConsole.MarkupLine($"[yellow]Dry run — would delete {plan.ToDelete.Count} index(es).[/]");
			return;
		}

		var deleted = 0;
		var failed = 0;
		foreach (var index in plan.ToDelete)
		{
			ct.ThrowIfCancellationRequested();
			var deleteResponse = await transport.DeleteAsync<JsonResponse>(index.Name, new DefaultRequestParameters(), null, ct);
			if (deleteResponse.ApiCallDetails.HasSuccessfulStatusCode)
			{
				deleted++;
				AnsiConsole.MarkupLine($"[dim]Deleted[/] [white]{Markup.Escape(index.Name)}[/]");
			}
			else
			{
				failed++;
				AnsiConsole.MarkupLine(
					$"[red]Failed to delete[/] [white]{Markup.Escape(index.Name)}[/] — HTTP {deleteResponse.ApiCallDetails.HttpStatusCode}");
			}
		}

		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine(
			$"[green]✓[/] Done — kept [white]{plan.ToKeep.Count}[/], deleted [white]{deleted}[/]" +
			(failed > 0 ? $", [red]failed {failed}[/]" : ""));

		if (failed > 0)
			Environment.Exit(1);
	}

	private static IReadOnlyDictionary<string, IReadOnlySet<string>> ParseAliasResponse(JsonNode? body)
	{
		if (body is not JsonObject root)
			return new Dictionary<string, IReadOnlySet<string>>();

		var result = new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase);
		foreach (var (indexName, indexNode) in root)
		{
			var aliasesNode = indexNode?["aliases"]?.AsObject();
			var aliases = aliasesNode is not null
				? aliasesNode.Select(a => a.Key).ToHashSet(StringComparer.OrdinalIgnoreCase)
				: [with(StringComparer.OrdinalIgnoreCase)];
			result[indexName] = aliases;
		}
		return result;
	}

	private static void RenderPlanTable(CleanupPlan plan)
	{
		var keepSet = plan.ToKeep.ToHashSet();
		var all = plan.ToKeep.Concat(plan.ToDelete)
			.OrderBy(b => b.Group.Source)
			.ThenBy(b => b.Group.Variant)
			.ThenByDescending(b => b.Date)
			.ToList();

		if (all.Count == 0)
			return;

		var table = new Table()
			.Border(TableBorder.Rounded)
			.BorderColor(Color.Grey)
			.Title("[aqua]Cleanup plan[/]")
			.AddColumn("Type")
			.AddColumn("Form")
			.AddColumn("Env")
			.AddColumn(new TableColumn("Date").RightAligned())
			.AddColumn(new TableColumn("Action").Centered());

		(string Source, string Variant)? lastGroup = null;
		foreach (var idx in all)
		{
			var currentGroup = (idx.Group.Source, idx.Group.Variant);
			if (lastGroup is not null && lastGroup != currentGroup)
				_ = table.AddEmptyRow();
			lastGroup = currentGroup;

			var action = idx.IsActive
				? "[green]KEEP-ACTIVE[/]"
				: keepSet.Contains(idx)
					? "[grey]KEEP[/]"
					: "[red]DELETE[/]";
			_ = table.AddRow(
				Markup.Escape(idx.Group.Source),
				Markup.Escape(idx.Group.Variant),
				Markup.Escape(idx.Group.Environment),
				idx.Date.ToString("yyyy-MM-dd HH:mm:ss"),
				action);
		}

		AnsiConsole.Write(table);
		AnsiConsole.WriteLine();
	}


	// ─── Remote sync helpers ────────────────────────────────────────────────────

	private (ElasticsearchEndpoint From, DistributedTransport ToTransport, string ToUri) ResolveSyncTransport(
		IndicesRemoteSyncOptions o)
	{
		var from = ResolveEndpoint(o.FromUrl, o.FromApiKey);

		var to = config.Destination ?? new ElasticsearchEndpoint { Uri = new Uri("http://localhost:9200") };
		if (o.ToUrl is not null)
			to.Uri = o.ToUrl;
		if (o.ToApiKey is not null)
		{
			to.ApiKey = o.ToApiKey;
			to.Username = null;
			to.Password = null;
		}

		if (to.Uri.Host == "localhost" && o.ToUrl is null && config.Destination is null)
		{
			AnsiConsole.MarkupLine("[red]✗ Destination cluster URL is not configured.[/]");
			AnsiConsole.MarkupLine("[dim]Set [white]DESTINATION_ELASTIC_URL[/] or pass [white]--to-url[/].[/]");
			Environment.Exit(1);
			// Environment.Exit throws; return is unreachable but required for the compiler.
			return (from, null, string.Empty);
		}

		return (from, ElasticsearchTransportFactory.Create(to), to.Uri.ToString().TrimEnd('/'));
	}

	private async Task ApplyAliasesAsync(
		DistributedTransport transport, string? applyAliases, string destIndex, ILogger logger, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(applyAliases))
			return;
		foreach (var alias in applyAliases.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			await PointAliasAsync(transport, alias, destIndex, logger, ct);
			AnsiConsole.MarkupLine($"[green]✓[/] {Markup.Escape(alias)} → {Markup.Escape(destIndex)}");
		}
	}

	private static RemoteSource BuildRemoteSource(ElasticsearchEndpoint from)
	{
		// ES remote reindex requires an explicit port — Uri.ToString() omits the default (443 for https).
		var uri = from.Uri;
		var host = $"{uri.Scheme}://{uri.Host}:{uri.Port}{uri.AbsolutePath.TrimEnd('/')}";
		return new RemoteSource
		{
			Host = host,
			ApiKey = from.ApiKey,
			Username = from.ApiKey is null ? from.Username : null,
			Password = from.ApiKey is null ? from.Password : null,
		};
	}

	/// <summary>
	/// Checks that <paramref name="target"/>, if it already exists, exposes all
	/// <paramref name="requiredFields"/> in its mapping. A missing index (404) is allowed —
	/// the reindex will create it and the cutoff falls back to epoch.
	/// Returns <c>false</c> (and prints an error) only when the index exists but a required
	/// field is absent from its mapping.
	/// </summary>
	private static async Task<bool> IndexHasFieldsAsync(
		DistributedTransport transport, string target, string[] requiredFields, CancellationToken ct)
	{
		var csv = string.Join(",", requiredFields);
		// Use StringResponse + plain string check to avoid Get<T> on object-typed nodes.
		// _field_caps returns {"fields":{"<name>":{...}}} — checking for the key name as a JSON key
		// is sufficient to confirm the field is mapped.
		var resp = await transport.RequestAsync<StringResponse>(
			Transport.HttpMethod.GET,
			$"{Uri.EscapeDataString(target)}/_field_caps?fields={csv}&filter_path=fields",
			cancellationToken: ct);

		if (!resp.ApiCallDetails.HasSuccessfulStatusCode)
		{
			var status = resp.ApiCallDetails.HttpStatusCode;
			// 404 = index/alias doesn't exist yet — first-run scenario, reindex will create it.
			if (status == 404)
			{
				AnsiConsole.MarkupLine($"[dim]  {Markup.Escape(target)} not found — will be created on first sync.[/]");
				return true;
			}
			// null status = connection-level failure (DNS, TLS, timeout) — hard fail.
			AnsiConsole.MarkupLine(
				$"[red]✗ Destination target [white]{Markup.Escape(target)}[/] not found or inaccessible " +
				$"(HTTP {status?.ToString() ?? "connection error"}).[/]");
			return false;
		}

		var body = resp.Body ?? string.Empty;
		var ok = true;
		foreach (var field in requiredFields)
		{
			// A mapped field appears as a JSON key: `"<fieldname>":`
			if (!body.Contains($"\"{field}\":", StringComparison.Ordinal))
			{
				AnsiConsole.MarkupLine(
					$"[red]✗ Field [white]{Markup.Escape(field)}[/] not found in mapping of [white]{Markup.Escape(target)}[/].[/]");
				AnsiConsole.MarkupLine(
					$"[dim]  [white]unify-incremental-sync[/] requires [white]{Markup.Escape(field)}[/] " +
					$"for incremental tracking. Run [white]indices unify[/] on the destination first.[/]");
				ok = false;
			}
		}
		return ok;
	}

	/// <summary>
	/// Returns the document count for <paramref name="target"/> matching <paramref name="queryBody"/>
	/// (a JSON query object, or <c>null</c> for match-all).
	/// </summary>
	private static async Task<long> CountAsync(
		DistributedTransport transport, string target, string? queryBody, CancellationToken ct)
	{
		var body = queryBody is null ? null : PostData.String("{\"query\":" + queryBody + "}");
		var resp = await transport.RequestAsync<JsonResponse>(
			Transport.HttpMethod.POST,
			$"{Uri.EscapeDataString(target)}/_count",
			body,
			cancellationToken: ct);
		return resp.Get<long?>("count") ?? 0;
	}

	private ElasticsearchEndpoint ResolveEndpoint(Uri? esUrl, string? esApiKey)
	{
		var endpoint = config.Elasticsearch;
		if (esUrl is not null)
			endpoint.Uri = esUrl;
		if (esApiKey is not null)
		{
			endpoint.ApiKey = esApiKey;
			endpoint.Username = null;
			endpoint.Password = null;
		}
		return endpoint;
	}
}
