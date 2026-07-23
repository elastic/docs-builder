// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Elastic.Changelog.Uploading;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Reconciliation;

/// <summary>
/// Reconciles a scope's <em>private</em> <c>registry.json</c> from the actual private objects:
/// adds missing entries, drops stale ones, and corrects object-divergent metadata. Writes go
/// through the same optimistic-concurrency conditional PUT the live upload path uses
/// (<c>If-Match</c> on update, <c>If-None-Match: *</c> on create), so a repair is safe against
/// concurrent uploads: a manifest changed underneath us fails the precondition and the repair
/// re-inspects (fresh registry read <em>and</em> fresh object listing) before retrying.
/// Repair is idempotent — a clean scope writes nothing.
/// </summary>
/// <remarks>
/// The public registry is scrubber-owned and is never written here; the repaired private manifest
/// reaches the public bucket through the scrubber's pass-through, triggered by this write's own
/// <c>ObjectCreated</c> event.
/// </remarks>
public sealed class ChangelogRegistryRepairService(
	ILoggerFactory logFactory,
	IAmazonS3? s3Client = null,
	TimeProvider? timeProvider = null
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogRegistryRepairService>();
	private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;

	// Bounds the optimistic-concurrency retry loop, mirroring RegistryBuilder.MaxWriteAttempts.
	private const int MaxWriteAttempts = 5;

	public async Task<bool> Repair(IDiagnosticsCollector collector, ChangelogRegistryRepairArguments args, Cancel ctx)
	{
		if (!args.TryResolveScope(collector, out var scope))
			return false;

		using var defaultClient = s3Client == null ? new AmazonS3Client() : null;
		var client = s3Client ?? defaultClient!;
		var reader = new S3ScopeReader(client, args.S3BucketName);
		var inspector = new RegistryScopeInspector(logFactory, timeProvider);

		for (var attempt = 1; attempt <= MaxWriteAttempts; attempt++)
		{
			ctx.ThrowIfCancellationRequested();

			var snapshot = await inspector.InspectAsync(reader, scope, ctx);
			RegistryStateFormatter.Log(_logger, snapshot);

			if (snapshot.IsClean)
			{
				_logger.LogInformation("Registry for {Scope} already matches the actual objects; nothing to repair", scope);
				return true;
			}

			if (snapshot.RegistryHealth == RegistryHealth.UnsupportedSchema)
			{
				collector.EmitError(string.Empty,
					$"Registry {scope.RegistryKey} declares a schema_version newer than this tool understands; " +
					"repairing would silently downgrade it. Update docs-builder instead.");
				return false;
			}

			if (snapshot.ExpectedEntries.Count == 0 && !args.AllowEmpty)
			{
				collector.EmitError(string.Empty,
					$"Scope {scope} contains no objects; refusing to write an empty registry. " +
					"Pass --allow-empty if the scope is intentionally empty.");
				return false;
			}

			LogAudit(snapshot);

			if (args.DryRun)
			{
				_logger.LogInformation("Dry run: registry for {Scope} was not modified", scope);
				return true;
			}

			if (await TryWriteRepairedManifest(client, args.S3BucketName, scope, snapshot, attempt, ctx))
				return true;
		}

		collector.EmitError(string.Empty,
			$"Registry for {scope} could not be repaired after {MaxWriteAttempts} attempts due to concurrent writes; re-run once uploads quiesce.");
		return false;
	}

	/// <summary>Audit trail: exactly which entries the repair adds, removes, or corrects.</summary>
	private void LogAudit(RegistryStateSnapshot snapshot)
	{
		foreach (var divergence in snapshot.Divergences)
		{
			switch (divergence.Kind)
			{
				case RegistryDivergenceKind.Missing:
					_logger.LogInformation("repair will add \"{File}\" (target {Target}, etag {ETag})",
						divergence.File, divergence.ObjectTarget ?? "<none>", divergence.ObjectETag);
					break;
				case RegistryDivergenceKind.Stale:
					_logger.LogInformation("repair will remove \"{File}\" (was target {Target}, etag {ETag})",
						divergence.File, divergence.RegistryTarget ?? "<none>", divergence.RegistryETag);
					break;
				case RegistryDivergenceKind.ObjectDivergent:
					_logger.LogInformation(
						"repair will correct \"{File}\": target {RegistryTarget} -> {ObjectTarget}, etag {RegistryETag} -> {ObjectETag}",
						divergence.File, divergence.RegistryTarget ?? "<none>", divergence.ObjectTarget ?? "<none>",
						divergence.RegistryETag, divergence.ObjectETag);
					break;
				case RegistryDivergenceKind.Corrupt:
					_logger.LogInformation("repair will rebuild the manifest from the actual objects: {Detail}", divergence.Detail);
					break;
				default:
					break;
			}
		}

		_logger.LogInformation("repair result: {Before} entr(ies) before, {After} after",
			snapshot.RegistryEntries.Count, snapshot.ExpectedEntries.Count);
	}

	private async Task<bool> TryWriteRepairedManifest(
		IAmazonS3 client, string bucketName, ChangelogScope scope, RegistryStateSnapshot snapshot, int attempt, Cancel ctx)
	{
		var manifest = new Registry
		{
			Product = scope.Group,
			GeneratedAt = _time.GetUtcNow(),
			Bundles = snapshot.ExpectedEntries
		};
		var json = JsonSerializer.Serialize(manifest, RegistryJsonContext.Default.Registry);

		var request = new PutObjectRequest
		{
			BucketName = bucketName,
			Key = scope.RegistryKey,
			ContentBody = json,
			ContentType = "application/json"
		};

		// Optimistic concurrency: update only if the manifest is unchanged since the inspection
		// read it, create only if it is still absent. A concurrent live upload's registry refresh
		// invalidates the precondition and we re-inspect.
		if (snapshot.RegistryETag is null)
			request.IfNoneMatch = "*";
		else
			request.IfMatch = $"\"{snapshot.RegistryETag}\"";

		try
		{
			_ = await client.PutObjectAsync(request, ctx);
			_logger.LogInformation("Repaired registry {Key} with {Count} entr(ies)", scope.RegistryKey, snapshot.ExpectedEntries.Count);
			return true;
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
		{
			_logger.LogInformation(
				"Registry for {Scope} changed concurrently (attempt {Attempt}/{Max}); re-inspecting and retrying",
				scope, attempt, MaxWriteAttempts);
			return false;
		}
	}
}
