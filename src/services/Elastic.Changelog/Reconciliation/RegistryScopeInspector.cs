// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.Changelog.Uploading;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.ReleaseNotes;
using Elastic.Documentation.Versions;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Reconciliation;

/// <summary>
/// Compares a scope's <c>registry.json</c> manifest against the actual objects in that scope and
/// classifies every divergence: <em>missing</em> (object exists, registry lacks it), <em>stale</em>
/// (registry entry, object gone), <em>corrupt</em> (unparseable/invalid manifest), and
/// <em>object-divergent</em> (registry metadata disagrees with the object). Built exclusively on
/// <see cref="IS3ScopeReader"/>, so an inspection can never write to any bucket.
/// </summary>
/// <remarks>
/// Registries merge additively under optimistic concurrency and are not authoritative for removals
/// or discovery — this inspector produces the trustworthy current-state snapshot
/// (<see cref="RegistryStateSnapshot"/>) that repair and backfill planning start from.
/// </remarks>
public sealed class RegistryScopeInspector(ILoggerFactory logFactory, TimeProvider? timeProvider = null)
{
	private readonly ILogger _logger = logFactory.CreateLogger<RegistryScopeInspector>();
	private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;

	/// <summary>Takes a full state snapshot of <paramref name="scope"/> in <paramref name="reader"/>'s bucket.</summary>
	public async Task<RegistryStateSnapshot> InspectAsync(IS3ScopeReader reader, ChangelogScope scope, Cancel ctx)
	{
		var diagnostics = new List<string>();

		// Read the registry before listing objects: any concurrent registry write after this read
		// changes the manifest ETag, which repair's conditional PUT then detects (see RegistryRepairer).
		var (registryEntries, registryETag, health, corruptFindings) = await FetchRegistry(reader, scope, ctx);
		var objects = await ListScopeObjects(reader, scope, diagnostics, ctx);
		var expected = await DeriveExpectedEntries(reader, scope, objects, registryEntries, diagnostics, ctx);

		// Per-file diffs are only meaningful against a readable manifest: a missing manifest diffs
		// against an empty entry list (every object is then "missing"), while corrupt and
		// unsupported-schema manifests have unknown entries, so only their scope-level finding stands.
		var divergences = new List<RegistryDivergence>(corruptFindings);
		if (health is RegistryHealth.Valid or RegistryHealth.Missing)
			divergences.AddRange(Diff(registryEntries, expected, objects));

		return new RegistryStateSnapshot
		{
			ScopeKind = scope.Kind,
			Scope = scope.Group,
			Bucket = reader.BucketName,
			RegistryKey = scope.RegistryKey,
			GeneratedAt = _time.GetUtcNow(),
			RegistryHealth = health,
			RegistryETag = registryETag,
			Objects = objects,
			RegistryEntries = registryEntries,
			ExpectedEntries = expected,
			Divergences = divergences,
			Diagnostics = diagnostics
		};
	}

	/// <summary>Reads and validates the scope's manifest; corrupt manifests yield findings instead of throwing.</summary>
	private async Task<(IReadOnlyList<RegistryBundle> Entries, string? ETag, RegistryHealth Health, List<RegistryDivergence> Findings)> FetchRegistry(
		IS3ScopeReader reader, ChangelogScope scope, Cancel ctx)
	{
		var manifest = await reader.TryGetObjectAsync(scope.RegistryKey, ctx);
		if (manifest is null)
			return ([], null, RegistryHealth.Missing, []);

		var (content, etag) = manifest.Value;
		Registry? registry;
		try
		{
			registry = JsonSerializer.Deserialize(content, RegistryJsonContext.Default.Registry);
		}
		catch (JsonException ex)
		{
			_logger.LogWarning("Manifest {Key} is unparseable: {Message}", scope.RegistryKey, ex.Message);
			return ([], etag, RegistryHealth.Corrupt, [Corrupt($"Manifest is not valid JSON: {ex.Message}")]);
		}

		if (registry is null)
			return ([], etag, RegistryHealth.Corrupt, [Corrupt("Manifest deserialized to null.")]);

		if (registry.SchemaVersion > 1)
			return ([], etag, RegistryHealth.UnsupportedSchema, []);

		var findings = ValidateEntries(registry.Bundles);
		return findings.Count > 0
			? ([], etag, RegistryHealth.Corrupt, findings)
			: (registry.Bundles, etag, RegistryHealth.Valid, findings);
	}

	private static List<RegistryDivergence> ValidateEntries(IReadOnlyList<RegistryBundle> entries)
	{
		var findings = new List<RegistryDivergence>();
		var seen = new HashSet<string>(StringComparer.Ordinal);
		foreach (var entry in entries)
		{
			if (!ChangelogKeys.IsSafeFileName(entry.File))
				findings.Add(Corrupt($"Manifest entry has an unsafe file name: \"{entry.File}\"."));
			else if (!seen.Add(entry.File))
				findings.Add(Corrupt($"Manifest lists \"{entry.File}\" more than once."));
		}
		return findings;
	}

	private static RegistryDivergence Corrupt(string detail) => new()
	{
		Kind = RegistryDivergenceKind.Corrupt,
		File = ChangelogKeys.RegistryFileName,
		Detail = detail
	};

	/// <summary>
	/// Lists the scope's content objects: single-segment YAML keys under the scope prefix, excluding
	/// the manifest itself. Deeper keys belong to nested scopes (a changelog pool whose branch extends
	/// this one) and are never part of this scope's registry.
	/// </summary>
	private async Task<IReadOnlyList<ScopeObject>> ListScopeObjects(
		IS3ScopeReader reader, ChangelogScope scope, List<string> diagnostics, Cancel ctx)
	{
		var listed = await reader.ListObjectsAsync(scope.Prefix, ctx);
		var objects = new List<ScopeObject>(listed.Count);
		foreach (var obj in listed)
		{
			var file = obj.Key[scope.Prefix.Length..];
			if (file.Length == 0 || file.Contains('/', StringComparison.Ordinal))
			{
				// A nested changelog pool (branch "main" vs "main/foo") is expected; a nested key
				// under a bundle scope is not, so surface the latter.
				if (scope.Kind == ChangelogScopeKind.Bundle)
					diagnostics.Add($"Ignored nested key outside this scope: {obj.Key}");
				continue;
			}

			if (string.Equals(file, ChangelogKeys.RegistryFileName, StringComparison.Ordinal))
				continue;

			if (!file.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) &&
				!file.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
			{
				diagnostics.Add($"Ignored unexpected non-YAML object: {obj.Key}");
				continue;
			}

			objects.Add(new ScopeObject
			{
				File = file,
				Key = obj.Key,
				ETag = obj.ETag,
				Size = obj.Size,
				LastModified = obj.LastModified
			});
		}

		return objects;
	}

	/// <summary>
	/// Derives the entries the registry should list from the actual objects. Bundle scopes read each
	/// bundle's YAML to record the scope product's target (falling back to the parent bundle for
	/// legacy amends that omit <c>products</c>, mirroring <see cref="RegistryBuilder"/>); changelog
	/// scopes only enumerate files and never record a target. When a bundle cannot be parsed its
	/// expected target is unknown, so the registry's currently recorded target is preserved rather
	/// than reported (and repaired) as a divergence.
	/// </summary>
	private async Task<IReadOnlyList<RegistryBundle>> DeriveExpectedEntries(
		IS3ScopeReader reader, ChangelogScope scope, IReadOnlyList<ScopeObject> objects,
		IReadOnlyList<RegistryBundle> registryEntries, List<string> diagnostics, Cancel ctx)
	{
		var contentCache = new Dictionary<string, string?>(StringComparer.Ordinal);
		var registryByFile = registryEntries.ToDictionary(e => e.File, e => e, StringComparer.Ordinal);
		var entries = new List<RegistryBundle>(objects.Count);
		foreach (var obj in objects)
		{
			string? target = null;
			if (scope.Kind == ChangelogScopeKind.Bundle)
			{
				var (derived, known) = await DeriveBundleTarget(reader, scope, obj, contentCache, diagnostics, ctx);
				target = known
					? derived
					: registryByFile.TryGetValue(obj.File, out var existing) ? existing.Target : null;
			}

			entries.Add(new RegistryBundle
			{
				File = obj.File,
				Target = target,
				ETag = obj.ETag
			});
		}

		return SortEntries(entries);
	}

	private async Task<(string? Target, bool Known)> DeriveBundleTarget(
		IS3ScopeReader reader, ChangelogScope scope, ScopeObject obj,
		Dictionary<string, string?> contentCache, List<string> diagnostics, Cancel ctx)
	{
		var bundle = await ReadBundle(reader, obj.Key, contentCache, diagnostics, ctx);
		if (bundle is null)
			return (null, Known: false);

		// Amends published before products were copied from the parent omit them; record the
		// parent bundle's target so :version:-filtered consumers still discover the amend.
		if (bundle.Products.Count == 0 && BundleAmendMerger.IsAmendFile(obj.File))
		{
			var parentFile = BundleAmendMerger.GetParentBundlePath(obj.File);
			if (parentFile is null)
				return (null, Known: true);

			var parent = await ReadBundle(reader, scope.Prefix + parentFile, contentCache, diagnostics, ctx);
			return (parent is { Products.Count: > 0 } ? TargetForProduct(parent, scope.Group) : null, Known: true);
		}

		return (bundle.Products.Count > 0 ? TargetForProduct(bundle, scope.Group) : null, Known: true);
	}

	private async Task<Bundle?> ReadBundle(
		IS3ScopeReader reader, string key, Dictionary<string, string?> contentCache, List<string> diagnostics, Cancel ctx)
	{
		if (!contentCache.TryGetValue(key, out var content))
		{
			var fetched = await reader.TryGetObjectAsync(key, ctx);
			content = fetched?.Content;
			contentCache[key] = content;
		}

		if (content is null)
			return null;

		try
		{
			return ReleaseNotesSerialization.DeserializeBundle(content);
		}
		catch (Exception ex)
		{
			_logger.LogWarning("Could not parse bundle {Key}: {Message}", key, ex.Message);
			diagnostics.Add($"Could not parse bundle {key}; its expected target is unknown: {ex.Message}");
			return null;
		}
	}

	private static string? TargetForProduct(Bundle bundle, string product)
	{
		var match = bundle.Products.FirstOrDefault(p => string.Equals(p.ProductId, product, StringComparison.Ordinal));
		return (match ?? bundle.Products[0]).Target;
	}

	/// <summary>Same ordering the registry writer produces: target descending, file-name tiebreak.</summary>
	internal static List<RegistryBundle> SortEntries(IEnumerable<RegistryBundle> entries) =>
		entries
			.OrderByDescending(b => VersionOrDate.Parse(b.Target ?? string.Empty))
			.ThenBy(b => b.File, StringComparer.Ordinal)
			.ToList();

	private static List<RegistryDivergence> Diff(
		IReadOnlyList<RegistryBundle> registryEntries,
		IReadOnlyList<RegistryBundle> expected,
		IReadOnlyList<ScopeObject> objects)
	{
		var divergences = new List<RegistryDivergence>();
		var registryByFile = registryEntries.ToDictionary(e => e.File, e => e, StringComparer.Ordinal);
		var objectsByFile = objects.ToDictionary(o => o.File, o => o, StringComparer.Ordinal);

		foreach (var want in expected)
		{
			if (!registryByFile.TryGetValue(want.File, out var have))
			{
				divergences.Add(new RegistryDivergence
				{
					Kind = RegistryDivergenceKind.Missing,
					File = want.File,
					Detail = "Object exists in the scope but the registry has no entry for it.",
					ObjectETag = want.ETag,
					ObjectTarget = want.Target
				});
				continue;
			}

			var etagMatches = string.Equals(S3ScopeReader.NormalizeETag(have.ETag), want.ETag, StringComparison.OrdinalIgnoreCase);
			var targetMatches = string.Equals(have.Target, want.Target, StringComparison.Ordinal);
			if (!etagMatches || !targetMatches)
			{
				divergences.Add(new RegistryDivergence
				{
					Kind = RegistryDivergenceKind.ObjectDivergent,
					File = want.File,
					Detail = etagMatches
						? "Registry target disagrees with the target derived from the object."
						: "Registry ETag disagrees with the actual object.",
					RegistryETag = have.ETag,
					ObjectETag = want.ETag,
					RegistryTarget = have.Target,
					ObjectTarget = want.Target
				});
			}
		}

		foreach (var have in registryEntries)
		{
			if (!objectsByFile.ContainsKey(have.File))
			{
				divergences.Add(new RegistryDivergence
				{
					Kind = RegistryDivergenceKind.Stale,
					File = have.File,
					Detail = "Registry lists a file whose object no longer exists in the scope.",
					RegistryETag = have.ETag,
					RegistryTarget = have.Target
				});
			}
		}

		return divergences;
	}
}
