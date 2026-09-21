// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.GitHub;

/// <summary>
/// A single downloadable asset attached to a GitHub release
/// </summary>
public record GitHubReleaseAsset
{
	/// <summary>
	/// The asset's file name (e.g., "changelog-scrubber-allowlist.json")
	/// </summary>
	public required string Name { get; init; }

	/// <summary>
	/// Direct download URL for the asset's content
	/// </summary>
	public required string BrowserDownloadUrl { get; init; }
}

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

	/// <summary>
	/// The date and time when this release was published on GitHub
	/// </summary>
	public DateTimeOffset? PublishedAt { get; init; }

	/// <summary>
	/// The downloadable assets attached to this release
	/// </summary>
	public IReadOnlyList<GitHubReleaseAsset> Assets { get; init; } = [];
}

/// <summary>
/// Result of a previous-release tag lookup.
/// Distinguishes a confirmed "no predecessor" (first release) from an indeterminate API failure.
/// </summary>
public sealed record PreviousTagResult
{
	private PreviousTagResult() { }

	/// <summary>The predecessor tag name. Non-null only when <see cref="Status"/> is <see cref="PreviousTagStatus.Found"/>.</summary>
	public string? Tag { get; private init; }

	/// <summary>The outcome of the lookup.</summary>
	public PreviousTagStatus Status { get; private init; }

	/// <summary>Predecessor found.</summary>
	public static PreviousTagResult Found(string tag) => new() { Tag = tag, Status = PreviousTagStatus.Found };

	/// <summary>All APIs scanned successfully; no predecessor exists in the same release line.</summary>
	public static PreviousTagResult FirstRelease { get; } = new() { Status = PreviousTagStatus.FirstRelease };

	/// <summary>An API or transport failure prevented a definitive answer; result is indeterminate.</summary>
	public static PreviousTagResult LookupFailed { get; } = new() { Status = PreviousTagStatus.LookupFailed };
}

/// <summary>Outcome of a <see cref="PreviousTagResult"/> lookup.</summary>
public enum PreviousTagStatus
{
	Found,
	FirstRelease,
	LookupFailed
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
	Task<GitHubReleaseInfo?> FetchReleaseAsync(string owner, string repo, string? version, CancellationToken ctx = default);

	/// <summary>
	/// Fetches the most recent releases from GitHub, newest first
	/// </summary>
	/// <param name="owner">Repository owner</param>
	/// <param name="repo">Repository name</param>
	/// <param name="count">Maximum number of releases to fetch</param>
	/// <param name="ctx">Cancellation token</param>
	/// <returns>The releases, or an empty list if the fetch fails</returns>
	Task<IReadOnlyList<GitHubReleaseInfo>> FetchReleasesAsync(string owner, string repo, int count, CancellationToken ctx = default);

	/// <summary>
	/// Determines the tag name of the release immediately preceding <paramref name="currentTag"/> in the same
	/// release line (same prefix and semver major version).
	/// <para>
	/// Uses <c>POST /repos/{owner}/{repo}/releases/generate-notes</c> as the primary path — GitHub's own
	/// algorithm handles interleaved multi-version histories. Falls back to paginating the releases list
	/// (compatible with <c>contents: read</c> tokens) when generate-notes requires higher permissions.
	/// </para>
	/// <para>
	/// When the tag carries a prefix (e.g. <c>agent-v1.2.0</c>), only releases sharing that prefix are
	/// considered. When the tag is semver, only releases with the same major version are considered.
	/// </para>
	/// </summary>
	/// <returns>
	/// <see cref="PreviousTagResult.Found"/> when a predecessor tag was found,
	/// <see cref="PreviousTagResult.FirstRelease"/> when the scans completed successfully but no predecessor
	/// exists in the same release line, or <see cref="PreviousTagResult.LookupFailed"/> when an API or
	/// transport failure prevented a definitive answer.
	/// </returns>
	Task<PreviousTagResult> FetchPreviousTagAsync(string owner, string repo, string currentTag, CancellationToken ctx = default);

	/// <summary>
	/// Downloads a release asset's content as text
	/// </summary>
	/// <param name="asset">The asset to download</param>
	/// <param name="ctx">Cancellation token</param>
	/// <returns>The asset content, or null if the download fails</returns>
	Task<string?> DownloadAssetTextAsync(GitHubReleaseAsset asset, CancellationToken ctx = default);

	/// <summary>
	/// Fetches the SHA of the oldest commit reachable from <paramref name="tagRef"/> in the repository.
	/// Used as a fallback start-ref when no previous release exists (i.e., this is the very first release).
	/// </summary>
	/// <param name="owner">Repository owner</param>
	/// <param name="repo">Repository name</param>
	/// <param name="tagRef">The tag or ref to walk back from</param>
	/// <param name="ctx">Cancellation token</param>
	/// <returns>The initial commit SHA, or <c>null</c> if it cannot be determined.</returns>
	Task<string?> FetchInitialCommitAsync(string owner, string repo, string tagRef, CancellationToken ctx = default);
}
