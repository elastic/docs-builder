// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Amazon.S3;
using Elastic.Changelog.Uploading;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Reconciliation;

/// <summary>How the public bucket's state diverges from the state expected from the private bucket.</summary>
public enum PublicDivergenceKind
{
	/// <summary>The public bucket has no <c>registry.json</c> for the scope while the private bucket does.</summary>
	MissingPublicRegistry,

	/// <summary>The public <c>registry.json</c> exists but cannot be parsed.</summary>
	CorruptPublicRegistry,

	/// <summary>The public <c>registry.json</c> content disagrees with the private one (pass-through should keep them identical).</summary>
	RegistryMismatch,

	/// <summary>A private object has no public counterpart — its scrub event is pending, failed, or was lost.</summary>
	MissingPublicObject,

	/// <summary>A public object has no private counterpart — its delete event is pending or was lost.</summary>
	StalePublicObject
}

/// <summary>One public-side divergence finding.</summary>
public sealed record PublicDivergence
{
	public required PublicDivergenceKind Kind { get; init; }
	public required string File { get; init; }
	public required string Detail { get; init; }
}

/// <summary>
/// Verifies that the scrubber-owned <em>public</em> bucket has converged to the state expected from
/// the <em>private</em> bucket for one scope: the public <c>registry.json</c> must equal the private
/// one (the scrubber passes it through verbatim), every private YAML object must have a public
/// counterpart, and no public object may outlive its private source. Divergence is re-checked under
/// a bounded retry policy to tolerate in-flight scrubber propagation (registry and YAML scrub events
/// are independent, so transient divergence is normal).
/// </summary>
/// <remarks>
/// This service is strictly read-only by construction: the comparison operates exclusively on
/// <see cref="IS3ScopeReader"/>, which exposes no write operations, so no code path can mutate
/// either bucket. Recovery from a persistently missing public object is the separate, explicit
/// <c>changelog registry republish</c> operation on the private side.
/// </remarks>
public sealed class ChangelogPublicVerificationService(
	ILoggerFactory logFactory,
	IAmazonS3? privateS3Client = null,
	IAmazonS3? publicS3Client = null,
	TimeProvider? timeProvider = null
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogPublicVerificationService>();
	private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;

	public async Task<bool> Verify(IDiagnosticsCollector collector, ChangelogPublicVerifyArguments args, Cancel ctx)
	{
		if (!args.TryResolveScope(collector, out var scope))
			return false;

		if (args.MaxAttempts < 1)
		{
			collector.EmitError(string.Empty, "--max-attempts must be at least 1.");
			return false;
		}

		using var defaultPrivateClient = privateS3Client == null ? new AmazonS3Client() : null;
		using var defaultPublicClient = publicS3Client == null ? new AmazonS3Client() : null;
		IS3ScopeReader privateReader = new S3ScopeReader(privateS3Client ?? defaultPrivateClient!, args.S3BucketName);
		IS3ScopeReader publicReader = new S3ScopeReader(publicS3Client ?? defaultPublicClient!, args.PublicS3BucketName);

		IReadOnlyList<PublicDivergence> findings = [];
		for (var attempt = 1; attempt <= args.MaxAttempts; attempt++)
		{
			ctx.ThrowIfCancellationRequested();

			findings = await Compare(privateReader, publicReader, scope, ctx);
			if (findings.Count == 0)
			{
				_logger.LogInformation("Public state for {Scope} converged after {Attempt} attempt(s)", scope, attempt);
				return true;
			}

			_logger.LogInformation(
				"Public state for {Scope} diverges in {Count} place(s) (attempt {Attempt}/{Max})",
				scope, findings.Count, attempt, args.MaxAttempts);

			if (attempt < args.MaxAttempts)
				await Task.Delay(args.PollInterval, _time, ctx);
		}

		foreach (var finding in findings)
			collector.EmitWarning(string.Empty, $"[{finding.Kind}] {finding.File}: {finding.Detail}");

		collector.EmitError(string.Empty,
			$"Public bucket state for {scope} still diverges from the private bucket after {args.MaxAttempts} attempt(s) " +
			$"({findings.Count} finding(s)). For a lost or dead-lettered scrub event, re-emit it explicitly with `changelog registry republish`.");
		return false;
	}

	private async Task<IReadOnlyList<PublicDivergence>> Compare(
		IS3ScopeReader privateReader, IS3ScopeReader publicReader, ChangelogScope scope, Cancel ctx)
	{
		var findings = new List<PublicDivergence>();

		await CompareRegistries(privateReader, publicReader, scope, findings, ctx);

		var privateFiles = ListScopeFiles(await privateReader.ListObjectsAsync(scope.Prefix, ctx), scope);
		var publicFiles = ListScopeFiles(await publicReader.ListObjectsAsync(scope.Prefix, ctx), scope);

		foreach (var file in privateFiles.Where(f => !publicFiles.Contains(f)))
		{
			findings.Add(new PublicDivergence
			{
				Kind = PublicDivergenceKind.MissingPublicObject,
				File = file,
				Detail = "Private object has no public counterpart; its scrub event is pending, failed scrubbing, or was lost."
			});
		}

		foreach (var file in publicFiles.Where(f => !privateFiles.Contains(f)))
		{
			findings.Add(new PublicDivergence
			{
				Kind = PublicDivergenceKind.StalePublicObject,
				File = file,
				Detail = "Public object has no private counterpart; its delete event is pending or was lost."
			});
		}

		return findings;
	}

	private async Task CompareRegistries(
		IS3ScopeReader privateReader, IS3ScopeReader publicReader, ChangelogScope scope, List<PublicDivergence> findings, Cancel ctx)
	{
		var privateManifest = await privateReader.TryGetObjectAsync(scope.RegistryKey, ctx);
		var publicManifest = await publicReader.TryGetObjectAsync(scope.RegistryKey, ctx);

		if (privateManifest is null)
		{
			// Nothing is expected on the public side; a lingering public manifest is stale.
			if (publicManifest is not null)
			{
				findings.Add(new PublicDivergence
				{
					Kind = PublicDivergenceKind.StalePublicObject,
					File = ChangelogKeys.RegistryFileName,
					Detail = "Public registry exists but the private registry is gone."
				});
			}
			return;
		}

		if (publicManifest is null)
		{
			findings.Add(new PublicDivergence
			{
				Kind = PublicDivergenceKind.MissingPublicRegistry,
				File = ChangelogKeys.RegistryFileName,
				Detail = "Private registry exists but its public pass-through copy does not."
			});
			return;
		}

		var privateRegistry = TryParse(privateManifest.Value.Content);
		var publicRegistry = TryParse(publicManifest.Value.Content);
		if (publicRegistry is null)
		{
			findings.Add(new PublicDivergence
			{
				Kind = PublicDivergenceKind.CorruptPublicRegistry,
				File = ChangelogKeys.RegistryFileName,
				Detail = "Public registry cannot be parsed."
			});
			return;
		}

		// The private manifest's health is the inspect command's concern; here it only matters
		// that pass-through kept both sides identical.
		if (privateRegistry is not null && !EntriesEqual(privateRegistry.Bundles, publicRegistry.Bundles))
		{
			findings.Add(new PublicDivergence
			{
				Kind = PublicDivergenceKind.RegistryMismatch,
				File = ChangelogKeys.RegistryFileName,
				Detail = "Public registry entries differ from the private registry; its pass-through event is pending or was lost."
			});
		}
	}

	private static Registry? TryParse(string content)
	{
		try
		{
			return JsonSerializer.Deserialize(content, RegistryJsonContext.Default.Registry);
		}
		catch (JsonException)
		{
			return null;
		}
	}

	private static bool EntriesEqual(IReadOnlyList<RegistryBundle> a, IReadOnlyList<RegistryBundle> b)
	{
		if (a.Count != b.Count)
			return false;

		for (var i = 0; i < a.Count; i++)
		{
			if (!string.Equals(a[i].File, b[i].File, StringComparison.Ordinal) ||
				!string.Equals(a[i].Target, b[i].Target, StringComparison.Ordinal) ||
				!string.Equals(a[i].ETag, b[i].ETag, StringComparison.Ordinal))
				return false;
		}

		return true;
	}

	/// <summary>Single-segment YAML file names in the scope, excluding the manifest and nested scopes.</summary>
	private static HashSet<string> ListScopeFiles(IReadOnlyList<ListedObject> listed, ChangelogScope scope)
	{
		var files = new HashSet<string>(StringComparer.Ordinal);
		foreach (var obj in listed)
		{
			var file = obj.Key[scope.Prefix.Length..];
			if (file.Length == 0 || file.Contains('/', StringComparison.Ordinal))
				continue;
			if (!file.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) && !file.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
				continue;
			_ = files.Add(file);
		}

		return files;
	}
}
