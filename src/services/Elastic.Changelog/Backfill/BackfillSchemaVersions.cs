// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.Backfill;

/// <summary>
/// The schema version this code reads and writes for each document family.
/// A document's envelope records the version it was written with; readers compare
/// it against these numbers and refuse anything different, so a shape change can
/// never be half-understood. Bump a family's number here when its shape changes
/// in a way old readers cannot safely ignore.
/// </summary>
public static class BackfillSchemaVersions
{
	/// <summary>Current version of the inventory document shape.</summary>
	public const int Inventory = 1;

	/// <summary>Current version of the overrides document shape.</summary>
	public const int Overrides = 1;

	/// <summary>Current version of the semantic-model document shape.</summary>
	public const int SemanticModel = 1;

	/// <summary>Current version of the plan document shape.</summary>
	public const int Plan = 1;

	/// <summary>Current version of the provenance document shape.</summary>
	public const int Provenance = 1;

	/// <summary>Current version of the ledger document shape.</summary>
	public const int Ledger = 1;

	/// <summary>The current version for the given document family.</summary>
	public static int Current(BackfillArtifactKind kind) => kind switch
	{
		BackfillArtifactKind.Inventory => Inventory,
		BackfillArtifactKind.Overrides => Overrides,
		BackfillArtifactKind.SemanticModel => SemanticModel,
		BackfillArtifactKind.Plan => Plan,
		BackfillArtifactKind.Provenance => Provenance,
		BackfillArtifactKind.Ledger => Ledger,
		_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown backfill artifact kind")
	};
}
