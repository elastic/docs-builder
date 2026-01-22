// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.GithubRelease;

/// <summary>
/// Input data for creating changelogs from a GitHub release
/// </summary>
public class GitHubReleaseInput
{
	/// <summary>
	/// Repository in owner/repo format (e.g., "elastic/elasticsearch")
	/// </summary>
	public required string Repository { get; init; }

	/// <summary>
	/// Version tag or "latest" (defaults to "latest")
	/// </summary>
	public string Version { get; init; } = "latest";

	/// <summary>
	/// Path to changelog.yml configuration file (optional)
	/// </summary>
	public string? Config { get; init; }

	/// <summary>
	/// Output directory for changelog files (optional, defaults to ./changelogs)
	/// </summary>
	public string? Output { get; init; }

	/// <summary>
	/// Whether to strip [prefix] from PR titles
	/// </summary>
	public bool StripTitlePrefix { get; init; }

	/// <summary>
	/// Whether to warn when Release Drafter type doesn't match label-derived type (defaults to true)
	/// </summary>
	public bool WarnOnTypeMismatch { get; init; } = true;
}
