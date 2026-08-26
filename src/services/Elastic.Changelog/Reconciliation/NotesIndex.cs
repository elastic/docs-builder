// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Changelog.Reconciliation;

/// <summary>
/// Notes index published at <c>changelog/{org}/{repo}/notes-{target}.json</c>.
/// Lists pool-relative paths of all <c>note-*.yml</c> fragments for one target,
/// across every branch of the repo.
/// </summary>
/// <remarks>
/// Contents are paths, not bodies — the note files remain the single source of truth.
/// A stale index can only omit or over-list, never serve stale prose. Bundling a target
/// is therefore 1 GET for the index + one GET per listed note.
/// </remarks>
public sealed record NotesIndex
{
	/// <summary>Pool-relative paths of notes for this target, e.g. <c>["main/note-slow-rollover.yml"]</c>.</summary>
	public required IReadOnlyList<string> Notes { get; init; }
}

[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(NotesIndex))]
public sealed partial class NotesIndexJsonContext : JsonSerializerContext;
