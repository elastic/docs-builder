// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Changelog.Backfill;

/// <summary>How one attempted step of an apply run ended.</summary>
public enum LedgerActionOutcome
{
	/// <summary>The object was created.</summary>
	[JsonStringEnumMemberName("created")]
	Created,

	/// <summary>Nothing was written: the object already existed with the expected bytes.</summary>
	[JsonStringEnumMemberName("skipped")]
	Skipped,

	/// <summary>Nothing was written: the key already existed with different bytes, and normal runs never overwrite.</summary>
	[JsonStringEnumMemberName("conflict")]
	Conflict,

	/// <summary>The step failed; the detail says how.</summary>
	[JsonStringEnumMemberName("failed")]
	Failed
}

/// <summary>How refreshing one registry manifest ended.</summary>
public enum RegistryRefreshOutcome
{
	/// <summary>The manifest was rewritten with the run's changes merged in.</summary>
	[JsonStringEnumMemberName("updated")]
	Updated,

	/// <summary>The manifest already matched; nothing was written.</summary>
	[JsonStringEnumMemberName("unchanged")]
	Unchanged,

	/// <summary>The manifest could not be updated. The run is incomplete until this is reconciled.</summary>
	[JsonStringEnumMemberName("failed")]
	Failed
}

/// <summary>The overall answer from the post-apply verification.</summary>
public enum VerificationOutcome
{
	/// <summary>The published result matches the expected semantic model.</summary>
	[JsonStringEnumMemberName("passed")]
	Passed,

	/// <summary>The published result does not match; the details say where.</summary>
	[JsonStringEnumMemberName("failed")]
	Failed,

	/// <summary>Verification never ran, e.g. because the run was interrupted first.</summary>
	[JsonStringEnumMemberName("not-run")]
	NotRun
}

/// <summary>One attempted step of an apply run and how it ended.</summary>
public sealed record LedgerAction
{
	/// <summary>What the plan wanted this step to do.</summary>
	public required PlanActionKind PlannedKind { get; init; }

	/// <summary>The S3 key the step worked on.</summary>
	public required string Key { get; init; }

	/// <summary>How the step ended.</summary>
	public required LedgerActionOutcome Outcome { get; init; }

	/// <summary>Extra detail when the outcome needs explaining, e.g. the error for a failed step.</summary>
	public string? Detail { get; init; }

	/// <summary>Adds a plain-English description of every problem in this action to <paramref name="problems"/>.</summary>
	public void Validate(IList<string> problems)
	{
		if (string.IsNullOrWhiteSpace(Key))
			problems.Add("A ledger action needs a non-empty key.");
		if (Outcome == LedgerActionOutcome.Failed && string.IsNullOrWhiteSpace(Detail))
			problems.Add("A failed ledger action needs a detail saying what went wrong.");
	}
}

/// <summary>The state one registry manifest ended up in after the run.</summary>
public sealed record RegistryRefresh
{
	/// <summary>The manifest's S3 key, e.g. <c>bundle/elasticsearch/registry.json</c>.</summary>
	public required string Key { get; init; }

	/// <summary>How refreshing this manifest ended.</summary>
	public required RegistryRefreshOutcome Outcome { get; init; }

	/// <summary>Extra detail when the outcome needs explaining.</summary>
	public string? Detail { get; init; }

	/// <summary>Adds a plain-English description of every problem in this refresh record to <paramref name="problems"/>.</summary>
	public void Validate(IList<string> problems)
	{
		if (string.IsNullOrWhiteSpace(Key))
			problems.Add("A registry refresh record needs a non-empty key.");
	}
}

/// <summary>What the post-apply verification concluded.</summary>
public sealed record VerificationResult
{
	/// <summary>The overall answer.</summary>
	public required VerificationOutcome Outcome { get; init; }

	/// <summary>Plain-English findings, e.g. each semantic difference when verification failed.</summary>
	public IReadOnlyList<string> Details { get; init; } = [];

	/// <summary>Adds a plain-English description of every problem in this result to <paramref name="problems"/>.</summary>
	public void Validate(IList<string> problems)
	{
		if (Outcome == VerificationOutcome.Failed && Details.Count == 0)
			problems.Add("A failed verification needs at least one detail saying what did not match.");
	}
}

/// <summary>
/// What actually happened when a plan was applied: every attempted step and its outcome,
/// what was created (with content hashes), what was skipped, what conflicted, how the
/// registries ended up, and whether verification passed. Written by the apply stage; read
/// by reruns (to resume an interrupted run safely — created keys are never re-attempted)
/// and by anyone auditing a run. Timestamps are recorded in UTC.
/// </summary>
public sealed record LedgerDocument : IBackfillDocument
{
	/// <inheritdoc />
	public static BackfillArtifactKind Kind => BackfillArtifactKind.Ledger;

	/// <summary>Hash of the plan this run executed, proving exactly which approved plan the outcomes belong to.</summary>
	public required string PlanHash { get; init; }

	/// <summary>The source repositories the run's inputs came from, pinned to exact commits.</summary>
	public required IReadOnlyList<PinnedSource> InputRefs { get; init; }

	/// <summary>Hash of the uploaded content for every created S3 key, so an auditor can verify the objects byte-for-byte.</summary>
	public required IReadOnlyDictionary<string, string> CreatedObjectHashes { get; init; }

	/// <summary>Every attempted step and how it ended, in the order they ran. Created, skipped, conflicting, and failed keys are all here.</summary>
	public required IReadOnlyList<LedgerAction> Actions { get; init; }

	/// <summary>The state each touched registry manifest ended up in.</summary>
	public IReadOnlyList<RegistryRefresh> RegistryState { get; init; } = [];

	/// <summary>What the post-apply verification concluded.</summary>
	public required VerificationResult Verification { get; init; }

	/// <summary>When the run started, in UTC.</summary>
	public required DateTimeOffset StartedAt { get; init; }

	/// <summary>When the run finished, in UTC. Null when the run was interrupted before finishing.</summary>
	public DateTimeOffset? FinishedAt { get; init; }

	/// <inheritdoc />
	public void Validate(IList<string> problems)
	{
		if (!BackfillHash.IsWellFormed(PlanHash))
			problems.Add($"The plan hash must look like sha256: plus 64 lower-case hex characters, but found '{PlanHash}'.");

		for (var i = 0; i < InputRefs.Count; i++)
		{
			var before = problems.Count;
			InputRefs[i].Validate(problems);
			ValidationProblems.PrefixNew(problems, before, $"input_refs[{i}]");
		}

		foreach (var (key, hash) in CreatedObjectHashes)
		{
			if (string.IsNullOrWhiteSpace(key))
				problems.Add("created_object_hashes: keys must be non-empty S3 keys.");
			if (!BackfillHash.IsWellFormed(hash))
				problems.Add($"created_object_hashes['{key}']: the hash must look like sha256: plus 64 lower-case hex characters, but found '{hash}'.");
		}

		for (var i = 0; i < Actions.Count; i++)
		{
			var before = problems.Count;
			Actions[i].Validate(problems);
			ValidationProblems.PrefixNew(problems, before, $"actions[{i}]");
		}

		for (var i = 0; i < RegistryState.Count; i++)
		{
			var before = problems.Count;
			RegistryState[i].Validate(problems);
			ValidationProblems.PrefixNew(problems, before, $"registry_state[{i}]");
		}

		var beforeVerification = problems.Count;
		Verification.Validate(problems);
		ValidationProblems.PrefixNew(problems, beforeVerification, "verification");

		if (StartedAt.Offset != TimeSpan.Zero)
			problems.Add($"The started-at timestamp must be in UTC, but found offset {StartedAt.Offset}.");
		if (FinishedAt is { } finished)
		{
			if (finished.Offset != TimeSpan.Zero)
				problems.Add($"The finished-at timestamp must be in UTC, but found offset {finished.Offset}.");
			if (finished < StartedAt)
				problems.Add("The run cannot finish before it started.");
		}
	}
}
