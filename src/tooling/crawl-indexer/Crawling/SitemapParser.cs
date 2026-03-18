// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Compression;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace CrawlIndexer.Crawling;

/// <summary>
/// Parses sitemap.xml files, supporting both sitemap indexes and regular sitemaps.
/// </summary>
public class SitemapParser(ILogger<SitemapParser> logger, HttpClient httpClient) : ISitemapParser
{
	private static readonly XNamespace SitemapNs = "http://www.sitemaps.org/schemas/sitemap/0.9";

	public async Task<IReadOnlyList<SitemapEntry>> ParseAsync(
		Uri sitemapUrl,
		Action<int, int, string>? onProgress = null,
		Cancel ctx = default
	)
	{
		try
		{
			logger.LogDebug("Fetching sitemap: {Url}", sitemapUrl);
			onProgress?.Invoke(0, 1, sitemapUrl.ToString());

			var content = await FetchContentAsync(sitemapUrl, ctx);

			logger.LogDebug("Parsing sitemap XML ({Length} bytes)", content.Length);
			var doc = XDocument.Parse(content);
			var root = doc.Root;

			if (root is null)
			{
				logger.LogWarning("Empty sitemap at {Url}", sitemapUrl);
				return [];
			}

			logger.LogDebug("Root element: {Name}", root.Name.LocalName);

			// Check if this is a sitemap index
			if (root.Name.LocalName == "sitemapindex")
				return await ParseSitemapIndexAsync(root, onProgress, ctx);

			// Regular sitemap
			return ParseUrlSet(root);
		}
		catch (HttpRequestException ex)
		{
			logger.LogError(ex, "HTTP error fetching sitemap {Url}: {Message}", sitemapUrl, ex.Message);
			throw;
		}
		catch (System.Xml.XmlException ex)
		{
			logger.LogError(ex, "XML parsing error for sitemap {Url}: {Message}", sitemapUrl, ex.Message);
			throw;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Unexpected error parsing sitemap {Url}: {Message}", sitemapUrl, ex.Message);
			throw;
		}
	}

	private async Task<string> FetchContentAsync(Uri url, Cancel ctx)
	{
		// Check if it's a gzipped sitemap
		if (url.AbsolutePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
		{
			logger.LogDebug("Fetching gzipped sitemap: {Url}", url);
			await using var stream = await httpClient.GetStreamAsync(url, ctx);
			await using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
			using var reader = new StreamReader(gzipStream);
			return await reader.ReadToEndAsync(ctx);
		}

		return await httpClient.GetStringAsync(url, ctx);
	}

	private async Task<IReadOnlyList<SitemapEntry>> ParseSitemapIndexAsync(
		XElement root,
		Action<int, int, string>? onProgress,
		Cancel ctx
	)
	{
		var sitemapUrls = root
			.Elements(SitemapNs + "sitemap")
			.Select(s => s.Element(SitemapNs + "loc")?.Value)
			.Where(loc => !string.IsNullOrWhiteSpace(loc))
			.Select(loc => new Uri(loc!))
			.ToList();

		logger.LogInformation("Sitemap index contains {Count} child sitemaps", sitemapUrls.Count);

		var allEntries = new List<SitemapEntry>();
		var current = 0;
		var total = sitemapUrls.Count;

		foreach (var childUrl in sitemapUrls)
		{
			current++;
			onProgress?.Invoke(current, total, childUrl.ToString());

			try
			{
				logger.LogDebug("Fetching child sitemap: {Url}", childUrl);
				var childContent = await FetchContentAsync(childUrl, ctx);

				logger.LogDebug("Parsing child sitemap XML ({Length} bytes)", childContent.Length);
				var childDoc = XDocument.Parse(childContent);
				var childRoot = childDoc.Root;

				if (childRoot is not null)
				{
					var entries = ParseUrlSet(childRoot);
					allEntries.AddRange(entries);
					logger.LogDebug("Parsed {Count} URLs from {Url}", entries.Count, childUrl);
				}
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Failed to parse child sitemap {Url}: {Message}", childUrl, ex.Message);
			}
		}

		logger.LogInformation("Total URLs discovered: {Count}", allEntries.Count);
		return allEntries;
	}

	private List<SitemapEntry> ParseUrlSet(XElement root) =>
		root
			.Elements(SitemapNs + "url")
			.Select(ParseUrlElement)
			.Where(e => e is not null)
			.Select(e => e!)
			.ToList();

	private static SitemapEntry? ParseUrlElement(XElement urlElement)
	{
		var loc = urlElement.Element(SitemapNs + "loc")?.Value;
		if (string.IsNullOrWhiteSpace(loc))
			return null;

		var lastmod = urlElement.Element(SitemapNs + "lastmod")?.Value;
		var changefreq = urlElement.Element(SitemapNs + "changefreq")?.Value;
		var priority = urlElement.Element(SitemapNs + "priority")?.Value;

		DateTimeOffset? lastModified = null;
		if (!string.IsNullOrWhiteSpace(lastmod) && DateTimeOffset.TryParse(lastmod, out var parsed))
			lastModified = parsed;

		double? priorityValue = null;
		if (!string.IsNullOrWhiteSpace(priority) && double.TryParse(priority, out var parsedPriority))
			priorityValue = parsedPriority;

		return new SitemapEntry(loc, lastModified, changefreq, priorityValue);
	}
}
