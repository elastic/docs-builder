// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Elastic.Changelog.Caching;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.GitHub;

/// <summary>
/// Reads the pull requests of a range 50 at a time through GraphQL, where REST needs one request each. A range of a minor
/// release holds thousands of pull requests. The body is reduced to its release note and linked issues on arrival.
/// <para>
/// A pull request that comes from a commit range already carries its title, labels and update time, so only its body is read,
/// and not even that when a run has read it for this state of the pull request: what is derived from the body is kept in the
/// cache until the <c>updatedAt</c> of the pull request moves.
/// </para>
/// </summary>
public sealed partial class GraphQlPullRequestSummaryProvider(
	ILoggerFactory logFactory,
	GitHubApiTransport? transport = null,
	ChangelogCache? cache = null
) : IPullRequestSummaryProvider
{
	private const int BatchSize = 50;
	private const int LabelPageSize = 100;

	private readonly ILogger _logger = logFactory.CreateLogger<GraphQlPullRequestSummaryProvider>();
	private readonly GitHubApiTransport _transport = transport
		?? new GitHubApiTransport(logger: logFactory.CreateLogger<GraphQlPullRequestSummaryProvider>());

	[GeneratedRegex("^[A-Za-z0-9_.-]+$")]
	private static partial Regex SafeIdentifierRegex();

	public async Task<IReadOnlyDictionary<int, PullRequestSummary?>?> GetAsync(
		IDiagnosticsCollector collector,
		string owner,
		string repo,
		IReadOnlyCollection<CommitRangePullRequest> pullRequests,
		Cancel ctx
	)
	{
		var distinct = pullRequests.GroupBy(pr => pr.Number).Select(g => g.First()).OrderBy(pr => pr.Number).ToList();
		var result = distinct.ToDictionary(pr => pr.Number, _ => (PullRequestSummary?)null);
		if (distinct.Count == 0)
			return result;

		if (string.IsNullOrWhiteSpace(_transport.ResolveToken()))
		{
			collector.EmitError(
				string.Empty,
				"Reading pull requests requires GitHub credentials. Set the GITHUB_TOKEN environment variable."
			);
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

		var store = cache?.PullRequestTexts(owner, repo);

		// A pull request of a range knows its title, labels and update time. One that stands in for a backport does not, so its
		// title, labels and update time are read first, which is a small query. The body is read only when no run has read it
		// for this state of the pull request.
		var known = new List<KnownPullRequest>();
		var titleless = new List<int>();
		foreach (var pr in distinct)
		{
			if (pr.Title is null)
				titleless.Add(pr.Number);
			else
				known.Add(new KnownPullRequest(pr.Number, pr.Title, pr.Labels, pr.UpdatedAt));
		}

		var details = await ReadAsync(collector, owner, repo, titleless, ReadKind.Details, ctx).ConfigureAwait(false);
		if (details is null)
			return null;

		foreach (var number in titleless)
		{
			if (details.GetValueOrDefault(number) is not { } node)
			{
				_logger.LogWarning("Pull request #{Number} of {Owner}/{Repo} could not be read", number, owner, repo);
				continue;
			}

			var labels = await AllLabelsAsync(collector, owner, repo, node, ctx).ConfigureAwait(false);
			if (labels is null)
				return null;

			known.Add(new KnownPullRequest(number, node.Title ?? string.Empty, labels, node.UpdatedAt));
		}

		var needBody = new List<KnownPullRequest>();
		var cached = 0;
		foreach (var pr in known)
		{
			if (store is not null && pr.UpdatedAt is { } updatedAt && store.TryGet(pr.Number, updatedAt, out var text))
			{
				result[pr.Number] = Summarize(pr.Number, pr.Title, pr.Labels, text);
				cached++;
			}
			else
				needBody.Add(pr);
		}

		var bodies = await ReadAsync(collector, owner, repo, needBody.Select(pr => pr.Number).ToList(), ReadKind.Body, ctx).ConfigureAwait(
			false
		);
		if (bodies is null)
			return null;

		foreach (var pr in needBody)
		{
			if (bodies.GetValueOrDefault(pr.Number) is not { } node)
			{
				_logger.LogWarning("Pull request #{Number} of {Owner}/{Repo} could not be read", pr.Number, owner, repo);
				continue;
			}

			var text = TextOf(owner, repo, node);
			if (pr.UpdatedAt is { } updatedAt)
				store?.Set(pr.Number, updatedAt, text);

			result[pr.Number] = Summarize(pr.Number, pr.Title, pr.Labels, text);
		}

		_logger.LogInformation(
			"Read {Count} pull request(s) of {Owner}/{Repo}: {Cached} from the cache, {Asked} from GitHub",
			distinct.Count,
			owner,
			repo,
			cached,
			needBody.Count
		);
		return result;
	}

	private sealed record KnownPullRequest(int Number, string Title, IReadOnlyList<string> Labels, DateTimeOffset? UpdatedAt);

	private enum ReadKind
	{
		Body,
		Details
	}

	private static CachedPullRequestText TextOf(string owner, string repo, Node node)
	{
		var body = node.Body ?? string.Empty;
		var note = ReleaseNotesExtractor.ExtractReleaseNote(body);
		return new CachedPullRequestText(
			note.Content,
			note.WasTruncated,
			LinkedIssueExtractor.Extract(body, owner, repo),
			ReleaseNotesExtractor.FindImpact(body),
			ReleaseNotesExtractor.FindAction(body)
		);
	}

	private static PullRequestSummary Summarize(int number, string title, IReadOnlyList<string> labels, CachedPullRequestText text) =>
		new(
			number,
			title,
			labels,
			new ReleaseNoteExtractionResult(text.ReleaseNote, text.ReleaseNoteTruncated),
			text.LinkedIssues,
			text.Impact,
			text.Action
		);

	/// <summary>Reads the pull requests 50 at a time: their body, or their title, labels and update time.</summary>
	private async Task<Dictionary<int, Node>?> ReadAsync(
		IDiagnosticsCollector collector,
		string owner,
		string repo,
		IReadOnlyList<int> numbers,
		ReadKind kind,
		Cancel ctx
	)
	{
		var found = new Dictionary<int, Node>();
		var batches = numbers.Chunk(BatchSize).ToList();
		var responses = await BatchRunner.RunAsync(
			batches,
			(batch, token) => QueryBatchAsync(collector, owner, repo, batch, kind, token),
			data => data is null,
			ctx
		).ConfigureAwait(false);
		if (responses is null)
			return null;

		for (var i = 0; i < batches.Count; i++)
		{
			foreach (var number in batches[i])
			{
				if (responses[i]!.GetValueOrDefault($"p{number}") is { } node)
					found[number] = node;
			}
		}

		return found;
	}

	private async Task<Dictionary<string, Node?>?> QueryBatchAsync(
		IDiagnosticsCollector collector,
		string owner,
		string repo,
		int[] batch,
		ReadKind kind,
		Cancel ctx
	)
	{
		var selection = new StringBuilder();
		foreach (var number in batch)
		{
			var fields = kind == ReadKind.Details
				? $"number title updatedAt labels(first: {LabelPageSize}) {{ pageInfo {{ hasNextPage endCursor }} nodes {{ name }} }}"
				: "number body";
			_ = selection.Append(CultureInfo.InvariantCulture, $" p{number}: pullRequest(number: {number}) {{ {fields} }}");
		}

		return await QueryAsync(collector, owner, repo, selection.ToString(), ctx).ConfigureAwait(false);
	}

	/// <summary>The labels of the pull request. A pull request with more than a page of them is read page by page.</summary>
	private async Task<List<string>?> AllLabelsAsync(IDiagnosticsCollector collector, string owner, string repo, Node node, Cancel ctx)
	{
		var labels = node.Labels?.Nodes?.Select(l => l?.Name).OfType<string>().ToList() ?? [];
		var page = node.Labels?.PageInfo;
		while (page is { HasNextPage: true, EndCursor: { } cursor })
		{
			var selection = string.Create(
				CultureInfo.InvariantCulture,
				$" p: pullRequest(number: {node.Number}) {{ labels(first: {LabelPageSize}, after: \"{cursor}\") {{ pageInfo {{ hasNextPage endCursor }} nodes {{ name }} }} }}"
			);
			var next = (await QueryAsync(collector, owner, repo, selection, ctx).ConfigureAwait(false))?.GetValueOrDefault("p")?.Labels;
			if (next is null)
			{
				collector.EmitError(string.Empty, $"GitHub did not return more labels of {owner}/{repo}#{node.Number}.");
				return null;
			}

			labels.AddRange(next.Nodes?.Select(l => l?.Name).OfType<string>() ?? []);
			page = next.PageInfo;
		}

		return labels;
	}

	private async Task<Dictionary<string, Node?>?> QueryAsync(
		IDiagnosticsCollector collector,
		string owner,
		string repo,
		string selection,
		Cancel ctx
	)
	{
		var query = $"query {{ repository(owner: \"{owner}\", name: \"{repo}\") {{{selection} }} }}";
		var body = JsonSerializer.Serialize(new Request { Query = query }, PullRequestJsonContext.Default.Request);

		using var response = await _transport.PostGraphQlOrNullAsync(
			body,
			failure => collector.EmitError(string.Empty, $"GitHub GraphQL request for {owner}/{repo} failed: {failure}"),
			ctx
		).ConfigureAwait(false);
		if (response is null)
			return null;

		if (!response.IsSuccessStatusCode)
		{
			collector.EmitError(
				string.Empty,
				$"GitHub GraphQL request for {owner}/{repo} failed: {(int)response.StatusCode} {response.ReasonPhrase}."
			);
			return null;
		}

		var json = await response.Content.ReadAsStringAsync(ctx).ConfigureAwait(false);
		Response? parsed;
		try
		{
			parsed = JsonSerializer.Deserialize(json, PullRequestJsonContext.Default.Response);
		}
		catch (JsonException ex)
		{
			collector.EmitError(
				string.Empty,
				$"GitHub GraphQL request for {owner}/{repo} returned a response that is not valid JSON ({(int)response.StatusCode}, {json.Length} byte(s)): {ex.Message}"
			);
			return null;
		}

		// A number that is not a pull request answers NOT_FOUND for its alias and the rest still comes back.
		if (parsed?.Errors is { Count: > 0 } && !parsed.Errors.All(e => e.Type == "NOT_FOUND"))
		{
			var messages = string.Join("; ", parsed.Errors.Select(e => e.Message).Where(m => !string.IsNullOrWhiteSpace(m)));
			collector.EmitError(string.Empty, $"GitHub GraphQL query for {owner}/{repo} returned errors: {messages}.");
			return null;
		}

		if (parsed?.Data?.Repository is null)
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

		[JsonPropertyName("type")]
		public string? Type { get; set; }
	}

	private sealed class ResponseData
	{
		[JsonPropertyName("repository")]
		public Dictionary<string, Node?>? Repository { get; set; }
	}

	private sealed class Node
	{
		[JsonPropertyName("number")]
		public int Number { get; set; }

		[JsonPropertyName("title")]
		public string? Title { get; set; }

		[JsonPropertyName("updatedAt")]
		public DateTimeOffset? UpdatedAt { get; set; }

		[JsonPropertyName("body")]
		public string? Body { get; set; }

		[JsonPropertyName("labels")]
		public LabelConnection? Labels { get; set; }
	}

	private sealed class LabelConnection
	{
		[JsonPropertyName("pageInfo")]
		public PageInfo? PageInfo { get; set; }

		[JsonPropertyName("nodes")]
		public List<Label?>? Nodes { get; set; }
	}

	private sealed class PageInfo
	{
		[JsonPropertyName("hasNextPage")]
		public bool HasNextPage { get; set; }

		[JsonPropertyName("endCursor")]
		public string? EndCursor { get; set; }
	}

	private sealed class Label
	{
		[JsonPropertyName("name")]
		public string? Name { get; set; }
	}

	[JsonSerializable(typeof(Request))]
	[JsonSerializable(typeof(Response))]
	private sealed partial class PullRequestJsonContext : JsonSerializerContext;
}
