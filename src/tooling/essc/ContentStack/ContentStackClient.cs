// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Elastic.SiteSearch.Cli.ContentStack;

internal sealed class ContentStackClient(
	HttpClient httpClient,
	ContentStackConfiguration configuration,
	ILogger<ContentStackClient> logger
)
{
	private const int MaxBodyRetries = 5;

	/// <summary>
	/// Runs an initial sync, optionally resuming from a pagination token.
	/// Calls <paramref name="onPage"/> after each page so callers can persist state incrementally.
	/// </summary>
	public async Task<SyncResult> InitialSyncAsync(
		string? resumePaginationToken = null,
		string? contentTypeUid = null,
		int maxPages = 0,
		IProgress<SyncProgress>? progress = null,
		Func<SyncResponse, Task>? onPage = null,
		Cancel ct = default
	)
	{
		ConfigureClient();

		string url;
		if (resumePaginationToken != null)
			url = $"/v3/stacks/sync?pagination_token={resumePaginationToken}";
		else
		{
			url = $"/v3/stacks/sync?init=true&type=entry_published&environment={configuration.Environment}";
			if (contentTypeUid != null)
				url += $"&content_type_uid={contentTypeUid}";
		}

		logger.LogDebug("Starting initial sync against {Url}", url);
		return await PaginateAsync(url, maxPages, progress, onPage, ct);
	}

	/// <summary>
	/// Performs a delta sync using a previously stored sync token.
	/// Calls <paramref name="onPage"/> after each page so callers can persist state incrementally.
	/// </summary>
	public async Task<SyncResult> DeltaSyncAsync(
		string syncToken,
		int maxPages = 0,
		IProgress<SyncProgress>? progress = null,
		Func<SyncResponse, Task>? onPage = null,
		Cancel ct = default
	)
	{
		ConfigureClient();

		var url = $"/v3/stacks/sync?sync_token={syncToken}";
		logger.LogDebug("Starting delta sync with token");
		return await PaginateAsync(url, maxPages, progress, onPage, ct);
	}

	private async Task<SyncResult> PaginateAsync(
		string url,
		int maxPages,
		IProgress<SyncProgress>? progress,
		Func<SyncResponse, Task>? onPage,
		Cancel ct
	)
	{
		var allItems = new List<SyncItem>();
		var page = 0;

		while (true)
		{
			ct.ThrowIfCancellationRequested();

			var response = await FetchPageAsync(url, ct);

			allItems.AddRange(response.Items);
			page++;

			progress?.Report(new SyncProgress(page, allItems.Count, response.TotalCount));

			logger.LogDebug(
				"Page {Page}: received {Count} items (total so far: {Total})",
				page, response.Items.Count, allItems.Count);

			if (onPage != null)
				await onPage(response);

			if (maxPages > 0 && page >= maxPages)
			{
				logger.LogDebug("Reached page limit of {Max} — stopping early", maxPages);
				return new SyncResult(allItems, response.SyncToken, page);
			}

			if (!string.IsNullOrEmpty(response.PaginationToken))
			{
				url = $"/v3/stacks/sync?pagination_token={response.PaginationToken}";
				continue;
			}

			if (!string.IsNullOrEmpty(response.SyncToken))
				return new SyncResult(allItems, response.SyncToken, page);

			logger.LogWarning("Response contained neither pagination_token nor sync_token — stopping");
			return new SyncResult(allItems, null, page);
		}
	}

	private async Task<SyncResponse> FetchPageAsync(string url, Cancel ct)
	{
		for (var attempt = 0; ; attempt++)
		{
			try
			{
				var response = await httpClient.GetFromJsonAsync(url, SyncJsonContext.Default.SyncResponse, ct);
				return response
					?? throw new InvalidOperationException($"Received null response from Contentstack sync API at {url}");
			}
			catch (HttpIOException ex) when (attempt < MaxBodyRetries)
			{
				var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
				logger.LogWarning(
					"Response body truncated (attempt {Attempt}/{Max}), retrying in {Delay}s: {Message}",
					attempt + 1, MaxBodyRetries, delay.TotalSeconds, ex.Message);
				await Task.Delay(delay, ct);
			}
		}
	}

	private readonly Lock _configureLock = new();
	private bool _configured;

	private void ConfigureClient()
	{
		if (_configured)
			return;
		lock (_configureLock)
		{
			if (_configured)
				return;
			httpClient.BaseAddress = configuration.BaseUrl;
			_ = httpClient.DefaultRequestHeaders.Remove("api_key");
			_ = httpClient.DefaultRequestHeaders.Remove("access_token");
			httpClient.DefaultRequestHeaders.Add("api_key", configuration.ApiKey);
			httpClient.DefaultRequestHeaders.Add("access_token", configuration.DeliveryToken);
			_configured = true;
		}
	}
}

internal sealed record SyncProgress(int PagesCompleted, int ItemsSoFar, int TotalCount);
internal sealed record SyncResult(List<SyncItem> Items, string? SyncToken, int PagesCompleted);
