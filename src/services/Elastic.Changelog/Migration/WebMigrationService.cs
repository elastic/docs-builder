// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Abstractions;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Elastic.Documentation.Versions;
using Microsoft.Extensions.Logging;
using Nullean.ScopedFileSystem;

namespace Elastic.Changelog.Migration;

public record MigrateFromWebArguments
{
	/// <summary>Optional product-id selection; empty covers every product in the checked-in scope table.</summary>
	public IReadOnlyList<string> Products { get; init; } = [];

	/// <summary>Destination S3 bucket. Optional in dry-run mode (no S3 access at all when omitted).</summary>
	public string S3BucketName { get; init; } = "";

	/// <summary>When true, does everything except the S3 writes and reports what would be created.</summary>
	public bool DryRun { get; init; }

	/// <summary>Optional exact-version filter; when set, only these versions are migrated.</summary>
	public IReadOnlyList<string> Versions { get; init; } = [];
}

/// <summary>Per-key outcome of a migration run, printed as the run report / paper trail.</summary>
public sealed record MigrationKeyResult(string Key, string Outcome, string? ETag, string Detail);

/// <summary>
/// TEMPORARY (elastic/docs-eng-team#736): one-off migration of published release notes into the
/// S3 bundle store. Fetches the release-notes Markdown that backs the published pages (pinned ref,
/// raw.githubusercontent.com), maps it to the existing bundle YAML shape, and uploads with
/// create-only semantics (<c>If-None-Match: *</c>) — existing keys are skipped, never overwritten.
/// Delete once the migration rollout (elastic/docs-eng-team#683) completes.
/// </summary>
public class WebMigrationService(
	ILoggerFactory logFactory,
	ScopedFileSystem fileSystem,
	IAmazonS3? s3Client = null,
	HttpMessageHandler? httpMessageHandler = null
) : IService
{
	private const string OutcomeCreated = "created";
	private const string OutcomeWouldCreate = "would-create";
	private const string OutcomeSkipped = "skipped";
	private const string OutcomeFailed = "failed";

	private readonly ILogger _logger = logFactory.CreateLogger<WebMigrationService>();
	private readonly IFileSystem _fileSystem = fileSystem;

	/// <summary>Per-key results of the most recent run; exposed for tests.</summary>
	internal IReadOnlyList<MigrationKeyResult> LastResults { get; private set; } = [];

	public async Task<bool> MigrateFromWeb(IDiagnosticsCollector collector, MigrateFromWebArguments args, Cancel ctx)
	{
		var scopes = MigrateFromWebScope.Select(collector, args.Products);
		if (scopes is null)
			return false;

		// One product's failure never blocks the others: the default run covers the whole table,
		// so a broken source page should still let every other product migrate — the run itself
		// fails at the end so nothing goes unnoticed.
		var allResults = new List<MigrationKeyResult>();
		var report = new StringBuilder();
		var failedProducts = 0;
		foreach (var scope in scopes)
		{
			ctx.ThrowIfCancellationRequested();
			var results = await MigrateProduct(collector, args, scope, ctx);
			if (results is null)
			{
				failedProducts++;
				continue;
			}

			allResults.AddRange(results);
			_ = report.Append(FormatReport(scope, args, results));
			_ = report.AppendLine();
		}

		LastResults = allResults;
		Console.Write(report.ToString());

		var failed = allResults.Count(r => r.Outcome == OutcomeFailed);
		if (failed > 0)
			collector.EmitError(string.Empty, $"{failed} key(s) failed to migrate; see the run report above.");
		if (failedProducts > 0)
			collector.EmitError(string.Empty, $"{failedProducts} product(s) failed before upload; see the errors above.");

		return failed == 0 && failedProducts == 0;
	}

	/// <summary>
	/// Runs one product's fetch → parse → filter → stage → upload chain. Returns null (with errors
	/// emitted) when the product fails before the upload phase — the caller continues with the
	/// remaining products and fails the run at the end.
	/// </summary>
	private async Task<List<MigrationKeyResult>?> MigrateProduct(
		IDiagnosticsCollector collector,
		MigrateFromWebArguments args,
		MigrateFromWebScope scope,
		Cancel ctx
	)
	{
		var sourceUrl = $"https://raw.githubusercontent.com/{scope.Owner}/{scope.Repo}/{scope.Ref}/{scope.Path}";
		var markdown = await FetchMarkdown(collector, sourceUrl, ctx);
		if (markdown is null)
			return null;

		var releases = ReleaseNotesPageParser.Parse(collector, markdown, sourceUrl, scope);
		if (releases.Count == 0)
		{
			collector.EmitError(
				sourceUrl,
				$"No release sections were parsed from the published release notes for '{scope.ProductId}'; refusing to continue with an empty scope."
			);
			return null;
		}

		_logger.LogInformation("Parsed {Count} release section(s) from {Url}", releases.Count, sourceUrl);

		var (inScope, results) = ApplyScopeFilters(releases, scope, args.Versions);
		var errorsBeforeStaging = collector.Errors;
		var staged = StageBundles(collector, scope, inScope);
		if (collector.Errors > errorsBeforeStaging)
			return null;

		var uploadResults = await UploadCreateOnly(args, staged, ctx);
		results.AddRange(uploadResults);
		return results;
	}

	private async Task<string?> FetchMarkdown(IDiagnosticsCollector collector, string sourceUrl, Cancel ctx)
	{
		try
		{
			using var client = httpMessageHandler is null
				? new HttpClient { Timeout = TimeSpan.FromSeconds(30) }
				: new HttpClient(httpMessageHandler, disposeHandler: false) { Timeout = TimeSpan.FromSeconds(30) };
			client.DefaultRequestHeaders.Add("User-Agent", "docs-builder");

			using var response = await client.GetAsync(sourceUrl, ctx);
			if (!response.IsSuccessStatusCode)
			{
				collector.EmitError(
					sourceUrl,
					$"Fetching published release notes failed with HTTP {(int)response.StatusCode} ({response.StatusCode})."
				);
				return null;
			}

			return await response.Content.ReadAsStringAsync(ctx);
		}
		catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
		{
			collector.EmitError(sourceUrl, $"Fetching published release notes failed: {ex.Message}", ex);
			return null;
		}
	}

	/// <summary>
	/// Partitions parsed releases into the in-scope set and out-of-scope report lines. Versions above
	/// the configured cutoff belong to the live pipeline; an explicit <c>--versions</c> selection
	/// narrows the scope further.
	/// </summary>
	private static (List<MigratedRelease> InScope, List<MigrationKeyResult> Results) ApplyScopeFilters(
		IReadOnlyList<MigratedRelease> releases,
		MigrateFromWebScope scope,
		IReadOnlyList<string> versions
	)
	{
		var cutoff = VersionOrDate.Parse(scope.Cutoff);
		var selection = versions.Count > 0 ? new HashSet<string>(versions, StringComparer.OrdinalIgnoreCase) : null;

		var inScope = new List<MigratedRelease>();
		var results = new List<MigrationKeyResult>();
		foreach (var release in releases)
		{
			var key = ChangelogKeys.BundleFileKey(scope.ProductId, $"{release.Version}.yaml");
			if (VersionOrDate.Parse(release.Version).CompareTo(cutoff) > 0)
			{
				results.Add(new MigrationKeyResult(key, OutcomeSkipped, null, $"beyond cutoff {scope.Cutoff}; owned by the live pipeline"));
				continue;
			}

			if (selection is not null && !selection.Contains(release.Version))
			{
				results.Add(new MigrationKeyResult(key, OutcomeSkipped, null, "not in --versions selection"));
				continue;
			}

			inScope.Add(release);
		}

		return (inScope, results);
	}

	private sealed record StagedBundle(string Key, string LocalPath, string LocalETag);

	/// <summary>
	/// Serializes each in-scope release to bundle YAML and stages it in a temp directory, computing
	/// the local single-part ETag used to distinguish identical from divergent remote content.
	/// </summary>
	[SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms", Justification = "MD5 matches the S3 single-part ETag, used for content comparison only")]
	private List<StagedBundle> StageBundles(
		IDiagnosticsCollector collector,
		MigrateFromWebScope scope,
		IReadOnlyList<MigratedRelease> releases
	)
	{
		var stagingDir = _fileSystem.Path.Join(_fileSystem.Path.GetTempPath(), "docs-builder-migrate-from-web", scope.ProductId);
		var staged = new List<StagedBundle>(releases.Count);

		try
		{
			_ = _fileSystem.Directory.CreateDirectory(stagingDir);
			foreach (var release in releases)
			{
				var yaml = ReleaseNotesSerialization.SerializeBundle(release.Bundle);
				var bytes = Encoding.UTF8.GetBytes(yaml);
				var localPath = _fileSystem.Path.Join(stagingDir, $"{release.Version}.yaml");
				_fileSystem.File.WriteAllBytes(localPath, bytes);

				var key = ChangelogKeys.BundleFileKey(scope.ProductId, $"{release.Version}.yaml");
				staged.Add(new StagedBundle(key, localPath, Convert.ToHexStringLower(MD5.HashData(bytes))));
			}
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			collector.EmitError(stagingDir, $"Could not stage mapped bundles: {ex.Message}", ex);
		}

		return staged;
	}

	private async Task<List<MigrationKeyResult>> UploadCreateOnly(
		MigrateFromWebArguments args,
		IReadOnlyList<StagedBundle> staged,
		Cancel ctx
	)
	{
		// A credential-free dry run: without a bucket there is nothing to compare against, so every
		// in-scope key is reported as would-create.
		if (args.DryRun && string.IsNullOrWhiteSpace(args.S3BucketName))
			return [
				.. staged.Select(
					s => new MigrationKeyResult(s.Key, OutcomeWouldCreate, s.LocalETag, "no bucket specified; existence not checked")
				)
			];

		using var defaultClient = s3Client is null ? new AmazonS3Client() : null;
		var client = s3Client ?? defaultClient!;

		// No registry write, by design: the scrubber Lambda owns the public bundle/{product}/
		// registry.json manifests and the shallow per-tree maps, reconciling them from the S3
		// events these creates emit (elastic/docs-builder#3738); the client-side refresh is
		// retired (elastic/docs-builder#3760).
		var results = new List<MigrationKeyResult>(staged.Count);
		foreach (var bundle in staged)
		{
			ctx.ThrowIfCancellationRequested();
			var result = await MigrateKey(client, args, bundle, ctx);
			results.Add(result);
		}

		return results;
	}

	private async Task<MigrationKeyResult> MigrateKey(IAmazonS3 client, MigrateFromWebArguments args, StagedBundle bundle, Cancel ctx)
	{
		try
		{
			var remoteEtag = await GetRemoteEtag(client, args.S3BucketName, bundle.Key, ctx);
			if (remoteEtag is not null)
			{
				var detail = remoteEtag == bundle.LocalETag
					? "already exists with identical content"
					: "already exists with different content; never overwritten";
				return new MigrationKeyResult(bundle.Key, OutcomeSkipped, remoteEtag, detail);
			}

			if (args.DryRun)
				return new MigrationKeyResult(bundle.Key, OutcomeWouldCreate, bundle.LocalETag, "dry run; no write performed");

			// The conditional PUT is the actual race guard: a key created between the inspection above
			// and this write surfaces as a 412 and is skipped, never overwritten.
			var response = await PutCreateOnly(client, args.S3BucketName, bundle, ctx);
			return new MigrationKeyResult(bundle.Key, OutcomeCreated, response.ETag?.Trim('"'), "");
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
		{
			return new MigrationKeyResult(bundle.Key, OutcomeSkipped, null, "created concurrently by another writer; never overwritten");
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogError(ex, "Failed to migrate {Key}", bundle.Key);
			return new MigrationKeyResult(bundle.Key, OutcomeFailed, null, ex.Message);
		}
	}

	private static async Task<string?> GetRemoteEtag(IAmazonS3 client, string bucketName, string key, Cancel ctx)
	{
		try
		{
			var response = await client.GetObjectMetadataAsync(new GetObjectMetadataRequest { BucketName = bucketName, Key = key }, ctx);
			return response.ETag.Trim('"');
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			return null;
		}
	}

	private async Task<PutObjectResponse> PutCreateOnly(IAmazonS3 client, string bucketName, StagedBundle bundle, Cancel ctx)
	{
		_logger.LogInformation("Creating s3://{Bucket}/{Key} (If-None-Match: *)", bucketName, bundle.Key);
		await using var stream = _fileSystem.FileStream.New(bundle.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
		var request = new PutObjectRequest
		{
			BucketName = bucketName,
			Key = bundle.Key,
			InputStream = stream,
			ChecksumAlgorithm = ChecksumAlgorithm.SHA256,
			IfNoneMatch = "*"
		};
		return await client.PutObjectAsync(request, ctx);
	}

	/// <summary>Formats the run report — one line per key — in a form that can be pasted into the tracking issue.</summary>
	public static string FormatReport(MigrateFromWebScope scope, MigrateFromWebArguments args, IReadOnlyList<MigrationKeyResult> results)
	{
		var sb = new StringBuilder();
		_ = sb.AppendLine("### Run report: changelog migrate-from-web");
		_ = sb.AppendLine();
		_ = sb.AppendLine(CultureInfo.InvariantCulture, $"- product: `{scope.ProductId}`");
		_ = sb.AppendLine(CultureInfo.InvariantCulture, $"- source: `{scope.Owner}/{scope.Repo}@{scope.Ref}` `{scope.Path}`");
		_ = sb.AppendLine(CultureInfo.InvariantCulture, $"- cutoff: `{scope.Cutoff}`");
		_ =
			sb.AppendLine(
				CultureInfo.InvariantCulture,
				$"- mode: {(args.DryRun ? "dry-run (no S3 writes)" : $"upload to `{args.S3BucketName}`")}"
			);
		_ = sb.AppendLine();
		_ = sb.AppendLine("| key | outcome | etag | detail |");
		_ = sb.AppendLine("|---|---|---|---|");

		foreach (var result in results.OrderBy(r => r.Key, StringComparer.Ordinal))
			_ =
				sb.AppendLine(
					CultureInfo.InvariantCulture,
					$"| `{result.Key}` | {result.Outcome} | {(result.ETag is null ? "" : $"`{result.ETag}`")} | {result.Detail} |"
				);

		var counts = results.GroupBy(r => r.Outcome).OrderBy(g => g.Key, StringComparer.Ordinal).Select(g => $"{g.Key} {g.Count()}");
		_ = sb.AppendLine();
		_ = sb.AppendLine(CultureInfo.InvariantCulture, $"totals: {string.Join(", ", counts)}");
		return sb.ToString();
	}
}
