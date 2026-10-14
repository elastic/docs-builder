// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.GitHub;

/// <summary>
/// Arguments for resolving the pull requests contained in a git commit range.
/// </summary>
public record CommitRangeArguments
{
	/// <summary>GitHub repository owner (org).</summary>
	public required string Owner { get; init; }

	/// <summary>GitHub repository name.</summary>
	public required string Repo { get; init; }

	/// <summary>Start ref (exclusive) of the range, e.g. the previously published endpoint ref.</summary>
	public required string StartRef { get; init; }

	/// <summary>End ref (inclusive) of the range, e.g. the currently published endpoint ref.</summary>
	public required string EndRef { get; init; }

	/// <summary>
	/// When set, only pull requests whose base branch is this branch are resolved. Pull requests
	/// merged into another branch (for example a feature branch that later merged into the
	/// integration branch) are reported in <see cref="CommitRangeResolution.ExcludedPullRequests"/>
	/// instead. <c>null</c> keeps every merged pull request.
	/// </summary>
	public string? BaseRef { get; init; }

	/// <summary>
	/// Pull requests that carry any of these labels are left out and reported in
	/// <see cref="CommitRangeResolution.ExcludedPullRequests"/>, unless they also carry a label in
	/// <see cref="KeepLabels"/>. Labels are compared case-insensitively. <c>null</c> or empty keeps every pull request
	/// and skips fetching labels.
	/// </summary>
	public IReadOnlyList<string>? ExcludeLabels { get; init; }

	/// <summary>
	/// A pull request that carries any of these labels is never excluded by <see cref="ExcludeLabels"/>.
	/// With <c>exclude: stateful</c> and <c>keep: serverless</c>, a pull request labelled with both stays in.
	/// </summary>
	public IReadOnlyList<string>? KeepLabels { get; init; }
}

/// <summary>Why a merged pull request was left out of a range.</summary>
public enum CommitRangeExclusionReason
{
	/// <summary>Merged into a branch other than <see cref="CommitRangeArguments.BaseRef"/>.</summary>
	BaseBranch,

	/// <summary>Carries a label in <see cref="CommitRangeArguments.ExcludeLabels"/>.</summary>
	Label
}

/// <summary>A merged pull request left out of a range, because of its base branch or one of its labels.</summary>
public record CommitRangeExcludedPullRequest
{
	/// <summary>Pull request number in the repository the range was resolved against.</summary>
	public required int Number { get; init; }

	/// <summary>Canonical https://github.com/{owner}/{repo}/pull/{number} URL.</summary>
	public required string Url { get; init; }

	/// <summary>The pull request's base branch.</summary>
	public required string BaseRef { get; init; }

	/// <summary>Why the pull request was left out.</summary>
	public CommitRangeExclusionReason Reason { get; init; } = CommitRangeExclusionReason.BaseBranch;

	/// <summary>The label that excluded the pull request, when <see cref="Reason"/> is <see cref="CommitRangeExclusionReason.Label"/>.</summary>
	public string? Label { get; init; }
}

/// <summary>
/// A pull request resolved from a commit range, with the range commits that led to it.
/// </summary>
public record CommitRangePullRequest
{
	/// <summary>Pull request number in the repository the range was resolved against.</summary>
	public required int Number { get; init; }

	/// <summary>Canonical https://github.com/{owner}/{repo}/pull/{number} URL.</summary>
	public required string Url { get; init; }

	/// <summary>Shas of the range commits associated with this pull request (provenance).</summary>
	public required IReadOnlyList<string> CommitShas { get; init; }
}

/// <summary>
/// The pull requests contained in a commit range, plus the commits that could not be
/// attributed to any merged pull request (reported, never silently dropped).
/// </summary>
public record CommitRangeResolution
{
	/// <summary>Total commits reported by the compare API for the range.</summary>
	public required int TotalCommits { get; init; }

	/// <summary>
	/// Merged pull requests in the range, de-duplicated, ordered by first appearance
	/// in the commit range (oldest commit first).
	/// </summary>
	public required IReadOnlyList<CommitRangePullRequest> PullRequests { get; init; }

	/// <summary>Shas of range commits with no associated merged pull request.</summary>
	public required IReadOnlyList<string> CommitsWithoutPullRequest { get; init; }

	/// <summary>Merged pull requests excluded because of <see cref="CommitRangeArguments.BaseRef"/> or <see cref="CommitRangeArguments.ExcludeLabels"/>.</summary>
	public IReadOnlyList<CommitRangeExcludedPullRequest> ExcludedPullRequests { get; init; } = [];
}

/// <summary>
/// Resolves the merged pull requests contained in a git commit range
/// (<c>start..end</c>) of a GitHub repository.
/// </summary>
public interface IGitHubCommitRangeService
{
	/// <summary>
	/// Enumerates the commits in <c>start..end</c> and resolves each to its merged pull request.
	/// Returns <c>null</c> after emitting an error when the range cannot be resolved
	/// (unknown refs, missing credentials, API failures).
	/// </summary>
	Task<CommitRangeResolution?> ResolvePullRequestsAsync(IDiagnosticsCollector collector, CommitRangeArguments args, Cancel ctx);
}
