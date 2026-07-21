// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Changelog.Backfill;

/// <summary>
/// Where a recovered fact came from, ordered from strongest to weakest. When two sources
/// disagree, the stronger one wins; when only a default remains, the fact is marked as
/// such so quality metrics can count it.
/// </summary>
public enum EvidenceSource
{
	/// <summary>An existing native changelog entry or bundle already said so.</summary>
	[JsonStringEnumMemberName("native-artifact")]
	NativeArtifact,

	/// <summary>The release-note source itself said so — its structure, substitutions, or comments.</summary>
	[JsonStringEnumMemberName("release-note-source")]
	ReleaseNoteSource,

	/// <summary>The repository's changelog configuration mapped it, e.g. a label-to-type mapping in <c>changelog.yml</c>.</summary>
	[JsonStringEnumMemberName("changelog-config")]
	ChangelogConfig,

	/// <summary>GitHub metadata said so: labels, PR body, linked issues, milestone, or release data.</summary>
	[JsonStringEnumMemberName("github-metadata")]
	GithubMetadata,

	/// <summary>Nothing better was available; a deterministic default rule filled it in.</summary>
	[JsonStringEnumMemberName("default-rule")]
	DefaultRule
}

/// <summary>How much we trust a recovered fact.</summary>
public enum EvidenceConfidence
{
	/// <summary>Direct, unambiguous evidence.</summary>
	[JsonStringEnumMemberName("high")]
	High,

	/// <summary>Good evidence with some interpretation involved.</summary>
	[JsonStringEnumMemberName("medium")]
	Medium,

	/// <summary>A guess a human may want to double-check, e.g. a default rule.</summary>
	[JsonStringEnumMemberName("low")]
	Low
}

/// <summary>
/// The paper trail for one recovered fact: which field of which entry (or release) got
/// which value, based on what evidence, and how sure we are. A reviewer reading one of
/// these should be able to re-check the fact by hand.
/// </summary>
public sealed record ProvenanceRecord
{
	/// <summary>The product the fact belongs to.</summary>
	public required string Product { get; init; }

	/// <summary>The release target the fact belongs to, e.g. <c>9.0.0</c>.</summary>
	public required string Target { get; init; }

	/// <summary>The entry the fact is about. Null when the fact is about the release itself (e.g. its release date).</summary>
	public EntryIdentity? Entry { get; init; }

	/// <summary>The field that was filled in, named as in the semantic model, e.g. <c>precise_type</c> or <c>release_date</c>.</summary>
	public required string Field { get; init; }

	/// <summary>The value the field was given, as text.</summary>
	public required string Value { get; init; }

	/// <summary>Where the value came from, on the strongest-to-weakest ladder.</summary>
	public required EvidenceSource Source { get; init; }

	/// <summary>How much we trust the value.</summary>
	public required EvidenceConfidence Confidence { get; init; }

	/// <summary>Human-readable pointer to the evidence itself, e.g. a label name or a URL, so the fact can be re-checked by hand.</summary>
	public string? Evidence { get; init; }

	/// <summary>Where in the source the evidence sits, when it came from a file.</summary>
	public SourceLocation? Location { get; init; }

	/// <summary>Adds a plain-English description of every problem in this record to <paramref name="problems"/>.</summary>
	public void Validate(IList<string> problems)
	{
		if (string.IsNullOrWhiteSpace(Product))
			problems.Add("A provenance record needs a non-empty product.");
		if (string.IsNullOrWhiteSpace(Target))
			problems.Add("A provenance record needs a non-empty target.");
		if (string.IsNullOrWhiteSpace(Field))
			problems.Add("A provenance record needs a non-empty field name.");
		if (string.IsNullOrWhiteSpace(Value))
			problems.Add("A provenance record needs a non-empty value.");
		Entry?.Validate(problems);
		Location?.Validate(problems);
	}
}

/// <summary>
/// The evidence trail: why we believe each recovered fact. Written by the parser and the
/// enrichment stage as they fill in fields; read by humans reviewing a scope before it is
/// approved. Not consumed by the apply stage — it exists so decisions can be audited, not
/// so they can be replayed.
/// </summary>
public sealed record ProvenanceDocument : IBackfillDocument
{
	/// <inheritdoc />
	public static BackfillArtifactKind Kind => BackfillArtifactKind.Provenance;

	/// <summary>One record per recovered fact.</summary>
	public required IReadOnlyList<ProvenanceRecord> Records { get; init; }

	/// <inheritdoc />
	public void Validate(IList<string> problems)
	{
		for (var i = 0; i < Records.Count; i++)
		{
			var before = problems.Count;
			Records[i].Validate(problems);
			ValidationProblems.PrefixNew(problems, before, $"records[{i}]");
		}
	}
}
