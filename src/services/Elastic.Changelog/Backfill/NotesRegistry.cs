// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Changelog.Backfill;

/// <summary>
/// Per-version registry of PR-less note files written by <c>changelog backfill</c>.
/// Written alongside note-*.yaml files so consumers can enumerate them without a directory listing.
/// Stored at <c>{output}/{product}/changelog/notes-{target}.json</c>.
/// </summary>
/// <remarks>
/// <para>
/// This is a local-disk artifact, not published to S3/CDN. The scrubber Lambda's
/// <see cref="Elastic.Changelog.Uploading.RegistryJsonContext"/> is a separate published contract;
/// they must stay independent so their schemas can evolve at different rates.
/// </para>
/// <para>
/// Note: <c>changelog/{org}/{repo}/notes-{target}.json</c> is shallower than the
/// <c>changelog/{org}/{repo}/{branch}/registry.json</c> layout that <c>ChangelogKeys.IsRegistry</c>
/// accepts, so the scrubber Lambda would reject it if uploaded. Publishing notes registries is a
/// separate concern tracked in elastic/docs-eng-team#789.
/// </para>
/// </remarks>
public sealed record NotesRegistry
{
	public const int CurrentSchemaVersion = 1;

	public int SchemaVersion { get; init; } = CurrentSchemaVersion;

	public required DateTimeOffset GeneratedAt { get; init; }

	/// <summary>The version target this registry covers (e.g. <c>1.9.0</c>).</summary>
	public required string Target { get; init; }

	/// <summary>Sorted list of <c>note-*.yaml</c> file names for this target version.</summary>
	public required IReadOnlyList<string> Notes { get; init; }
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(NotesRegistry))]
public sealed partial class BackfillJsonContext : JsonSerializerContext;
