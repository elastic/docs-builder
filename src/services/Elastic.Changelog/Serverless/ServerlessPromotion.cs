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

/// <summary>Pure helpers for resolving a serverless promotion's commit range from serverless-gitops history.</summary>
public static partial class ServerlessPromotion
{
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
