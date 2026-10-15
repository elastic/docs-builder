// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net.Http.Headers;
using System.Text;

namespace Elastic.Changelog.GitHub;

/// <summary>
/// Shared HTTP transport for the GitHub API services (<see cref="GitHubPrService"/>,
/// <see cref="GitHubReleaseService"/>, <see cref="GitHubCommitRangeService"/>): one process-wide
/// <see cref="HttpClient"/>, consistent <c>User-Agent</c>/<c>Accept</c> headers, <c>GITHUB_TOKEN</c>
/// bearer authentication, and an injectable <see cref="HttpMessageHandler"/> so every consumer can
/// be tested at the HTTP level. Response handling policy (lenient warn-and-null vs strict
/// fail-the-run) stays with each service; this type only issues requests.
/// </summary>
public sealed class GitHubApiTransport : IDisposable
{
	private const string GraphQlEndpoint = "https://api.github.com/graphql";

	private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(60);
	private static readonly TimeSpan MaxRateLimitWait = TimeSpan.FromSeconds(90);
	private const int MaxRateLimitAttempts = 4;

	/// <summary>
	/// Process-wide client shared by every transport built for the production (no injected handler)
	/// path. Intentionally never disposed — it lives for the lifetime of the process.
	/// </summary>
	private static readonly HttpClient SharedHttpClient = CreateClient(null);

	private readonly HttpClient _httpClient;

	/// <summary>
	/// Non-null only when a caller injects its own <see cref="HttpMessageHandler"/> (tests): in that
	/// case we own a per-instance client and must dispose it. On the production path
	/// <see cref="_httpClient"/> points at <see cref="SharedHttpClient"/>, which is never disposed.
	/// </summary>
	private readonly HttpClient? _ownedHttpClient;
	private readonly string? _githubToken;
	private readonly Func<TimeSpan, Cancel, Task> _delay;

	/// <param name="handler">Optional HTTP handler override (tests). Owned by the caller.</param>
	/// <param name="githubToken">Optional token override; defaults to the <c>GITHUB_TOKEN</c> environment variable.</param>
	/// <param name="delay">Optional wait override (tests), used between retries of a rate-limited request.</param>
	public GitHubApiTransport(HttpMessageHandler? handler = null, string? githubToken = null, Func<TimeSpan, Cancel, Task>? delay = null)
	{
		_githubToken = githubToken;
		_delay = delay ?? Task.Delay;
		if (handler is null)
			_httpClient = SharedHttpClient;
		else
		{
			// disposeHandler: false — the injected handler is owned by the caller (tests), not by us.
			_ownedHttpClient = CreateClient(handler);
			_httpClient = _ownedHttpClient;
		}
	}

	private static HttpClient CreateClient(HttpMessageHandler? handler)
	{
		var client = handler is null
			? new HttpClient(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) })
			: new HttpClient(handler, disposeHandler: false);
		client.Timeout = FetchTimeout;
		client.DefaultRequestHeaders.Add("User-Agent", "docs-builder");
		return client;
	}

	/// <summary>The effective token: the constructor override, else the <c>GITHUB_TOKEN</c> environment variable.</summary>
	public string? ResolveToken() => _githubToken ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");

	/// <summary>
	/// Issues an authenticated (when a token resolves) GET against the GitHub REST API.
	/// The caller owns the response and its status-code policy.
	/// </summary>
	public async Task<HttpResponseMessage> GetAsync(string url, Cancel ctx = default) =>
		await SendWithRateLimitRetryAsync(
			() =>
			{
				var request = new HttpRequestMessage(HttpMethod.Get, url);
				request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
				AttachAuthorization(request);
				return request;
			},
			ctx
		).ConfigureAwait(false);

	/// <summary>
	/// Issues an authenticated POST against a GitHub REST API endpoint with a JSON body.
	/// The caller owns the response and its status-code policy.
	/// </summary>
	public async Task<HttpResponseMessage> PostAsync(string url, string jsonBody, Cancel ctx = default)
	{
		using var request = new HttpRequestMessage(HttpMethod.Post, url);
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
		request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
		AttachAuthorization(request);
		return await _httpClient.SendAsync(request, ctx).ConfigureAwait(false);
	}

	/// <summary>
	/// Issues an authenticated PATCH against a GitHub REST API endpoint with a JSON body.
	/// The caller owns the response and its status-code policy.
	/// </summary>
	public async Task<HttpResponseMessage> PatchAsync(string url, string jsonBody, Cancel ctx = default)
	{
		using var request = new HttpRequestMessage(HttpMethod.Patch, url);
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
		request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
		AttachAuthorization(request);
		return await _httpClient.SendAsync(request, ctx).ConfigureAwait(false);
	}

	/// <summary>
	/// Issues an authenticated DELETE against a GitHub REST API endpoint.
	/// The caller owns the response and its status-code policy.
	/// </summary>
	public async Task<HttpResponseMessage> DeleteAsync(string url, Cancel ctx = default)
	{
		using var request = new HttpRequestMessage(HttpMethod.Delete, url);
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
		AttachAuthorization(request);
		return await _httpClient.SendAsync(request, ctx).ConfigureAwait(false);
	}

	/// <summary>
	/// Posts a JSON body to the GitHub GraphQL endpoint. The GraphQL API rejects anonymous
	/// requests, so callers should verify <see cref="ResolveToken"/> before building queries.
	/// </summary>
	public async Task<HttpResponseMessage> PostGraphQlAsync(string jsonBody, Cancel ctx = default) =>
		await SendWithRateLimitRetryAsync(
			() =>
			{
				var request = new HttpRequestMessage(HttpMethod.Post, GraphQlEndpoint)
				{
					Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
				};
				AttachAuthorization(request);
				return request;
			},
			ctx
		).ConfigureAwait(false);

	/// <summary>
	/// Sends a read request and waits out a rate limit instead of failing: HTTP 429, or 403 with a <c>Retry-After</c>
	/// header or an exhausted <c>x-ratelimit-remaining</c>. Bulk reads run several requests at once, so a secondary
	/// limit can hit mid-run, and a failed read would drop an entry from the bundle. The wait is capped, because a
	/// primary limit that resets in an hour cannot be waited out; that response goes back to the caller.
	/// </summary>
	private async Task<HttpResponseMessage> SendWithRateLimitRetryAsync(Func<HttpRequestMessage> create, Cancel ctx)
	{
		for (var attempt = 1; ; attempt++)
		{
			using var request = create();
			var response = await _httpClient.SendAsync(request, ctx).ConfigureAwait(false);
			var wait = attempt < MaxRateLimitAttempts ? RateLimitWait(response, attempt) : null;
			if (wait is null)
				return response;

			response.Dispose();
			await _delay(wait.Value, ctx).ConfigureAwait(false);
		}
	}

	private static TimeSpan? RateLimitWait(HttpResponseMessage response, int attempt)
	{
		var status = (int)response.StatusCode;
		var retryAfter = response.Headers.RetryAfter?.Delta;
		var exhausted = response.Headers.TryGetValues("x-ratelimit-remaining", out var remaining) && remaining.FirstOrDefault() == "0";
		if (status != 429 && !(status == 403 && (retryAfter is not null || exhausted)))
			return null;

		var wait = retryAfter ?? TimeSpan.FromSeconds(Math.Pow(2, attempt) * 5);
		if (
			retryAfter is null
			&& response.Headers.TryGetValues("x-ratelimit-reset", out var reset)
			&& long.TryParse(reset.FirstOrDefault(), out var resetAt)
		)
			wait = DateTimeOffset.FromUnixTimeSeconds(resetAt) - DateTimeOffset.UtcNow + TimeSpan.FromSeconds(1);

		return wait > MaxRateLimitWait ? null : wait < TimeSpan.Zero ? TimeSpan.Zero : wait;
	}

	private void AttachAuthorization(HttpRequestMessage request)
	{
		var token = ResolveToken();
		if (!string.IsNullOrEmpty(token))
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
	}

	/// <summary>
	/// Disposes the per-instance client created for an injected handler; the shared production
	/// client is process-lived and intentionally not disposed.
	/// </summary>
	public void Dispose() => _ownedHttpClient?.Dispose();
}
