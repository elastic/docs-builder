// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace CrawlIndexer.Crawling;

/// <summary>
/// Represents a discovered version from the sitemap.
/// </summary>
public record DiscoveredVersion(string Version, int MajorVersion);

/// <summary>
/// Discovers available versions from guide URLs.
/// </summary>
public interface IVersionDiscovery
{
	/// <summary>
	/// Analyze guide URLs to discover all available versions.
	/// Returns all 8.x versions and only the latest 7.x version.
	/// </summary>
	IReadOnlyList<DiscoveredVersion> DiscoverVersions(IEnumerable<SitemapEntry> guideUrls);
}
