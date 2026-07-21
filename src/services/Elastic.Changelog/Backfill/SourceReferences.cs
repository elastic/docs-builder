// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.Backfill;

/// <summary>A GitHub repository, named by its owner (organization or user) and repository name.</summary>
public sealed record GitRepository
{
	/// <summary>The GitHub owner, e.g. <c>elastic</c>.</summary>
	public required string Owner { get; init; }

	/// <summary>The repository name, e.g. <c>elasticsearch</c>.</summary>
	public required string Name { get; init; }

	/// <summary>Adds a plain-English description of every problem in this repository name to <paramref name="problems"/>.</summary>
	public void Validate(IList<string> problems)
	{
		if (string.IsNullOrWhiteSpace(Owner))
			problems.Add("A repository needs a non-empty owner.");
		if (string.IsNullOrWhiteSpace(Name))
			problems.Add("A repository needs a non-empty name.");
	}
}

/// <summary>
/// A repository pinned to one exact commit. Plans and ledgers record these so a run can
/// be reproduced (or audited) later even after the branch or tag has moved on: the
/// human-friendly ref says what was asked for, the commit says exactly what was used.
/// </summary>
public sealed record PinnedSource
{
	/// <summary>The repository the content came from.</summary>
	public required GitRepository Repository { get; init; }

	/// <summary>The ref that was requested, e.g. <c>main</c> or <c>v9.0.0</c>.</summary>
	public required string GitRef { get; init; }

	/// <summary>The full 40-character commit SHA the ref pointed at when the document was produced.</summary>
	public required string Commit { get; init; }

	/// <summary>Adds a plain-English description of every problem in this pinned source to <paramref name="problems"/>.</summary>
	public void Validate(IList<string> problems)
	{
		Repository.Validate(problems);
		if (string.IsNullOrWhiteSpace(GitRef))
			problems.Add("A pinned source needs a non-empty git ref.");
		if (!IsFullCommitSha(Commit))
			problems.Add($"A pinned source needs a full 40-character commit SHA, but found '{Commit}'.");
	}

	private static bool IsFullCommitSha(string? value)
	{
		if (value is null || value.Length != 40)
			return false;

		foreach (var c in value)
		{
			if (c is (< '0' or > '9') and (< 'a' or > 'f'))
				return false;
		}
		return true;
	}
}

/// <summary>
/// Where in a source file something was found: the file path (relative to its repository
/// root) and, when known, the line range. Lets a human jump from a parsed entry or a
/// triage message straight to the text it came from.
/// </summary>
public sealed record SourceLocation
{
	/// <summary>File path relative to the repository root, e.g. <c>docs/release-notes/index.md</c>.</summary>
	public required string Path { get; init; }

	/// <summary>First line of the relevant text (1-based), when known.</summary>
	public int? StartLine { get; init; }

	/// <summary>Last line of the relevant text (1-based, inclusive), when known.</summary>
	public int? EndLine { get; init; }

	/// <summary>Adds a plain-English description of every problem in this location to <paramref name="problems"/>.</summary>
	public void Validate(IList<string> problems)
	{
		if (string.IsNullOrWhiteSpace(Path))
			problems.Add("A source location needs a non-empty path.");
		if (StartLine is < 1)
			problems.Add($"A source location's start line must be 1 or greater, but found {StartLine}.");
		if (EndLine is < 1)
			problems.Add($"A source location's end line must be 1 or greater, but found {EndLine}.");
		if (StartLine is { } start && EndLine is { } end && end < start)
			problems.Add($"A source location's end line ({end}) cannot come before its start line ({start}).");
	}
}
