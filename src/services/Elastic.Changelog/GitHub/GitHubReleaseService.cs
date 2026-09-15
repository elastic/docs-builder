// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.GitHub;

/// <summary>
/// Service for fetching release information from GitHub
/// </summary>
public partial class GitHubReleaseService(
	ILoggerFactory loggerFactory,
	GitHubApiTransport? transport = null,
	Func<int, TimeSpan>? retryDelay = null
) : IGitHubReleaseService
{
	private readonly ILogger<GitHubReleaseService> _logger = loggerFactory.CreateLogger<GitHubReleaseService>();
	private readonly GitHubApiTransport _transport = transport ?? new GitHubApiTransport();
	private readonly Func<int, TimeSpan> _retryDelay = retryDelay ?? (attempt => TimeSpan.FromSeconds(1 << attempt));

	/// <inheritdoc />
	public async Task<GitHubReleaseInfo?> FetchReleaseAsync(string owner, string repo, string? version, CancellationToken ctx = default)
	{
		try
		{
			// Build URL: /repos/{owner}/{repo}/releases/latest or /releases/tags/{version}
			var isLatest = string.IsNullOrWhiteSpace(version) || version.Equals("latest", StringComparison.OrdinalIgnoreCase);

			var url = isLatest
				? $"https://api.github.com/repos/{owner}/{repo}/releases/latest"
				: $"https://api.github.com/repos/{owner}/{repo}/releases/tags/{version}";

			var result = await FetchReleaseFromUrl(url, ctx);

			// If not found and version doesn't start with 'v', try with 'v' prefix
			if (result == null && !isLatest && !version!.StartsWith('v'))
			{
				_logger.LogDebug("Release not found for {Version}, trying with 'v' prefix", version);
				url = $"https://api.github.com/repos/{owner}/{repo}/releases/tags/v{version}";
				result = await FetchReleaseFromUrl(url, ctx);
			}

			return result;
		}
		catch (HttpRequestException ex)
		{
			_logger.LogWarning(ex, "HTTP error fetching release info from GitHub");
			return null;
		}
		catch (TaskCanceledException)
		{
			_logger.LogWarning("Request timeout fetching release info from GitHub");
			return null;
		}
		catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException or ThreadAbortException))
		{
			_logger.LogWarning(ex, "Unexpected error fetching release info from GitHub");
			return null;
		}
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<GitHubReleaseInfo>> FetchReleasesAsync(
		string owner,
		string repo,
		int count,
		CancellationToken ctx = default
	)
	{
		try
		{
			var url = $"https://api.github.com/repos/{owner}/{repo}/releases?per_page={count}";
			_logger.LogDebug("Fetching releases from: {ApiUrl}", url);

			using var response = await _transport.GetAsync(url, ctx);
			if (!response.IsSuccessStatusCode)
			{
				_logger.LogDebug(
					"Failed to fetch releases. Status: {StatusCode}, Reason: {ReasonPhrase}",
					response.StatusCode,
					response.ReasonPhrase
				);
				return [];
			}

			var jsonContent = await response.Content.ReadAsStringAsync(ctx);
			var releases = JsonSerializer.Deserialize(jsonContent, GitHubReleaseJsonContext.Default.GitHubReleaseResponseArray);
			return releases == null ? [] : releases.Select(ToReleaseInfo).ToArray();
		}
		catch (HttpRequestException ex)
		{
			_logger.LogWarning(ex, "HTTP error fetching releases from GitHub");
			return [];
		}
		catch (TaskCanceledException)
		{
			_logger.LogWarning("Request timeout fetching releases from GitHub");
			return [];
		}
	}

	/// <inheritdoc />
	public async Task<string?> DownloadAssetTextAsync(GitHubReleaseAsset asset, CancellationToken ctx = default)
	{
		try
		{
			_logger.LogDebug("Downloading release asset: {AssetUrl}", asset.BrowserDownloadUrl);

			using var response = await _transport.GetAsync(asset.BrowserDownloadUrl, ctx);
			if (!response.IsSuccessStatusCode)
			{
				_logger.LogDebug(
					"Failed to download asset {AssetName}. Status: {StatusCode}, Reason: {ReasonPhrase}",
					asset.Name,
					response.StatusCode,
					response.ReasonPhrase
				);
				return null;
			}

			return await response.Content.ReadAsStringAsync(ctx);
		}
		catch (HttpRequestException ex)
		{
			_logger.LogWarning(ex, "HTTP error downloading release asset {AssetName}", asset.Name);
			return null;
		}
		catch (TaskCanceledException)
		{
			_logger.LogWarning("Request timeout downloading release asset {AssetName}", asset.Name);
			return null;
		}
	}

	/// <inheritdoc />
	public async Task<string?> FetchPreviousTagAsync(string owner, string repo, string currentTag, CancellationToken ctx = default)
	{
		try
		{
			var (currentPrefix, currentMajor, currentIsPreRelease) = ParseTagIdentity(currentTag);
			var currentVersion = ParseTagVersion(currentTag);

			var result = await ScanReleasesForPreviousTagAsync(
				owner,
				repo,
				currentTag,
				currentPrefix,
				currentMajor,
				currentIsPreRelease,
				currentVersion,
				ctx
			);
			if (result is not null)
				return result;

			_logger.LogDebug("Releases API yielded no predecessor for {CurrentTag}; trying git tags API", currentTag);
			return await ScanTagsApiForPreviousTagAsync(
				owner,
				repo,
				currentTag,
				currentPrefix,
				currentMajor,
				currentIsPreRelease,
				currentVersion,
				ctx
			);
		}
		catch (HttpRequestException ex)
		{
			_logger.LogWarning(ex, "HTTP error scanning for previous tag of {CurrentTag}", currentTag);
			return null;
		}
		catch (TaskCanceledException)
		{
			_logger.LogWarning("Request timeout scanning for previous tag of {CurrentTag}", currentTag);
			return null;
		}
	}

	private async Task<string?> ScanReleasesForPreviousTagAsync(
		string owner,
		string repo,
		string currentTag,
		string currentPrefix,
		int currentMajor,
		bool currentIsPreRelease,
		SemVer? currentVersion,
		CancellationToken ctx
	)
	{
		const int pageSize = 100;
		var page = 1;
		var foundCurrent = false;
		string? bestMatch = null;
		SemVer? bestSemver = null;

		while (true)
		{
			var url = $"https://api.github.com/repos/{owner}/{repo}/releases?per_page={pageSize}&page={page}";
			_logger.LogDebug("Scanning release list for previous tag (page {Page}): GET {ApiUrl}", page, url);

			using var response = await GetWithRetryAsync(url, ctx);
			if (response is null)
				return null;

			var jsonContent = await response.Content.ReadAsStringAsync(ctx);
			var releases = JsonSerializer.Deserialize(jsonContent, GitHubReleaseJsonContext.Default.GitHubReleaseResponseArray);
			if (releases == null || releases.Length == 0)
				break;

			foreach (var r in releases)
			{
				var tagName = r.TagName ?? string.Empty;

				if (currentVersion is null)
				{
					// Non-semver: find the current tag first, then return the first prefix match after it.
					if (!foundCurrent)
					{
						if (string.Equals(tagName, currentTag, StringComparison.OrdinalIgnoreCase))
							foundCurrent = true;
						continue;
					}
					var (candidatePrefix, _, _) = ParseTagIdentity(tagName);
					if (!string.Equals(candidatePrefix, currentPrefix, StringComparison.OrdinalIgnoreCase))
						continue;
					return r.TagName;
				}

				// Semver: accumulate the highest candidate strictly below the current version.
				if (!IsSemverCandidate(tagName, currentTag, currentPrefix, currentMajor, currentIsPreRelease, out var candidateVersion))
					continue;
				if (candidateVersion.Value.CompareTo(currentVersion.Value) >= 0)
					continue;

				if (bestSemver is null || candidateVersion.Value.CompareTo(bestSemver.Value) > 0)
				{
					bestSemver = candidateVersion;
					bestMatch = r.TagName;
					// Definitive exact predecessor found — no tag can rank higher and still be below current.
					if (IsDefiniteExactPredecessor(candidateVersion.Value, currentVersion.Value))
						return bestMatch;
				}
			}

			// Stop paginating once the best candidate so far is already in the expected range.
			if (currentVersion is not null && CanBailAfterPage(bestSemver, currentVersion.Value))
				break;

			if (releases.Length < pageSize)
				break;
			page++;
		}

		return bestMatch;
	}

	private async Task<string?> ScanTagsApiForPreviousTagAsync(
		string owner,
		string repo,
		string currentTag,
		string currentPrefix,
		int currentMajor,
		bool currentIsPreRelease,
		SemVer? currentVersion,
		CancellationToken ctx
	)
	{
		const int pageSize = 100;
		var page = 1;
		var foundCurrent = false;
		string? bestMatch = null;
		SemVer? bestSemver = null;

		while (true)
		{
			var url = $"https://api.github.com/repos/{owner}/{repo}/tags?per_page={pageSize}&page={page}";
			_logger.LogDebug("Scanning tags API for previous tag (page {Page}): GET {ApiUrl}", page, url);

			using var response = await GetWithRetryAsync(url, ctx);
			if (response is null)
				return null;

			var jsonContent = await response.Content.ReadAsStringAsync(ctx);
			var tags = JsonSerializer.Deserialize(jsonContent, GitHubReleaseJsonContext.Default.GitHubTagResponseArray);
			if (tags == null || tags.Length == 0)
				break;

			foreach (var t in tags)
			{
				var tagName = t.Name ?? string.Empty;

				if (currentVersion is null)
				{
					if (!foundCurrent)
					{
						if (string.Equals(tagName, currentTag, StringComparison.OrdinalIgnoreCase))
							foundCurrent = true;
						continue;
					}
					var (candidatePrefix, _, _) = ParseTagIdentity(tagName);
					if (!string.Equals(candidatePrefix, currentPrefix, StringComparison.OrdinalIgnoreCase))
						continue;
					return t.Name;
				}

				if (!IsSemverCandidate(tagName, currentTag, currentPrefix, currentMajor, currentIsPreRelease, out var candidateVersion))
					continue;
				if (candidateVersion.Value.CompareTo(currentVersion.Value) >= 0)
					continue;

				if (bestSemver is null || candidateVersion.Value.CompareTo(bestSemver.Value) > 0)
				{
					bestSemver = candidateVersion;
					bestMatch = t.Name;
					if (IsDefiniteExactPredecessor(bestSemver.Value, currentVersion.Value))
						return bestMatch;
				}
			}

			if (currentVersion is not null && CanBailAfterPage(bestSemver, currentVersion.Value))
				break;

			if (tags.Length < pageSize)
				break;
			page++;
		}

		return bestMatch;
	}

	/// <summary>
	/// Returns true if <paramref name="tagName"/> qualifies as a candidate predecessor for the current tag.
	/// Filters by prefix, major version, and prerelease boundary; also skips the current tag itself.
	/// Sets <paramref name="candidateVersion"/> when true.
	/// </summary>
	private static bool IsSemverCandidate(
		string tagName,
		string currentTag,
		string currentPrefix,
		int currentMajor,
		bool currentIsPreRelease,
		[System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out SemVer? candidateVersion
	)
	{
		candidateVersion = null;
		if (string.Equals(tagName, currentTag, StringComparison.OrdinalIgnoreCase))
			return false;

		var (cPrefix, cMajor, cIsPreRelease) = ParseTagIdentity(tagName);
		if (!string.Equals(cPrefix, currentPrefix, StringComparison.OrdinalIgnoreCase))
			return false;
		if (currentMajor >= 0 && cMajor >= 0 && cMajor != currentMajor)
			return false;
		if (!currentIsPreRelease && cIsPreRelease)
			return false;

		candidateVersion = ParseTagVersion(tagName);
		return candidateVersion is not null;
	}

	/// <summary>
	/// Returns true when <paramref name="candidate"/> is definitively the best possible predecessor
	/// for a stable <paramref name="current"/> tag with <c>Patch &gt; 0</c>. Because semver patch
	/// increments by one, no stable tag can exist between <c>Patch-1</c> and <c>Patch</c>.
	/// </summary>
	private static bool IsDefiniteExactPredecessor(SemVer candidate, SemVer current) =>
		!current.IsPreRelease
			&& current.Patch > 0
			&& candidate.Major == current.Major
			&& candidate.Minor == current.Minor
			&& candidate.Patch == current.Patch - 1
			&& !candidate.IsPreRelease;

	/// <summary>
	/// Returns true when the best candidate found so far is already in the expected predecessor range
	/// for a stable <paramref name="current"/> tag with <c>Patch &gt; 0</c>, so further pages cannot
	/// improve the result. Only safe for patch releases: the definitive predecessor is exactly
	/// <c>(Major, Minor, Patch-1, stable)</c>, and no higher patch in the same minor can exist.
	/// <para>
	/// X.Y.0 lookups require a full scan because backport patches (e.g. v4.1.2 created after v4.1.9)
	/// mean the highest semver in the previous minor may appear on a later page.
	/// </para>
	/// Not applied to pre-release tags; their predecessor sets are complex enough to require a full scan.
	/// </summary>
	private static bool CanBailAfterPage(SemVer? bestSoFar, SemVer current)
	{
		if (bestSoFar is null || current.IsPreRelease || current.Patch == 0)
			return false;
		var best = bestSoFar.Value;

		// Stable patch release: the definitive predecessor is (Major, Minor, Patch-1, stable).
		return best.Major == current.Major && best.Minor == current.Minor && best.Patch == current.Patch - 1;
	}

	/// <summary>
	/// Issues a GET request with up to three retries for transient failures (HTTP 429 and 5xx).
	/// Returns the successful <see cref="HttpResponseMessage"/> (caller must dispose), or <c>null</c>
	/// when all attempts fail — in which case the caller should treat the result as indeterminate
	/// rather than returning a potentially incomplete best-match.
	/// Non-transient failures (4xx other than 429) are not retried.
	/// </summary>
	private async Task<HttpResponseMessage?> GetWithRetryAsync(string url, CancellationToken ctx)
	{
		const int maxAttempts = 3;
		for (var attempt = 0; attempt < maxAttempts; attempt++)
		{
			var response = await _transport.GetAsync(url, ctx);
			if (response.IsSuccessStatusCode)
				return response;

			var status = (int)response.StatusCode;
			var isTransient = status is 429 or >= 500;
			if (!isTransient || attempt == maxAttempts - 1)
			{
				_logger.LogWarning(
					"GitHub API {Url} returned HTTP {StatusCode} after {Attempts} attempt(s) — " +
						"treating predecessor lookup as indeterminate to avoid returning an incomplete result",
					url,
					status,
					attempt + 1
				);
				response.Dispose();
				return null;
			}

			var delay = _retryDelay(attempt);
			_logger.LogDebug(
				"Transient HTTP {StatusCode} from {Url}; retrying in {DelayMs}ms (attempt {Attempt}/{MaxAttempts})",
				status,
				url,
				delay.TotalMilliseconds,
				attempt + 1,
				maxAttempts
			);
			response.Dispose();
			await Task.Delay(delay, ctx);
		}
		return null; // unreachable — maxAttempts > 0 always exits above
	}

	/// <summary>
	/// Extracts the non-numeric prefix, semver major version, and pre-release flag from a release tag.
	/// <list type="bullet">
	///   <item>Semver tags ("v2.3.1"): prefix="v", major=2, isPreRelease=false.</item>
	///   <item>Semver pre-release tags ("v1.2.0-beta.1"): prefix="v", major=1, isPreRelease=true.</item>
	///   <item>Non-semver tags ("release-20260901"): prefix="release-", major=-1, isPreRelease=false.</item>
	///   <item>Bare-digit tags ("20260901"): prefix="", major=-1, isPreRelease=false.</item>
	/// </list>
	/// When major = -1, callers skip the major-version filter and match on prefix only.
	/// A pre-release tag is one whose semver base (X.Y.Z) is immediately followed by a hyphen.
	/// </summary>
	private static (string Prefix, int Major, bool IsPreRelease) ParseTagIdentity(string tag)
	{
		var semverMatch = SemverTagRegex().Match(tag);
		if (semverMatch.Success)
		{
			var prefix = semverMatch.Groups["prefix"].Value;
			var major = int.Parse(semverMatch.Groups["major"].Value, System.Globalization.CultureInfo.InvariantCulture);
			var isPreRelease = semverMatch.Length < tag.Length && tag[semverMatch.Length] == '-';
			return (prefix, major, isPreRelease);
		}

		var prefixMatch = NonSemverPrefixRegex().Match(tag);
		return (prefixMatch.Success ? prefixMatch.Groups["prefix"].Value : string.Empty, -1, false);
	}

	private static SemVer? ParseTagVersion(string tag)
	{
		var m = SemverTagRegex().Match(tag);
		if (!m.Success)
			return null;
		var major = int.Parse(m.Groups["major"].Value, System.Globalization.CultureInfo.InvariantCulture);
		var minor = int.Parse(m.Groups["minor"].Value, System.Globalization.CultureInfo.InvariantCulture);
		var patch = int.Parse(m.Groups["patch"].Value, System.Globalization.CultureInfo.InvariantCulture);
		var isPreRelease = m.Length < tag.Length && tag[m.Length] == '-';
		var suffix = isPreRelease ? tag[(m.Length + 1)..] : string.Empty;
		return new SemVer(major, minor, patch, isPreRelease, suffix);
	}

	private readonly record struct SemVer(int Major, int Minor, int Patch, bool IsPreRelease, string PreReleaseSuffix) : IComparable<SemVer>
	{
		public int CompareTo(SemVer other)
		{
			var c = Major.CompareTo(other.Major);
			if (c != 0)
				return c;
			c = Minor.CompareTo(other.Minor);
			if (c != 0)
				return c;
			c = Patch.CompareTo(other.Patch);
			if (c != 0)
				return c;
			// stable (no prerelease) ranks higher than any prerelease on the same X.Y.Z
			if (!IsPreRelease && other.IsPreRelease)
				return 1;
			if (IsPreRelease && !other.IsPreRelease)
				return -1;
			return ComparePreReleaseSuffixes(PreReleaseSuffix, other.PreReleaseSuffix);
		}

		/// <summary>
		/// Compares two pre-release suffix strings token-by-token per SemVer 2.0 precedence rules:
		/// dot-separated identifiers, numeric tokens compared as integers, numeric &lt; alphanumeric,
		/// longer wins when all leading tokens are equal.
		/// </summary>
		private static int ComparePreReleaseSuffixes(string a, string b)
		{
			var tokensA = a.Split('.');
			var tokensB = b.Split('.');
			var len = Math.Min(tokensA.Length, tokensB.Length);
			for (var i = 0; i < len; i++)
			{
				var tokenA = tokensA[i];
				var tokenB = tokensB[i];
				var aIsNum = int.TryParse(tokenA, out var numA);
				var bIsNum = int.TryParse(tokenB, out var numB);
				if (aIsNum && bIsNum)
				{
					var nc = numA.CompareTo(numB);
					if (nc != 0)
						return nc;
				}
				else if (aIsNum)
					return -1; // numeric < alphanumeric

				else if (bIsNum)
					return 1;
				else
				{
					var sc = string.Compare(tokenA, tokenB, StringComparison.OrdinalIgnoreCase);
					if (sc != 0)
						return sc;
				}
			}
			return tokensA.Length.CompareTo(tokensB.Length);
		}
	}

	[GeneratedRegex(@"^(?<prefix>.*?)(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)", RegexOptions.None)]
	private static partial Regex SemverTagRegex();

	[GeneratedRegex(@"^(?<prefix>[^\d]+)", RegexOptions.None)]
	private static partial Regex NonSemverPrefixRegex();

	private async Task<GitHubReleaseInfo?> FetchReleaseFromUrl(string url, CancellationToken ctx)
	{
		_logger.LogDebug("Fetching release info from: {ApiUrl}", url);

		using var response = await _transport.GetAsync(url, ctx);
		if (!response.IsSuccessStatusCode)
		{
			_logger.LogDebug(
				"Failed to fetch release info. Status: {StatusCode}, Reason: {ReasonPhrase}",
				response.StatusCode,
				response.ReasonPhrase
			);
			return null;
		}

		var jsonContent = await response.Content.ReadAsStringAsync(ctx);
		var releaseData = JsonSerializer.Deserialize(jsonContent, GitHubReleaseJsonContext.Default.GitHubReleaseResponse);

		if (releaseData == null)
		{
			_logger.LogWarning("Failed to deserialize release response");
			return null;
		}

		return ToReleaseInfo(releaseData);
	}

	private static GitHubReleaseInfo ToReleaseInfo(GitHubReleaseResponse releaseData) =>
		new()
		{
			TagName = releaseData.TagName ?? string.Empty,
			Name = releaseData.Name ?? string.Empty,
			Body = releaseData.Body ?? string.Empty,
			Prerelease = releaseData.Prerelease,
			Draft = releaseData.Draft,
			HtmlUrl = releaseData.HtmlUrl ?? string.Empty,
			PublishedAt = releaseData.PublishedAt,
			Assets = releaseData.Assets is { Count: > 0 }
				? releaseData
					.Assets
					.Where(a => a is { Name: not null, BrowserDownloadUrl: not null })
					.Select(a => new GitHubReleaseAsset { Name = a.Name!, BrowserDownloadUrl = a.BrowserDownloadUrl! })
					.ToArray()
				: []
		};

	private sealed class GitHubReleaseAssetResponse
	{
		[JsonPropertyName("name")]
		public string? Name { get; set; }

		[JsonPropertyName("browser_download_url")]
		public string? BrowserDownloadUrl { get; set; }
	}

	private sealed class GitHubReleaseResponse
	{
		[JsonPropertyName("tag_name")]
		public string? TagName { get; set; }

		[JsonPropertyName("name")]
		public string? Name { get; set; }

		[JsonPropertyName("body")]
		public string? Body { get; set; }

		[JsonPropertyName("prerelease")]
		public bool Prerelease { get; set; }

		[JsonPropertyName("draft")]
		public bool Draft { get; set; }

		[JsonPropertyName("html_url")]
		public string? HtmlUrl { get; set; }

		[JsonPropertyName("published_at")]
		public DateTimeOffset? PublishedAt { get; set; }

		[JsonPropertyName("assets")]
		public List<GitHubReleaseAssetResponse>? Assets { get; set; }
	}

	private sealed class GitHubTagResponse
	{
		[JsonPropertyName("name")]
		public string? Name { get; set; }
	}

	[JsonSerializable(typeof(GitHubReleaseResponse))]
	[JsonSerializable(typeof(GitHubReleaseResponse[]))]
	[JsonSerializable(typeof(GitHubTagResponse[]))]
	private sealed partial class GitHubReleaseJsonContext : JsonSerializerContext;
}
