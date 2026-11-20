// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text;

namespace Elastic.Documentation.Assembler.ApiCrawl;

/// <summary>A text document the crawl reads whole, such as a sitemap or <c>links.json</c>. <see cref="Status"/> is 0 when no response came back.</summary>
public sealed record FetchedText(int Status, string Body, string? Error = null)
{
	public bool IsOk => Status == 200;

	public string Describe() => Error is { } error ? $"error ({error})" : $"HTTP {Status}";
}

/// <summary>
/// Fetches pages without following redirects, so each hop is counted, and reads only the <c>&lt;head&gt;</c> of a page.
/// A 5xx answer or a timeout is retried once.
/// </summary>
public sealed class ApiCrawlClient : IDisposable
{
	private const int MaxHops = 5;
	private const int MaxHeadChars = 512 * 1024;
	private const string HeadEnd = "</head>";

	private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(30);

	private static readonly HttpClient SharedHttpClient = new(new SocketsHttpHandler
	{
		AllowAutoRedirect = false,
		AutomaticDecompression = DecompressionMethods.All,
		PooledConnectionLifetime = TimeSpan.FromMinutes(5)
	})
	{ Timeout = FetchTimeout };

	private readonly HttpClient _httpClient;
	private readonly HttpClient? _ownedHttpClient;
	private readonly Uri _site;

	public ApiCrawlClient(Uri site, HttpMessageHandler? handler = null)
	{
		_site = site;
		if (handler is null)
			_httpClient = SharedHttpClient;
		else
		{
			_ownedHttpClient = new HttpClient(handler, disposeHandler: false) { Timeout = FetchTimeout };
			_httpClient = _ownedHttpClient;
		}
	}

	public async Task<FetchedText> GetTextAsync(Uri uri, Cancel ctx)
	{
		try
		{
			using var response = await SendAsync(uri, ctx).ConfigureAwait(false);
			var body = await response.Content.ReadAsStringAsync(ctx).ConfigureAwait(false);
			return new FetchedText((int)response.StatusCode, body.TrimStart('﻿'));
		}
		catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ctx.IsCancellationRequested)
		{
			return new FetchedText(0, string.Empty, ex.Message);
		}
	}

	/// <summary>Requests <paramref name="path"/> on the site and follows up to five redirects by hand.</summary>
	public async Task<ObservedPage> ObserveAsync(string path, Cancel ctx)
	{
		var hops = new List<RedirectHop>();
		var uri = new Uri(_site, path);
		try
		{
			while (true)
			{
				using var response = await SendAsync(uri, ctx).ConfigureAwait(false);
				var status = (int)response.StatusCode;
				if (status is >= 300 and < 400 && response.Headers.Location is { } location && hops.Count < MaxHops)
				{
					uri = new Uri(uri, location);
					hops.Add(new RedirectHop(status, OnSite(uri)));
					continue;
				}

				if (status != 200)
					return new ObservedPage(path, status, hops, null, null);

				var head = ApiPageHead.Parse(await ReadHeadAsync(response, ctx).ConfigureAwait(false));
				return new ObservedPage(path, status, hops, head.Canonical, head.Robots);
			}
		}
		catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException && !ctx.IsCancellationRequested)
		{
			return new ObservedPage(path, 0, hops, null, null, ex.Message);
		}
	}

	private string OnSite(Uri uri) =>
		Uri.Compare(uri, _site, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0
			? uri.PathAndQuery
			: uri.AbsoluteUri;

	private async Task<HttpResponseMessage> SendAsync(Uri uri, Cancel ctx)
	{
		try
		{
			var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ctx).ConfigureAwait(false);
			if ((int)response.StatusCode < 500)
				return response;
			response.Dispose();
		}
		catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ctx.IsCancellationRequested) { }

		return await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ctx).ConfigureAwait(false);
	}

	private static async Task<string> ReadHeadAsync(HttpResponseMessage response, Cancel ctx)
	{
		var stream = await response.Content.ReadAsStreamAsync(ctx).ConfigureAwait(false);
		await using (stream.ConfigureAwait(false))
		{
			using var reader = new StreamReader(stream, Encoding.UTF8);
			var head = new StringBuilder();
			var buffer = new char[16 * 1024];
			int read;
			while (head.Length < MaxHeadChars && (read = await reader.ReadAsync(buffer, ctx).ConfigureAwait(false)) > 0)
			{
				_ = head.Append(buffer, 0, read);
				var tail = head.ToString(Math.Max(0, head.Length - read - HeadEnd.Length), Math.Min(head.Length, read + HeadEnd.Length));
				if (tail.Contains(HeadEnd, StringComparison.OrdinalIgnoreCase))
					break;
			}

			return head.ToString();
		}
	}

	public void Dispose() => _ownedHttpClient?.Dispose();
}
