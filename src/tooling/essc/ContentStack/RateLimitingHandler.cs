// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.RateLimiting;

namespace Elastic.SiteSearch.Cli.ContentStack;

/// <summary>
/// DelegatingHandler that gates outbound requests through a <see cref="RateLimiter"/>.
/// Sits in the HttpClient pipeline so every request is automatically rate-limited
/// regardless of call site.
/// </summary>
internal sealed class RateLimitingHandler(RateLimiter limiter) : DelegatingHandler
{
	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken ct
	)
	{
		using var lease = await limiter.AcquireAsync(1, ct);
		if (!lease.IsAcquired)
			throw new HttpRequestException("Rate limiter rejected the request — queue full");

		return await base.SendAsync(request, ct);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
			limiter.Dispose();

		base.Dispose(disposing);
	}

	/// <summary>
	/// Creates a <see cref="TokenBucketRateLimiter"/> tuned for Contentstack's
	/// 100 req/s per-org origin rate limit with headroom.
	/// </summary>
	public static RateLimitingHandler CreateForContentStack() =>
		new(new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
		{
			TokenLimit = 20,
			ReplenishmentPeriod = TimeSpan.FromSeconds(1),
			TokensPerPeriod = 80,
			QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
			QueueLimit = 500,
			AutoReplenishment = true
		}));
}
