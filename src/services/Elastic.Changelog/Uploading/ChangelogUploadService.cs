// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Amazon.S3;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Integrations.S3;
using Elastic.Documentation.ReleaseNotes;
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
}

public partial class ChangelogUploadService(
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

	[GeneratedRegex(@"^[a-zA-Z0-9_-]+$")]
	private static partial Regex ProductNameRegex();

	private static readonly YamlDotNet.Serialization.IDeserializer EntryDeserializer =
		ReleaseNotesSerialization.GetEntryDeserializer();

	public async Task<bool> Upload(IDiagnosticsCollector collector, ChangelogUploadArguments args, Cancel ctx)
	{
		if (args.Target == UploadTargetKind.Elasticsearch)
		{
			_logger.LogWarning("Elasticsearch upload target is not yet implemented; skipping");
			return true;
		}

		var directory = args.ArtifactType == ArtifactType.Bundle
			? await ResolveBundleDirectory(collector, args, ctx)
			: await ResolveChangelogDirectory(collector, args, ctx);

		if (directory == null)
			return false;

		if (!_fileSystem.Directory.Exists(directory))
		{
			_logger.LogInformation("{ArtifactType} directory {Directory} does not exist; nothing to upload", args.ArtifactType, directory);
			return true;
		}

		var targets = args.ArtifactType == ArtifactType.Bundle
			? DiscoverBundleUploadTargets(collector, directory)
			: DiscoverUploadTargets(collector, directory);

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
		var result = await uploader.Upload(targets, ctx);

		_logger.LogInformation("Upload complete: {Uploaded} uploaded, {Skipped} skipped, {Failed} failed", result.Uploaded, result.Skipped, result.Failed);

		if (result.Failed > 0)
			collector.EmitError(string.Empty, $"{result.Failed} file(s) failed to upload");

		// On a successful upload, refresh the per-product registry.json so consumers can enumerate
		// content without an S3 listing: the bundle index (consumed by the changelog directive in
		// cdn: mode) for bundle uploads, and the changelog-entry index (consumed by `changelog
		// bundle` when sourcing entries from the CDN) for changelog uploads.
		// Failures here are logged but don't fail the upload — the objects themselves are already in S3.
		if (result.Failed == 0 && targets.Count > 0)
		{
			var scope = args.ArtifactType == ArtifactType.Bundle ? RegistryScope.Bundle : RegistryScope.Changelog;
			await RefreshRegistries(collector, client, etagCalculator, args, targets, scope, ctx);
		}

		return result.Failed == 0;
	}

	private async Task RefreshRegistries(
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
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			// Leaving the manifest stale is non-fatal — bundle objects are unaffected.
			_logger.LogWarning(ex, "Registry refresh failed; bundles uploaded successfully but manifests may be stale");
			collector.EmitWarning(string.Empty, $"Failed to refresh registry manifest(s): {ex.Message}");
		}
	}

	internal IReadOnlyList<UploadTarget> DiscoverUploadTargets(IDiagnosticsCollector collector, string changelogDir)
	{
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

			var products = ReadProductsFromFragment(filePath);
			if (products.Count == 0)
			{
				_logger.LogDebug("No products found in {File}, skipping", filePath);
				continue;
			}

			var fileName = _fileSystem.Path.GetFileName(filePath);

			foreach (var product in products)
			{
				if (!ProductNameRegex().IsMatch(product))
				{
					collector.EmitWarning(filePath, $"Skipping invalid product name \"{product}\" (must match [a-zA-Z0-9_-]+)");
					continue;
				}

				var s3Key = $"{product}/changelog/{fileName}";
				targets.Add(new UploadTarget(filePath, s3Key));
			}
		}

		return targets;
	}

	private List<string> ReadProductsFromFragment(string filePath)
	{
		try
		{
			var content = _fileSystem.File.ReadAllText(filePath);
			var normalized = ReleaseNotesSerialization.NormalizeYaml(content);
			var entry = EntryDeserializer.Deserialize<ChangelogEntryDto>(normalized);
			if (entry?.Products == null)
				return [];

			return entry.Products
				.Select(p => p?.Product)
				.Where(p => !string.IsNullOrWhiteSpace(p))
				.Select(p => p!)
				.ToList();
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Could not read products from {File}", filePath);
			return [];
		}
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

			var products = ReadProductsFromBundle(filePath);
			if (products.Count == 0)
			{
				_logger.LogDebug("No products found in bundle {File}, skipping", filePath);
				continue;
			}

			var fileName = _fileSystem.Path.GetFileName(filePath);

			foreach (var product in products)
			{
				if (!ProductNameRegex().IsMatch(product))
				{
					collector.EmitWarning(filePath, $"Skipping invalid product name \"{product}\" (must match [a-zA-Z0-9_-]+)");
					continue;
				}

				var s3Key = $"{product}/bundle/{fileName}";
				targets.Add(new UploadTarget(filePath, s3Key));
			}
		}

		return targets;
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
