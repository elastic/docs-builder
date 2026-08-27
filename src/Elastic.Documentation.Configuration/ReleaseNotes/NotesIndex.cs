// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Documentation.Configuration.ReleaseNotes;

/// <summary>
/// One entry in a <see cref="NotesIndex"/> — a pool-relative path to a <c>note-*.yml</c>
/// file and a derived sequence number that records how many published bundle files (original +
/// amends) already include this note.
/// </summary>
/// <remarks>
/// <para>
/// The origin branch is the leading segment(s) of <see cref="Path"/> before the last <c>/</c>
/// (e.g. <c>path[..path.LastIndexOf('/')]</c>) and is not stored separately to avoid a second
/// source of truth that can disagree with the path.
/// </para>
/// <para>
/// <b>bundle_seq values:</b>
/// <list type="bullet">
/// <item>0 — no bundle is published for this version yet; the note is unreleased.</item>
/// <item>1 — the note shipped in the original bundle.</item>
/// <item>2 — the note was picked up by the reconciler-owned <c>{parent}.amend-notes.yaml</c>.</item>
/// </list>
/// The field is derived and updated on every reconcile pass; it is never authored manually.
/// </para>
/// </remarks>
public sealed record NoteIndexEntry
{
	/// <summary>Pool-relative path, e.g. <c>main/note-slow-rollover.yml</c>.</summary>
	public required string Path { get; init; }

	/// <summary>
	/// How many published bundle files for this version already contain this note.
	/// 0 = unreleased, 1 = in original bundle, 2 = in reconciler amend sidecar.
	/// Derived on every reconcile; never authored.
	/// </summary>
	public int BundleSeq { get; init; }
}

/// <summary>
/// Notes index published at <c>changelog/{org}/{repo}/notes-{version}.json</c>.
/// Lists all <c>note-*.yml</c> fragments for one release version,
/// across every branch of the repo.
/// </summary>
/// <remarks>
/// Contents are paths, not bodies — the note files remain the single source of truth.
/// A stale index can only omit or over-list, never serve stale prose. Bundling a version
/// is therefore 1 GET for the index + one GET per listed note.
/// </remarks>
public sealed record NotesIndex
{
	/// <summary>Schema version — bumped when consumers must change their parser.</summary>
	public int SchemaVersion { get; init; } = CurrentSchemaVersion;

	/// <summary>Current schema version constant.</summary>
	public const int CurrentSchemaVersion = 1;

	/// <summary>
	/// Notes for this version. Each entry carries the pool-relative path, origin branch,
	/// and a derived <c>bundle_seq</c>.
	/// </summary>
	public required IReadOnlyList<NoteIndexEntry> Notes { get; init; }
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(NotesIndex))]
[JsonSerializable(typeof(NoteIndexEntry))]
public sealed partial class NotesIndexJsonContext : JsonSerializerContext;
