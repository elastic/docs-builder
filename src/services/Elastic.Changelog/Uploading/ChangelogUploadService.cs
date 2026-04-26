// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Amazon.S3;
using Elastic.Changelog.Configuration;
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

		if (args.ArtifactType == ArtifactType.Bundle)
		{
			_logger.LogWarning("Bundle artifact upload is not yet implemented; skipping");
			return true;
		}

		var changelogDir = await ResolveChangelogDirectory(collector, args, ctx);
		if (changelogDir == null)
			return false;

		if (!_fileSystem.Directory.Exists(changelogDir))
		{
			_logger.LogInformation("Changelog directory {Directory} does not exist; nothing to upload", changelogDir);
			return true;
		}

		var targets = DiscoverUploadTargets(collector, changelogDir);
		if (targets.Count == 0)
		{
			_logger.LogInformation("No changelog files found to upload in {Directory}", changelogDir);
			return true;
		}

		_logger.LogInformation("Found {Count} upload target(s) from {Directory}", targets.Count, changelogDir);

		using var defaultClient = s3Client == null ? new AmazonS3Client() : null;
		var client = s3Client ?? defaultClient!;
		var etagCalculator = new S3EtagCalculator(logFactory, _fileSystem);
		var uploader = new S3IncrementalUploader(logFactory, client, _fileSystem, etagCalculator, args.S3BucketName);
		var result = await uploader.Upload(targets, ctx);

		_logger.LogInformation("Upload complete: {Uploaded} uploaded, {Skipped} skipped, {Failed} failed", result.Uploaded, result.Skipped, result.Failed);

		if (result.Failed > 0)
			collector.EmitError(string.Empty, $"{result.Failed} file(s) failed to upload");

		return result.Failed == 0;
	}

	internal IReadOnlyList<UploadTarget> DiscoverUploadTargets(IDiagnosticsCollector collector, string changelogDir)
	{
		var yamlFiles = _fileSystem.Directory.GetFiles(changelogDir, "*.yaml", SearchOption.TopDirectoryOnly)
			.Concat(_fileSystem.Directory.GetFiles(changelogDir, "*.yml", SearchOption.TopDirectoryOnly))
			.ToList();

		var targets = new List<UploadTarget>();

		foreach (var filePath in yamlFiles)
		{
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

				var s3Key = $"{product}/changelogs/{fileName}";
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
