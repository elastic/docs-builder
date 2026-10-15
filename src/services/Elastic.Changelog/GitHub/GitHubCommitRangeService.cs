// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Elastic.Changelog.Caching;
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
	GitHubApiTransport? transport = null,
	ChangelogCache? cache = null
) : IGitHubCommitRangeService
{
	private const int ComparePageSize = 100;
	private const int GraphQlBatchSize = 25;
	private const int PullRequestDetailBatchSize = 50;
	private const int MaxAssociatedPullRequests = 10;

	private readonly ILogger _logger = logFactory.CreateLogger<GitHubCommitRangeService>();
	private readonly GitHubApiTransport _transport = transport
		?? new GitHubApiTransport(logger: logFactory.CreateLogger<GitHubCommitRangeService>());

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
	/// Enumerates the commit shas in <c>start...end</c> via the compare REST API. The first page says how many commits there are,
	/// so the other pages are read side by side, and in order. A range of a minor release is dozens of pages.
	/// </summary>
	private async Task<IReadOnlyList<string>?> FetchCompareCommitsAsync(
		IDiagnosticsCollector collector,
		CommitRangeArguments args,
		Cancel ctx
	)
	{
		var basehead = $"{Uri.EscapeDataString(args.StartRef)}...{Uri.EscapeDataString(args.EndRef)}";
		var commits = new List<string>();

		var first = await FetchComparePageAsync(collector, args, basehead, 1, ctx).ConfigureAwait(false);
		if (first == null)
			return null;

		var totalCommits = first.TotalCommits;
		if (
			string.Equals(first.Status, "behind", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(first.Status, "identical", StringComparison.OrdinalIgnoreCase)
		)
		{
			collector.EmitWarning(
				string.Empty,
				$"Commit range {args.StartRef}...{args.EndRef} for {args.Owner}/{args.Repo} is '{first.Status}' — the end ref adds no commits over the start ref."
			);
			return [];
		}

		if (string.Equals(first.Status, "diverged", StringComparison.OrdinalIgnoreCase))
		{
			collector.EmitWarning(
				string.Empty,
				$"Refs {args.StartRef} and {args.EndRef} for {args.Owner}/{args.Repo} have diverged; only commits reachable from {args.EndRef} but not {args.StartRef} are considered."
			);
		}

		AddShas(commits, first);

		var lastPage = (totalCommits + ComparePageSize - 1) / ComparePageSize;
		if (lastPage > 1)
		{
			var rest = Enumerable.Range(2, lastPage - 1).ToList();
			var pages = await BatchRunner.RunAsync(
				rest,
				(page, token) => FetchComparePageAsync(collector, args, basehead, page, token),
				compare => compare == null,
				ctx
			).ConfigureAwait(false);
			if (pages == null)
				return null;

			foreach (var page in pages)
				AddShas(commits, page!);
		}

		// A page that comes back short, because GitHub caps it lower than asked, leaves a gap that the pages above did not
		// cover. Read on page by page until the count is complete or a page is empty, as a range was always read.
		for (var page = lastPage + 1; commits.Count < totalCommits; page++)
		{
			var more = await FetchComparePageAsync(collector, args, basehead, page, ctx).ConfigureAwait(false);
			if (more == null)
				return null;

			if (more.Commits is not { Count: > 0 })
				break;

			AddShas(commits, more);
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

	private static void AddShas(List<string> commits, GitHubCompareResponse page)
	{
		foreach (var commit in page.Commits ?? [])
		{
			if (!string.IsNullOrWhiteSpace(commit.Sha))
				commits.Add(commit.Sha);
		}
	}

	/// <summary>One page of the compare API, or <c>null</c> after reporting why it could not be read.</summary>
	private async Task<GitHubCompareResponse?> FetchComparePageAsync(
		IDiagnosticsCollector collector,
		CommitRangeArguments args,
		string basehead,
		int page,
		Cancel ctx
	)
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
		GitHubCompareResponse? compare;
		try
		{
			compare = JsonSerializer.Deserialize(json, CommitRangeJsonContext.Default.GitHubCompareResponse);
		}
		catch (JsonException)
		{
			compare = null;
		}

		if (compare == null)
			collector.EmitError(string.Empty, "Failed to deserialize GitHub compare response.");

		return compare;
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
		var excludedByBase = new Dictionary<int, CommitRangeExcludedPullRequest>();
		var repoFullName = $"{args.Owner}/{args.Repo}";

		var nodes = await ResolveCommitNodesAsync(collector, args, commits, repoFullName, ctx).ConfigureAwait(false);
		if (nodes is null)
			return null;

		foreach (var sha in commits)
		{
			_ = nodes.TryGetValue(sha, out var commitNode);
			var selected = SelectPullRequest(
				collector,
				sha,
				commitNode,
				repoFullName,
				args.BaseRef,
				excludedByBase,
				out var onlyExcludedByBase
			);
			if (selected == null)
			{
				// A commit whose merged PRs were all excluded by base branch is already reported as
				// excluded; only a commit with no merged PR at all is "without an associated PR".
				if (!onlyExcludedByBase)
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

		// Titles and labels change after a merge, so they are read on every run, for the pull requests of the range only.
		var details = await FetchPullRequestDetailsAsync(collector, args, orderedPrNumbers, ctx).ConfigureAwait(false);
		if (details is null)
			return null;

		var pullRequests = orderedPrNumbers.Select(number =>
		{
			var detail = details.GetValueOrDefault(number);
			return new CommitRangePullRequest
			{
				Number = number,
				Url = prsByNumber[number].Url,
				CommitShas = prsByNumber[number].Shas,
				Title = detail?.Title,
				Labels = detail?.Labels ?? [],
				UpdatedAt = detail?.UpdatedAt
			};
		}).ToList();

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
			CommitsWithoutPullRequest = commitsWithoutPr,
			ExcludedPullRequests = [.. excludedByBase.Values.OrderBy(pr => pr.Number)]
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
		string repoFullName,
		string? baseRef,
		Dictionary<int, CommitRangeExcludedPullRequest> excludedByBase,
		out bool onlyExcludedByBase
	)
	{
		onlyExcludedByBase = false;
		var merged = commitNode?.AssociatedPullRequests?.Nodes?.OfType<GraphQlPullRequest>()
			.Where(pr => pr.Merged && string.Equals(pr.BaseRepository?.NameWithOwner, repoFullName, StringComparison.OrdinalIgnoreCase))
			.ToList()
			?? [];

		// A pull request merged into another branch (for example a feature branch) reaches the range
		// when that branch later merges into the integration branch. Leave it to the PR that merged
		// the branch, and report it.
		var candidates = merged;
		if (!string.IsNullOrWhiteSpace(baseRef))
		{
			candidates = [.. merged.Where(pr => string.Equals(pr.BaseRefName, baseRef, StringComparison.Ordinal))];
			foreach (var pr in merged.Except(candidates))
			{
				if (!excludedByBase.ContainsKey(pr.Number))
				{
					excludedByBase[pr.Number] = new CommitRangeExcludedPullRequest
					{
						Number = pr.Number,
						Url = pr.Url ?? $"https://github.com/{repoFullName}/pull/{pr.Number}",
						BaseRef = pr.BaseRefName ?? string.Empty
					};
				}
			}
		}

		if (candidates.Count == 0)
		{
			onlyExcludedByBase = merged.Count > 0;
			return null;
		}

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

	/// <summary>
	/// The associated pull requests of every commit. What an earlier run learned comes from the cache, and only the commits it has
	/// not seen are asked of GitHub, which is where a long range spends its time. A commit with a merged pull request is
	/// remembered, because that fact does not change. One with none is remembered too, but only believed once it is a day old:
	/// GitHub may not have linked a young commit yet.
	/// </summary>
	private async Task<Dictionary<string, GraphQlCommit?>?> ResolveCommitNodesAsync(
		IDiagnosticsCollector collector,
		CommitRangeArguments args,
		IReadOnlyList<string> commits,
		string repoFullName,
		Cancel ctx
	)
	{
		var store = cache?.Commits(args.Owner, args.Repo);
		var nodes = new Dictionary<string, GraphQlCommit?>(StringComparer.Ordinal);
		var uncached = new List<string>();
		foreach (var sha in commits)
		{
			if (store is not null && store.TryGet(sha, out var known))
				nodes[sha] = ToNode(sha, known);
			else
				uncached.Add(sha);
		}

		var batches = uncached.Chunk(GraphQlBatchSize).ToList();
		var responses = await BatchRunner.RunAsync(
			batches,
			(batch, token) => FetchAssociatedPullRequestsBatchAsync(collector, args, batch, token),
			byAlias => byAlias == null,
			ctx
		).ConfigureAwait(false);
		if (responses == null)
			return null;

		for (var index = 0; index < batches.Count; index++)
		{
			for (var i = 0; i < batches[index].Length; i++)
			{
				var sha = batches[index][i];
				_ = responses[index]!.TryGetValue($"c{i}", out var node);
				nodes[sha] = node;
				if (store is null)
					continue;

				if (
					ToCacheable(node) is { Count: > 0 } merged
					&& merged.Any(a => string.Equals(a.BaseRepository, repoFullName, StringComparison.OrdinalIgnoreCase))
				)
					store.Set(sha, merged);
				else
					store.SetNoPullRequest(sha);
			}
		}

		if (store is not null)
			_logger.LogInformation(
				"{Cached} of {Total} commit(s) of {Owner}/{Repo} came from the cache, {Asked} were asked of GitHub",
				commits.Count - uncached.Count,
				commits.Count,
				args.Owner,
				args.Repo,
				uncached.Count
			);

		return nodes;
	}

	private static GraphQlCommit ToNode(string sha, IReadOnlyList<CachedAssociation> known) =>
		new()
		{
			Oid = sha,
			AssociatedPullRequests = new GraphQlPullRequestConnection
			{
				Nodes =
				[
					.. known.Select(
						a => new GraphQlPullRequest
						{
							Number = a.Number,
							Url = a.Url,
							Merged = a.Merged,
							BaseRefName = a.BaseRefName,
							MergeCommit = a.MergeCommit is null ? null : new GraphQlCommitRef { Oid = a.MergeCommit },
							BaseRepository = new GraphQlRepositoryRef { NameWithOwner = a.BaseRepository }
						}
					)
				]
			}
		};

	/// <summary>The merged pull requests of a commit, as the cache keeps them.</summary>
	private static List<CachedAssociation>? ToCacheable(GraphQlCommit? node) =>
		node?.AssociatedPullRequests?.Nodes?.OfType<GraphQlPullRequest>()
			.Where(pr => pr.Merged)
			.Select(
				pr => new CachedAssociation(
					pr.Number,
					pr.Url,
					pr.Merged,
					pr.BaseRefName,
					pr.MergeCommit?.Oid,
					pr.BaseRepository?.NameWithOwner
				)
			)
			.ToList();

	private sealed record PullRequestDetail(string? Title, IReadOnlyList<string> Labels, DateTimeOffset? UpdatedAt);

	/// <summary>Reads the title, the labels and the update time of the pull requests, 50 per query.</summary>
	private async Task<Dictionary<int, PullRequestDetail>?> FetchPullRequestDetailsAsync(
		IDiagnosticsCollector collector,
		CommitRangeArguments args,
		IReadOnlyList<int> numbers,
		Cancel ctx
	)
	{
		var batches = numbers.Chunk(PullRequestDetailBatchSize).ToList();
		var responses = await BatchRunner.RunAsync(
			batches,
			(batch, token) => QueryPullRequestDetailsAsync(collector, args, batch, token),
			data => data == null,
			ctx
		).ConfigureAwait(false);
		if (responses == null)
			return null;

		var details = new Dictionary<int, PullRequestDetail>();
		for (var index = 0; index < batches.Count; index++)
		{
			foreach (var number in batches[index])
			{
				if (responses[index]!.GetValueOrDefault($"p{number}") is not { } node)
					continue;

				node.Number = number;
				var labels = await CompleteLabelsAsync(collector, args, node, ctx).ConfigureAwait(false);
				if (labels == null)
					return null;

				details[number] = new PullRequestDetail(node.Title, labels, node.UpdatedAt);
			}
		}

		return details;
	}

	private async Task<Dictionary<string, GraphQlPullRequest?>?> QueryPullRequestDetailsAsync(
		IDiagnosticsCollector collector,
		CommitRangeArguments args,
		IReadOnlyList<int> batch,
		Cancel ctx
	)
	{
		var selection = string.Concat(
			batch.Select(
				n => string.Create(
					CultureInfo.InvariantCulture,
					$" p{n}: pullRequest(number: {n}) {{ title updatedAt labels(first: 100) {{ pageInfo {{ hasNextPage endCursor }} nodes {{ name }} }} }}"
				)
			)
		);
		var query = $"query {{ repository(owner: \"{args.Owner}\", name: \"{args.Repo}\") {{{selection} }} }}";
		var body = JsonSerializer.Serialize(new GraphQlRequest { Query = query }, CommitRangeJsonContext.Default.GraphQlRequest);

		using var response = await _transport.PostGraphQlOrNullAsync(
			body,
			failure => collector.EmitError(string.Empty, $"GitHub GraphQL request for {args.Owner}/{args.Repo} failed: {failure}"),
			ctx
		).ConfigureAwait(false);
		if (response is null)
			return null;

		var json = await response.Content.ReadAsStringAsync(ctx).ConfigureAwait(false);
		GraphQlPullRequestsResponse? parsed = null;
		if (response.IsSuccessStatusCode)
		{
			try
			{
				parsed = JsonSerializer.Deserialize(json, CommitRangeJsonContext.Default.GraphQlPullRequestsResponse);
			}
			catch (JsonException ex)
			{
				collector.EmitError(
					string.Empty,
					$"GitHub GraphQL request for {args.Owner}/{args.Repo} returned a response that is not valid JSON ({(int)response.StatusCode}, {json.Length} byte(s)): {ex.Message}"
				);
				return null;
			}
		}

		// A number that is not a pull request answers NOT_FOUND for its alias and the rest still comes back.
		if (parsed?.Data?.Repository is null || (parsed.Errors is { Count: > 0 } && !parsed.Errors.All(e => e.Type == "NOT_FOUND")))
		{
			collector.EmitError(
				string.Empty,
				$"GitHub GraphQL request for the pull requests of {args.Owner}/{args.Repo} failed: {(int)response.StatusCode} {response.ReasonPhrase}."
			);
			return null;
		}

		return parsed.Data.Repository;
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

		using var response = await _transport.PostGraphQlOrNullAsync(
			body,
			failure => collector.EmitError(string.Empty, $"GitHub GraphQL request for {args.Owner}/{args.Repo} failed: {failure}"),
			ctx
		).ConfigureAwait(false);
		if (response is null)
			return null;

		if (!response.IsSuccessStatusCode)
		{
			collector.EmitError(
				string.Empty,
				$"GitHub GraphQL request for {args.Owner}/{args.Repo} failed: {(int)response.StatusCode} {response.ReasonPhrase}."
			);
			return null;
		}

		var json = await response.Content.ReadAsStringAsync(ctx).ConfigureAwait(false);
		GraphQlResponse? parsed;
		try
		{
			parsed = JsonSerializer.Deserialize(json, CommitRangeJsonContext.Default.GraphQlResponse);
		}
		catch (JsonException ex)
		{
			collector.EmitError(
				string.Empty,
				$"GitHub GraphQL request for {args.Owner}/{args.Repo} returned a response that is not valid JSON ({(int)response.StatusCode}, {json.Length} byte(s)): {ex.Message}"
			);
			return null;
		}

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
	/// Returns every label of the pull request. The batch query returns the first page; a pull request with more
	/// labels is read page by page, because a missing label would hide a backport.
	/// </summary>
	private async Task<List<string>?> CompleteLabelsAsync(
		IDiagnosticsCollector collector,
		CommitRangeArguments args,
		GraphQlPullRequest pr,
		Cancel ctx
	)
	{
		var labels = pr.Labels?.Nodes?.Select(l => l?.Name).OfType<string>().ToList() ?? [];
		var page = pr.Labels?.PageInfo;
		while (page is { HasNextPage: true, EndCursor: { } cursor })
		{
			var query = $"query {{ repository(owner: \"{args.Owner}\", name: \"{args.Repo}\") {{ pullRequest(number: {pr.Number}) {{ "
				+ $"labels(first: 100, after: \"{cursor}\") {{ pageInfo {{ hasNextPage endCursor }} nodes {{ name }} }} }} }} }}";
			var body = JsonSerializer.Serialize(new GraphQlRequest { Query = query }, CommitRangeJsonContext.Default.GraphQlRequest);

			using var response = await _transport.PostGraphQlOrNullAsync(
				body,
				failure => collector.EmitError(
					string.Empty,
					$"GitHub GraphQL request for the labels of {args.Owner}/{args.Repo}#{pr.Number} failed: {failure}"
				),
				ctx
			).ConfigureAwait(false);
			if (response is null)
				return null;

			GraphQlLabelPageResponse? parsed = null;
			if (response.IsSuccessStatusCode)
			{
				var json = await response.Content.ReadAsStringAsync(ctx).ConfigureAwait(false);
				try
				{
					parsed = JsonSerializer.Deserialize(json, CommitRangeJsonContext.Default.GraphQlLabelPageResponse);
				}
				catch (JsonException ex)
				{
					collector.EmitError(
						string.Empty,
						$"GitHub GraphQL request for the labels of {args.Owner}/{args.Repo}#{pr.Number} returned a response that is not valid JSON ({(int)response.StatusCode}, {json.Length} byte(s)): {ex.Message}"
					);
					return null;
				}
			}

			var next = parsed?.Errors is { Count: > 0 } ? null : parsed?.Data?.Repository?.PullRequest?.Labels;
			if (next == null)
			{
				collector.EmitError(string.Empty, $"GitHub GraphQL request for the labels of {args.Owner}/{args.Repo}#{pr.Number} failed.");
				return null;
			}

			labels.AddRange(next.Nodes?.Select(l => l?.Name).OfType<string>() ?? []);
			page = next.PageInfo;
		}

		return labels;
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
				.Append(" number url merged baseRefName mergeCommit { oid } baseRepository { nameWithOwner }")
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

		[JsonPropertyName("type")]
		public string? Type { get; set; }
	}

	private sealed class GraphQlPullRequestsResponse
	{
		[JsonPropertyName("data")]
		public GraphQlPullRequestsData? Data { get; set; }

		[JsonPropertyName("errors")]
		public List<GraphQlError>? Errors { get; set; }
	}

	private sealed class GraphQlPullRequestsData
	{
		[JsonPropertyName("repository")]
		public Dictionary<string, GraphQlPullRequest?>? Repository { get; set; }
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

		[JsonPropertyName("title")]
		public string? Title { get; set; }

		[JsonPropertyName("updatedAt")]
		public DateTimeOffset? UpdatedAt { get; set; }

		[JsonPropertyName("labels")]
		public GraphQlLabelConnection? Labels { get; set; }

		[JsonPropertyName("merged")]
		public bool Merged { get; set; }

		[JsonPropertyName("baseRefName")]
		public string? BaseRefName { get; set; }

		[JsonPropertyName("mergeCommit")]
		public GraphQlCommitRef? MergeCommit { get; set; }

		[JsonPropertyName("baseRepository")]
		public GraphQlRepositoryRef? BaseRepository { get; set; }
	}

	private sealed class GraphQlPageInfo
	{
		[JsonPropertyName("hasNextPage")]
		public bool HasNextPage { get; set; }

		[JsonPropertyName("endCursor")]
		public string? EndCursor { get; set; }
	}

	private sealed class GraphQlLabelPageResponse
	{
		[JsonPropertyName("data")]
		public GraphQlLabelPageData? Data { get; set; }

		[JsonPropertyName("errors")]
		public List<GraphQlError>? Errors { get; set; }
	}

	private sealed class GraphQlLabelPageData
	{
		[JsonPropertyName("repository")]
		public GraphQlLabelPageRepository? Repository { get; set; }
	}

	private sealed class GraphQlLabelPageRepository
	{
		[JsonPropertyName("pullRequest")]
		public GraphQlPullRequest? PullRequest { get; set; }
	}

	private sealed class GraphQlLabelConnection
	{
		[JsonPropertyName("pageInfo")]
		public GraphQlPageInfo? PageInfo { get; set; }

		[JsonPropertyName("nodes")]
		public List<GraphQlLabel?>? Nodes { get; set; }
	}

	private sealed class GraphQlLabel
	{
		[JsonPropertyName("name")]
		public string? Name { get; set; }
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
	[JsonSerializable(typeof(GraphQlLabelPageResponse))]
	[JsonSerializable(typeof(GraphQlPullRequestsResponse))]
	private sealed partial class CommitRangeJsonContext : JsonSerializerContext;
}
