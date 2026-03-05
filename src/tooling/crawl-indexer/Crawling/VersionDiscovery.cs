// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace CrawlIndexer.Crawling;

/// <summary>
/// Discovers versions from guide URLs.
/// </summary>
public partial class VersionDiscovery(ILogger<VersionDiscovery> logger) : IVersionDiscovery
{
	// Matches version patterns like 8.15, 7.17, 8.0
	[GeneratedRegex(@"/(\d+)\.(\d+)/", RegexOptions.Compiled)]
	private static partial Regex VersionPattern();

	public IReadOnlyList<DiscoveredVersion> DiscoverVersions(IEnumerable<SitemapEntry> guideUrls)
	{
		var versions = new Dictionary<string, DiscoveredVersion>(StringComparer.OrdinalIgnoreCase);

		foreach (var entry in guideUrls)
		{
			var match = VersionPattern().Match(entry.Location);
			if (!match.Success)
				continue;

			var major = int.Parse(match.Groups[1].Value);
			var minor = int.Parse(match.Groups[2].Value);
			var version = $"{major}.{minor}";

			if (!versions.ContainsKey(version))
				versions[version] = new DiscoveredVersion(version, major);
		}

		logger.LogDebug("Discovered {Count} unique versions", versions.Count);

		// Group by major version
		var byMajor = versions.Values
			.GroupBy(v => v.MajorVersion)
			.ToDictionary(g => g.Key, g => g.ToList());

		var result = new List<DiscoveredVersion>();

		// Include all 8.x versions
		if (byMajor.TryGetValue(8, out var v8))
		{
			result.AddRange(v8.OrderByDescending(v => ParseMinor(v.Version)));
			logger.LogInformation("Including all {Count} 8.x versions", v8.Count);
		}

		// Include only the latest 7.x version
		if (byMajor.TryGetValue(7, out var v7))
		{
			var latest7 = v7.OrderByDescending(v => ParseMinor(v.Version)).First();
			result.Add(latest7);
			logger.LogInformation("Including latest 7.x version: {Version}", latest7.Version);
		}

		return result;
	}

	private static int ParseMinor(string version)
	{
		var parts = version.Split('.');
		return parts.Length >= 2 && int.TryParse(parts[1], out var minor) ? minor : 0;
	}
}
