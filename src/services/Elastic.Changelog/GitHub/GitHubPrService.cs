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
/// Service for fetching pull request information from GitHub
/// </summary>
public partial class GitHubPrService(ILoggerFactory loggerFactory, GitHubApiTransport? transport = null) : IGitHubPrService
{
	private readonly ILogger<GitHubPrService> _logger = loggerFactory.CreateLogger<GitHubPrService>();
	private readonly GitHubApiTransport _transport = transport ?? new GitHubApiTransport();

	/// <summary>
	/// Fetches pull request information from GitHub
	/// </summary>
	/// <param name="prUrl">The PR URL (e.g., https://github.com/owner/repo/pull/123, owner/repo#123, or just a number if owner/repo are provided)</param>
	/// <param name="owner">Optional: GitHub repository owner (used when prUrl is just a number)</param>
	/// <param name="repo">Optional: GitHub repository name (used when prUrl is just a number)</param>
	/// <param name="ctx">Cancellation token</param>
	/// <returns>PR information or null if fetch fails</returns>
	public async Task<GitHubPrInfo?> FetchPrInfoAsync(
		string prUrl,
		string? owner = null,
		string? repo = null,
		CancellationToken ctx = default
	)
	{
		try
		{
			var (parsedOwner, parsedRepo, prNumber) = ParsePrUrl(prUrl, owner, repo);
			if (parsedOwner == null || parsedRepo == null || prNumber == null)
			{
				_logger.LogWarning("Unable to parse PR URL: {PrUrl}. Owner: {Owner}, Repo: {Repo}", prUrl, owner, repo);
				return null;
			}

			var url = $"https://api.github.com/repos/{parsedOwner}/{parsedRepo}/pulls/{prNumber}";
			_logger.LogDebug("Fetching PR info from: {ApiUrl}", url);

			using var response = await _transport.GetAsync(url, ctx);
			if (!response.IsSuccessStatusCode)
			{
				_logger.LogWarning(
					"Failed to fetch PR info. Status: {StatusCode}, Reason: {ReasonPhrase}",
					response.StatusCode,
					response.ReasonPhrase
				);
				return null;
			}

			var jsonContent = await response.Content.ReadAsStringAsync(ctx);
			var prData = JsonSerializer.Deserialize(jsonContent, GitHubPrJsonContext.Default.GitHubPrResponse);

			if (prData == null)
			{
				_logger.LogWarning("Failed to deserialize PR response");
				return null;
			}

			// Extract linked issues from PR body
			var linkedIssues = ExtractLinkedIssues(prData.Body ?? string.Empty, parsedOwner, parsedRepo);

			return new GitHubPrInfo
			{
				Title = prData.Title,
				Body = prData.Body ?? string.Empty,
				Labels = prData.Labels?.Select(l => l.Name).ToList() ?? [],
				LinkedIssues = linkedIssues,
				HeadSha = prData.Head?.Sha,
				HeadRef = prData.Head?.Ref,
				IsFork = prData.Head?.Repo?.Fork ?? false
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

	private static (string? owner, string? repo, int? prNumber) ParsePrUrl(
		string prUrl,
		string? defaultOwner = null,
		string? defaultRepo = null
	)
	{
		// Handle full URL: https://github.com/owner/repo/pull/123
		if (
			prUrl.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase)
			|| prUrl.StartsWith("http://github.com/", StringComparison.OrdinalIgnoreCase)
		)
		{
			var uri = new Uri(prUrl);
			var segments = uri.Segments;
			// segments[0] is "/", segments[1] is "owner/", segments[2] is "repo/", segments[3] is "pull/", segments[4] is "123"
			if (segments.Length >= 5 && segments[3].Equals("pull/", StringComparison.OrdinalIgnoreCase))
			{
				var owner = segments[1].TrimEnd('/');
				var repo = segments[2].TrimEnd('/');
				if (int.TryParse(segments[4], out var prNum))
					return (owner, repo, prNum);
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
					return (repoParts[0], repoParts[1], prNum);
			}
		}

		// Handle just a PR number when owner/repo are provided
		if (int.TryParse(prUrl, out var prNumber) && !string.IsNullOrWhiteSpace(defaultOwner) && !string.IsNullOrWhiteSpace(defaultRepo))
			return (defaultOwner, defaultRepo, prNumber);

		return (null, null, null);
	}

	/// <summary>
	/// Extracts linked issues from PR body.
	/// Matches patterns like "Fixes #123", "Closes #456", "Resolves #789", "Fixes owner/repo#123"
	/// </summary>
	private static IReadOnlyList<string> ExtractLinkedIssues(string body, string prOwner, string prRepo)
	{
		if (string.IsNullOrWhiteSpace(body))
			return [];

		var issues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		// Pattern for keywords followed by issue references
		// Matches: Fixes #123, Closes owner/repo#456, Resolves https://github.com/owner/repo/issues/789
		var keywordPattern = @"(?:fix(?:es|ed)?|close[sd]?|resolve[sd]?)\s+";

		// Pattern for cross-repo issue: owner/repo#123
		var crossRepoPattern = $@"{keywordPattern}([a-zA-Z0-9_-]+/[a-zA-Z0-9_-]+)#(\d+)";
		foreach (Match match in Regex.Matches(body, crossRepoPattern, RegexOptions.IgnoreCase))
		{
			var repoPath = match.Groups[1].Value;
			var issueNum = match.Groups[2].Value;
			_ = issues.Add($"https://github.com/{repoPath}/issues/{issueNum}");
		}

		// Pattern for full GitHub issue URL
		var urlPattern = $@"{keywordPattern}(https://github\.com/([a-zA-Z0-9_-]+/[a-zA-Z0-9_-]+)/issues/(\d+))";
		foreach (Match match in Regex.Matches(body, urlPattern, RegexOptions.IgnoreCase))
			_ = issues.Add(match.Groups[1].Value);

		// Pattern for same-repo issue: #123
		var sameRepoPattern = $@"{keywordPattern}#(\d+)";
		foreach (Match match in Regex.Matches(body, sameRepoPattern, RegexOptions.IgnoreCase))
		{
			// Skip if this is part of a cross-repo reference (already handled above)
			var issueNum = match.Groups[1].Value;
			var fullMatch = match.Value;
			if (!fullMatch.Contains('/'))
				_ = issues.Add($"https://github.com/{prOwner}/{prRepo}/issues/{issueNum}");
		}

		return [.. issues];
	}

	/// <summary>
	/// Fetches issue information from GitHub
	/// </summary>
	public async Task<GitHubIssueInfo?> FetchIssueInfoAsync(
		string issueUrl,
		string? owner = null,
		string? repo = null,
		CancellationToken ctx = default
	)
	{
		try
		{
			var (parsedOwner, parsedRepo, issueNumber) = ParseIssueUrl(issueUrl, owner, repo);
			if (parsedOwner == null || parsedRepo == null || issueNumber == null)
			{
				_logger.LogWarning("Unable to parse issue URL: {IssueUrl}. Owner: {Owner}, Repo: {Repo}", issueUrl, owner, repo);
				return null;
			}

			var url = $"https://api.github.com/repos/{parsedOwner}/{parsedRepo}/issues/{issueNumber}";
			_logger.LogDebug("Fetching issue info from: {ApiUrl}", url);

			using var response = await _transport.GetAsync(url, ctx);
			if (!response.IsSuccessStatusCode)
			{
				_logger.LogWarning(
					"Failed to fetch issue info. Status: {StatusCode}, Reason: {ReasonPhrase}",
					response.StatusCode,
					response.ReasonPhrase
				);
				return null;
			}

			var jsonContent = await response.Content.ReadAsStringAsync(ctx);
			var issueData = JsonSerializer.Deserialize(jsonContent, GitHubPrJsonContext.Default.GitHubIssueResponse);

			if (issueData == null)
			{
				_logger.LogWarning("Failed to deserialize issue response");
				return null;
			}

			var linkedPrs = ExtractLinkedPrs(issueData.Body ?? string.Empty, parsedOwner, parsedRepo);

			return new GitHubIssueInfo
			{
				Title = issueData.Title,
				Body = issueData.Body ?? string.Empty,
				Labels = issueData.Labels?.Select(l => l.Name).ToList() ?? [],
				LinkedPrs = linkedPrs
			};
		}
		catch (HttpRequestException ex)
		{
			_logger.LogWarning(ex, "HTTP error fetching issue info from GitHub");
			return null;
		}
		catch (TaskCanceledException)
		{
			_logger.LogWarning("Request timeout fetching issue info from GitHub");
			return null;
		}
		catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException or ThreadAbortException))
		{
			_logger.LogWarning(ex, "Unexpected error fetching issue info from GitHub");
			return null;
		}
	}

	/// <inheritdoc />
	public async Task<string?> FetchCommitAuthorAsync(string owner, string repo, string sha, CancellationToken ctx = default)
	{
		try
		{
			var url = $"https://api.github.com/repos/{owner}/{repo}/commits/{sha}";
			_logger.LogDebug("Fetching commit author from: {ApiUrl}", url);

			using var response = await _transport.GetAsync(url, ctx);
			if (!response.IsSuccessStatusCode)
			{
				_logger.LogWarning("Failed to fetch commit info. Status: {StatusCode}", response.StatusCode);
				return null;
			}

			var json = await response.Content.ReadAsStringAsync(ctx);
			var commit = JsonSerializer.Deserialize(json, GitHubPrJsonContext.Default.GitHubCommitResponse);
			return commit?.Author?.Login;
		}
		catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
		{
			_logger.LogWarning(ex, "Error fetching commit author for {Sha}", sha);
			return null;
		}
	}

	/// <inheritdoc />
	public async Task<string?> FetchLastFileCommitAuthorAsync(
		string owner,
		string repo,
		string filePath,
		string branch,
		CancellationToken ctx = default
	)
	{
		try
		{
			var url =
				$"https://api.github.com/repos/{owner}/{repo}/commits?path={Uri.EscapeDataString(filePath)}&sha={Uri.EscapeDataString(branch)}&per_page=1";
			_logger.LogDebug("Fetching last file commit author from: {ApiUrl}", url);

			using var response = await _transport.GetAsync(url, ctx);
			if (!response.IsSuccessStatusCode)
			{
				_logger.LogWarning("Failed to fetch file commit history. Status: {StatusCode}", response.StatusCode);
				return null;
			}

			var json = await response.Content.ReadAsStringAsync(ctx);
			var commits = JsonSerializer.Deserialize(json, GitHubPrJsonContext.Default.ListGitHubCommitListItem);
			if (commits is not { Count: > 0 })
				return null;

			return commits[0].Author?.Login;
		}
		catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
		{
			_logger.LogWarning(ex, "Error fetching last file commit author for {FilePath}", filePath);
			return null;
		}
	}

	private static (string? owner, string? repo, int? issueNumber) ParseIssueUrl(
		string issueUrl,
		string? defaultOwner = null,
		string? defaultRepo = null
	)
	{
		if (
			issueUrl.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase)
			|| issueUrl.StartsWith("http://github.com/", StringComparison.OrdinalIgnoreCase)
		)
		{
			var uri = new Uri(issueUrl);
			var segments = uri.Segments;
			if (segments.Length >= 5 && segments[3].Equals("issues/", StringComparison.OrdinalIgnoreCase))
			{
				var owner = segments[1].TrimEnd('/');
				var repo = segments[2].TrimEnd('/');
				if (int.TryParse(segments[4], out var issueNum))
					return (owner, repo, issueNum);
			}
		}

		var hashIndex = issueUrl.LastIndexOf('#');
		if (hashIndex > 0 && hashIndex < issueUrl.Length - 1)
		{
			var repoPart = issueUrl[..hashIndex];
			var issuePart = issueUrl[(hashIndex + 1)..];
			if (int.TryParse(issuePart, out var issueNum))
			{
				var repoParts = repoPart.Split('/');
				if (repoParts.Length == 2)
					return (repoParts[0], repoParts[1], issueNum);
			}
		}

		if (
			int.TryParse(issueUrl, out var issueNumber)
			&& !string.IsNullOrWhiteSpace(defaultOwner)
			&& !string.IsNullOrWhiteSpace(defaultRepo)
		)
			return (defaultOwner, defaultRepo, issueNumber);

		return (null, null, null);
	}

	/// <summary>
	/// Extracts linked PRs from issue body.
	/// Matches patterns like "Fixed by #123", "PR #456", "https://github.com/owner/repo/pull/789"
	/// </summary>
	private static IReadOnlyList<string> ExtractLinkedPrs(string body, string issueOwner, string issueRepo)
	{
		if (string.IsNullOrWhiteSpace(body))
			return [];

		var prs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		// Full GitHub PR URL
		foreach (Match match in MyRegex().Matches(body))
			_ = prs.Add(match.Value);

		// Cross-repo: owner/repo#123 in context of PR (e.g., "Fixed by owner/repo#123")
		var crossRepoPattern = @"(?:fixed\s+by|pr|merge[sd]?|via)\s+([a-zA-Z0-9_-]+/[a-zA-Z0-9_-]+)#(\d+)";
		foreach (Match match in Regex.Matches(body, crossRepoPattern, RegexOptions.IgnoreCase))
		{
			var repoPath = match.Groups[1].Value;
			var prNum = match.Groups[2].Value;
			_ = prs.Add($"https://github.com/{repoPath}/pull/{prNum}");
		}

		// Same-repo: #123
		var sameRepoPattern = @"(?:fixed\s+by|pr|merge[sd]?|via)\s+#(\d+)";
		foreach (var prNum in Enumerable.Select(
			Enumerable.Cast<Match>(Regex.Matches(body, sameRepoPattern, RegexOptions.IgnoreCase)),
			match => match.Groups[1].Value
		))
		{
			_ = prs.Add($"https://github.com/{issueOwner}/{issueRepo}/pull/{prNum}");
		}

		return [.. prs];
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<string>?> FetchChangedFilesAsync(
		string owner,
		string repo,
		int prNumber,
		CancellationToken ctx = default
	)
	{
		try
		{
			var files = new List<string>();
			var page = 1;
			while (true)
			{
				var url = $"https://api.github.com/repos/{owner}/{repo}/pulls/{prNumber}/files?per_page=100&page={page}";
				_logger.LogDebug("Fetching PR changed files page {Page}: {Url}", page, url);

				using var response = await _transport.GetAsync(url, ctx);
				if (!response.IsSuccessStatusCode)
				{
					_logger.LogWarning("Failed to fetch PR files. Status: {StatusCode}", response.StatusCode);
					return null;
				}

				var json = await response.Content.ReadAsStringAsync(ctx);
				var items = JsonSerializer.Deserialize(json, GitHubPrJsonContext.Default.ListGitHubPrFileItem);
				if (items is null or { Count: 0 })
					break;

				foreach (var item in items)
				{
					if (item.Filename is not null && (item.Status == "added" || item.Status == "modified"))
						files.Add(item.Filename);
				}

				if (items.Count < 100)
					break;
				page++;
			}
			return files;
		}
		catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
		{
			_logger.LogWarning(ex, "Error fetching PR changed files for PR #{PrNumber}", prNumber);
			return null;
		}
	}

	private const int PrExistenceBatchSize = 50;

	[GeneratedRegex("^[A-Za-z0-9_.-]+$")]
	private static partial Regex SafeGraphQlIdentifierRegex();

	/// <inheritdoc />
	public async Task<IReadOnlyDictionary<int, bool>> CheckPullRequestsExistAsync(
		string owner,
		string repo,
		IReadOnlyList<int> numbers,
		CancellationToken ctx = default
	)
	{
		if (numbers.Count == 0)
			return new Dictionary<int, bool>();

		var token = _transport.ResolveToken();
		if (string.IsNullOrWhiteSpace(token))
		{
			_logger.LogWarning("No GITHUB_TOKEN — skipping PR existence check for {Count} numbers", numbers.Count);
			return new Dictionary<int, bool>();
		}

		if (!SafeGraphQlIdentifierRegex().IsMatch(owner) || !SafeGraphQlIdentifierRegex().IsMatch(repo))
		{
			_logger.LogWarning("Invalid owner/repo for GraphQL: {Owner}/{Repo}", owner, repo);
			return new Dictionary<int, bool>();
		}

		var result = new Dictionary<int, bool>();

		for (var batchStart = 0; batchStart < numbers.Count; batchStart += PrExistenceBatchSize)
		{
			var batch = numbers.Skip(batchStart).Take(PrExistenceBatchSize).ToList();
			var batchResult = await CheckPrBatchAsync(owner, repo, batch, ctx);
			foreach (var (num, exists) in batchResult)
				result[num] = exists;
		}

		return result;
	}

	private async Task<IReadOnlyDictionary<int, bool>> CheckPrBatchAsync(
		string owner,
		string repo,
		IReadOnlyList<int> numbers,
		CancellationToken ctx
	)
	{
		var query = BuildPrExistenceQuery(owner, repo, numbers);
		var requestJson = JsonSerializer.Serialize(new GraphQlRequest { Query = query }, GitHubPrJsonContext.Default.GraphQlRequest);

		try
		{
			using var response = await _transport.PostGraphQlAsync(requestJson, ctx);
			var responseJson = await response.Content.ReadAsStringAsync(ctx);

			if (!response.IsSuccessStatusCode)
			{
				_logger.LogWarning("GraphQL PR existence check failed. Status: {StatusCode}", response.StatusCode);
				return new Dictionary<int, bool>();
			}

			var parsed = JsonSerializer.Deserialize(responseJson, GitHubPrJsonContext.Default.PrExistenceGraphQlResponse);
			if (parsed?.Data?.Repository is null)
			{
				_logger.LogWarning("GraphQL PR existence response had no repository data");
				return new Dictionary<int, bool>();
			}

			// Build a set of numbers that had NOT_FOUND errors
			var notFoundErrors = new HashSet<int>();
			if (parsed.Errors is { Count: > 0 })
			{
				// Errors reference path like ["repository", "p1234"] — extract the index
				foreach (var error in parsed.Errors)
				{
					if (error.Path is { Count: >= 2 } && error.Path[1] is string alias && alias.StartsWith('p'))
					{
						if (int.TryParse(alias[1..], out var idx) && idx >= 0 && idx < numbers.Count)
							_ = notFoundErrors.Add(numbers[idx]);
					}
				}
			}

			var result = new Dictionary<int, bool>();
			for (var i = 0; i < numbers.Count; i++)
			{
				var num = numbers[i];
				var alias = $"p{i}";
				if (parsed.Data.Repository.TryGetValue(alias, out var node))
					result[num] = node is not null;
				else
					// alias not present in response — treat as unknown (omit)
					_logger.LogDebug("PR #{Number} not present in GraphQL response; skipping", num);
			}

			return result;
		}
		catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
		{
			_logger.LogWarning(ex, "Error during GraphQL PR existence batch check");
			return new Dictionary<int, bool>();
		}
	}

	private static string BuildPrExistenceQuery(string owner, string repo, IReadOnlyList<int> numbers)
	{
		var sb = new System.Text.StringBuilder();
		_ = sb.Append("query { repository(owner: \"").Append(owner).Append("\", name: \"").Append(repo).Append("\") {");
		for (var i = 0; i < numbers.Count; i++)
			_ = sb.Append(" p").Append(i).Append(": pullRequest(number: ").Append(numbers[i]).Append(") { number }");
		_ = sb.Append(" } }");
		return sb.ToString();
	}

	private sealed class GitHubPrResponse
	{
		public string Title { get; set; } = string.Empty;
		public string Body { get; set; } = string.Empty;
		public List<GitHubLabel>? Labels { get; set; }
		public GitHubHeadRef? Head { get; set; }
	}

	private sealed class GitHubIssueResponse
	{
		public string Title { get; set; } = string.Empty;
		public string Body { get; set; } = string.Empty;
		public List<GitHubLabel>? Labels { get; set; }
	}

	private sealed class GitHubLabel
	{
		public string Name { get; set; } = string.Empty;
	}

	private sealed class GitHubHeadRef
	{
		public string Sha { get; set; } = string.Empty;
		public string Ref { get; set; } = string.Empty;
		public GitHubRepoRef? Repo { get; set; }
	}

	private sealed class GitHubRepoRef
	{
		[JsonPropertyName("full_name")]
		public string FullName { get; set; } = string.Empty;

		public bool Fork { get; set; }
	}

	private sealed class GitHubCommitResponse
	{
		public GitHubCommitAuthor? Author { get; set; }
	}

	private sealed class GitHubCommitAuthor
	{
		public string Login { get; set; } = string.Empty;
	}

	private sealed class GitHubCommitListItem
	{
		public GitHubCommitAuthor? Author { get; set; }
	}

	private sealed class GitHubPrFileItem
	{
		[JsonPropertyName("filename")]
		public string? Filename { get; set; }

		[JsonPropertyName("status")]
		public string? Status { get; set; }
	}

	private sealed class GraphQlRequest
	{
		[JsonPropertyName("query")]
		public string Query { get; set; } = string.Empty;
	}

	private sealed class PrExistenceGraphQlResponse
	{
		[JsonPropertyName("data")]
		public PrExistenceData? Data { get; set; }

		[JsonPropertyName("errors")]
		public List<PrExistenceError>? Errors { get; set; }
	}

	private sealed class PrExistenceData
	{
		[JsonPropertyName("repository")]
		public Dictionary<string, PrExistenceNode?>? Repository { get; set; }
	}

	private sealed class PrExistenceNode
	{
		[JsonPropertyName("number")]
		public int Number { get; set; }
	}

	private sealed class PrExistenceError
	{
		[JsonPropertyName("message")]
		public string? Message { get; set; }

		[JsonPropertyName("path")]
		public List<string>? Path { get; set; }
	}

	[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
	[JsonSerializable(typeof(GitHubPrResponse))]
	[JsonSerializable(typeof(GitHubIssueResponse))]
	[JsonSerializable(typeof(GitHubLabel))]
	[JsonSerializable(typeof(List<GitHubLabel>))]
	[JsonSerializable(typeof(GitHubHeadRef))]
	[JsonSerializable(typeof(GitHubRepoRef))]
	[JsonSerializable(typeof(GitHubCommitResponse))]
	[JsonSerializable(typeof(GitHubCommitAuthor))]
	[JsonSerializable(typeof(GitHubCommitListItem))]
	[JsonSerializable(typeof(List<GitHubCommitListItem>))]
	[JsonSerializable(typeof(GitHubPrFileItem))]
	[JsonSerializable(typeof(List<GitHubPrFileItem>))]
	[JsonSerializable(typeof(GraphQlRequest))]
	[JsonSerializable(typeof(PrExistenceGraphQlResponse))]
	[JsonSerializable(typeof(PrExistenceData))]
	[JsonSerializable(typeof(Dictionary<string, PrExistenceNode?>))]
	[JsonSerializable(typeof(PrExistenceNode))]
	[JsonSerializable(typeof(PrExistenceError))]
	[JsonSerializable(typeof(List<PrExistenceError>))]
	private sealed partial class GitHubPrJsonContext : JsonSerializerContext;

	[GeneratedRegex(@"https://github\.com/([a-zA-Z0-9_-]+/[a-zA-Z0-9_-]+)/pull/(\d+)", RegexOptions.IgnoreCase
		| RegexOptions.CultureInvariant)]
	private static partial Regex MyRegex();
}
