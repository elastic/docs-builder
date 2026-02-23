// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.GitHub;

/// <summary>
/// Service interface for fetching pull request and issue information from GitHub
/// </summary>
public interface IGitHubPrService
{
	/// <summary>
	/// Fetches pull request information from GitHub
	/// </summary>
	/// <param name="prUrl">The PR URL (e.g., https://github.com/owner/repo/pull/123, owner/repo#123, or just a number if owner/repo are provided)</param>
	/// <param name="owner">Optional: GitHub repository owner (used when prUrl is just a number)</param>
	/// <param name="repo">Optional: GitHub repository name (used when prUrl is just a number)</param>
	/// <param name="ctx">Cancellation token</param>
	/// <returns>PR information or null if fetch fails</returns>
	Task<GitHubPrInfo?> FetchPrInfoAsync(string prUrl, string? owner = null, string? repo = null, CancellationToken ctx = default);

	/// <summary>
	/// Fetches issue information from GitHub
	/// </summary>
	/// <param name="issueUrl">The issue URL (e.g., https://github.com/owner/repo/issues/123, owner/repo#123, or just a number if owner/repo are provided)</param>
	/// <param name="owner">Optional: GitHub repository owner (used when issueUrl is just a number)</param>
	/// <param name="repo">Optional: GitHub repository name (used when issueUrl is just a number)</param>
	/// <param name="ctx">Cancellation token</param>
	/// <returns>Issue information or null if fetch fails</returns>
	Task<GitHubIssueInfo?> FetchIssueInfoAsync(string issueUrl, string? owner = null, string? repo = null, CancellationToken ctx = default);
}
