// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search.Common;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Search;

/// <summary>Singleton manager for a shared Elasticsearch Point In Time (PIT).</summary>
public sealed partial class SharedPointInTimeManager(
	ElasticsearchClientAccessor clientAccessor,
	ILogger<SharedPointInTimeManager> logger
) : IAsyncDisposable
{
	private static readonly TimeSpan KeepAliveDuration = TimeSpan.FromMinutes(5);
	public const string PitKeepAlive = "5m";

	private readonly SemaphoreSlim _semaphore = new(1, 1);
	private string? _pitId;
	private DateTimeOffset _expiresAt;

	/// <summary>Returns a valid PIT ID, opening a new one if needed.</summary>
	public async Task<string> GetPitIdAsync(Cancel ctx)
	{
		await _semaphore.WaitAsync(ctx);
		try
		{
			if (_pitId is not null && DateTimeOffset.UtcNow < _expiresAt)
				return _pitId;

			_pitId = await OpenPit(ctx);
			_expiresAt = DateTimeOffset.UtcNow.Add(KeepAliveDuration);
			return _pitId;
		}
		finally
		{
			_ = _semaphore.Release();
		}
	}

	/// <summary>Nullifies the current PIT only if it matches the expired one, so a concurrent refresh is not discarded.</summary>
	public async Task HandleExpiredPitAsync(string expiredPitId, Cancel ctx)
	{
		await _semaphore.WaitAsync(ctx);
		try
		{
			if (_pitId == expiredPitId)
			{
				LogPitExpired(logger);
				_pitId = null;
			}
		}
		finally
		{
			_ = _semaphore.Release();
		}
	}

	/// <summary>Bumps the local expiry after a successful search (ES extends the PIT server-side via KeepAlive).</summary>
	public void RefreshKeepAlive() => _expiresAt = DateTimeOffset.UtcNow.Add(KeepAliveDuration);

	private async Task<string> OpenPit(Cancel ctx)
	{
		var response = await clientAccessor.Client.OpenPointInTimeAsync(
			clientAccessor.SearchIndex,
			r => r.KeepAlive(PitKeepAlive),
			ctx
		);

		if (!response.IsValidResponse)
		{
			throw new InvalidOperationException(
				$"Failed to open PIT: {response.ElasticsearchServerError?.Error.Reason ?? "Unknown"}"
			);
		}

		LogPitOpened(logger, response.Id);
		return response.Id;
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (_pitId is null)
			return;

		try
		{
			_ = await clientAccessor.Client.ClosePointInTimeAsync(r => r.Id(_pitId));
			LogPitClosed(logger, _pitId);
		}
		catch (OperationCanceledException ex)
		{
			logger.LogWarning(ex, "PIT close operation was canceled during shutdown for {PitId}", _pitId);
		}
		finally
		{
			_semaphore.Dispose();
		}
	}

	[LoggerMessage(Level = LogLevel.Debug, Message = "Opened new shared PIT: {PitId}")]
	private static partial void LogPitOpened(ILogger logger, string pitId);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Shared PIT expired or not found, will open a new one")]
	private static partial void LogPitExpired(ILogger logger);

	[LoggerMessage(Level = LogLevel.Debug, Message = "Closed shared PIT: {PitId}")]
	private static partial void LogPitClosed(ILogger logger, string pitId);
}
