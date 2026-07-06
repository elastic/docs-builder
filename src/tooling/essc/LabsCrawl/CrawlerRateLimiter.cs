// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.RateLimiting;

namespace Elastic.SiteSearch.Cli.LabsCrawl;

public sealed class CrawlerRateLimiter(CrawlerSettings settings) : IDisposable
{
	private TokenBucketRateLimiter? _limiter;

	private void EnsureInitialized()
	{
		if (_limiter is not null || !settings.RateLimitingEnabled)
			return;

		_limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
		{
			TokenLimit = settings.Rps,
			ReplenishmentPeriod = TimeSpan.FromSeconds(1),
			TokensPerPeriod = settings.Rps,
			QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
			QueueLimit = 10000
		});
	}

	public async ValueTask<RateLimitLease?> AcquireAsync(CancellationToken ct = default)
	{
		EnsureInitialized();
		if (_limiter is null)
			return null;

		return await _limiter.AcquireAsync(1, ct);
	}

	public void Dispose() => _limiter?.Dispose();
}
