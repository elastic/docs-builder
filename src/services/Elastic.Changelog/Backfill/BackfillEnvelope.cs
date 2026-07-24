// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.Backfill;

/// <summary>
/// Implemented by the root type of each backfill document family (inventory, overrides,
/// semantic model, plan, provenance, ledger). Ties the type to its artifact name so the
/// reading and writing helpers in <see cref="BackfillDocuments"/> can check "is this file
/// really the kind of document you asked for?" before parsing the payload.
/// </summary>
public interface IBackfillDocument
{
	/// <summary>Which of the six document families this type is the root of.</summary>
	static abstract BackfillArtifactKind Kind { get; }

	/// <summary>
	/// Adds a plain-English description of every problem found in this document to
	/// <paramref name="problems"/>. An empty list afterwards means the document is valid.
	/// Run automatically on every read and write.
	/// </summary>
	void Validate(IList<string> problems);
}

/// <summary>
/// The small header wrapper around every persisted backfill document. It records what
/// kind of document the file contains (<see cref="Artifact"/>) and which schema version
/// wrote it (<see cref="SchemaVersion"/>), so a reader can fail fast — with a clear
/// error — on files it does not understand, instead of guessing at a payload shape.
/// </summary>
public sealed record BackfillEnvelope<T>
{
	/// <summary>The document family name, e.g. <c>inventory</c> or <c>semantic-model</c>. See <see cref="BackfillArtifactKinds"/>.</summary>
	public required string Artifact { get; init; }

	/// <summary>The schema version of <see cref="Payload"/> at the time of writing. Compared against <see cref="BackfillSchemaVersions"/> on read.</summary>
	public required int SchemaVersion { get; init; }

	/// <summary>The document itself.</summary>
	public required T Payload { get; init; }
}
