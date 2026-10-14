// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.GitHub;

/// <summary>
/// Finds the original pull request of each backport. It reads the title, body and commits of the backports,
/// resolves cherry-pick trailers to pull requests, and keeps only numbers that are merged pull requests on the
/// default branch of the same repository, which drops issue numbers and references to other repositories.
/// The reading lives in <see cref="BackportReferences"/>. Everything here is batched GraphQL.
/// </summary>
public sealed partial class GitHubBackportService(ILoggerFactory logFactory, GitHubApiTransport? transport = null) : IGitHubBackportService
{
	private const int PullRequestBatchSize = 20;
	private const int CommitBatchSize = 30;
	private const int ValidationBatchSize = 25;

	private readonly ILogger _logger = logFactory.CreateLogger<GitHubBackportService>();
	private readonly GitHubApiTransport _transport = transport ?? new GitHubApiTransport();

	[GeneratedRegex("^[A-Za-z0-9_.-]+$")]
	private static partial Regex SafeIdentifierRegex();

	[GeneratedRegex("^[0-9a-f]{40}$")]
	private static partial Regex ShaRegex();

	/// <inheritdoc />
	public async Task<IReadOnlyDictionary<int, BackportRoots?>?> ResolveRootsAsync(
		IDiagnosticsCollector collector,
		string owner,
		string repo,
		IReadOnlyCollection<int> backportNumbers,
		Cancel ctx
	)
	{
		var result = backportNumbers.ToDictionary(n => n, _ => (BackportRoots?)null);
		if (backportNumbers.Count == 0)
			return result;

		if (string.IsNullOrWhiteSpace(_transport.ResolveToken()))
		{
			collector.EmitError(string.Empty, "Tracing backports requires GitHub credentials. Set the GITHUB_TOKEN environment variable.");
			return null;
		}

		if (!SafeIdentifierRegex().IsMatch(owner) || !SafeIdentifierRegex().IsMatch(repo))
		{
			collector.EmitError(
				string.Empty,
				$"Invalid repository '{owner}/{repo}': owner and repo must contain only letters, digits, '.', '_' or '-'."
			);
			return null;
		}

		var repoName = $"{owner}/{repo}";
		var defaultBranch = await FetchDefaultBranchAsync(collector, owner, repo, ctx).ConfigureAwait(false);
		if (defaultBranch == null)
			return null;

		var pullRequests = await FetchPullRequestsAsync(collector, owner, repo, [.. backportNumbers.Order()], ctx).ConfigureAwait(false);
		if (pullRequests == null)
			return null;

		var candidates = new Dictionary<int, BackportCandidates>();
		foreach (var (number, node) in pullRequests)
		{
			var messages = node.Commits?.Nodes?.Select(c => c?.Commit?.Message).OfType<string>() ?? [];
			candidates[number] = BackportReferences.Extract(owner, repo, number, node.Title ?? string.Empty, node.Body, messages);
		}

		var trailerRoots = await ResolveTrailersAsync(collector, owner, repo, repoName, defaultBranch, candidates, ctx).ConfigureAwait(
			false
		);
		if (trailerRoots == null)
			return null;

		var textNumbers = candidates.Values.SelectMany(c => c.SubjectRefs.Concat(c.TitleRefs).Concat(c.BodyRefs)).Distinct().ToList();
		var valid = await ValidateAsync(collector, owner, repo, repoName, defaultBranch, textNumbers, ctx).ConfigureAwait(false);
		if (valid == null)
			return null;

		foreach (var (number, c) in candidates)
		{
			var trailer = c
				.CherryPickShas
				.SelectMany(sha => trailerRoots.GetValueOrDefault(sha) ?? [])
				.Where(n => n != number)
				.Distinct()
				.ToList();
			result[number] = BackportReferences.Choose(
				trailer,
				[.. c.SubjectRefs.Where(valid.Contains)],
				[.. c.TitleRefs.Where(valid.Contains)],
				[.. c.BodyRefs.Where(valid.Contains)]
			);
		}

		_logger.LogInformation(
			"Traced {Resolved} of {Total} backport(s) in {Repo} to their original pull request",
			result.Values.Count(r => r != null),
			result.Count,
			repoName
		);
		return result;
	}

	private async Task<string?> FetchDefaultBranchAsync(IDiagnosticsCollector collector, string owner, string repo, Cancel ctx)
	{
		var data = await QueryAsync(collector, owner, repo, " def: defaultBranchRef { name }", ctx).ConfigureAwait(false);
		var name = data?.GetValueOrDefault("def")?.Name;
		if (string.IsNullOrWhiteSpace(name))
		{
			collector.EmitError(string.Empty, $"GitHub did not return the default branch of {owner}/{repo}.");
			return null;
		}

		return name;
	}

	private async Task<Dictionary<int, Node>?> FetchPullRequestsAsync(
		IDiagnosticsCollector collector,
		string owner,
		string repo,
		IReadOnlyList<int> numbers,
		Cancel ctx
	)
	{
		var found = new Dictionary<int, Node>();
		for (var offset = 0; offset < numbers.Count; offset += PullRequestBatchSize)
		{
			var batch = numbers.Skip(offset).Take(PullRequestBatchSize).ToList();
			var selection = new StringBuilder();
			foreach (var n in batch)
				_ = selection.Append(
					CultureInfo.InvariantCulture,
					$" p{n}: pullRequest(number: {n}) {{ title body commits(first: 50) {{ nodes {{ commit {{ message }} }} }} }}"
				);

			var data = await QueryAsync(collector, owner, repo, selection.ToString(), ctx).ConfigureAwait(false);
			if (data == null)
				return null;

			foreach (var n in batch)
			{
				if (data.GetValueOrDefault($"p{n}") is { } node)
					found[n] = node;
				else
					_logger.LogWarning("Backport #{Number} of {Owner}/{Repo} could not be read", n, owner, repo);
			}
		}

		return found;
	}

	/// <summary>Resolves each cherry-picked commit to the merged pull requests on the default branch that contain it.</summary>
	private async Task<Dictionary<string, List<int>>?> ResolveTrailersAsync(
		IDiagnosticsCollector collector,
		string owner,
		string repo,
		string repoName,
		string defaultBranch,
		Dictionary<int, BackportCandidates> candidates,
		Cancel ctx
	)
	{
		var shas = candidates.Values.SelectMany(c => c.CherryPickShas).Where(s => ShaRegex().IsMatch(s)).Distinct().ToList();
		var roots = new Dictionary<string, List<int>>();
		for (var offset = 0; offset < shas.Count; offset += CommitBatchSize)
		{
			var batch = shas.Skip(offset).Take(CommitBatchSize).ToList();
			var selection = new StringBuilder();
			for (var i = 0; i < batch.Count; i++)
				_ = selection.Append(
					CultureInfo.InvariantCulture,
					$" c{i}: object(oid: \"{batch[i]}\") {{ ... on Commit {{ associatedPullRequests(first: 10) {{ nodes {{ number merged baseRefName baseRepository {{ nameWithOwner }} }} }} }} }}"
				);

			var data = await QueryAsync(collector, owner, repo, selection.ToString(), ctx).ConfigureAwait(false);
			if (data == null)
				return null;

			for (var i = 0; i < batch.Count; i++)
			{
				var nodes = data.GetValueOrDefault($"c{i}")?.AssociatedPullRequests?.Nodes?.OfType<Node>() ?? [];
				roots[batch[i]] = [.. nodes.Where(n => IsMergedOnDefaultBranch(n, repoName, defaultBranch)).Select(n => n.Number)];
			}
		}

		return roots;
	}

	/// <summary>Keeps the numbers that are merged pull requests on the default branch of this repository.</summary>
	private async Task<HashSet<int>?> ValidateAsync(
		IDiagnosticsCollector collector,
		string owner,
		string repo,
		string repoName,
		string defaultBranch,
		IReadOnlyList<int> numbers,
		Cancel ctx
	)
	{
		var valid = new HashSet<int>();
		for (var offset = 0; offset < numbers.Count; offset += ValidationBatchSize)
		{
			var batch = numbers.Skip(offset).Take(ValidationBatchSize).ToList();
			var selection = new StringBuilder();
			foreach (var n in batch)
				_ = selection.Append(
					CultureInfo.InvariantCulture,
					$" v{n}: pullRequest(number: {n}) {{ number merged baseRefName baseRepository {{ nameWithOwner }} }}"
				);

			// A number that is not a pull request (an issue) makes GitHub answer with a "not found" error for that alias
			// and still return the other aliases, so errors are tolerated here and the missing node simply does not count.
			var data = await QueryAsync(collector, owner, repo, selection.ToString(), ctx, tolerateErrors: true).ConfigureAwait(false);
			if (data == null)
				return null;

			foreach (var n in batch)
			{
				if (
					data.GetValueOrDefault($"v{n}") is { } node
					&& IsMergedOnDefaultBranch(node with { Number = n }, repoName, defaultBranch)
				)
					_ = valid.Add(n);
			}
		}

		return valid;
	}

	private static bool IsMergedOnDefaultBranch(Node node, string repoName, string defaultBranch) =>
		node.Merged
			&& string.Equals(node.BaseRefName, defaultBranch, StringComparison.Ordinal)
			&& string.Equals(node.BaseRepository?.NameWithOwner, repoName, StringComparison.OrdinalIgnoreCase);

	private async Task<Dictionary<string, Node?>?> QueryAsync(
		IDiagnosticsCollector collector,
		string owner,
		string repo,
		string selection,
		Cancel ctx,
		bool tolerateErrors = false
	)
	{
		var query = $"query {{ repository(owner: \"{owner}\", name: \"{repo}\") {{{selection} }} }}";
		var body = JsonSerializer.Serialize(new Request { Query = query }, BackportJsonContext.Default.Request);

		using var response = await _transport.PostGraphQlAsync(body, ctx).ConfigureAwait(false);
		if (!response.IsSuccessStatusCode)
		{
			collector.EmitError(
				string.Empty,
				$"GitHub GraphQL request for {owner}/{repo} failed: {(int)response.StatusCode} {response.ReasonPhrase}."
			);
			return null;
		}

		var json = await response.Content.ReadAsStringAsync(ctx).ConfigureAwait(false);
		var parsed = JsonSerializer.Deserialize(json, BackportJsonContext.Default.Response);

		if (!tolerateErrors && parsed?.Errors is { Count: > 0 })
		{
			var messages = string.Join("; ", parsed.Errors.Select(e => e.Message).Where(m => !string.IsNullOrWhiteSpace(m)));
			collector.EmitError(string.Empty, $"GitHub GraphQL query for {owner}/{repo} returned errors: {messages}.");
			return null;
		}

		if (parsed?.Data?.Repository == null)
		{
			collector.EmitError(
				string.Empty,
				$"GitHub GraphQL query could not resolve repository {owner}/{repo}. Ensure the token can read it."
			);
			return null;
		}

		return parsed.Data.Repository;
	}

	private sealed class Request
	{
		[JsonPropertyName("query")]
		public string Query { get; set; } = string.Empty;
	}

	private sealed class Response
	{
		[JsonPropertyName("data")]
		public ResponseData? Data { get; set; }

		[JsonPropertyName("errors")]
		public List<ResponseError>? Errors { get; set; }
	}

	private sealed class ResponseError
	{
		[JsonPropertyName("message")]
		public string? Message { get; set; }
	}

	private sealed class ResponseData
	{
		[JsonPropertyName("repository")]
		public Dictionary<string, Node?>? Repository { get; set; }
	}

	/// <summary>One shape for every aliased node: a pull request, a commit with its pull requests, or the default branch.</summary>
	private sealed record Node
	{
		[JsonPropertyName("number")]
		public int Number { get; set; }

		[JsonPropertyName("title")]
		public string? Title { get; set; }

		[JsonPropertyName("body")]
		public string? Body { get; set; }

		[JsonPropertyName("merged")]
		public bool Merged { get; set; }

		[JsonPropertyName("baseRefName")]
		public string? BaseRefName { get; set; }

		[JsonPropertyName("name")]
		public string? Name { get; set; }

		[JsonPropertyName("baseRepository")]
		public RepositoryRef? BaseRepository { get; set; }

		[JsonPropertyName("commits")]
		public CommitConnection? Commits { get; set; }

		[JsonPropertyName("associatedPullRequests")]
		public PullRequestConnection? AssociatedPullRequests { get; set; }
	}

	private sealed class RepositoryRef
	{
		[JsonPropertyName("nameWithOwner")]
		public string? NameWithOwner { get; set; }
	}

	private sealed class CommitConnection
	{
		[JsonPropertyName("nodes")]
		public List<CommitNode?>? Nodes { get; set; }
	}

	private sealed class CommitNode
	{
		[JsonPropertyName("commit")]
		public CommitMessage? Commit { get; set; }
	}

	private sealed class CommitMessage
	{
		[JsonPropertyName("message")]
		public string? Message { get; set; }
	}

	private sealed class PullRequestConnection
	{
		[JsonPropertyName("nodes")]
		public List<Node?>? Nodes { get; set; }
	}

	[JsonSerializable(typeof(Request))]
	[JsonSerializable(typeof(Response))]
	private sealed partial class BackportJsonContext : JsonSerializerContext;
}
