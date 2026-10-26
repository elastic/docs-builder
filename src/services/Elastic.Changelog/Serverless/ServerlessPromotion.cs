// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.RegularExpressions;

namespace Elastic.Changelog.Serverless;

/// <summary>A repository vendored as a git submodule of a service repository, whose PRs ship with the service.</summary>
/// <param name="Repository">Repository in the <c>elastic</c> org (for example <c>elasticsearch</c>).</param>
/// <param name="Path">Submodule path in the service repository's tree.</param>
public sealed record ServerlessSubmodule(string Repository, string Path);

/// <summary>A serverless service whose promotions publish date-keyed release notes.</summary>
/// <param name="Id">Service name as emitted by gpctl (for example <c>kibana</c>).</param>
/// <param name="Repository">Repository in the <c>elastic</c> org that owns the commit range and <c>docs/changelog.yml</c>.</param>
/// <param name="Profile">Bundle profile in each repository's <c>docs/changelog.yml</c>.</param>
/// <param name="VersionsPath">Path of the service's <c>versions.yaml</c> in <c>elastic/serverless-gitops</c>.</param>
/// <param name="Submodules">Repositories vendored as submodules; each gets its own bundle over the range of submodule commits.</param>
public sealed record ServerlessService(
	string Id,
	string Repository,
	string Profile,
	string VersionsPath,
	IReadOnlyList<ServerlessSubmodule> Submodules
);

/// <summary>A commit to a service's <c>versions.yaml</c> in serverless-gitops.</summary>
public sealed record GitopsCommit(string Sha, string Message);

/// <summary>How a walk of the gitops history ended.</summary>
public enum HistoryWalkStatus
{
	/// <summary>A previous final-slice version was found.</summary>
	Found,

	/// <summary>The whole history was read and holds no different final-slice version.</summary>
	NotFound,

	/// <summary>A request failed; the previous version is unknown.</summary>
	Failed,

	/// <summary>The page limit was reached before the history ended.</summary>
	CapReached
}

/// <summary>The result of <see cref="ServerlessPromotion.FindPreviousVersionAsync"/>.</summary>
public sealed record HistoryWalkResult(HistoryWalkStatus Status, string? PreviousVersion, int ScannedCommits);

/// <summary>Pure helpers for resolving a serverless promotion's commit range from serverless-gitops history.</summary>
public static partial class ServerlessPromotion
{
	/// <summary>Release notes cover pull requests merged into this branch. A pull request merged into a
	/// feature branch reaches the range when the branch merges, and the PR that merged the branch carries the note.</summary>
	public const string BaseBranch = "main";

	/// <summary>The last production slice of a rollout; release notes publish when it completes.</summary>
	public const string FinalSlice = "production-noncanary-ds-5";

	/// <summary>Services that can be bundled. An Elasticsearch release spans <c>elasticsearch-serverless</c>
	/// and its <c>elasticsearch</c> submodule, so it produces two bundles.</summary>
	public static IReadOnlyDictionary<string, ServerlessService> Services { get; } = new Dictionary<string, ServerlessService>(
		StringComparer.OrdinalIgnoreCase
	)
	{
		["kibana"] = new("kibana", "kibana", "serverless-release", "services/kibana/versions.yaml", []),
		["elasticsearch"] = new(
			"elasticsearch",
			"elasticsearch-serverless",
			"serverless-release",
			"services/elasticsearch/versions.yaml",
			[new ServerlessSubmodule("elasticsearch", "elasticsearch")]
		)
	};

	[GeneratedRegex(@"\bproduction-noncanary-ds-5\b")]
	private static partial Regex FinalSliceMessageRegex();

	[GeneratedRegex(@"^\s*production-noncanary-ds-5:\s*[""']?([0-9a-fA-F]{7,40})[""']?\s*$", RegexOptions.Multiline)]
	private static partial Regex FinalSliceVersionRegex();

	/// <summary>Whether a gitops commit promoted the final slice. Its message names the slices,
	/// for example <c>gitops: production-noncanary-ds-4,production-noncanary-ds-5 Artifact promotion for kibana to 7dd981dcd7d3</c>.
	/// Most commits to <c>versions.yaml</c> are dev, QA, or staging promotions.</summary>
	public static bool IsFinalSlicePromotion(string commitMessage) => FinalSliceMessageRegex().IsMatch(commitMessage.Split('\n', 2)[0]);

	/// <summary>Reads the <see cref="FinalSlice"/> version from a <c>versions.yaml</c>, or <c>null</c>.</summary>
	public static string? ParseFinalSliceVersion(string versionsYaml)
	{
		var match = FinalSliceVersionRegex().Match(versionsYaml);
		return match.Success ? match.Groups[1].Value : null;
	}

	/// <summary>Gitops stores 12-character hashes and callers may send a full SHA in any case,
	/// so two refs are the same when their first 12 characters match, case-insensitively.</summary>
	public static bool SameRef(string a, string b)
	{
		static string Short(string s) => s.Length > 12 ? s[..12] : s;

		return string.Equals(Short(a), Short(b), StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Walks final-slice versions from newest to oldest and returns the first one that differs from
	/// <paramref name="currentVersion"/>: the previously published endpoint ref.
	/// </summary>
	public static string? FindPreviousVersion(IEnumerable<string?> finalSliceVersionsNewestFirst, string currentVersion) =>
		finalSliceVersionsNewestFirst.FirstOrDefault(v => !string.IsNullOrEmpty(v) && !SameRef(v, currentVersion));

	/// <summary>Maximum pages of gitops history to read (100 commits each). Most commits to
	/// <c>versions.yaml</c> are dev promotions, so a quiet week is hundreds of commits.</summary>
	public const int MaxHistoryPages = 100;

	/// <summary>
	/// Walks the commit history of a service's <c>versions.yaml</c>, newest first, and returns the first
	/// <see cref="FinalSlice"/> version that differs from <paramref name="currentVersion"/>.
	/// A failed request is a failure of the walk: skipping a candidate would silently pick an older
	/// version, and bundle the wrong commit range.
	/// </summary>
	/// <param name="getCommitsPage">Returns the commits of a 1-based page, or <c>null</c> when the request failed.
	/// An empty page ends the history.</param>
	/// <param name="getVersionsYaml">Returns the file content at a commit, or <c>null</c> when the request failed.</param>
	public static async Task<HistoryWalkResult> FindPreviousVersionAsync(
		Func<int, Task<IReadOnlyList<GitopsCommit>?>> getCommitsPage,
		Func<string, Task<string?>> getVersionsYaml,
		string currentVersion,
		int maxPages = MaxHistoryPages
	)
	{
		var scanned = 0;
		for (var page = 1; page <= maxPages; page++)
		{
			var commits = await getCommitsPage(page);
			if (commits is null)
				return new HistoryWalkResult(HistoryWalkStatus.Failed, null, scanned);
			if (commits.Count == 0)
				return new HistoryWalkResult(HistoryWalkStatus.NotFound, null, scanned);
			scanned += commits.Count;

			foreach (var commit in commits.Where(c => IsFinalSlicePromotion(c.Message)))
			{
				var yaml = await getVersionsYaml(commit.Sha);
				if (yaml is null)
					return new HistoryWalkResult(HistoryWalkStatus.Failed, null, scanned);

				var version = ParseFinalSliceVersion(yaml);
				if (version is not null && !SameRef(version, currentVersion))
					return new HistoryWalkResult(HistoryWalkStatus.Found, version, scanned);
			}
		}

		return new HistoryWalkResult(HistoryWalkStatus.CapReached, null, scanned);
	}

	/// <summary>Reads a submodule's pinned commit from a GitHub git-tree response
	/// (<c>GET /repos/{owner}/{repo}/git/trees/{sha}</c>): the entry with the given path and type <c>commit</c>.</summary>
	public static string? FindSubmoduleSha(string treeJson, string submodulePath)
	{
		using var doc = JsonDocument.Parse(treeJson);
		if (!doc.RootElement.TryGetProperty("tree", out var tree))
			return null;
		foreach (var entry in tree.EnumerateArray())
		{
			if (entry.GetProperty("path").GetString() == submodulePath && entry.GetProperty("type").GetString() == "commit")
				return entry.GetProperty("sha").GetString();
		}
		return null;
	}
}
