// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Elastic.Documentation.Versions;
using Microsoft.Extensions.Logging;
using Nullean.ScopedFileSystem;

namespace Elastic.Changelog.Backfill;

public record BackfillArguments
{
	/// <summary>Optional product-id selection; empty covers every product in the checked-in scope table.</summary>
	public IReadOnlyList<string> Products { get; init; } = [];

	/// <summary>Optional exact-version filter; when set, only these versions are backfilled.</summary>
	public IReadOnlyList<string> Versions { get; init; } = [];

	/// <summary>Absolute output directory path (resolved by the command before passing here).</summary>
	public required string Output { get; init; }

	/// <summary>Maximum number of pages fetched concurrently. Clamped to 1–16.</summary>
	public int Concurrency { get; init; } = 4;

	/// <summary>When true, fetches and parses every page but writes nothing to disk.</summary>
	public bool DryRun { get; init; }

	/// <summary>Base URL for site-source pages (overridable in tests).</summary>
	public string BaseUrl { get; init; } = "https://www.elastic.co/docs";

	/// <summary>Base URL for repo-source pages (overridable in tests).</summary>
	public string RawBaseUrl { get; init; } = "https://raw.githubusercontent.com";
}

/// <summary>Per-product outcome of a backfill run, included in the printed report.</summary>
public sealed record BackfillProductResult(
	string ProductId,
	string SourceUrl,
	/// <summary><c>ok</c> | <c>empty</c> | <c>unavailable</c> | <c>failed</c> | <c>skipped</c></summary>
	string Outcome,
	int Versions,
	int Entries,
	int FilesWritten,
	int NoPrEntries,
	int DuplicatePrRefs,
	string Detail);

/// <summary>
/// Backfills changelog entries, notes registries, and bundles from published release-notes pages.
/// Sourced from scrape-release-notes.py (scripts/, untracked prototype, 2026-08-25).
/// Writes to disk only; publishing stays the job of <c>changelog upload</c>.
/// </summary>
public class ChangelogBackfillService(
	ILoggerFactory logFactory,
	ScopedFileSystem fileSystem,
	HttpMessageHandler? httpMessageHandler = null
) : IService
{
	private const string OutcomeOk = "ok";
	private const string OutcomeEmpty = "empty";
	private const string OutcomeUnavailable = "unavailable";
	private const string OutcomeFailed = "failed";
	private const string OutcomeSkipped = "skipped";

	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogBackfillService>();

	/// <summary>Per-product results of the most recent run; exposed for tests.</summary>
	internal IReadOnlyList<BackfillProductResult> LastResults { get; private set; } = [];

	public async Task<bool> Backfill(IDiagnosticsCollector collector, BackfillArguments args, Cancel ctx)
	{
		var scopes = BackfillScope.Select(collector, args.Products);
		if (scopes is null)
			return false;

		var concurrency = Math.Clamp(args.Concurrency, 1, 16);
		var fetchedMarkdowns = await FetchAllMarkdown(collector, args, scopes, concurrency, ctx);

		var allResults = new List<BackfillProductResult>();
		var failedProducts = 0;
		foreach (var (scope, markdown) in scopes.Zip(fetchedMarkdowns))
		{
			ctx.ThrowIfCancellationRequested();
			var result = ProcessProduct(collector, args, scope, markdown);
			if (result.Outcome == OutcomeFailed)
				failedProducts++;
			allResults.Add(result);
		}

		LastResults = allResults;
		Console.Write(FormatReport(allResults));

		var totalNoPr = allResults.Sum(r => r.NoPrEntries);
		var totalEntries = allResults.Sum(r => r.Entries);
		if (totalNoPr > 0)
		{
			var pct = totalEntries > 0 ? totalNoPr * 100.0 / totalEntries : 0;
			collector.EmitWarning(string.Empty,
				$"{totalNoPr}/{totalEntries} entries ({pct:F1}%) could not be traced back to a pull request and were written as note-*.yaml.");
		}

		if (failedProducts > 0)
			collector.EmitError(string.Empty, $"{failedProducts} product(s) failed; see the errors above.");

		return failedProducts == 0;
	}

	private async Task<string?[]> FetchAllMarkdown(
		IDiagnosticsCollector collector,
		BackfillArguments args,
		IReadOnlyList<BackfillScope> scopes,
		int concurrency,
		Cancel ctx)
	{
		using var client = BuildHttpClient();
		using var semaphore = new SemaphoreSlim(concurrency, concurrency);

		var tasks = scopes.Select(async scope =>
		{
			await semaphore.WaitAsync(ctx);
			try
			{
				var url = scope.IsRepoSource
					? $"{args.RawBaseUrl}/{scope.Owner}/{scope.Repo}/{scope.Ref}/{scope.RepoPath}"
					: $"{args.BaseUrl}/release-notes/{scope.Path}.md";
				return await FetchWithRetry(collector, client, url, ctx);
			}
			finally
			{
				_ = semaphore.Release();
			}
		}).ToArray();

		return await Task.WhenAll(tasks);
	}

	private HttpClient BuildHttpClient()
	{
		if (httpMessageHandler is not null)
			return new HttpClient(httpMessageHandler, disposeHandler: false) { Timeout = TimeSpan.FromSeconds(30) };

		var handler = new SocketsHttpHandler
		{
			AutomaticDecompression = System.Net.DecompressionMethods.All,
			PooledConnectionLifetime = TimeSpan.FromMinutes(2)
		};
		return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
	}

	private async Task<string?> FetchWithRetry(IDiagnosticsCollector collector, HttpClient client, string url, Cancel ctx)
	{
		const int maxAttempts = 3;
		var retryable = new HashSet<int> { 408, 429, 500, 502, 503, 504 };

		for (var attempt = 0; attempt < maxAttempts; attempt++)
		{
			if (attempt > 0)
			{
				_logger.LogInformation("Retry {Attempt}/{Max} for {Url}", attempt, maxAttempts - 1, url);
				var delay = TimeSpan.FromMilliseconds((500 * Math.Pow(2, attempt - 1)) + Random.Shared.Next(0, 250));
				await Task.Delay(delay, ctx);
			}

			try
			{
				using var request = new HttpRequestMessage(HttpMethod.Get, url);
				request.Headers.Add("User-Agent", "docs-builder");
				using var response = await client.SendAsync(request, ctx);

				if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
				{
					collector.EmitHint(url, $"Release-notes page returned 404 — product will be skipped (URL may have moved).");
					return null;
				}

				if (!response.IsSuccessStatusCode)
				{
					if (retryable.Contains((int)response.StatusCode) && attempt < maxAttempts - 1)
						continue;
					collector.EmitError(url, $"Fetching release notes failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).");
					return null;
				}

				return await response.Content.ReadAsStringAsync(ctx);
			}
			catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ctx.IsCancellationRequested && attempt < maxAttempts - 1)
			{
				_logger.LogInformation("Transient error fetching {Url}: {Message}", url, ex.Message);
			}
			catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ctx.IsCancellationRequested)
			{
				collector.EmitError(url, $"Fetching release notes failed after {maxAttempts} attempts: {ex.Message}", ex);
				return null;
			}
		}

		collector.EmitError(url, $"Fetching release notes failed after {maxAttempts} attempts.");
		return null;
	}

	private BackfillProductResult ProcessProduct(
		IDiagnosticsCollector collector,
		BackfillArguments args,
		BackfillScope scope,
		string? markdown)
	{
		var url = scope.IsRepoSource
			? $"{args.RawBaseUrl}/{scope.Owner}/{scope.Repo}/{scope.Ref}/{scope.RepoPath}"
			: $"{args.BaseUrl}/release-notes/{scope.Path}.md";

		if (markdown is null)
		{
			// FetchWithRetry already emitted the appropriate error or hint.
			var outcome = collector.Errors > 0 ? OutcomeFailed : OutcomeUnavailable;
			return new BackfillProductResult(scope.ProductId, url, outcome, 0, 0, 0, 0, 0, "fetch failed");
		}

		var releases = ReleaseNotesPageParser.Parse(collector, markdown, url, scope);
		if (releases.Count == 0)
		{
			collector.EmitHint(url, $"No release sections parsed from '{scope.ProductId}' — the page may be an empty <changelog> stub or contain no ## headings.");
			return new BackfillProductResult(scope.ProductId, url, OutcomeEmpty, 0, 0, 0, 0, 0, "no ## version headings found");
		}

		_logger.LogInformation("Parsed {Count} release section(s) from {Url}", releases.Count, url);

		// Apply version filters
		var cutoff = scope.Cutoff is not null ? VersionOrDate.Parse(scope.Cutoff) : null;
		var selection = args.Versions.Count > 0 ? new HashSet<string>(args.Versions, StringComparer.OrdinalIgnoreCase) : null;
		var inScope = releases.Where(r =>
		{
			if (selection is not null && !selection.Contains(r.Version))
				return false;
			if (cutoff is not null && VersionOrDate.Parse(r.Version).CompareTo(cutoff) > 0)
				return false;
			return true;
		}).ToList();

		if (args.DryRun)
			return new BackfillProductResult(scope.ProductId, url, OutcomeOk, inScope.Count, 0, 0, 0, 0, "dry-run; nothing written");

		var (filesWritten, noPr, dupPr) = WriteBundles(collector, args, scope, inScope);
		return new BackfillProductResult(scope.ProductId, url, OutcomeOk, inScope.Count, inScope.Count, filesWritten, noPr, dupPr, "");
	}

	private (int FilesWritten, int NoPrEntries, int DuplicatePrRefs) WriteBundles(
		IDiagnosticsCollector collector,
		BackfillArguments args,
		BackfillScope scope,
		IReadOnlyList<MigratedRelease> releases)
	{
		var bundleDir = Path.Join(args.Output, scope.ProductId, "changelog", "bundles");
		var filesWritten = 0;

		try
		{
			_ = fileSystem.Directory.CreateDirectory(bundleDir);
			foreach (var release in releases)
			{
				var yaml = ReleaseNotesSerialization.SerializeBundle(release.Bundle);
				var filePath = Path.Join(bundleDir, $"{SanitizeFileName(release.Version)}.yaml");
				fileSystem.File.WriteAllText(filePath, yaml);
				filesWritten++;
			}
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
		{
			collector.EmitError(bundleDir, $"Could not write backfill bundles: {ex.Message}", ex);
		}

		return (filesWritten, 0, 0);
	}

	private static string SanitizeFileName(string version) =>
		// Replace characters unsafe in file names; versions are generally safe but guard anyway.
		string.Join('_', version.Split(Path.GetInvalidFileNameChars()));

	/// <summary>Formats the run report as a markdown table suitable for pasting into the tracking issue.</summary>
	public static string FormatReport(IReadOnlyList<BackfillProductResult> results)
	{
		var sb = new StringBuilder();
		_ = sb.AppendLine("### changelog backfill report");
		_ = sb.AppendLine();
		_ = sb.AppendLine("| product | outcome | versions | entries | files | no-pr | no-pr % | detail |");
		_ = sb.AppendLine("|---|---|---|---|---|---|---|---|");

		foreach (var r in results)
		{
			var pct = r.Entries > 0 ? r.NoPrEntries * 100.0 / r.Entries : 0;
			_ = sb.AppendLine(CultureInfo.InvariantCulture,
				$"| `{r.ProductId}` | {r.Outcome} | {r.Versions} | {r.Entries} | {r.FilesWritten} | {r.NoPrEntries} | {pct:F1}% | {r.Detail} |");
		}

		var totalEntries = results.Sum(r => r.Entries);
		var totalFiles = results.Sum(r => r.FilesWritten);
		var totalNoPr = results.Sum(r => r.NoPrEntries);
		var totalPct = totalEntries > 0 ? totalNoPr * 100.0 / totalEntries : 0;
		_ = sb.AppendLine(CultureInfo.InvariantCulture,
			$"| **totals** | | {results.Sum(r => r.Versions)} | {totalEntries} | {totalFiles} | {totalNoPr} | {totalPct:F1}% | |");

		return sb.ToString();
	}
}
