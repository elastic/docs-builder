// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.GitHub;

/// <summary>
/// Information about a GitHub release
/// </summary>
public record GitHubReleaseInfo
{
	/// <summary>
	/// The git tag name for this release (e.g., "v1.0.0")
	/// </summary>
	public string TagName { get; init; } = "";

	/// <summary>
	/// The release title/name
	/// </summary>
	public string Name { get; init; } = "";

	/// <summary>
	/// The release body containing release notes (markdown)
	/// </summary>
	public string Body { get; init; } = "";

	/// <summary>
	/// Whether this is marked as a prerelease
	/// </summary>
	public bool Prerelease { get; init; }

	/// <summary>
	/// Whether this is a draft release
	/// </summary>
	public bool Draft { get; init; }

	/// <summary>
	/// The URL to the release page on GitHub
	/// </summary>
	public string HtmlUrl { get; init; } = "";
}

/// <summary>
/// Service interface for fetching release information from GitHub
/// </summary>
public interface IGitHubReleaseService
{
	/// <summary>
	/// Fetches release information from GitHub
	/// </summary>
	/// <param name="owner">Repository owner</param>
	/// <param name="repo">Repository name</param>
	/// <param name="version">Version tag or "latest" (null defaults to latest)</param>
	/// <param name="ctx">Cancellation token</param>
	/// <returns>Release information or null if fetch fails</returns>
	Task<GitHubReleaseInfo?> FetchReleaseAsync(
		string owner,
		string repo,
		string? version,
		CancellationToken ctx = default);
}
