// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.ContentSources;

/// <summary>
/// <see cref="IPullRequestLookup"/> backed by the GitHub REST API. Requires a token: the pulls
/// endpoint is rate limited per IP when anonymous, which makes it unreliable on shared CI runners.
/// </summary>
public sealed partial class GitHubPullRequestLookup : IPullRequestLookup, IDisposable
{
	private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(30);

	private readonly ILogger _logger;
	private readonly HttpClient _httpClient;
	private readonly string _token;

	/// <param name="logFactory">Logger factory.</param>
	/// <param name="token">GitHub token with read access to pull requests.</param>
	/// <param name="handler">Optional HTTP handler override (tests). Owned by the caller.</param>
	public GitHubPullRequestLookup(ILoggerFactory logFactory, string token, HttpMessageHandler? handler = null)
	{
		_logger = logFactory.CreateLogger<GitHubPullRequestLookup>();
		_token = token;
		_httpClient = handler is null
			? new HttpClient(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) })
			: new HttpClient(handler, disposeHandler: false);
		_httpClient.Timeout = FetchTimeout;
		_httpClient.DefaultRequestHeaders.Add("User-Agent", "docs-builder");
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<PullRequestSummary>> ListOpenPullRequestsByHead(string repository, string headBranch, Cancel ctx)
	{
		var owner = repository.Split('/')[0];
		var url =
			$"https://api.github.com/repos/{repository}/pulls?state=open&per_page=100&head={Uri.EscapeDataString($"{owner}:{headBranch}")}";
		_logger.LogDebug("Listing open pull requests with head '{Head}' from {Url}", headBranch, url);

		using var request = new HttpRequestMessage(HttpMethod.Get, url);
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

		using var response = await _httpClient.SendAsync(request, ctx);
		if (!response.IsSuccessStatusCode)
		{
			throw new HttpRequestException(
				$"GitHub returned {(int)response.StatusCode} {response.ReasonPhrase} while listing pull requests with head '{headBranch}' in {repository}"
			);
		}

		var json = await response.Content.ReadAsStringAsync(ctx);
		var items = JsonSerializer.Deserialize(json, PullRequestJsonContext.Default.ListPullRequestItem) ?? [];
		return items
			.Where(item => item.Head?.Ref is not null && item.Base?.Ref is not null)
			.Select(item => new PullRequestSummary(item.Number, item.Head!.Repo?.FullName ?? string.Empty, item.Head.Ref!, item.Base!.Ref!))
			.ToList();
	}

	/// <inheritdoc />
	public void Dispose() => _httpClient.Dispose();

	private sealed class PullRequestItem
	{
		[JsonPropertyName("number")]
		public int Number { get; set; }

		[JsonPropertyName("head")]
		public GitRef? Head { get; set; }

		[JsonPropertyName("base")]
		public GitRef? Base { get; set; }
	}

	private sealed class GitRef
	{
		[JsonPropertyName("ref")]
		public string? Ref { get; set; }

		[JsonPropertyName("repo")]
		public RepositoryRef? Repo { get; set; }
	}

	private sealed class RepositoryRef
	{
		[JsonPropertyName("full_name")]
		public string? FullName { get; set; }
	}

	[JsonSerializable(typeof(List<PullRequestItem>))]
	[JsonSerializable(typeof(PullRequestItem))]
	[JsonSerializable(typeof(GitRef))]
	[JsonSerializable(typeof(RepositoryRef))]
	private sealed partial class PullRequestJsonContext : JsonSerializerContext;
}
