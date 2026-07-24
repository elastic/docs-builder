// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Changelog.Backfill;

/// <summary>What an override does to the value it targets.</summary>
public enum OverrideOperation
{
	/// <summary>Replace the value (or add it when it was missing).</summary>
	[JsonStringEnumMemberName("set")]
	Set,

	/// <summary>Remove the value entirely.</summary>
	[JsonStringEnumMemberName("remove")]
	Remove
}

/// <summary>
/// Which slice of the backfill an override applies to: always a product, optionally
/// narrowed to one contributing repository and/or one release target.
/// </summary>
public sealed record OverrideScope
{
	/// <summary>The product the override applies to.</summary>
	public required string Product { get; init; }

	/// <summary>The contributing repository, when the override only applies to one. Null means all repositories.</summary>
	public GitRepository? Repository { get; init; }

	/// <summary>The release target (e.g. <c>9.0.0</c>), when the override only applies to one. Null means all targets.</summary>
	public string? Target { get; init; }

	/// <summary>Adds a plain-English description of every problem in this scope to <paramref name="problems"/>.</summary>
	public void Validate(IList<string> problems)
	{
		if (string.IsNullOrWhiteSpace(Product))
			problems.Add("An override scope needs a non-empty product.");
		Repository?.Validate(problems);
	}
}

/// <summary>
/// One manual correction: "in this scope, set (or remove) this field, and here is why".
/// Overrides live in their own document instead of being edits to generated output, so a
/// rerun of the pipeline reproduces the same corrected result instead of losing the fix.
/// </summary>
public sealed record BackfillOverride
{
	/// <summary>A stable, unique ID for this override, so inventories and plans can say which overrides they applied.</summary>
	public required string Id { get; init; }

	/// <summary>Which slice of the backfill this override applies to.</summary>
	public required OverrideScope Scope { get; init; }

	/// <summary>
	/// Plain path to the field being corrected, e.g. <c>entries[3].title</c> or
	/// <c>release_date</c>. Interpreted by the planning stage against the scoped document.
	/// </summary>
	public required string Path { get; init; }

	/// <summary>Whether the override sets a new value or removes the existing one.</summary>
	public required OverrideOperation Operation { get; init; }

	/// <summary>The replacement value, as text. Required when <see cref="Operation"/> is <see cref="OverrideOperation.Set"/>; must be absent for removals.</summary>
	public string? Value { get; init; }

	/// <summary>Why this correction is right — required, because a correction nobody can explain later is a liability.</summary>
	public required string Reason { get; init; }

	/// <summary>Adds a plain-English description of every problem in this override to <paramref name="problems"/>.</summary>
	public void Validate(IList<string> problems)
	{
		if (string.IsNullOrWhiteSpace(Id))
			problems.Add("An override needs a non-empty id.");
		Scope.Validate(problems);
		if (string.IsNullOrWhiteSpace(Path))
			problems.Add("An override needs a non-empty path saying which field it corrects.");
		if (string.IsNullOrWhiteSpace(Reason))
			problems.Add("An override needs a non-empty reason.");
		if (Operation == OverrideOperation.Set && Value is null)
			problems.Add("An override that sets a value needs the value to set.");
		if (Operation == OverrideOperation.Remove && Value is not null)
			problems.Add("An override that removes a value must not also carry a value.");
	}
}

/// <summary>
/// Manual corrections an operator feeds into planning, each with a reason attached.
/// Written by a human, read by the planning stage. Kept separate from all generated
/// documents so reruns stay reproducible: the pipeline output is always
/// "generated result + these corrections", never a hand-edited file.
/// </summary>
public sealed record OverridesDocument : IBackfillDocument
{
	/// <inheritdoc />
	public static BackfillArtifactKind Kind => BackfillArtifactKind.Overrides;

	/// <summary>The corrections, in no particular order. IDs must be unique.</summary>
	public required IReadOnlyList<BackfillOverride> Overrides { get; init; }

	/// <inheritdoc />
	public void Validate(IList<string> problems)
	{
		var seenIds = new HashSet<string>(StringComparer.Ordinal);
		for (var i = 0; i < Overrides.Count; i++)
		{
			var before = problems.Count;
			Overrides[i].Validate(problems);
			ValidationProblems.PrefixNew(problems, before, $"overrides[{i}]");

			var id = Overrides[i].Id;
			if (!string.IsNullOrWhiteSpace(id) && !seenIds.Add(id))
				problems.Add($"overrides[{i}]: The id '{id}' is used by more than one override; ids must be unique.");
		}
	}
}
