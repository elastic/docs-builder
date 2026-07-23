// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Amazon.S3;
using Elastic.Changelog.Bundling;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Integrations.S3;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.ScopedFileSystem;

namespace Elastic.Changelog.Uploading;

public enum ArtifactType { Changelog, Bundle }

public enum UploadTargetKind { S3, Elasticsearch }

public record ChangelogUploadArguments
{
	public required ArtifactType ArtifactType { get; init; }
	public required UploadTargetKind Target { get; init; }
	public required string S3BucketName { get; init; }
	public string? Config { get; init; }
	public string? Directory { get; init; }

	/// <summary>
	/// Authoring repository identifier used to scope changelog-entry keys
	/// (<c>changelog/{org}/{repo}/{branch}/{file}</c>). Required for <see cref="ArtifactType.Changelog"/>
	/// uploads; unused for bundle uploads (which are product-scoped from the bundle YAML). Resolved by the
	/// CLI via the precedence <c>--repo</c> &gt; <c>bundle.repo</c> &gt; git remote origin.
	/// </summary>
	public string? Repo { get; init; }

	/// <summary>
	/// GitHub owner (org), the first segment of changelog-entry keys
	/// (<c>changelog/{org}/{repo}/{branch}/{file}</c>). Required for <see cref="ArtifactType.Changelog"/>
	/// uploads; unused for bundle uploads. Resolved by the CLI via the precedence
	/// <c>--owner</c> &gt; <c>bundle.owner</c> &gt; git remote origin.
	/// </summary>
	public string? Owner { get; init; }

	/// <summary>
	/// Branch segment of changelog-entry keys (<c>changelog/{org}/{repo}/{branch}/{file}</c>), stored
	/// verbatim so any <c>/</c> in the branch become real key separators (e.g. <c>feature/foo</c>).
	/// Required for <see cref="ArtifactType.Changelog"/> uploads; unused for bundle uploads. Resolved by the
	/// CLI via the precedence <c>--branch</c> &gt; the current git branch.
	/// </summary>
	public string? Branch { get; init; }

	/// <summary>
	/// When true, upload every discovered file even when its content hash matches the remote object.
	/// Useful to re-trigger downstream scrubbers without changing file content.
	/// Incompatible with <see cref="Backfill"/>, whose create-only semantics rely on the content comparison.
	/// </summary>
	public bool SkipEtagCheck { get; init; }

	/// <summary>
	/// Backfill publishing mode for historical bundles. Only the files in <see cref="Files"/> are
	/// uploaded (no directory discovery), objects are written create-only (an existing key with
	/// different content is a conflict, never an overwrite), and a registry that cannot be reconciled
	/// fails the operation. Only valid for <see cref="ArtifactType.Bundle"/> uploads.
	/// </summary>
	public bool Backfill { get; init; }

	/// <summary>
	/// Exact bundle YAML files to upload in <see cref="Backfill"/> mode. Values are file paths or a
	/// newline-delimited path-list file; relative paths resolve against the bundle directory. Required
	/// when <see cref="Backfill"/> is set; not valid otherwise.
	/// </summary>
	public IReadOnlyList<string> Files { get; init; } = [];
}

public class ChangelogUploadService(
	ILoggerFactory logFactory,
	IConfigurationContext? configurationContext = null,
	ScopedFileSystem? fileSystem = null,
	IAmazonS3? s3Client = null
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogUploadService>();
	private readonly IFileSystem _fileSystem = fileSystem ?? FileSystemFactory.RealRead;
	private readonly ChangelogConfigurationLoader? _configLoader = configurationContext != null
		? new ChangelogConfigurationLoader(logFactory, configurationContext, fileSystem ?? FileSystemFactory.RealRead)
		: null;

	public async Task<bool> Upload(IDiagnosticsCollector collector, ChangelogUploadArguments args, Cancel ctx)
	{
		if (args.Target == UploadTargetKind.Elasticsearch)
		{
			_logger.LogWarning("Elasticsearch upload target is not yet implemented; skipping");
			return true;
		}

		if (!ValidateBackfillArguments(collector, args))
			return false;

		var directory = args.ArtifactType == ArtifactType.Bundle
			? await ResolveBundleDirectory(collector, args, ctx)
			: await ResolveChangelogDirectory(collector, args, ctx);

		if (directory == null)
			return false;

		var targets = await ResolveUploadTargets(collector, args, directory, ctx);
		if (targets == null)
			return true;

		// Aborts (rather than no-ops) when a key cannot be derived: entry uploads with an unresolved
		// repo would produce unscoped keys, and a backfill selection that cannot be mapped in full must
		// fail before any write. A silent skip would look like "nothing to upload".
		if (collector.Errors > 0)
			return false;

		if (targets.Count == 0)
		{
			_logger.LogInformation("No {ArtifactType} files found to upload in {Directory}", args.ArtifactType, directory);
			return true;
		}

		_logger.LogInformation("Found {Count} {ArtifactType} upload target(s) from {Directory}", targets.Count, args.ArtifactType, directory);

		using var defaultClient = s3Client == null ? new AmazonS3Client() : null;
		var client = s3Client ?? defaultClient!;
		var etagCalculator = new S3EtagCalculator(logFactory, _fileSystem);
		var uploader = new S3IncrementalUploader(logFactory, client, _fileSystem, etagCalculator, args.S3BucketName);
		var writePolicy = args.Backfill ? S3WritePolicy.CreateOnly : S3WritePolicy.Overwrite;
		var result = await uploader.Upload(targets, args.SkipEtagCheck, writePolicy, ctx);

		_logger.LogInformation("Upload complete: {Uploaded} uploaded, {Skipped} skipped, {Conflicted} conflicted, {Failed} failed",
			result.Uploaded, result.Skipped, result.Conflicts.Count, result.Failed);

		if (result.Failed > 0)
			collector.EmitError(string.Empty, $"{result.Failed} file(s) failed to upload");

		foreach (var conflict in result.Conflicts)
		{
			collector.EmitError(conflict.LocalPath,
				$"Refusing to overwrite s3://{args.S3BucketName}/{conflict.S3Key}: the key already exists with different content. " +
				"Backfill uploads are create-only; correcting a published bundle requires the explicit corrections workflow.");
		}

		// On a successful upload, refresh the per-product registry.json so consumers can enumerate
		// content without an S3 listing: the bundle index (consumed by the changelog directive in
		// cdn: mode) for bundle uploads, and the changelog-entry index (consumed by `changelog
		// bundle` when sourcing entries from the CDN) for changelog uploads.
		var registryReconciled = true;
		if (result.Failed == 0 && targets.Count > 0)
		{
			var scope = args.ArtifactType == ArtifactType.Bundle ? RegistryScope.Bundle : RegistryScope.Changelog;

			// Conflicted targets are excluded: their remote content differs from the local file, so
			// registering the local ETag would misrepresent what is actually published.
			var registryTargets = result.Conflicts.Count == 0
				? targets
				: targets.Where(t => !result.Conflicts.Contains(t)).ToList();

			if (registryTargets.Count > 0)
				registryReconciled = await RefreshRegistries(collector, client, etagCalculator, args, registryTargets, scope, ctx);
		}

		// In backfill mode an unreconciled registry is an incomplete operation.
		if (args.Backfill && !registryReconciled)
			return false;

		return result.Failed == 0 && result.Conflicts.Count == 0;
	}

	private bool ValidateBackfillArguments(IDiagnosticsCollector collector, ChangelogUploadArguments args)
	{
		if (!args.Backfill)
		{
			if (args.Files.Count > 0)
			{
				collector.EmitError(string.Empty, "--files requires --backfill: explicit file selection is only supported in backfill mode.");
				return false;
			}
			return true;
		}

		if (args.ArtifactType != ArtifactType.Bundle)
		{
			collector.EmitError(string.Empty, "--backfill only supports --artifact-type bundle; the backfill publishes bundles only.");
			return false;
		}

		if (args.Files.Count == 0)
		{
			collector.EmitError(string.Empty, "--backfill requires an explicit file selection via --files; directory discovery is not allowed in backfill mode.");
			return false;
		}

		if (args.SkipEtagCheck)
		{
			collector.EmitError(string.Empty, "--skip-etag-check cannot be combined with --backfill: create-only uploads rely on the content comparison to distinguish an idempotent re-run from a conflict.");
			return false;
		}

		return true;
	}

	/// <summary>
	/// Builds the upload target list: explicit selection in backfill mode, directory discovery otherwise.
	/// Returns null when the source directory does not exist (a discovery no-op, reported as success).
	/// </summary>
	private async Task<IReadOnlyList<UploadTarget>?> ResolveUploadTargets(
		IDiagnosticsCollector collector,
		ChangelogUploadArguments args,
		string directory,
		Cancel ctx)
	{
		if (args.Backfill)
			return await SelectBundleUploadTargets(collector, args.Files, directory, ctx);

		if (!_fileSystem.Directory.Exists(directory))
		{
			_logger.LogInformation("{ArtifactType} directory {Directory} does not exist; nothing to upload", args.ArtifactType, directory);
			return null;
		}

		return args.ArtifactType == ArtifactType.Bundle
			? DiscoverBundleUploadTargets(collector, directory)
			: DiscoverUploadTargets(collector, directory, args.Owner, args.Repo, args.Branch);
	}

	private async Task<bool> RefreshRegistries(
		IDiagnosticsCollector collector,
		IAmazonS3 client,
		IS3EtagCalculator etagCalculator,
		ChangelogUploadArguments args,
		IReadOnlyList<UploadTarget> uploadTargets,
		RegistryScope scope,
		Cancel ctx)
	{
		try
		{
			var builder = new RegistryBuilder(logFactory, _fileSystem, client, etagCalculator, args.S3BucketName);
			var result = await builder.RefreshAsync(collector, uploadTargets, ctx, scope);
			_logger.LogInformation("Registry refresh ({Scope}): {Updated} updated, {Unchanged} unchanged, {Failed} failed",
				scope, result.Updated, result.Unchanged, result.Failed);

			if (result.Failed > 0 && args.Backfill)
			{
				collector.EmitError(string.Empty,
					$"Registry refresh failed for {result.Failed} manifest(s); the uploaded objects are not enumerable by consumers and the backfill operation is incomplete. Re-run once the concurrent writes settle.");
			}

			return result.Failed == 0;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			if (args.Backfill)
			{
				// An unreconciled registry leaves the uploaded objects invisible to consumers; backfill
				// must treat that as an incomplete operation, not a stale-manifest inconvenience.
				collector.EmitError(string.Empty,
					$"Failed to refresh registry manifest(s): {ex.Message}. The backfill operation is incomplete; re-run to reconcile the registry.", ex);
				return false;
			}

			// Leaving the manifest stale is non-fatal in live mode — bundle objects are unaffected.
			_logger.LogWarning(ex, "Registry refresh failed; bundles uploaded successfully but manifests may be stale");
			collector.EmitWarning(string.Empty, $"Failed to refresh registry manifest(s): {ex.Message}");
			return false;
		}
	}

	internal IReadOnlyList<UploadTarget> DiscoverUploadTargets(IDiagnosticsCollector collector, string changelogDir, string? org, string? repo, string? branch)
	{
		// Option AD: entries live once, under the authoring org/repo/branch pool — independent of which
		// products later consume them. Org, repo, and branch must all resolve (CLI flags > bundle config >
		// git); a missing/invalid value is fatal because every entry key derives from them.
		if (!ChangelogKeys.IsValidOrg(org))
		{
			collector.EmitError(string.Empty,
				$"A valid GitHub owner is required to upload changelog entries (resolved: \"{org ?? "<none>"}\"). " +
				"Set --owner, bundle.owner in changelog.yml, or run inside a checkout with a github.com origin remote.");
			return [];
		}

		if (!ChangelogKeys.IsValidRepo(repo))
		{
			collector.EmitError(string.Empty,
				$"A valid repository identifier is required to upload changelog entries (resolved: \"{repo ?? "<none>"}\"). " +
				"Set --repo, bundle.repo in changelog.yml, or run inside a checkout with a github.com origin remote.");
			return [];
		}

		if (!ChangelogKeys.IsValidBranch(branch))
		{
			collector.EmitError(string.Empty,
				$"A valid branch is required to upload changelog entries (resolved: \"{branch ?? "<none>"}\"). " +
				"Set --branch or run inside a checkout with a current branch.");
			return [];
		}

		var rootDir = _fileSystem.DirectoryInfo.New(changelogDir);

		var yamlFiles = _fileSystem.Directory.GetFiles(changelogDir, "*.yaml", SearchOption.TopDirectoryOnly)
			.Concat(_fileSystem.Directory.GetFiles(changelogDir, "*.yml", SearchOption.TopDirectoryOnly))
			.ToList();

		var targets = new List<UploadTarget>();

		foreach (var filePath in yamlFiles)
		{
			var fileInfo = _fileSystem.FileInfo.New(filePath);
			if (SymlinkValidator.ValidateFileAccess(fileInfo, rootDir) is { } accessError)
			{
				collector.EmitWarning(filePath, $"Skipping: {accessError}");
				continue;
			}

			var fileName = _fileSystem.Path.GetFileName(filePath);
			var s3Key = ChangelogKeys.ChangelogFileKey(org, repo, branch, fileName);
			targets.Add(new UploadTarget(filePath, s3Key));
		}

		return targets;
	}

	internal IReadOnlyList<UploadTarget> DiscoverBundleUploadTargets(IDiagnosticsCollector collector, string bundleDir)
	{
		var rootDir = _fileSystem.DirectoryInfo.New(bundleDir);

		var yamlFiles = _fileSystem.Directory.GetFiles(bundleDir, "*.yaml", SearchOption.TopDirectoryOnly)
			.Concat(_fileSystem.Directory.GetFiles(bundleDir, "*.yml", SearchOption.TopDirectoryOnly))
			.ToList();

		var targets = new List<UploadTarget>();

		foreach (var filePath in yamlFiles)
		{
			var fileInfo = _fileSystem.FileInfo.New(filePath);
			if (SymlinkValidator.ValidateFileAccess(fileInfo, rootDir) is { } accessError)
			{
				collector.EmitWarning(filePath, $"Skipping: {accessError}");
				continue;
			}

			targets.AddRange(CreateBundleTargetsForFile(collector, filePath, explicitSelection: false));
		}

		return targets;
	}

	/// <summary>
	/// Resolves an explicit backfill selection (file paths or a newline-delimited path-list file) into
	/// bundle upload targets. Unlike <see cref="DiscoverBundleUploadTargets"/>, every problem is an
	/// error: an operator-selected file that cannot be mapped to a destination must abort the run
	/// before any write instead of being skipped silently.
	/// </summary>
	internal async Task<IReadOnlyList<UploadTarget>> SelectBundleUploadTargets(
		IDiagnosticsCollector collector,
		IReadOnlyList<string> files,
		string? baseDirectory,
		Cancel ctx)
	{
		var loader = new FileFilterLoader(_fileSystem);
		var filterResult = await loader.LoadFilesAsync(collector, [.. files], baseDirectory, ctx);
		if (!filterResult.IsValid)
			return [];

		var targets = new List<UploadTarget>();

		foreach (var filePath in filterResult.FilePaths.Distinct(StringComparer.Ordinal))
		{
			var fileInfo = _fileSystem.FileInfo.New(filePath);
			if (fileInfo.Directory is { } parentDir && SymlinkValidator.ValidateFileAccess(fileInfo, parentDir) is { } accessError)
			{
				collector.EmitError(filePath, $"Cannot upload: {accessError}");
				continue;
			}

			targets.AddRange(CreateBundleTargetsForFile(collector, filePath, explicitSelection: true));
		}

		return targets;
	}

	/// <summary>
	/// Maps one bundle YAML to its per-product upload targets. With <paramref name="explicitSelection"/>
	/// a file that yields no destination is an error (the operator named it); during directory
	/// discovery it is skipped with the historical warning/debug diagnostics.
	/// </summary>
	private List<UploadTarget> CreateBundleTargetsForFile(IDiagnosticsCollector collector, string filePath, bool explicitSelection)
	{
		var products = ReadProductsFromBundle(filePath);

		// Amends published before products were copied from the parent omit them; derive the
		// destination from the parent bundle next to the amend so they are not silently skipped.
		if (products.Count == 0 && BundleAmendMerger.IsAmendFile(filePath))
		{
			products = ReadProductsFromParentBundle(filePath);
			if (products.Count == 0)
			{
				EmitSelectionDiagnostic(collector, filePath, explicitSelection,
					"Amend bundle declares no products and its parent bundle is missing or has none; " +
					"skipping upload. Re-create the amend with a current docs-builder so it carries the parent's products.");
				return [];
			}
		}

		if (products.Count == 0)
		{
			if (explicitSelection)
				collector.EmitError(filePath, "Bundle declares no products; an upload destination cannot be derived.");
			else
				_logger.LogDebug("No products found in bundle {File}, skipping", filePath);
			return [];
		}

		var fileName = _fileSystem.Path.GetFileName(filePath);
		var targets = new List<UploadTarget>();

		foreach (var product in products)
		{
			if (!ChangelogKeys.IsValidProduct(product))
			{
				EmitSelectionDiagnostic(collector, filePath, explicitSelection,
					$"Skipping invalid product name \"{product}\" (must match [a-zA-Z0-9_-]+)");
				continue;
			}

			var s3Key = ChangelogKeys.BundleFileKey(product, fileName);
			targets.Add(new UploadTarget(filePath, s3Key));
		}

		return targets;
	}

	private static void EmitSelectionDiagnostic(IDiagnosticsCollector collector, string filePath, bool explicitSelection, string message)
	{
		if (explicitSelection)
			collector.EmitError(filePath, message);
		else
			collector.EmitWarning(filePath, message);
	}

	private List<string> ReadProductsFromParentBundle(string amendFilePath)
	{
		var parentPath = BundleAmendMerger.GetParentBundlePath(amendFilePath);
		return parentPath != null && _fileSystem.File.Exists(parentPath)
			? ReadProductsFromBundle(parentPath)
			: [];
	}

	private List<string> ReadProductsFromBundle(string filePath)
	{
		try
		{
			var content = _fileSystem.File.ReadAllText(filePath);
			var bundle = ReleaseNotesSerialization.DeserializeBundle(content);

			return bundle.Products
				.Select(p => p.ProductId)
				.Where(p => !string.IsNullOrWhiteSpace(p))
				.Distinct()
				.ToList();
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Could not read products from bundle {File}", filePath);
			return [];
		}
	}

	private async Task<string?> ResolveChangelogDirectory(IDiagnosticsCollector collector, ChangelogUploadArguments args, Cancel ctx)
	{
		if (!string.IsNullOrWhiteSpace(args.Directory))
			return args.Directory;

		if (_configLoader == null)
			return "docs/changelog";

		var config = await _configLoader.LoadChangelogConfiguration(collector, args.Config, ctx);
		return config?.Bundle?.Directory ?? "docs/changelog";
	}

	private async Task<string?> ResolveBundleDirectory(IDiagnosticsCollector collector, ChangelogUploadArguments args, Cancel ctx)
	{
		if (!string.IsNullOrWhiteSpace(args.Directory))
			return args.Directory;

		if (_configLoader == null)
			return "docs/releases";

		var config = await _configLoader.LoadChangelogConfiguration(collector, args.Config, ctx);
		return config?.Bundle?.OutputDirectory ?? config?.Bundle?.Directory ?? "docs/releases";
	}
}
