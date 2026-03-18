// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace CrawlIndexer.Caching;

/// <summary>
/// Lightweight record for cached document metadata loaded from Elasticsearch.
/// </summary>
public record CachedDocInfo(
	string Url,
	string Hash,
	DateTimeOffset LastUpdated,
	string? HttpEtag,
	DateTimeOffset? HttpLastModified,
	string? EnrichmentKey,
	string? EnrichmentPromptHash
);
