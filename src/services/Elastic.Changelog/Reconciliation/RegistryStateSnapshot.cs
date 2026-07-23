// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Changelog.Uploading;

namespace Elastic.Changelog.Reconciliation;

/// <summary>Overall health of a scope's <c>registry.json</c> manifest object.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RegistryHealth>))]
public enum RegistryHealth
{
	/// <summary>The manifest exists and parses as a <see cref="Registry"/> with safe entries.</summary>
	Valid,

	/// <summary>No manifest object exists at the scope's registry key.</summary>
	Missing,

	/// <summary>The manifest exists but cannot be parsed, or contains invalid entries.</summary>
	Corrupt,

	/// <summary>
	/// The manifest declares a <c>schema_version</c> newer than this tool understands. Not corrupt —
	/// but repair refuses to touch it, because rebuilding would silently downgrade the schema.
	/// </summary>
	UnsupportedSchema
}

/// <summary>How a registry entry (or the manifest itself) diverges from the actual scoped objects.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RegistryDivergenceKind>))]
public enum RegistryDivergenceKind
{
	/// <summary>An object exists in the scope but the registry has no entry for it.</summary>
	Missing,

	/// <summary>The registry lists a file whose object no longer exists in the scope.</summary>
	Stale,

	/// <summary>The manifest itself is unparseable or contains invalid (unsafe) entries.</summary>
	Corrupt,

	/// <summary>A registry entry's recorded metadata (ETag or target) disagrees with the actual object.</summary>
	ObjectDivergent
}

/// <summary>One divergence finding between a registry and its scope's actual objects.</summary>
public sealed record RegistryDivergence
{
	/// <summary>The divergence class.</summary>
	public required RegistryDivergenceKind Kind { get; init; }

	/// <summary>The file the finding concerns (<c>registry.json</c> for <see cref="RegistryDivergenceKind.Corrupt"/>).</summary>
	public required string File { get; init; }

	/// <summary>Human-readable explanation of the finding.</summary>
	public required string Detail { get; init; }

	/// <summary>The ETag recorded in the registry entry, when one exists.</summary>
	public string? RegistryETag { get; init; }

	/// <summary>The actual object's ETag, when the object exists.</summary>
	public string? ObjectETag { get; init; }

	/// <summary>The target recorded in the registry entry, when one exists.</summary>
	public string? RegistryTarget { get; init; }

	/// <summary>The target derived from the actual object, when derivable.</summary>
	public string? ObjectTarget { get; init; }
}

/// <summary>One actual content object (bundle or entry YAML) found in the scope.</summary>
public sealed record ScopeObject
{
	/// <summary>File name (last key segment) of the object.</summary>
	public required string File { get; init; }

	/// <summary>Full S3 key of the object.</summary>
	public required string Key { get; init; }

	/// <summary>The object's S3 ETag, normalized (no surrounding quotes).</summary>
	public required string ETag { get; init; }

	/// <summary>Object size in bytes.</summary>
	public long Size { get; init; }

	/// <summary>Object last-modified timestamp, when the listing reported one.</summary>
	public DateTimeOffset? LastModified { get; init; }
}

/// <summary>
/// A machine-readable snapshot of one registry scope's current state: the actual content objects
/// in the private bucket, the registry manifest's entries, and every divergence between the two.
/// Produced by <see cref="RegistryScopeInspector"/>; consumed by the repair operation and — as a
/// serialized artifact — by backfill planning, which needs a trustworthy view of current state
/// because registries merge additively and are not authoritative for removals or discovery.
/// </summary>
public sealed record RegistryStateSnapshot
{
	/// <summary>Snapshot schema version. Incremented when consumers must change their parser.</summary>
	public int SchemaVersion { get; init; } = 1;

	/// <summary>The scope family: <c>bundle</c> or <c>changelog</c>.</summary>
	public required ChangelogScopeKind ScopeKind { get; init; }

	/// <summary>The scope's grouping segment(s): product, or <c>{org}/{repo}/{branch}</c>.</summary>
	public required string Scope { get; init; }

	/// <summary>The bucket the snapshot was taken from.</summary>
	public required string Bucket { get; init; }

	/// <summary>The S3 key of the scope's <c>registry.json</c> manifest.</summary>
	public required string RegistryKey { get; init; }

	/// <summary>Time the snapshot was taken, in UTC.</summary>
	public required DateTimeOffset GeneratedAt { get; init; }

	/// <summary>Health of the manifest object itself.</summary>
	public required RegistryHealth RegistryHealth { get; init; }

	/// <summary>The manifest object's ETag as read (normalized), when the object exists.</summary>
	public string? RegistryETag { get; init; }

	/// <summary>The actual content objects (YAML files) currently in the scope, excluding the manifest.</summary>
	public required IReadOnlyList<ScopeObject> Objects { get; init; }

	/// <summary>The entries the registry manifest currently lists (empty when missing or corrupt).</summary>
	public required IReadOnlyList<RegistryBundle> RegistryEntries { get; init; }

	/// <summary>
	/// The entries the registry <em>should</em> list, derived purely from the actual objects
	/// (file name, object ETag, and — for bundle scopes — the target read from the bundle YAML).
	/// This is what a repair converges the registry to.
	/// </summary>
	public required IReadOnlyList<RegistryBundle> ExpectedEntries { get; init; }

	/// <summary>Every divergence found between the registry and the actual objects.</summary>
	public required IReadOnlyList<RegistryDivergence> Divergences { get; init; }

	/// <summary>Non-classified observations (unreadable bundle YAML, unexpected non-YAML keys, …).</summary>
	public required IReadOnlyList<string> Diagnostics { get; init; }

	/// <summary>
	/// True when the registry matches the actual objects exactly. A missing manifest over an empty
	/// scope is clean (nothing has been published there yet); a missing manifest over a populated
	/// scope produces per-object <see cref="RegistryDivergenceKind.Missing"/> findings and is not.
	/// </summary>
	public bool IsClean => Divergences.Count == 0 && RegistryHealth is RegistryHealth.Valid or RegistryHealth.Missing;
}

[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(RegistryStateSnapshot))]
public sealed partial class RegistryStateJsonContext : JsonSerializerContext;
