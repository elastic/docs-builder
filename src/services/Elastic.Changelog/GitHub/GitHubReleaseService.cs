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
public partial class GitHubReleaseService(ILoggerFactory loggerFactory) : IGitHubReleaseService
{
	private readonly ILogger<GitHubReleaseService> _logger = loggerFactory.CreateLogger<GitHubReleaseService>();
	private static readonly HttpClient HttpClient = new();

	static GitHubReleaseService()
	{
		HttpClient.DefaultRequestHeaders.Add("User-Agent", "docs-builder");
		HttpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
	}

	/// <inheritdoc />
	public async Task<GitHubReleaseInfo?> FetchReleaseAsync(
		string owner,
		string repo,
		string? version,
		CancellationToken ctx = default)
	{
		try
		{
			// Build URL: /repos/{owner}/{repo}/releases/latest or /releases/tags/{version}
			var isLatest = string.IsNullOrWhiteSpace(version) ||
				version.Equals("latest", StringComparison.OrdinalIgnoreCase);

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

	private async Task<GitHubReleaseInfo?> FetchReleaseFromUrl(string url, CancellationToken ctx)
	{
		// Add GitHub token if available (for rate limiting and private repos)
		var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
		using var request = new HttpRequestMessage(HttpMethod.Get, url);
		if (!string.IsNullOrEmpty(githubToken))
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);

		_logger.LogDebug("Fetching release info from: {ApiUrl}", url);

		var response = await HttpClient.SendAsync(request, ctx);
		if (!response.IsSuccessStatusCode)
		{
			_logger.LogDebug("Failed to fetch release info. Status: {StatusCode}, Reason: {ReasonPhrase}",
				response.StatusCode, response.ReasonPhrase);
			return null;
		}

		var jsonContent = await response.Content.ReadAsStringAsync(ctx);
		var releaseData = JsonSerializer.Deserialize(jsonContent, GitHubReleaseJsonContext.Default.GitHubReleaseResponse);

		if (releaseData == null)
		{
			_logger.LogWarning("Failed to deserialize release response");
			return null;
		}

		return new GitHubReleaseInfo
		{
			TagName = releaseData.TagName ?? string.Empty,
			Name = releaseData.Name ?? string.Empty,
			Body = releaseData.Body ?? string.Empty,
			Prerelease = releaseData.Prerelease,
			Draft = releaseData.Draft,
			HtmlUrl = releaseData.HtmlUrl ?? string.Empty
		};
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
	}

	[JsonSerializable(typeof(GitHubReleaseResponse))]
	private sealed partial class GitHubReleaseJsonContext : JsonSerializerContext;
}
