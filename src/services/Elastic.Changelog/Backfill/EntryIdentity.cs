// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Elastic.Changelog.Backfill;

/// <summary>The ways a changelog entry can be identified. See <see cref="EntryIdentity"/>.</summary>
public enum EntryIdentityKind
{
	/// <summary>Identified by the pull request that made the change (the strongest common identity).</summary>
	[JsonStringEnumMemberName("pull-request")]
	PullRequest,

	/// <summary>Identified by an issue, used when the source explicitly treats the issue as the entry's primary reference.</summary>
	[JsonStringEnumMemberName("issue")]
	Issue,

	/// <summary>
	/// Identified by a made-up but stable file name plus a checksum of the entry's content.
	/// Used when no PR or issue is available; also what bundle amend files match on when
	/// retracting a previously published entry.
	/// </summary>
	[JsonStringEnumMemberName("synthetic-file")]
	SyntheticFile
}

/// <summary>
/// The stable identity of one changelog entry — the answer to "is this the same entry
/// we already have?". Used for de-duplication, for matching a highlights mention to its
/// entry, and for reconciling against entries already published in live bundles.
/// Exactly one of the identity fields is set, matching <see cref="Kind"/>: a canonical
/// GitHub URL for <see cref="EntryIdentityKind.PullRequest"/> and
/// <see cref="EntryIdentityKind.Issue"/>, or a name-plus-checksum pair for
/// <see cref="EntryIdentityKind.SyntheticFile"/>.
/// </summary>
public sealed record EntryIdentity
{
	/// <summary>Which way this entry is identified.</summary>
	public required EntryIdentityKind Kind { get; init; }

	/// <summary>
	/// The canonical GitHub URL, e.g. <c>https://github.com/elastic/kibana/pull/12345</c>.
	/// Always the full URL — never a bare number, because a bare number is only meaningful
	/// relative to some repository. Set for PR and issue identities, null for synthetic files.
	/// </summary>
	public string? Url { get; init; }

	/// <summary>The stable name and checksum. Set for synthetic-file identities, null otherwise.</summary>
	public SyntheticFileIdentity? File { get; init; }

	/// <summary>Builds a pull-request identity with the canonical URL for the given repository and PR number.</summary>
	public static EntryIdentity ForPullRequest(string owner, string repository, int number) =>
		new() { Kind = EntryIdentityKind.PullRequest, Url = $"https://github.com/{owner}/{repository}/pull/{number}" };

	/// <summary>Builds an issue identity with the canonical URL for the given repository and issue number.</summary>
	public static EntryIdentity ForIssue(string owner, string repository, int number) =>
		new() { Kind = EntryIdentityKind.Issue, Url = $"https://github.com/{owner}/{repository}/issues/{number}" };

	/// <summary>Builds a synthetic-file identity from a stable name and a checksum of the entry's content.</summary>
	public static EntryIdentity ForFile(string name, string checksum) =>
		new() { Kind = EntryIdentityKind.SyntheticFile, File = new SyntheticFileIdentity { Name = name, Checksum = checksum } };

	/// <summary>Adds a plain-English description of every problem in this identity to <paramref name="problems"/>.</summary>
	public void Validate(IList<string> problems)
	{
		switch (Kind)
		{
			case EntryIdentityKind.PullRequest:
				if (File is not null)
					problems.Add("A pull-request identity must not carry a file block.");
				if (Url is null || !CanonicalGitHubUrls.IsPullRequestUrl(Url))
					problems.Add($"A pull-request identity needs a canonical URL like https://github.com/{{owner}}/{{repo}}/pull/{{number}}, but found '{Url}'.");
				break;
			case EntryIdentityKind.Issue:
				if (File is not null)
					problems.Add("An issue identity must not carry a file block.");
				if (Url is null || !CanonicalGitHubUrls.IsIssueUrl(Url))
					problems.Add($"An issue identity needs a canonical URL like https://github.com/{{owner}}/{{repo}}/issues/{{number}}, but found '{Url}'.");
				break;
			case EntryIdentityKind.SyntheticFile:
				if (Url is not null)
					problems.Add("A synthetic-file identity must not carry a URL.");
				if (File is null)
					problems.Add("A synthetic-file identity needs a file block with a name and checksum.");
				else
					File.Validate(problems);
				break;
			default:
				problems.Add($"Unknown identity kind '{Kind}'.");
				break;
		}
	}
}

/// <summary>
/// A made-up but stable file name plus a checksum of the entry's content. Entries created
/// by the backfill never exist as real files, but they still carry this block because
/// published bundles can only be corrected by amend files that match entries on exactly
/// these two fields.
/// </summary>
public sealed record SyntheticFileIdentity
{
	/// <summary>The stable file name, e.g. <c>backfill-elasticsearch-9.0.0-0001.yaml</c>. Derived from the entry's identity so reruns produce the same name.</summary>
	public required string Name { get; init; }

	/// <summary>Checksum of the entry's canonical content, so a changed entry is never mistaken for the original.</summary>
	public required string Checksum { get; init; }

	/// <summary>Adds a plain-English description of every problem in this file identity to <paramref name="problems"/>.</summary>
	public void Validate(IList<string> problems)
	{
		if (string.IsNullOrWhiteSpace(Name))
			problems.Add("A synthetic file identity needs a non-empty name.");
		if (string.IsNullOrWhiteSpace(Checksum))
			problems.Add("A synthetic file identity needs a non-empty checksum.");
	}
}

/// <summary>
/// Recognizes canonical GitHub PR and issue URLs. The backfill always stores full URLs
/// (never bare numbers) because a bare number is ambiguous across repositories and the
/// scrubber's link allowlist matches on URLs.
/// </summary>
public static partial class CanonicalGitHubUrls
{
	[GeneratedRegex(@"^https://github\.com/[A-Za-z0-9-]+/[A-Za-z0-9._-]+/pull/[1-9][0-9]*$")]
	private static partial Regex PullRequestUrl();

	[GeneratedRegex(@"^https://github\.com/[A-Za-z0-9-]+/[A-Za-z0-9._-]+/issues/[1-9][0-9]*$")]
	private static partial Regex IssueUrl();

	/// <summary>True when <paramref name="url"/> is a canonical PR URL like <c>https://github.com/elastic/kibana/pull/12345</c>.</summary>
	public static bool IsPullRequestUrl(string url) => PullRequestUrl().IsMatch(url);

	/// <summary>True when <paramref name="url"/> is a canonical issue URL like <c>https://github.com/elastic/kibana/issues/12345</c>.</summary>
	public static bool IsIssueUrl(string url) => IssueUrl().IsMatch(url);
}
