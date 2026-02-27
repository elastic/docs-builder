// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Parser for promotion report HTML files to extract PR lists
/// </summary>
public partial class PromotionReportParser(ILoggerFactory logFactory, IFileSystem? fileSystem = null)
{
	private readonly ILogger _logger = logFactory.CreateLogger<PromotionReportParser>();
	private readonly IFileSystem _fileSystem = fileSystem ?? new FileSystem();
	private static readonly HttpClient HttpClient = new();

	static PromotionReportParser()
	{
		HttpClient.DefaultRequestHeaders.Add("User-Agent", "docs-builder");
		HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
	}

	[GeneratedRegex(@"github\.com/([^/]+)/([^/]+)/pull/(\d+)", RegexOptions.IgnoreCase)]
	private static partial Regex GitHubPrUrlRegex();

	/// <summary>
	/// Determines if the argument is a version number, URL, or file path
	/// </summary>
	public static ProfileArgumentType DetectArgumentType(string argument)
	{
		if (string.IsNullOrWhiteSpace(argument))
			return ProfileArgumentType.Unknown;

		// Check if it's a URL
		if (argument.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
			argument.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
			return ProfileArgumentType.PromotionReportUrl;

		// Check if it's a file path that exists (could be a promotion report file)
		// Note: This is checked at runtime when file system is available

		// Otherwise, assume it's a version number
		return ProfileArgumentType.Version;
	}

	/// <summary>
	/// Extracts PR URLs from a promotion report (URL or local file)
	/// </summary>
	public async Task<PromotionReportResult> ParsePromotionReportAsync(string source, CancellationToken ctx = default)
	{
		try
		{
			string htmlContent;

			if (source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
			source.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
			{
				// Fetch URL content
				_logger.LogInformation("Fetching promotion report from URL: {Url}", source);
				var response = await HttpClient.GetAsync(source, ctx);
				if (!response.IsSuccessStatusCode)
				{
					return new PromotionReportResult
					{
						IsValid = false,
						ErrorMessage = $"Failed to fetch promotion report from URL: {response.StatusCode}"
					};
				}
				htmlContent = await response.Content.ReadAsStringAsync(ctx);
			}
			else if (_fileSystem.File.Exists(source))
			{
				// Read local file
				_logger.LogInformation("Reading promotion report from file: {FilePath}", source);
				htmlContent = await _fileSystem.File.ReadAllTextAsync(source, ctx);
			}
			else
			{
				return new PromotionReportResult
				{
					IsValid = false,
					ErrorMessage = $"Promotion report source not found: {source}"
				};
			}

			// Extract PR URLs from HTML content
			var prUrls = ExtractPrUrlsFromHtml(htmlContent);

			if (prUrls.Count == 0)
			{
				return new PromotionReportResult
				{
					IsValid = false,
					ErrorMessage = "No PR URLs found in promotion report"
				};
			}

			_logger.LogInformation("Extracted {Count} PR URLs from promotion report", prUrls.Count);

			return new PromotionReportResult
			{
				IsValid = true,
				PrUrls = prUrls
			};
		}
		catch (HttpRequestException ex)
		{
			_logger.LogWarning(ex, "HTTP error fetching promotion report");
			return new PromotionReportResult
			{
				IsValid = false,
				ErrorMessage = $"HTTP error fetching promotion report: {ex.Message}"
			};
		}
		catch (IOException ex)
		{
			_logger.LogWarning(ex, "IO error reading promotion report");
			return new PromotionReportResult
			{
				IsValid = false,
				ErrorMessage = $"IO error reading promotion report: {ex.Message}"
			};
		}
	}

	private List<string> ExtractPrUrlsFromHtml(string html)
	{
		var prUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		// Find all GitHub PR URLs in the HTML
		foreach (Match match in GitHubPrUrlRegex().Matches(html))
		{
			var owner = match.Groups[1].Value;
			var repo = match.Groups[2].Value;
			var prNumber = match.Groups[3].Value;
			var prUrl = $"https://github.com/{owner}/{repo}/pull/{prNumber}";
			_ = prUrls.Add(prUrl);
		}

		return [.. prUrls];
	}
}

/// <summary>
/// Type of profile argument detected
/// </summary>
public enum ProfileArgumentType
{
	Unknown,
	Version,
	PromotionReportUrl,
	PromotionReportFile,
	/// <summary>A newline-delimited file containing fully-qualified GitHub PR or issue URLs.</summary>
	UrlListFile
}

/// <summary>
/// Result of parsing a promotion report
/// </summary>
public record PromotionReportResult
{
	public bool IsValid { get; init; }
	public string? ErrorMessage { get; init; }
	public List<string> PrUrls { get; init; } = [];
}
