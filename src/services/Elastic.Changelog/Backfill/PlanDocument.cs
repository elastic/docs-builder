// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Changelog.Backfill;

/// <summary>The things a plan can decide to do (or flag) for one product/target.</summary>
public enum PlanActionKind
{
	/// <summary>Create a brand-new bundle for a target that has none from this contributor.</summary>
	[JsonStringEnumMemberName("create-bundle")]
	CreateBundle,

	/// <summary>Add missing entries to an existing bundle via an amend file, when the parent bundle is unambiguous.</summary>
	[JsonStringEnumMemberName("create-amend")]
	CreateAmend,

	/// <summary>Create an additional bundle for a target that already has one, when amending would risk picking the wrong parent.</summary>
	[JsonStringEnumMemberName("create-supplemental-bundle")]
	CreateSupplementalBundle,

	/// <summary>Do nothing: the key already exists with exactly the bytes we would have written.</summary>
	[JsonStringEnumMemberName("skip-existing")]
	SkipExisting,

	/// <summary>A human must decide; the plan explains why in the action's reason.</summary>
	[JsonStringEnumMemberName("manual-review")]
	ManualReview,

	/// <summary>The key already exists with different bytes. Never overwritten in a normal run.</summary>
	[JsonStringEnumMemberName("conflict")]
	Conflict
}

/// <summary>Which slice of the backfill a plan covers: one product, optionally narrowed to one repository and a target range.</summary>
public sealed record PlanScope
{
	/// <summary>The product this plan covers.</summary>
	public required string Product { get; init; }

	/// <summary>The contributing repository, when the plan covers only one.</summary>
	public GitRepository? Repository { get; init; }

	/// <summary>Human-readable description of the covered targets, e.g. <c>9.0.0..9.3.0</c> or <c>2025-01 onwards</c>.</summary>
	public string? TargetRange { get; init; }

	/// <summary>Adds a plain-English description of every problem in this scope to <paramref name="problems"/>.</summary>
	public void Validate(IList<string> problems)
	{
		if (string.IsNullOrWhiteSpace(Product))
			problems.Add("A plan scope needs a non-empty product.");
		Repository?.Validate(problems);
	}
}

/// <summary>
/// Identifies exactly which link allowlist the deployed scrubber Lambda is running with.
/// The allowlist is baked into the Lambda at deploy time, so the local checkout can be
/// ahead of or behind it; a plan pins the deployed identity so "which links will survive
/// publication" was answered against reality, not against the local files. At least one
/// of the two fields is set.
/// </summary>
public sealed record ScrubberAllowlist
{
	/// <summary>Hash of the deployed allowlist content, as <c>sha256:</c> + 64 hex characters.</summary>
	public string? Sha256 { get; init; }

	/// <summary>The docs-builder commit the deployed scrubber was built from (full 40-character SHA).</summary>
	public string? DeploymentCommit { get; init; }

	/// <summary>Adds a plain-English description of every problem in this allowlist identity to <paramref name="problems"/>.</summary>
	public void Validate(IList<string> problems)
	{
		if (Sha256 is null && DeploymentCommit is null)
			problems.Add("A scrubber allowlist identity needs its content hash, its deployment commit, or both — otherwise the plan cannot say which allowlist it was checked against.");
		if (Sha256 is not null && !BackfillHash.IsWellFormed(Sha256))
			problems.Add($"The scrubber allowlist hash must look like sha256: plus 64 lower-case hex characters, but found '{Sha256}'.");
	}
}

/// <summary>
/// One object that existed in the private bucket when the plan was computed. The plan
/// records these so the apply stage can detect that the world moved under it: if a key
/// the plan saw (or didn't see) has changed, the plan is stale and must be recomputed.
/// </summary>
public sealed record RemoteObject
{
	/// <summary>The object's S3 key, e.g. <c>bundle/elasticsearch/elasticsearch-9.0.0.yaml</c>.</summary>
	public required string Key { get; init; }

	/// <summary>The object's ETag as reported by S3, when available.</summary>
	public string? ETag { get; init; }

	/// <summary>Hash of the object's content as <c>sha256:</c> + 64 hex characters, when it was downloaded and hashed.</summary>
	public string? Sha256 { get; init; }

	/// <summary>Adds a plain-English description of every problem in this object record to <paramref name="problems"/>.</summary>
	public void Validate(IList<string> problems)
	{
		if (string.IsNullOrWhiteSpace(Key))
			problems.Add("A remote object needs a non-empty key.");
		if (Sha256 is not null && !BackfillHash.IsWellFormed(Sha256))
			problems.Add($"A remote object's hash must look like sha256: plus 64 lower-case hex characters, but found '{Sha256}'.");
	}
}

/// <summary>
/// One decision in a plan: what to create (with the exact content, identified by hash),
/// what to skip, or what needs a human. Create actions name the S3 key they will write
/// and the hash of the bytes they will write there, so apply can verify it wrote exactly
/// what was approved.
/// </summary>
public sealed record PlanAction
{
	/// <summary>What this action does.</summary>
	public required PlanActionKind Kind { get; init; }

	/// <summary>The product the action belongs to.</summary>
	public required string Product { get; init; }

	/// <summary>The release target the action belongs to, e.g. <c>9.0.0</c>.</summary>
	public required string Target { get; init; }

	/// <summary>The S3 key involved. Required for create and skip/conflict actions; may be null for manual-review when no key was determined.</summary>
	public string? Key { get; init; }

	/// <summary>Hash of the exact bytes a create action will upload, as <c>sha256:</c> + 64 hex characters. Required for create actions.</summary>
	public string? ContentSha256 { get; init; }

	/// <summary>For <see cref="PlanActionKind.CreateAmend"/>: the S3 key of the bundle the amend attaches to.</summary>
	public string? ParentKey { get; init; }

	/// <summary>Why the plan decided this — required for skips, manual reviews, and conflicts so a reviewer never has to guess.</summary>
	public string? Reason { get; init; }

	/// <summary>Adds a plain-English description of every problem in this action to <paramref name="problems"/>.</summary>
	public void Validate(IList<string> problems)
	{
		if (string.IsNullOrWhiteSpace(Product))
			problems.Add("A plan action needs a non-empty product.");
		if (string.IsNullOrWhiteSpace(Target))
			problems.Add("A plan action needs a non-empty target.");

		var creates = Kind is PlanActionKind.CreateBundle or PlanActionKind.CreateAmend or PlanActionKind.CreateSupplementalBundle;
		if (creates)
		{
			if (string.IsNullOrWhiteSpace(Key))
				problems.Add($"A {DescribeKind()} action needs the S3 key it will create.");
			if (!BackfillHash.IsWellFormed(ContentSha256))
				problems.Add($"A {DescribeKind()} action needs the hash of the content it will upload (sha256: plus 64 lower-case hex characters), but found '{ContentSha256}'.");
		}
		if (Kind == PlanActionKind.CreateAmend && string.IsNullOrWhiteSpace(ParentKey))
			problems.Add("A create-amend action needs the key of the parent bundle it amends.");
		if (Kind is PlanActionKind.SkipExisting or PlanActionKind.ManualReview or PlanActionKind.Conflict && string.IsNullOrWhiteSpace(Reason))
			problems.Add($"A {DescribeKind()} action needs a reason so a reviewer never has to guess.");
		if (Kind is PlanActionKind.SkipExisting or PlanActionKind.Conflict && string.IsNullOrWhiteSpace(Key))
			problems.Add($"A {DescribeKind()} action needs the existing S3 key it refers to.");
	}

	private string DescribeKind() => Kind switch
	{
		PlanActionKind.CreateBundle => "create-bundle",
		PlanActionKind.CreateAmend => "create-amend",
		PlanActionKind.CreateSupplementalBundle => "create-supplemental-bundle",
		PlanActionKind.SkipExisting => "skip-existing",
		PlanActionKind.ManualReview => "manual-review",
		PlanActionKind.Conflict => "conflict",
		_ => Kind.ToString()
	};
}

/// <summary>
/// Exactly what we intend to create in S3, pinned to all of its inputs. Written by the
/// planning stage; approved by a human; executed by the apply stage. A plan is
/// "content-addressed": its identity is the hash of its canonical content (see
/// <see cref="BackfillDocuments.ComputeHash{T}(T)"/>), so the same inputs always produce
/// a plan with the same identity, and the ledger can prove which plan a run executed.
/// No S3 write ever happens from an unapproved or stale plan.
/// </summary>
public sealed record PlanDocument : IBackfillDocument
{
	/// <inheritdoc />
	public static BackfillArtifactKind Kind => BackfillArtifactKind.Plan;

	/// <summary>Which slice of the backfill this plan covers.</summary>
	public required PlanScope Scope { get; init; }

	/// <summary>The source repositories, pinned to the exact commits the plan was computed from.</summary>
	public required IReadOnlyList<PinnedSource> SourceRefs { get; init; }

	/// <summary>Hash of the inventory document the plan consumed.</summary>
	public required string InventoryHash { get; init; }

	/// <summary>Hash of the semantic-model document the plan consumed.</summary>
	public required string SemanticModelHash { get; init; }

	/// <summary>Hash of the overrides document the plan consumed. Null when no overrides were in play.</summary>
	public string? OverridesHash { get; init; }

	/// <summary>Hash of the enrichment snapshot (recovered GitHub metadata) the plan consumed. Null when enrichment was not used.</summary>
	public string? EnrichmentSnapshotHash { get; init; }

	/// <summary>Which link allowlist the deployed scrubber was running with when the plan was computed.</summary>
	public required ScrubberAllowlist ScrubberAllowlist { get; init; }

	/// <summary>The objects that existed in the plan's slice of the private bucket when it was computed. Empty when the slice was empty.</summary>
	public required IReadOnlyList<RemoteObject> CurrentState { get; init; }

	/// <summary>The plan's decisions, one per product/target/key.</summary>
	public required IReadOnlyList<PlanAction> Actions { get; init; }

	/// <inheritdoc />
	public void Validate(IList<string> problems)
	{
		var before = problems.Count;
		Scope.Validate(problems);
		ValidationProblems.PrefixNew(problems, before, "scope");

		for (var i = 0; i < SourceRefs.Count; i++)
		{
			before = problems.Count;
			SourceRefs[i].Validate(problems);
			ValidationProblems.PrefixNew(problems, before, $"source_refs[{i}]");
		}

		if (!BackfillHash.IsWellFormed(InventoryHash))
			problems.Add($"The inventory hash must look like sha256: plus 64 lower-case hex characters, but found '{InventoryHash}'.");
		if (!BackfillHash.IsWellFormed(SemanticModelHash))
			problems.Add($"The semantic-model hash must look like sha256: plus 64 lower-case hex characters, but found '{SemanticModelHash}'.");
		if (OverridesHash is not null && !BackfillHash.IsWellFormed(OverridesHash))
			problems.Add($"The overrides hash must look like sha256: plus 64 lower-case hex characters, but found '{OverridesHash}'.");
		if (EnrichmentSnapshotHash is not null && !BackfillHash.IsWellFormed(EnrichmentSnapshotHash))
			problems.Add($"The enrichment snapshot hash must look like sha256: plus 64 lower-case hex characters, but found '{EnrichmentSnapshotHash}'.");

		before = problems.Count;
		ScrubberAllowlist.Validate(problems);
		ValidationProblems.PrefixNew(problems, before, "scrubber_allowlist");

		for (var i = 0; i < CurrentState.Count; i++)
		{
			before = problems.Count;
			CurrentState[i].Validate(problems);
			ValidationProblems.PrefixNew(problems, before, $"current_state[{i}]");
		}

		for (var i = 0; i < Actions.Count; i++)
		{
			before = problems.Count;
			Actions[i].Validate(problems);
			ValidationProblems.PrefixNew(problems, before, $"actions[{i}]");
		}
	}
}
