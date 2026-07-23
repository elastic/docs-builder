// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.Json;
using Amazon.S3;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.ScopedFileSystem;

namespace Elastic.Changelog.Reconciliation;

/// <summary>
/// Read-only registry state discovery: compares a scope's private <c>registry.json</c> against the
/// actual objects in the private bucket, reports every divergence (missing, stale, corrupt,
/// object-divergent), and optionally writes the machine-readable
/// <see cref="RegistryStateSnapshot"/> for downstream consumers such as backfill planning.
/// Never writes to any bucket.
/// </summary>
public sealed class ChangelogRegistryInspectionService(
	ILoggerFactory logFactory,
	IAmazonS3? s3Client = null,
	ScopedFileSystem? fileSystem = null,
	TimeProvider? timeProvider = null
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogRegistryInspectionService>();
	private readonly IFileSystem _fileSystem = fileSystem ?? FileSystemFactory.RealWrite;

	public async Task<bool> Inspect(IDiagnosticsCollector collector, ChangelogRegistryInspectArguments args, Cancel ctx)
	{
		if (!args.TryResolveScope(collector, out var scope))
			return false;

		using var defaultClient = s3Client == null ? new AmazonS3Client() : null;
		var reader = new S3ScopeReader(s3Client ?? defaultClient!, args.S3BucketName);

		var inspector = new RegistryScopeInspector(logFactory, timeProvider);
		var snapshot = await inspector.InspectAsync(reader, scope, ctx);

		RegistryStateFormatter.Log(_logger, snapshot);

		if (!string.IsNullOrWhiteSpace(args.Out))
		{
			var json = JsonSerializer.Serialize(snapshot, RegistryStateJsonContext.Default.RegistryStateSnapshot);
			await _fileSystem.File.WriteAllTextAsync(args.Out, json, ctx);
			_logger.LogInformation("Wrote state snapshot to {Out}", args.Out);
		}

		if (snapshot.IsClean)
			return true;

		var hint = snapshot.RegistryHealth == RegistryHealth.UnsupportedSchema
			? "The manifest schema is newer than this tool understands; update docs-builder before reconciling."
			: "Run `changelog registry repair` to reconcile the private registry.";
		collector.EmitError(string.Empty,
			$"Registry scope {scope} diverged from the actual objects: registry is {snapshot.RegistryHealth} " +
			$"with {snapshot.Divergences.Count} divergence(s). {hint}");
		return false;
	}
}
