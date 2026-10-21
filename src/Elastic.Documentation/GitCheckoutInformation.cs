// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Documentation;

/// <summary>The git checkout a build ran in: branch, remote and ref (and the GitHub ref when known).</summary>
/// <remarks>
/// Constructed with <c>GitCheckoutInformation.Resolved</c>, <c>ForClonedRepository</c> or <c>ForElasticRepository</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
public record GitCheckoutInformation
{
	public static GitCheckoutInformation Unavailable { get; } = new()
	{
		Branch = "unavailable",
		Remote = "unavailable",
		Ref = "unavailable",
		RepositoryName = "unavailable"
	};

	/// <summary>
	/// The canned checkout tests that seed no layout get from <c>GitCheckoutInformationFactory</c> (a mock filesystem with no <c>.git</c>), so
	/// they do not each have to build one.
	/// </summary>
	public static GitCheckoutInformation LegacyTestCheckout { get; } = new()
	{
		Branch = "test-e35fcb27-5f60-4e",
		Remote = "elastic/docs-builder",
		Ref = "e35fcb27-5f60-4e",
		RepositoryName = "docs-builder"
	};

	/// <summary>
	/// The checkout read from a <c>.git</c> folder: the repository is named by the last segment of <paramref name="remote"/>, and an empty
	/// <paramref name="gitHubRef"/> (the <c>GITHUB_REF</c> of a build outside CI) is no ref.
	/// </summary>
	public static GitCheckoutInformation Resolved(string gitRef, string branch, string remote, string? gitHubRef) =>
		new()
		{
			Ref = gitRef,
			Branch = branch,
			Remote = remote,
			RepositoryName = remote.Split('/').Last(),
			GitHubRef = string.IsNullOrEmpty(gitHubRef) ? null : gitHubRef
		};

	/// <summary>The checkout of an elastic repository an assembler build cloned: its remote is <c>elastic/{repositoryName}</c>.</summary>
	public static GitCheckoutInformation ForElasticRepository(string repositoryName, string gitRef, string branch) =>
		new() { RepositoryName = repositoryName, Ref = gitRef, Remote = $"elastic/{repositoryName}", Branch = branch };

	/// <summary>The checkout of a repository a codex build cloned, named and located as its reference says.</summary>
	public static GitCheckoutInformation ForClonedRepository(
		string repositoryName,
		string remote,
		string branch,
		string commitHash,
		string? gitHubRef
	) => new() { Branch = branch, Remote = remote, Ref = commitHash, RepositoryName = repositoryName, GitHubRef = gitHubRef };

	[JsonPropertyName("branch")]
	public required string Branch { get; init; }

	[JsonPropertyName("remote")]
	public required string Remote { get; init; }

	[JsonPropertyName("ref")]
	public required string Ref { get; init; }

	[JsonPropertyName("name")]
	[JsonInclude]
	public string RepositoryName { get; internal init; } = "unavailable";

	/// <summary>Whether git checkout information was resolved (false for the <see cref="Unavailable"/> sentinel).</summary>
	[JsonIgnore]
	public bool IsAvailable => RepositoryName != Unavailable.RepositoryName;

	/// <summary>
	/// The full git ref from GitHub Actions (e.g. refs/pull/123/merge). Set from GITHUB_REF when running in CI.
	/// </summary>
	[JsonPropertyName("github_ref")]
	[JsonInclude]
	public string? GitHubRef { get; internal init; }

	/// <summary>
	/// The GitHub repository in <c>org/repo</c> format, derived from the git remote URL.
	/// Returns <see langword="null"/> when the remote is unavailable or cannot be resolved
	/// to a valid GitHub <c>org/repo</c> path. Callers should skip GitHub links when null.
	/// </summary>
	[JsonIgnore]
	public string? GitHubRepository => Remote is "elastic/docs-builder-unknown" ? null : ExtractGitHubOrgRepo(Remote);

	/// <summary>Extracts a validated <c>org/repo</c> path from a GitHub remote URL, or returns <c>null</c>.</summary>
	/// <remarks>
	/// Handles the common remote formats:
	/// <list type="bullet">
	///   <item><c>https://github.com/org/repo.git</c></item>
	///   <item><c>git@github.com:org/repo.git</c></item>
	///   <item><c>ssh://git@github.com/org/repo.git</c></item>
	///   <item><c>org/repo</c> (bare, e.g. from <c>GITHUB_REPOSITORY</c>)</item>
	/// </list>
	/// </remarks>
	private static string? ExtractGitHubOrgRepo(string? remote)
	{
		if (string.IsNullOrEmpty(remote) || remote == "unavailable")
			return null;

		var path = NormalizeToGitHubPath(remote);
		if (path is null)
			return null;

		if (path.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
			path = path[..^4];

		var parts = path.Split('/');
		if (parts.Length != 2 || string.IsNullOrEmpty(parts[0]) || string.IsNullOrEmpty(parts[1]))
			return null;

		return path;
	}

	/// <summary>Normalises the remote to the <c>org/repo[.git]</c> path portion, or <c>null</c> if not a GitHub remote.</summary>
	private static string? NormalizeToGitHubPath(string remote)
	{
		const string githubHost = "github.com";

		if (remote.StartsWith("git@github.com:", StringComparison.OrdinalIgnoreCase))
			return remote["git@github.com:".Length..].TrimStart('/');

		if (remote.StartsWith("ssh://git@github.com/", StringComparison.OrdinalIgnoreCase))
			return remote["ssh://git@github.com/".Length..].TrimStart('/');

		if (remote.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase))
			return remote["https://github.com/".Length..].TrimStart('/');
		if (remote.StartsWith("http://github.com/", StringComparison.OrdinalIgnoreCase))
			return remote["http://github.com/".Length..].TrimStart('/');

		if (!remote.Contains("://") && !remote.Contains('@') && !remote.Contains(githubHost, StringComparison.OrdinalIgnoreCase))
			return remote.TrimStart('/');

		return null;
	}
}
