// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search;

namespace CrawlIndexer.Html;

/// <summary>
/// Extracts content from HTML pages for guide indexing.
/// </summary>
public interface IGuideHtmlExtractor
{
	/// <summary>
	/// Extract a DocumentationDocument from guide HTML content.
	/// </summary>
	Task<DocumentationDocument?> ExtractAsync(string url, string html, DateTimeOffset? sitemapLastModified, Cancel ctx = default);
}

/// <summary>
/// Extracts content from site HTML pages for indexing.
/// </summary>
public interface ISiteHtmlExtractor
{
	/// <summary>
	/// Extract a SiteDocument from site HTML content.
	/// </summary>
	Task<SiteDocument?> ExtractAsync(
		string url,
		string html,
		DateTimeOffset? sitemapLastModified,
		string language,
		string pageType,
		Cancel ctx = default
	);
}
