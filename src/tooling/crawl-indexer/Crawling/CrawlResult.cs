// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace CrawlIndexer.Crawling;

/// <summary>
/// Result of crawling a single URL.
/// </summary>
public record CrawlResult
{
	public required string Url { get; init; }
	public required bool Success { get; init; }
	public string? Content { get; init; }
	public DateTimeOffset? LastModified { get; init; }
	public string? Error { get; init; }
	public int? StatusCode { get; init; }

	// HTTP caching fields
	public string? HttpEtag { get; init; }
	public DateTimeOffset? HttpLastModified { get; init; }
	public bool NotModified { get; init; }
	public string? CachedHash { get; init; }

	public static CrawlResult Succeeded(
		string url,
		string content,
		DateTimeOffset? lastModified,
		string? etag = null,
		DateTimeOffset? httpLastModified = null
	) =>
		new()
		{
			Url = url,
			Success = true,
			Content = content,
			LastModified = lastModified,
			HttpEtag = etag,
			HttpLastModified = httpLastModified
		};

	public static CrawlResult NotModifiedResult(string url, string cachedHash) =>
		new()
		{
			Url = url,
			Success = true,
			NotModified = true,
			CachedHash = cachedHash,
			StatusCode = 304
		};

	public static CrawlResult Failed(string url, string error, int? statusCode = null) =>
		new() { Url = url, Success = false, Error = error, StatusCode = statusCode };
}
