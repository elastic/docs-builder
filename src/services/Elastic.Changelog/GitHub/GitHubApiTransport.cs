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

	/// <param name="handler">Optional HTTP handler override (tests). Owned by the caller.</param>
	/// <param name="githubToken">Optional token override; defaults to the <c>GITHUB_TOKEN</c> environment variable.</param>
	public GitHubApiTransport(HttpMessageHandler? handler = null, string? githubToken = null)
	{
		_githubToken = githubToken;
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
	public async Task<HttpResponseMessage> GetAsync(string url, Cancel ctx = default)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, url);
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
		AttachAuthorization(request);
		return await _httpClient.SendAsync(request, ctx).ConfigureAwait(false);
	}

	/// <summary>
	/// Posts a JSON body to the GitHub GraphQL endpoint. The GraphQL API rejects anonymous
	/// requests, so callers should verify <see cref="ResolveToken"/> before building queries.
	/// </summary>
	public async Task<HttpResponseMessage> PostGraphQlAsync(string jsonBody, Cancel ctx = default)
	{
		using var request = new HttpRequestMessage(HttpMethod.Post, GraphQlEndpoint);
		request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
		AttachAuthorization(request);
		return await _httpClient.SendAsync(request, ctx).ConfigureAwait(false);
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
