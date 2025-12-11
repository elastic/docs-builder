// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Services.Changelog;

/// <summary>
/// Service for fetching pull request information from GitHub
/// </summary>
public partial class GitHubPrService(ILoggerFactory loggerFactory)
{
	private readonly ILogger<GitHubPrService> _logger = loggerFactory.CreateLogger<GitHubPrService>();
	private static readonly HttpClient HttpClient = new();

	static GitHubPrService()
	{
		HttpClient.DefaultRequestHeaders.Add("User-Agent", "docs-builder");
		HttpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
	}

	/// <summary>
	/// Fetches pull request information from GitHub
	/// </summary>
	/// <param name="prUrl">The PR URL (e.g., https://github.com/owner/repo/pull/123, owner/repo#123, or just a number if owner/repo are provided)</param>
	/// <param name="owner">Optional: GitHub repository owner (used when prUrl is just a number)</param>
	/// <param name="repo">Optional: GitHub repository name (used when prUrl is just a number)</param>
	/// <param name="ctx">Cancellation token</param>
	/// <returns>PR information or null if fetch fails</returns>
	public async Task<GitHubPrInfo?> FetchPrInfoAsync(string prUrl, string? owner = null, string? repo = null, CancellationToken ctx = default)
	{
		try
		{
			var (parsedOwner, parsedRepo, prNumber) = ParsePrUrl(prUrl, owner, repo);
			if (parsedOwner == null || parsedRepo == null || prNumber == null)
			{
				_logger.LogWarning("Unable to parse PR URL: {PrUrl}. Owner: {Owner}, Repo: {Repo}", prUrl, owner, repo);
				return null;
			}

			// Add GitHub token if available (for rate limiting and private repos)
			var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
			using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{parsedOwner}/{parsedRepo}/pulls/{prNumber}");
			if (!string.IsNullOrEmpty(githubToken))
			{
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);
			}

			_logger.LogDebug("Fetching PR info from: {ApiUrl}", request.RequestUri);

			var response = await HttpClient.SendAsync(request, ctx);
			if (!response.IsSuccessStatusCode)
			{
				_logger.LogWarning("Failed to fetch PR info. Status: {StatusCode}, Reason: {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
				return null;
			}

			var jsonContent = await response.Content.ReadAsStringAsync(ctx);
			var prData = JsonSerializer.Deserialize(jsonContent, GitHubPrJsonContext.Default.GitHubPrResponse);

			if (prData == null)
			{
				_logger.LogWarning("Failed to deserialize PR response");
				return null;
			}

			return new GitHubPrInfo
			{
				Title = prData.Title,
				Labels = prData.Labels?.Select(l => l.Name).ToArray() ?? []
			};
		}
		catch (HttpRequestException ex)
		{
			_logger.LogWarning(ex, "HTTP error fetching PR info from GitHub");
			return null;
		}
		catch (TaskCanceledException)
		{
			_logger.LogWarning("Request timeout fetching PR info from GitHub");
			return null;
		}
		catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException or ThreadAbortException))
		{
			_logger.LogWarning(ex, "Unexpected error fetching PR info from GitHub");
			return null;
		}
	}

	private static (string? owner, string? repo, int? prNumber) ParsePrUrl(string prUrl, string? defaultOwner = null, string? defaultRepo = null)
	{
		// Handle full URL: https://github.com/owner/repo/pull/123
		if (prUrl.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase) ||
			prUrl.StartsWith("http://github.com/", StringComparison.OrdinalIgnoreCase))
		{
			var uri = new Uri(prUrl);
			var segments = uri.Segments;
			// segments[0] is "/", segments[1] is "owner/", segments[2] is "repo/", segments[3] is "pull/", segments[4] is "123"
			if (segments.Length >= 5 && segments[3].Equals("pull/", StringComparison.OrdinalIgnoreCase))
			{
				var owner = segments[1].TrimEnd('/');
				var repo = segments[2].TrimEnd('/');
				if (int.TryParse(segments[4], out var prNum))
				{
					return (owner, repo, prNum);
				}
			}
		}

		// Handle short format: owner/repo#123
		var hashIndex = prUrl.LastIndexOf('#');
		if (hashIndex > 0 && hashIndex < prUrl.Length - 1)
		{
			var repoPart = prUrl[..hashIndex];
			var prPart = prUrl[(hashIndex + 1)..];
			if (int.TryParse(prPart, out var prNum))
			{
				var repoParts = repoPart.Split('/');
				if (repoParts.Length == 2)
				{
					return (repoParts[0], repoParts[1], prNum);
				}
			}
		}

		// Handle just a PR number when owner/repo are provided
		if (int.TryParse(prUrl, out var prNumber) &&
			!string.IsNullOrWhiteSpace(defaultOwner) && !string.IsNullOrWhiteSpace(defaultRepo))
		{
			return (defaultOwner, defaultRepo, prNumber);
		}

		return (null, null, null);
	}

	private sealed class GitHubPrResponse
	{
		public string Title { get; set; } = string.Empty;
		public List<GitHubLabel>? Labels { get; set; }
	}

	private sealed class GitHubLabel
	{
		public string Name { get; set; } = string.Empty;
	}

	[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
	[JsonSerializable(typeof(GitHubPrResponse))]
	[JsonSerializable(typeof(GitHubLabel))]
	[JsonSerializable(typeof(List<GitHubLabel>))]
	private sealed partial class GitHubPrJsonContext : JsonSerializerContext;
}

/// <summary>
/// Information about a GitHub pull request
/// </summary>
public class GitHubPrInfo
{
	public string Title { get; set; } = string.Empty;
	public string[] Labels { get; set; } = [];
}

