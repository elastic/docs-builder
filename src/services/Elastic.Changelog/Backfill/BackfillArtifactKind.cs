// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.Backfill;

/// <summary>
/// The kinds of document the backfill pipeline passes between its stages.
/// Each persisted file contains exactly one of these, named in its envelope
/// (e.g. <c>"artifact": "semantic-model"</c>) so a reader knows what it is
/// looking at before parsing the payload. See the README in this folder for
/// a tour of who writes and reads each one.
/// </summary>
public enum BackfillArtifactKind
{
	/// <summary>The census: which products and release-note sources exist, where they came from, and what we decided about each.</summary>
	Inventory,

	/// <summary>Manual corrections an operator feeds into planning, each with a reason attached.</summary>
	Overrides,

	/// <summary>The release notes reduced to their meaning, with formatting stripped away.</summary>
	SemanticModel,

	/// <summary>Exactly what we intend to create in S3, pinned to all of its inputs.</summary>
	Plan,

	/// <summary>The evidence trail: why we believe each recovered fact about an entry or release.</summary>
	Provenance,

	/// <summary>What actually happened when a plan was applied: every attempted step and its outcome.</summary>
	Ledger
}

/// <summary>
/// Converts between <see cref="BackfillArtifactKind"/> values and the names used in
/// files (<c>inventory</c>, <c>overrides</c>, <c>semantic-model</c>, <c>plan</c>,
/// <c>provenance</c>, <c>ledger</c>). Kept as explicit code, not reflection, so the
/// on-disk names can never drift by accident and the library stays AOT-friendly.
/// </summary>
public static class BackfillArtifactKinds
{
	/// <summary>The name written to files for <paramref name="kind"/>, e.g. <c>semantic-model</c>.</summary>
	public static string Name(BackfillArtifactKind kind) => kind switch
	{
		BackfillArtifactKind.Inventory => "inventory",
		BackfillArtifactKind.Overrides => "overrides",
		BackfillArtifactKind.SemanticModel => "semantic-model",
		BackfillArtifactKind.Plan => "plan",
		BackfillArtifactKind.Provenance => "provenance",
		BackfillArtifactKind.Ledger => "ledger",
		_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown backfill artifact kind")
	};

	/// <summary>
	/// Looks up the kind for a name read from a file. Returns false for anything unknown.
	/// Case-sensitive on purpose: these files are written by tools, so a case mismatch
	/// means something is wrong and should surface rather than be papered over.
	/// </summary>
	public static bool TryParse(string? name, out BackfillArtifactKind kind)
	{
		switch (name)
		{
			case "inventory":
				kind = BackfillArtifactKind.Inventory;
				return true;
			case "overrides":
				kind = BackfillArtifactKind.Overrides;
				return true;
			case "semantic-model":
				kind = BackfillArtifactKind.SemanticModel;
				return true;
			case "plan":
				kind = BackfillArtifactKind.Plan;
				return true;
			case "provenance":
				kind = BackfillArtifactKind.Provenance;
				return true;
			case "ledger":
				kind = BackfillArtifactKind.Ledger;
				return true;
			default:
				kind = default;
				return false;
		}
	}
}
