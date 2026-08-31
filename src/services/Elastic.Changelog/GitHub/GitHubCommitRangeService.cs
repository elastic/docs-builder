// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.GitHub;

/// <summary>
/// Resolves the merged pull requests contained in a commit range using the GitHub compare REST API
/// (commit enumeration, paginated) and the GraphQL <c>associatedPullRequests</c> connection
/// (commit → PR association). Works for squash and merge commits on protected integration branches;
/// commits with no associated merged PR are reported, not silently dropped.
/// </summary>
public sealed partial class GitHubCommitRangeService(
	ILoggerFactory logFactory,
	GitHubApiTransport? transport = null
) : IGitHubCommitRangeService
{
	private const int ComparePageSize = 100;
	private const int GraphQlBatchSize = 50;
	private const int MaxAssociatedPullRequests = 10;

	private readonly ILogger _logger = logFactory.CreateLogger<GitHubCommitRangeService>();
	private readonly GitHubApiTransport _transport = transport ?? new GitHubApiTransport();

	[GeneratedRegex("^[0-9a-fA-F]{7,40}$")]
	private static partial Regex CommitShaRegex();

	[GeneratedRegex("^[A-Za-z0-9_.-]+$")]
	private static partial Regex SafeGraphQlIdentifierRegex();

	/// <inheritdoc />
	public async Task<CommitRangeResolution?> ResolvePullRequestsAsync(
		IDiagnosticsCollector collector,
		CommitRangeArguments args,
		Cancel ctx
	)
	{
		var token = _transport.ResolveToken();
		if (string.IsNullOrWhiteSpace(token))
		{
			collector.EmitError(
				string.Empty,
				"Resolving pull requests from a commit range requires GitHub credentials. " +
					"Set the GITHUB_TOKEN environment variable (the GraphQL API used for commit→PR association does not accept anonymous requests)."
			);
			return null;
		}

		if (!SafeGraphQlIdentifierRegex().IsMatch(args.Owner) || !SafeGraphQlIdentifierRegex().IsMatch(args.Repo))
		{
			collector.EmitError(
				string.Empty,
				$"Invalid repository '{args.Owner}/{args.Repo}': owner and repo must contain only letters, digits, '.', '_' or '-'."
			);
			return null;
		}

		var commits = await FetchCompareCommitsAsync(collector, args, ctx).ConfigureAwait(false);
		if (commits == null)
			return null;

		if (commits.Count == 0)
		{
			collector.EmitWarning(
				string.Empty,
				$"Commit range {args.StartRef}..{args.EndRef} for {args.Owner}/{args.Repo} contains no commits."
			);
			return new CommitRangeResolution { TotalCommits = 0, PullRequests = [], CommitsWithoutPullRequest = [] };
		}

		return await AssociatePullRequestsAsync(collector, args, commits, ctx).ConfigureAwait(false);
	}

	/// <summary>
	/// Enumerates the commit shas in <c>start...end</c> via the compare REST API, following
	/// pagination until <c>total_commits</c> commits have been collected.
	/// </summary>
	private async Task<IReadOnlyList<string>?> FetchCompareCommitsAsync(
		IDiagnosticsCollector collector,
		CommitRangeArguments args,
		Cancel ctx
	)
	{
		var basehead = $"{Uri.EscapeDataString(args.StartRef)}...{Uri.EscapeDataString(args.EndRef)}";
		var commits = new List<string>();
		var totalCommits = 0;
		var page = 1;

		while (true)
		{
			var url = $"https://api.github.com/repos/{args.Owner}/{args.Repo}/compare/{basehead}?per_page={ComparePageSize}&page={page}";
			_logger.LogDebug("Fetching compare page {Page}: {Url}", page, url);

			using var response = await _transport.GetAsync(url, ctx).ConfigureAwait(false);
			if (response.StatusCode == HttpStatusCode.NotFound)
			{
				collector.EmitError(
					string.Empty,
					$"GitHub could not compare {args.StartRef}...{args.EndRef} in {args.Owner}/{args.Repo} (404). " +
						"Ensure both refs exist in the repository and the token can read it."
				);
				return null;
			}

			if (!response.IsSuccessStatusCode)
			{
				collector.EmitError(
					string.Empty,
					$"GitHub compare request for {args.Owner}/{args.Repo} {args.StartRef}...{args.EndRef} failed: {(int)response.StatusCode} {response.ReasonPhrase}."
				);
				return null;
			}

			var json = await response.Content.ReadAsStringAsync(ctx).ConfigureAwait(false);
			var compare = JsonSerializer.Deserialize(json, CommitRangeJsonContext.Default.GitHubCompareResponse);
			if (compare == null)
			{
				collector.EmitError(string.Empty, "Failed to deserialize GitHub compare response.");
				return null;
			}

			if (page == 1)
			{
				totalCommits = compare.TotalCommits;
				if (
					string.Equals(compare.Status, "behind", StringComparison.OrdinalIgnoreCase)
					|| string.Equals(compare.Status, "identical", StringComparison.OrdinalIgnoreCase)
				)
				{
					collector.EmitWarning(
						string.Empty,
						$"Commit range {args.StartRef}...{args.EndRef} for {args.Owner}/{args.Repo} is '{compare.Status}' — the end ref adds no commits over the start ref."
					);
					return [];
				}

				if (string.Equals(compare.Status, "diverged", StringComparison.OrdinalIgnoreCase))
				{
					collector.EmitWarning(
						string.Empty,
						$"Refs {args.StartRef} and {args.EndRef} for {args.Owner}/{args.Repo} have diverged; only commits reachable from {args.EndRef} but not {args.StartRef} are considered."
					);
				}
			}

			var pageCommits = compare.Commits ?? [];
			foreach (var commit in pageCommits)
			{
				if (!string.IsNullOrWhiteSpace(commit.Sha))
					commits.Add(commit.Sha);
			}

			if (commits.Count >= totalCommits || pageCommits.Count == 0)
				break;
			page++;
		}

		_logger.LogInformation(
			"Compare {Start}...{End} for {Owner}/{Repo}: {Count} commit(s)",
			args.StartRef,
			args.EndRef,
			args.Owner,
			args.Repo,
			commits.Count
		);

		if (commits.Count < totalCommits)
		{
			collector.EmitError(
				string.Empty,
				$"GitHub compare pagination for {args.Owner}/{args.Repo} returned {commits.Count} of {totalCommits} commits; refusing to resolve a partial range."
			);
			return null;
		}

		return commits;
	}

	/// <summary>
	/// Resolves each commit to its merged pull request via GraphQL <c>associatedPullRequests</c>,
	/// batching commits per query and caching nothing across runs (each run is self-contained).
	/// </summary>
	private async Task<CommitRangeResolution?> AssociatePullRequestsAsync(
		IDiagnosticsCollector collector,
		CommitRangeArguments args,
		IReadOnlyList<string> commits,
		Cancel ctx
	)
	{
		var invalidShas = commits.Where(sha => !CommitShaRegex().IsMatch(sha)).ToList();
		if (invalidShas.Count > 0)
		{
			collector.EmitError(string.Empty, $"GitHub compare returned malformed commit sha(s): {string.Join(", ", invalidShas)}.");
			return null;
		}

		var prsByNumber = new Dictionary<int, (string Url, List<string> Shas)>();
		var orderedPrNumbers = new List<int>();
		var commitsWithoutPr = new List<string>();
		var repoFullName = $"{args.Owner}/{args.Repo}";

		for (var offset = 0; offset < commits.Count; offset += GraphQlBatchSize)
		{
			var batch = commits.Skip(offset).Take(GraphQlBatchSize).ToList();
			var byAlias = await FetchAssociatedPullRequestsBatchAsync(collector, args, batch, ctx).ConfigureAwait(false);
			if (byAlias == null)
				return null;

			for (var i = 0; i < batch.Count; i++)
			{
				var sha = batch[i];
				_ = byAlias.TryGetValue($"c{i}", out var commitNode);
				var selected = SelectPullRequest(collector, sha, commitNode, repoFullName);
				if (selected == null)
				{
					commitsWithoutPr.Add(sha);
					continue;
				}

				if (prsByNumber.TryGetValue(selected.Number, out var existing))
					existing.Shas.Add(sha);
				else
				{
					prsByNumber[selected.Number] = (selected.Url ?? $"https://github.com/{repoFullName}/pull/{selected.Number}", [sha]);
					orderedPrNumbers.Add(selected.Number);
				}
			}
		}

		var pullRequests = orderedPrNumbers.Select(
			number => new CommitRangePullRequest { Number = number, Url = prsByNumber[number].Url, CommitShas = prsByNumber[number].Shas }
		).ToList();

		_logger.LogInformation(
			"Resolved {PrCount} pull request(s) from {CommitCount} commit(s) in {Owner}/{Repo} {Start}...{End} ({NoPrCount} commit(s) without an associated PR)",
			pullRequests.Count,
			commits.Count,
			args.Owner,
			args.Repo,
			args.StartRef,
			args.EndRef,
			commitsWithoutPr.Count
		);

		return new CommitRangeResolution
		{
			TotalCommits = commits.Count,
			PullRequests = pullRequests,
			CommitsWithoutPullRequest = commitsWithoutPr
		};
	}

	/// <summary>
	/// Picks the merged, same-repository pull request for a commit. Prefers the PR whose merge
	/// commit is the commit itself (squash/merge on the integration branch); warns on ambiguity
	/// and resolves it deterministically by lowest PR number (RFC review: don't overdesign before
	/// the first real deploy).
	/// </summary>
	private static GraphQlPullRequest? SelectPullRequest(
		IDiagnosticsCollector collector,
		string sha,
		GraphQlCommit? commitNode,
		string repoFullName
	)
	{
		var candidates = commitNode?.AssociatedPullRequests?.Nodes?.OfType<GraphQlPullRequest>()
			.Where(pr => pr.Merged && string.Equals(pr.BaseRepository?.NameWithOwner, repoFullName, StringComparison.OrdinalIgnoreCase))
			.ToList()
			?? [];

		if (candidates.Count == 0)
			return null;

		var mergeCommitMatches = candidates
			.Where(pr => string.Equals(pr.MergeCommit?.Oid, sha, StringComparison.OrdinalIgnoreCase))
			.OrderBy(pr => pr.Number)
			.ToList();

		if (mergeCommitMatches.Count == 1)
			return mergeCommitMatches[0];

		if (candidates.Count > 1)
		{
			var pool = mergeCommitMatches.Count > 0 ? mergeCommitMatches : candidates;
			var chosen = pool.OrderBy(pr => pr.Number).First();
			var numbers = string.Join(", ", candidates.Select(pr => pr.Number).Order().Select(n => $"#{n}"));
			collector.EmitWarning(
				string.Empty,
				$"Commit {sha} is associated with multiple merged pull requests ({numbers}); using #{chosen.Number}."
			);
			return chosen;
		}

		return candidates[0];
	}

	private async Task<Dictionary<string, GraphQlCommit?>?> FetchAssociatedPullRequestsBatchAsync(
		IDiagnosticsCollector collector,
		CommitRangeArguments args,
		IReadOnlyList<string> shas,
		Cancel ctx
	)
	{
		var query = BuildBatchQuery(args.Owner, args.Repo, shas);
		var body = JsonSerializer.Serialize(new GraphQlRequest { Query = query }, CommitRangeJsonContext.Default.GraphQlRequest);

		using var response = await _transport.PostGraphQlAsync(body, ctx).ConfigureAwait(false);
		if (!response.IsSuccessStatusCode)
		{
			collector.EmitError(
				string.Empty,
				$"GitHub GraphQL request for {args.Owner}/{args.Repo} failed: {(int)response.StatusCode} {response.ReasonPhrase}."
			);
			return null;
		}

		var json = await response.Content.ReadAsStringAsync(ctx).ConfigureAwait(false);
		var parsed = JsonSerializer.Deserialize(json, CommitRangeJsonContext.Default.GraphQlResponse);

		if (parsed?.Errors is { Count: > 0 })
		{
			var messages = string.Join("; ", parsed.Errors.Select(e => e.Message).Where(m => !string.IsNullOrWhiteSpace(m)));
			collector.EmitError(string.Empty, $"GitHub GraphQL query for {args.Owner}/{args.Repo} returned errors: {messages}.");
			return null;
		}

		if (parsed?.Data?.Repository == null)
		{
			collector.EmitError(
				string.Empty,
				$"GitHub GraphQL query could not resolve repository {args.Owner}/{args.Repo}. Ensure the token can read it."
			);
			return null;
		}

		return parsed.Data.Repository;
	}

	/// <summary>
	/// Builds one aliased GraphQL query resolving <c>associatedPullRequests</c> for every sha in
	/// the batch. Shas are validated hex and owner/repo validated identifiers before interpolation.
	/// </summary>
	private static string BuildBatchQuery(string owner, string repo, IReadOnlyList<string> shas)
	{
		var sb = new StringBuilder();
		_ = sb.Append("query { repository(owner: \"").Append(owner).Append("\", name: \"").Append(repo).Append("\") {");
		for (var i = 0; i < shas.Count; i++)
		{
			_ = sb
				.Append(" c")
				.Append(i)
				.Append(": object(oid: \"")
				.Append(shas[i])
				.Append("\") { ... on Commit {")
				.Append(" oid associatedPullRequests(first: ")
				.Append(MaxAssociatedPullRequests)
				.Append(") { nodes {")
				.Append(" number url merged mergeCommit { oid } baseRepository { nameWithOwner }")
				.Append(" } } } }");
		}

		_ = sb.Append(" } }");
		return sb.ToString();
	}

	private sealed class GitHubCompareResponse
	{
		[JsonPropertyName("status")]
		public string? Status { get; set; }

		[JsonPropertyName("total_commits")]
		public int TotalCommits { get; set; }

		[JsonPropertyName("commits")]
		public List<GitHubCompareCommit>? Commits { get; set; }
	}

	private sealed class GitHubCompareCommit
	{
		[JsonPropertyName("sha")]
		public string? Sha { get; set; }
	}

	private sealed class GraphQlRequest
	{
		[JsonPropertyName("query")]
		public string Query { get; set; } = string.Empty;
	}

	private sealed class GraphQlResponse
	{
		[JsonPropertyName("data")]
		public GraphQlData? Data { get; set; }

		[JsonPropertyName("errors")]
		public List<GraphQlError>? Errors { get; set; }
	}

	private sealed class GraphQlError
	{
		[JsonPropertyName("message")]
		public string? Message { get; set; }
	}

	private sealed class GraphQlData
	{
		[JsonPropertyName("repository")]
		public Dictionary<string, GraphQlCommit?>? Repository { get; set; }
	}

	private sealed class GraphQlCommit
	{
		[JsonPropertyName("oid")]
		public string? Oid { get; set; }

		[JsonPropertyName("associatedPullRequests")]
		public GraphQlPullRequestConnection? AssociatedPullRequests { get; set; }
	}

	private sealed class GraphQlPullRequestConnection
	{
		[JsonPropertyName("nodes")]
		public List<GraphQlPullRequest?>? Nodes { get; set; }
	}

	private sealed class GraphQlPullRequest
	{
		[JsonPropertyName("number")]
		public int Number { get; set; }

		[JsonPropertyName("url")]
		public string? Url { get; set; }

		[JsonPropertyName("merged")]
		public bool Merged { get; set; }

		[JsonPropertyName("mergeCommit")]
		public GraphQlCommitRef? MergeCommit { get; set; }

		[JsonPropertyName("baseRepository")]
		public GraphQlRepositoryRef? BaseRepository { get; set; }
	}

	private sealed class GraphQlCommitRef
	{
		[JsonPropertyName("oid")]
		public string? Oid { get; set; }
	}

	private sealed class GraphQlRepositoryRef
	{
		[JsonPropertyName("nameWithOwner")]
		public string? NameWithOwner { get; set; }
	}

	[JsonSerializable(typeof(GitHubCompareResponse))]
	[JsonSerializable(typeof(GraphQlRequest))]
	[JsonSerializable(typeof(GraphQlResponse))]
	private sealed partial class CommitRangeJsonContext : JsonSerializerContext;
}
