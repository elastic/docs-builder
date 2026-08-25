// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;
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
public partial class ChangelogBackfillService(
	ILoggerFactory logFactory,
	ScopedFileSystem fileSystem,
	HttpMessageHandler? httpMessageHandler = null,
	TimeProvider? timeProvider = null
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
		foreach (var (scope, (markdown, fetchError)) in scopes.Zip(fetchedMarkdowns))
		{
			ctx.ThrowIfCancellationRequested();
			var result = ProcessProduct(collector, args, scope, markdown, fetchError);
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

	private async Task<(string? Markdown, bool IsError)[]> FetchAllMarkdown(
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

	private async Task<(string? Markdown, bool IsError)> FetchWithRetry(IDiagnosticsCollector collector, HttpClient client, string url, Cancel ctx)
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
					return (null, false);
				}

				if (!response.IsSuccessStatusCode)
				{
					if (retryable.Contains((int)response.StatusCode) && attempt < maxAttempts - 1)
						continue;
					collector.EmitError(url, $"Fetching release notes failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).");
					return (null, true);
				}

				return (await response.Content.ReadAsStringAsync(ctx), false);
			}
			catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ctx.IsCancellationRequested && attempt < maxAttempts - 1)
			{
				_logger.LogInformation("Transient error fetching {Url}: {Message}", url, ex.Message);
			}
			catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ctx.IsCancellationRequested)
			{
				collector.EmitError(url, $"Fetching release notes failed after {maxAttempts} attempts: {ex.Message}", ex);
				return (null, true);
			}
		}

		collector.EmitError(url, $"Fetching release notes failed after {maxAttempts} attempts.");
		return (null, true);
	}

	private BackfillProductResult ProcessProduct(
		IDiagnosticsCollector collector,
		BackfillArguments args,
		BackfillScope scope,
		string? markdown,
		bool fetchError)
	{
		var url = scope.IsRepoSource
			? $"{args.RawBaseUrl}/{scope.Owner}/{scope.Repo}/{scope.Ref}/{scope.RepoPath}"
			: $"{args.BaseUrl}/release-notes/{scope.Path}.md";

		if (markdown is null)
		{
			// FetchWithRetry already emitted the appropriate error or hint.
			// Use fetchError (from this product's fetch only) — not collector.Errors which is global.
			var outcome = fetchError ? OutcomeFailed : OutcomeUnavailable;
			return new BackfillProductResult(scope.ProductId, url, outcome, 0, 0, 0, 0, 0, fetchError ? "fetch failed" : "404 unavailable");
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

		var totalEntries = inScope.Sum(r => r.Bundle.Entries?.Count ?? 0);

		if (args.DryRun)
			return new BackfillProductResult(scope.ProductId, url, OutcomeOk, inScope.Count, totalEntries, 0, 0, 0, "dry-run; nothing written");

		var (succeeded, filesWritten, noPr, dupPr) = WriteBundles(collector, args, scope, inScope);
		var writeOutcome = succeeded ? OutcomeOk : OutcomeFailed;
		return new BackfillProductResult(scope.ProductId, url, writeOutcome, inScope.Count, totalEntries, filesWritten, noPr, dupPr, succeeded ? "" : "write failed");
	}

	[GeneratedRegex(@"/pull/(?<number>\d+)$")]
	private static partial Regex PrNumberFromUrlRegex();

	private (bool Succeeded, int FilesWritten, int NoPrEntries, int DuplicatePrRefs) WriteBundles(
		IDiagnosticsCollector collector,
		BackfillArguments args,
		BackfillScope scope,
		IReadOnlyList<MigratedRelease> releases)
	{
		var changelogDir = Path.Join(args.Output, scope.ProductId, "changelog");
		var bundleDir = Path.Join(changelogDir, "bundles");
		var filesWritten = 0;
		var noPrEntries = 0;
		var dupPrRefs = 0;

		// Cross-version first-wins dedup for PR numbers and note slugs.
		var seenPrNumbers = new HashSet<string>(StringComparer.Ordinal);
		// Key: "slug|version" → count of uses within that version (for same-version collision counter).
		var slugVersionCounts = new Dictionary<string, int>(StringComparer.Ordinal);
		// All note file names ever claimed (across versions), for global uniqueness.
		var claimedNoteFiles = new HashSet<string>(StringComparer.Ordinal);

		var now = (timeProvider ?? TimeProvider.System).GetUtcNow();

		try
		{
			_ = fileSystem.Directory.CreateDirectory(bundleDir);
			_ = fileSystem.Directory.CreateDirectory(changelogDir);

			foreach (var release in releases)
			{
				var yaml = ReleaseNotesSerialization.SerializeBundle(release.Bundle);
				var filePath = Path.Join(bundleDir, $"{SanitizeFileName(release.Version)}.yaml");
				fileSystem.File.WriteAllText(filePath, yaml);
				filesWritten++;

				var noteFilesForVersion = new List<string>();
				foreach (var entry in release.Bundle.Entries ?? [])
				{
					var firstPr = entry.Prs is { Count: > 0 } prs ? prs[0] : null;
					if (firstPr is not null)
					{
						var prNumberMatch = PrNumberFromUrlRegex().Match(firstPr);
						if (prNumberMatch.Success)
						{
							var prNumber = prNumberMatch.Groups["number"].Value;
							if (!seenPrNumbers.Add(prNumber))
							{
								dupPrRefs++;
								_logger.LogDebug("Duplicate PR #{PrNumber} for {Product} {Version}; skipping", prNumber, scope.ProductId, release.Version);
								continue;
							}

							var entryYaml = SerializeEntry(entry, scope, release.Version);
							fileSystem.File.WriteAllText(Path.Join(changelogDir, $"{prNumber}.yaml"), entryYaml);
							filesWritten++;
						}
					}
					else
					{
						noPrEntries++;
						var noteFile = AllocateNoteFileName(entry.Title ?? string.Empty, release.Version, slugVersionCounts, claimedNoteFiles);
						if (noteFile is null)
						{
							_logger.LogDebug("Could not generate slug for PR-less entry in {Product} {Version}: {Title}", scope.ProductId, release.Version, entry.Title);
							continue;
						}

						noteFilesForVersion.Add(noteFile);
						var entryYaml = SerializeEntry(entry, scope, release.Version);
						fileSystem.File.WriteAllText(Path.Join(changelogDir, noteFile), entryYaml);
						filesWritten++;
					}
				}

				if (noteFilesForVersion.Count > 0)
				{
					var registry = new NotesRegistry
					{
						GeneratedAt = now,
						Target = release.Version,
						Notes = noteFilesForVersion.Order(StringComparer.Ordinal).ToList()
					};
					var registryJson = JsonSerializer.Serialize(registry, BackfillJsonContext.Default.NotesRegistry);
					fileSystem.File.WriteAllText(Path.Join(changelogDir, $"notes-{SanitizeFileName(release.Version)}.json"), registryJson);
					filesWritten++;
				}
			}
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
		{
			collector.EmitError(changelogDir, $"Could not write backfill output: {ex.Message}", ex);
			return (false, filesWritten, noPrEntries, dupPrRefs);
		}

		if (noPrEntries > 0)
		{
			var total = releases.Sum(r => r.Bundle.Entries?.Count ?? 0);
			collector.EmitHint(args.Output,
				$"{scope.ProductId}: {noPrEntries}/{total} entries had no PR reference; written as note-*.yaml.");
		}

		return (true, filesWritten, noPrEntries, dupPrRefs);
	}

	private static string? AllocateNoteFileName(
		string title,
		string version,
		Dictionary<string, int> slugVersionCounts,
		HashSet<string> claimedNoteFiles)
	{
		if (string.IsNullOrWhiteSpace(title))
			return null;

		var slug = ChangelogTextUtilities.GenerateSlug(title, maxWords: 8);
		if (string.IsNullOrEmpty(slug))
			return null;

		var versionKey = $"{slug}|{version}";
		var candidate = $"note-{slug}.yaml";

		if (claimedNoteFiles.Add(candidate))
		{
			slugVersionCounts[versionKey] = 1;
			return candidate;
		}

		if (!slugVersionCounts.TryGetValue(versionKey, out var value))
		{
			// Cross-version collision: append sanitized version suffix.
			candidate = $"note-{slug}-{SanitizeSlugSegment(version)}.yaml";
			_ = claimedNoteFiles.Add(candidate);
			value = 1;
			slugVersionCounts[versionKey] = value;
			return candidate;
		}

		// Same-version collision: increment counter suffix.
		var count = value + 1;
		slugVersionCounts[versionKey] = count;
		candidate = $"note-{slug}-{count}.yaml";
		_ = claimedNoteFiles.Add(candidate);
		return candidate;
	}

	private static string SerializeEntry(BundledEntry bundled, BackfillScope scope, string version)
	{
		var entry = new ChangelogEntry
		{
			Type = bundled.Type ?? ChangelogEntryType.Other,
			Title = bundled.Title ?? string.Empty,
			Areas = bundled.Areas,
			Prs = bundled.Prs,
			Issues = bundled.Issues,
			Products = [new ProductReference { ProductId = scope.ProductId, Target = version }]
		};
		return ReleaseNotesSerialization.SerializeEntry(entry);
	}

	private static string SanitizeFileName(string version) =>
		string.Join('_', version.Split(Path.GetInvalidFileNameChars()));

	private static string SanitizeSlugSegment(string value) =>
		string.Join('-', value.Split(Path.GetInvalidFileNameChars()))
			.Replace('.', '-')
			.Trim('-');

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
