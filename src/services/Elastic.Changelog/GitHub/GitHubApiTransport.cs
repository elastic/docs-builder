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
			ctx,
			retryTransient: true
		).ConfigureAwait(false);

	/// <summary>
	/// Sends a read request and waits out a rate limit instead of failing. Bulk reads run several requests at once, so a
	/// secondary limit can hit mid-run, and a failed read would drop an entry from the bundle. The wait is capped,
	/// because a primary limit that resets in an hour cannot be waited out; that response goes back to the caller.
	/// </summary>
	private async Task<HttpResponseMessage> SendWithRateLimitRetryAsync(
		Func<HttpRequestMessage> create,
		Cancel ctx,
		bool retryTransient = false
	)
	{
		for (var attempt = 1; ; attempt++)
		{
			using var request = create();
			HttpResponseMessage response;
			try
			{
				response = await _httpClient.SendAsync(request, ctx).ConfigureAwait(false);
			}
			catch (HttpRequestException) when (retryTransient && attempt < MaxRateLimitAttempts)
			{
				// A dropped connection is as transient as a 502. The last attempt lets the exception through.
				await _delay(TransientWait(attempt), ctx).ConfigureAwait(false);
				continue;
			}

			var wait = attempt < MaxRateLimitAttempts
				? await RetryWaitAsync(response, attempt, retryTransient, ctx).ConfigureAwait(false)
				: null;
			if (wait is null)
				return response;

			response.Dispose();
			await _delay(wait.Value, ctx).ConfigureAwait(false);
		}
	}

	/// <summary>How long to wait before retrying a response, or <c>null</c> when it is final: a rate limit, then a transient failure.</summary>
	private static async Task<TimeSpan?> RetryWaitAsync(HttpResponseMessage response, int attempt, bool retryTransient, Cancel ctx) =>
		await RateLimitWaitAsync(response, attempt, ctx).ConfigureAwait(false)
			?? await TransientFailureWaitAsync(response, attempt, retryTransient, ctx).ConfigureAwait(false);

	/// <summary>
	/// For GraphQL, a gateway error (502, 503, 504), a dropped connection, and a successful response with no body, which
	/// GitHub sends under load, are retried. Without this a long run over thousands of commits dies on one bad response.
	/// REST reads keep the retries of their callers, so they are not retried twice.
	/// </summary>
	private static async Task<TimeSpan?> TransientFailureWaitAsync(
		HttpResponseMessage response,
		int attempt,
		bool retryTransient,
		Cancel ctx
	)
	{
		if (!retryTransient)
			return null;

		if ((int)response.StatusCode is 502 or 503 or 504)
			return TransientWait(attempt);

		if (!response.IsSuccessStatusCode)
			return null;

		var body = await response.Content.ReadAsStringAsync(ctx).ConfigureAwait(false);
		return string.IsNullOrWhiteSpace(body) ? TransientWait(attempt) : null;
	}

	private static TimeSpan TransientWait(int attempt) => TimeSpan.FromSeconds(Math.Pow(2, attempt));

	/// <summary>
	/// How long to wait before retrying a rate-limited response, or <c>null</c> when it is not one or waiting is no use.
	/// <list type="bullet">
	/// <item>A <c>Retry-After</c> header says how long.</item>
	/// <item>An exhausted primary budget (<c>x-ratelimit-remaining: 0</c>) resets at <c>x-ratelimit-reset</c>.</item>
	/// <item>A secondary limit has neither: a 429, or a 403 whose body says so. GitHub asks for at least a minute.</item>
	/// </list>
	/// The reset time only counts for an exhausted budget. A secondary limit can arrive with budget left and a reset an hour away.
	/// </summary>
	private static async Task<TimeSpan?> RateLimitWaitAsync(HttpResponseMessage response, int attempt, Cancel ctx)
	{
		var status = (int)response.StatusCode;
		if (status is not (429 or 403))
			return null;

		TimeSpan wait;
		if (response.Headers.RetryAfter is { } retryAfter && RetryAfterWait(retryAfter) is { } told)
			wait = told;
		else if (response.Headers.TryGetValues("x-ratelimit-remaining", out var remaining) && remaining.FirstOrDefault() == "0")
			wait = ResetWait(response);
		else if (status == 429 || await IsSecondaryLimitBodyAsync(response, ctx).ConfigureAwait(false))
			wait = TimeSpan.FromSeconds(Math.Min(60 * attempt, MaxRateLimitWait.TotalSeconds));
		else
			return null;

		return wait > MaxRateLimitWait ? null : wait < TimeSpan.Zero ? TimeSpan.Zero : wait;
	}

	private static TimeSpan? RetryAfterWait(System.Net.Http.Headers.RetryConditionHeaderValue retryAfter) =>
		retryAfter.Delta ?? (retryAfter.Date is { } date ? date - DateTimeOffset.UtcNow : null);

	private static TimeSpan ResetWait(HttpResponseMessage response) =>
		response.Headers.TryGetValues("x-ratelimit-reset", out var reset) && long.TryParse(reset.FirstOrDefault(), out var resetAt)
			? DateTimeOffset.FromUnixTimeSeconds(resetAt) - DateTimeOffset.UtcNow + TimeSpan.FromSeconds(1)
			: TimeSpan.FromSeconds(60);

	/// <summary>A 403 for a secondary limit says so in its body, unlike a 403 for a missing permission.</summary>
	private static async Task<bool> IsSecondaryLimitBodyAsync(HttpResponseMessage response, Cancel ctx)
	{
		var body = await response.Content.ReadAsStringAsync(ctx).ConfigureAwait(false);
		return body.Contains("secondary rate limit", StringComparison.OrdinalIgnoreCase)
			|| body.Contains("abuse detection", StringComparison.OrdinalIgnoreCase);
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
