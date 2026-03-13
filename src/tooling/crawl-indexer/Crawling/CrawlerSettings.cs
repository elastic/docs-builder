// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace CrawlIndexer.Crawling;

/// <summary>Runtime configuration for crawler rate limiting and concurrency.</summary>
public sealed class CrawlerSettings
{
	public const int MaxRps = 1000;
	public const int DefaultConcurrency = 1000;

	public int Rps { get; private set; }
	public int Concurrency { get; private set; } = DefaultConcurrency;
	public bool RateLimitingEnabled { get; private set; }

	/// <summary>Configure rate limiting. Call before crawling starts.</summary>
	public void Configure(int? rps)
	{
		if (rps is null or 0)
		{
			// No rate limiting - max concurrency
			RateLimitingEnabled = false;
			Concurrency = DefaultConcurrency;
			Rps = 0;
			return;
		}

		if (rps > MaxRps)
			throw new ArgumentException($"RPS cannot exceed {MaxRps}", nameof(rps));

		if (rps < 1)
			throw new ArgumentException("RPS must be at least 1", nameof(rps));

		RateLimitingEnabled = true;
		Rps = rps.Value;
		Concurrency = rps.Value; // Concurrency matches RPS
	}
}
