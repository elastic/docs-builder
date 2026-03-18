// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace CrawlIndexer.Crawling;

/// <summary>
/// Represents a single URL entry from a sitemap.
/// </summary>
public record SitemapEntry(string Location, DateTimeOffset? LastModified = null, string? ChangeFrequency = null, double? Priority = null);
