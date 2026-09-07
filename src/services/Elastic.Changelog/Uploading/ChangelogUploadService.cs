// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Amazon.S3;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Integrations.S3;
using Elastic.Documentation.ReleaseNotes;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Uploading;

public enum ArtifactType
{
	Changelog,
	Bundle,

	/// <summary>
	/// Amend sidecars only: files matching <c>*.amend-{N}.yaml|yml</c> in the bundle output directory.
	/// The Lambda-reserved <c>.amend-notes</c> suffix is excluded. Keyed identically to
	/// <see cref="Bundle"/> (product list comes from the sidecar, falling back to the parent bundle).
	/// Use this on <c>push</c> to sync manual post-release overrides from <c>main</c> without
	/// accidentally overwriting a freshly-published parent bundle.
	/// </summary>
	Amend
}

public enum UploadTargetKind
{
	S3,
	Elasticsearch
}

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
	/// </summary>
	public bool SkipEtagCheck { get; init; }
}

public class ChangelogUploadService(
	ILoggerFactory logFactory,
	IChangelogFileSystem fileSystem,
	IConfigurationContext? configurationContext = null,
	IAmazonS3? s3Client = null
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogUploadService>();
	private readonly IChangelogFileSystem _fileSystem = fileSystem;
	private readonly ChangelogConfigurationLoader? _configLoader = configurationContext != null
		? new ChangelogConfigurationLoader(logFactory, configurationContext, fileSystem)
		: null;

	public async Task<bool> Upload(IDiagnosticsCollector collector, ChangelogUploadArguments args, Cancel ctx)
	{
		if (args.Target == UploadTargetKind.Elasticsearch)
		{
			_logger.LogWarning("Elasticsearch upload target is not yet implemented; skipping");
			return true;
		}

		var directories = args.ArtifactType is ArtifactType.Bundle or ArtifactType.Amend
			? await ResolveBundleScanDirectories(collector, args, ctx)
			: [await ResolveChangelogDirectory(collector, args, ctx) ?? "docs/changelog"];

		var existing = directories.Where(d => !string.IsNullOrWhiteSpace(d) && _fileSystem.Directory.Exists(d)).ToList();
		if (existing.Count == 0)
		{
			_logger.LogInformation(
				"{ArtifactType} directory {Directory} does not exist; nothing to upload",
				args.ArtifactType,
				string.Join(", ", directories.Where(d => !string.IsNullOrWhiteSpace(d)))
			);
			return true;
		}

		var targets = DiscoverTargets(collector, args, existing);

		// Entry uploads abort (rather than no-op) when the repo cannot be resolved: the keys would be
		// unscoped and a silent skip would look like "nothing to upload".
		if (collector.Errors > 0)
			return false;

		if (targets.Count == 0)
		{
			_logger.LogInformation(
				"No {ArtifactType} files found to upload in {Directory}",
				args.ArtifactType,
				string.Join(", ", existing)
			);
			return true;
		}

		_logger.LogInformation(
			"Found {Count} {ArtifactType} upload target(s) from {Directory}",
			targets.Count,
			args.ArtifactType,
			string.Join(", ", existing)
		);

		using var defaultClient = s3Client == null ? new AmazonS3Client() : null;
		var client = s3Client ?? defaultClient!;
		var etagCalculator = new S3EtagCalculator(logFactory, _fileSystem);
		var uploader = new S3IncrementalUploader(logFactory, client, _fileSystem, etagCalculator, args.S3BucketName);
		var result = await uploader.Upload(targets, args.SkipEtagCheck, ctx);

		_logger.LogInformation(
			"Upload complete: {Uploaded} uploaded, {Skipped} skipped, {Failed} failed",
			result.Uploaded,
			result.Skipped,
			result.Failed
		);

		if (result.Failed > 0)
			collector.EmitError(string.Empty, $"{result.Failed} file(s) failed to upload");

		// No registry refresh here: the scrubber Lambda is the sole producer of the public
		// registry.json, reconciled from actual public bucket state on every S3 event this upload
		// just emitted (elastic/docs-eng-team#688). A private-bucket registry no longer exists.
		return result.Failed == 0;
	}

	internal IReadOnlyList<UploadTarget> DiscoverUploadTargets(
		IDiagnosticsCollector collector,
		string changelogDir,
		string? org,
		string? repo,
		string? branch
	)
	{
		// Option AD: entries live once, under the authoring org/repo/branch pool — independent of which
		// products later consume them. Org, repo, and branch must all resolve (CLI flags > bundle config >
		// git); a missing/invalid value is fatal because every entry key derives from them.
		if (!ChangelogKeys.IsValidOrg(org))
		{
			collector.EmitError(
				string.Empty,
				$"A valid GitHub owner is required to upload changelog entries (resolved: \"{org ?? "<none>"}\"). " +
					"Set --owner, bundle.owner in changelog.yml, or run inside a checkout with a github.com origin remote."
			);
			return [];
		}

		if (!ChangelogKeys.IsValidRepo(repo))
		{
			collector.EmitError(
				string.Empty,
				$"A valid repository identifier is required to upload changelog entries (resolved: \"{repo ?? "<none>"}\"). " +
					"Set --repo, bundle.repo in changelog.yml, or run inside a checkout with a github.com origin remote."
			);
			return [];
		}

		if (!ChangelogKeys.IsValidBranch(branch))
		{
			collector.EmitError(
				string.Empty,
				$"A valid branch is required to upload changelog entries (resolved: \"{branch ?? "<none>"}\"). " +
					"Set --branch or run inside a checkout with a current branch."
			);
			return [];
		}

		var rootDir = _fileSystem.DirectoryInfo.New(changelogDir);

		var yamlFiles = _fileSystem
			.Directory
			.GetFiles(changelogDir, "*.yaml", SearchOption.TopDirectoryOnly)
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

			if (fileName.StartsWith("note-", StringComparison.OrdinalIgnoreCase))
			{
				targets.Add(new UploadTarget(filePath, ChangelogKeys.ChangelogFileKey(org, repo, branch, fileName)));
				continue;
			}

			ChangelogEntry? entry = null;
			try
			{
				var content = _fileSystem.File.ReadAllText(filePath);
				entry = ReleaseNotesSerialization.DeserializeEntry(content);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Could not read entry from {File}; using filename for key", filePath);
			}

			var (canonicalFileName, markerEntries) = DeriveCanonicalFileNameAndMarkers(fileName, entry, _logger);
			var primaryKey = ChangelogKeys.ChangelogFileKey(org, repo, branch, canonicalFileName);
			targets.Add(new UploadTarget(filePath, primaryKey));

			foreach (var (markerFileName, markerContent) in markerEntries)
			{
				var markerKey = ChangelogKeys.ChangelogFileKey(org, repo, branch, markerFileName);
				targets.Add(new UploadTarget(string.Empty, markerKey, markerContent));
			}
		}

		return targets;
	}

	internal static (string CanonicalFileName, IReadOnlyList<(string FileName, string Content)> Markers) DeriveCanonicalFileNameAndMarkers(
		string fileName,
		ChangelogEntry? entry,
		ILogger? logger = null
	)
	{
		if (entry is null)
			return (fileName, []);

		var prNumbers = entry.Prs?.Select(pr => ChangelogTextUtilities.ExtractPrNumber(pr))
			.Where(n => n.HasValue)
			.Select(n => n!.Value)
			.Distinct()
			.OrderBy(n => n)
			.ToList();

		if (prNumbers is null or { Count: 0 })
		{
			logger?.LogWarning("Entry {File} has no PR references; using filename as-is for key", fileName);
			return (fileName, []);
		}

		var primaryPr = prNumbers[0]; // already sorted ascending, min is first
		var canonicalFileName = $"{primaryPr}.yaml";

		if (prNumbers.Count == 1)
			return (canonicalFileName, []);

		var markerContent = ReleaseNotesSerialization.SerializeEntry(new ChangelogEntry
		{
			Link = primaryPr.ToString(System.Globalization.CultureInfo.InvariantCulture)
		});
		var markers = prNumbers.Skip(1).Select(pr => ($"{pr}.yaml", markerContent)).ToList();

		return (canonicalFileName, markers);
	}

	/// <summary>
	/// Discovers numbered amend sidecars (<c>*.amend-{N}.yaml|yml</c>) in <paramref name="bundleDir"/>.
	/// The Lambda-reserved <c>.amend-notes.yaml</c> sidecar is excluded; only user-authored amends
	/// (those with a positive numeric suffix) are returned. Each sidecar is keyed identically to a
	/// parent bundle: product list comes from the sidecar, falling back to the sibling parent bundle
	/// file when the sidecar predates the products-copy feature.
	/// </summary>
	internal IReadOnlyList<UploadTarget> DiscoverAmendUploadTargets(IDiagnosticsCollector collector, string bundleDir)
	{
		var rootDir = _fileSystem.DirectoryInfo.New(bundleDir);

		var yamlFiles = _fileSystem
			.Directory
			.GetFiles(bundleDir, "*.yaml", SearchOption.TopDirectoryOnly)
			.Concat(_fileSystem.Directory.GetFiles(bundleDir, "*.yml", SearchOption.TopDirectoryOnly))
			.ToList();

		var targets = new List<UploadTarget>();

		foreach (var filePath in yamlFiles)
		{
			// Only numbered amend sidecars; skip parent bundles and the Lambda-reserved .amend-notes sidecar
			if (!BundleAmendMerger.IsAmendFile(filePath))
				continue;
			if (BundleAmendMerger.GetAmendFileNumber(filePath) <= 0)
				continue;

			var fileInfo = _fileSystem.FileInfo.New(filePath);
			if (SymlinkValidator.ValidateFileAccess(fileInfo, rootDir) is { } accessError)
			{
				collector.EmitWarning(filePath, $"Skipping: {accessError}");
				continue;
			}

			var products = ReadProductsFromBundle(filePath);
			if (products.Count == 0)
			{
				products = ReadProductsFromParentBundle(filePath);
				if (products.Count == 0)
				{
					collector.EmitWarning(
						filePath,
						"Amend bundle declares no products and its parent bundle is missing or has none; " +
							"skipping upload. Re-create the amend with a current docs-builder so it carries the parent's products."
					);
					continue;
				}
			}

			var fileName = _fileSystem.Path.GetFileName(filePath);
			foreach (var product in products)
			{
				if (!ChangelogKeys.IsValidProduct(product))
				{
					collector.EmitWarning(filePath, $"Skipping invalid product name \"{product}\" (must match [a-zA-Z0-9_-]+)");
					continue;
				}

				var s3Key = ChangelogKeys.BundleFileKey(product, fileName);
				targets.Add(new UploadTarget(filePath, s3Key));
			}
		}

		return targets;
	}

	internal IReadOnlyList<UploadTarget> DiscoverBundleUploadTargets(IDiagnosticsCollector collector, string bundleDir)
	{
		var rootDir = _fileSystem.DirectoryInfo.New(bundleDir);

		var yamlFiles = _fileSystem
			.Directory
			.GetFiles(bundleDir, "*.yaml", SearchOption.TopDirectoryOnly)
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

			var products = ReadProductsFromBundle(filePath);

			// Amends published before products were copied from the parent omit them; derive the
			// destination from the parent bundle next to the amend so they are not silently skipped.
			if (products.Count == 0 && BundleAmendMerger.IsAmendFile(filePath))
			{
				products = ReadProductsFromParentBundle(filePath);
				if (products.Count == 0)
				{
					collector.EmitWarning(
						filePath,
						"Amend bundle declares no products and its parent bundle is missing or has none; " +
							"skipping upload. Re-create the amend with a current docs-builder so it carries the parent's products."
					);
					continue;
				}
			}

			if (products.Count == 0)
			{
				_logger.LogDebug("No products found in bundle {File}, skipping", filePath);
				continue;
			}

			var fileName = _fileSystem.Path.GetFileName(filePath);

			foreach (var product in products)
			{
				if (!ChangelogKeys.IsValidProduct(product))
				{
					collector.EmitWarning(filePath, $"Skipping invalid product name \"{product}\" (must match [a-zA-Z0-9_-]+)");
					continue;
				}

				var s3Key = ChangelogKeys.BundleFileKey(product, fileName);
				targets.Add(new UploadTarget(filePath, s3Key));
			}
		}

		return targets;
	}

	private List<string> ReadProductsFromParentBundle(string amendFilePath)
	{
		var parentPath = BundleAmendMerger.GetParentBundlePath(amendFilePath);
		return parentPath != null && _fileSystem.File.Exists(parentPath) ? ReadProductsFromBundle(parentPath) : [];
	}

	private List<string> ReadProductsFromBundle(string filePath)
	{
		try
		{
			var content = _fileSystem.File.ReadAllText(filePath);
			var bundle = ReleaseNotesSerialization.DeserializeBundle(content);

			return bundle.Products.Select(p => p.ProductId).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Could not read products from bundle {File}", filePath);
			return [];
		}
	}

	private IReadOnlyList<UploadTarget> DiscoverTargets(
		IDiagnosticsCollector collector,
		ChangelogUploadArguments args,
		IReadOnlyList<string> directories
	)
	{
		var targets = new List<UploadTarget>();
		foreach (var directory in directories)
		{
			var found = args.ArtifactType switch
			{
				ArtifactType.Bundle => DiscoverBundleUploadTargets(collector, directory),
				ArtifactType.Amend => DiscoverAmendUploadTargets(collector, directory),
				_ => DiscoverUploadTargets(collector, directory, args.Owner, args.Repo, args.Branch)
			};
			targets.AddRange(found);
		}

		return targets;
	}

	/// <summary>
	/// Directories to scan for bundle/amend YAML: explicit <c>--directory</c>, else
	/// <c>bundle.output_directory</c> plus each profile <c>output_directory</c>, else
	/// <c>bundle.directory</c>, else <c>docs/releases</c>. Each directory is scanned
	/// <see cref="SearchOption.TopDirectoryOnly"/>.
	/// </summary>
	internal static IReadOnlyList<string> CollectBundleScanDirectories(string? explicitDirectory, ChangelogConfiguration? config)
	{
		if (!string.IsNullOrWhiteSpace(explicitDirectory))
			return [explicitDirectory];

		var dirs = new List<string>();
		var seen = new HashSet<string>(StringComparer.Ordinal);
		AddUnique(dirs, seen, config?.Bundle?.OutputDirectory);
		if (config?.Bundle?.Profiles != null)
		{
			foreach (var profile in config.Bundle.Profiles.Values)
				AddUnique(dirs, seen, profile.OutputDirectory);
		}

		if (dirs.Count == 0)
			AddUnique(dirs, seen, config?.Bundle?.Directory);
		if (dirs.Count == 0)
			AddUnique(dirs, seen, "docs/releases");
		return dirs;
	}

	private static void AddUnique(List<string> dirs, HashSet<string> seen, string? directory)
	{
		if (string.IsNullOrWhiteSpace(directory) || !seen.Add(directory))
			return;
		dirs.Add(directory);
	}

	private async Task<IReadOnlyList<string>> ResolveBundleScanDirectories(
		IDiagnosticsCollector collector,
		ChangelogUploadArguments args,
		Cancel ctx
	)
	{
		if (!string.IsNullOrWhiteSpace(args.Directory) || _configLoader == null)
			return CollectBundleScanDirectories(args.Directory, null);

		var config = await _configLoader.LoadChangelogConfiguration(collector, args.Config, ctx).ConfigureAwait(false);
		return CollectBundleScanDirectories(null, config);
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
}
