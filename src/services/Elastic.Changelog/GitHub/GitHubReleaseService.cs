// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.GitHub;

/// <summary>
/// Service for fetching release information from GitHub
/// </summary>
public partial class GitHubReleaseService(ILoggerFactory loggerFactory, GitHubApiTransport? transport = null) : IGitHubReleaseService
{
	private readonly ILogger<GitHubReleaseService> _logger = loggerFactory.CreateLogger<GitHubReleaseService>();
	private readonly GitHubApiTransport _transport = transport ?? new GitHubApiTransport();

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

	[JsonSerializable(typeof(GitHubReleaseResponse))]
	[JsonSerializable(typeof(GitHubReleaseResponse[]))]
	private sealed partial class GitHubReleaseJsonContext : JsonSerializerContext;
}
