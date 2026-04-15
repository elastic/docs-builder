// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;
using Nullean.ScopedFileSystem;

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Parser for promotion report HTML files to extract PR lists
/// </summary>
public partial class PromotionReportParser(ILoggerFactory logFactory, ScopedFileSystem? fileSystem = null)
{
	private readonly ILogger _logger = logFactory.CreateLogger<PromotionReportParser>();
	private readonly IFileSystem _fileSystem = fileSystem ?? FileSystemFactory.RealRead;

	private static readonly string[] AllowedHosts = ["github.com", "buildkite.com"];

	private static readonly HttpClient HttpClient = CreateHttpClient();

	private static HttpClient CreateHttpClient()
	{
		var handler = new SocketsHttpHandler
		{
			AllowAutoRedirect = false,
			ConnectTimeout = TimeSpan.FromSeconds(10),
			UseProxy = false
		};
		var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
		client.DefaultRequestHeaders.Add("User-Agent", "docs-builder");
		client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
		return client;
	}

	private static bool IsAllowedUrl(string url) =>
		Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
		uri.Scheme == Uri.UriSchemeHttps &&
		AllowedHosts.Any(domain =>
			uri.Host.Equals(domain, StringComparison.OrdinalIgnoreCase) ||
			uri.Host.EndsWith($".{domain}", StringComparison.OrdinalIgnoreCase));

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
	/// Parses a promotion report and returns the extracted PR URLs, or <c>null</c> on failure (emitting errors).
	/// </summary>
	public async Task<string[]?> ParseReportToPrUrlsAsync(
		IDiagnosticsCollector collector, string source, Cancel ctx)
	{
		var result = await ParsePromotionReportAsync(source, ctx);
		if (result.IsValid)
			return [.. result.PrUrls];

		collector.EmitError(string.Empty, result.ErrorMessage ?? "Failed to parse promotion report");
		return null;
	}

	/// <summary>
	/// Extracts PR URLs from a promotion report (URL or local file)
	/// </summary>
	private async Task<PromotionReportResult> ParsePromotionReportAsync(string source, CancellationToken ctx = default)
	{
		try
		{
			string htmlContent;

			if (source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
				source.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
			{
				var (content, error) = await FetchReportUrlAsync(source, ctx);
				if (error != null)
					return new PromotionReportResult { IsValid = false, ErrorMessage = error };
				htmlContent = content!;
			}
			else if (_fileSystem.File.Exists(source))
			{
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

			return new PromotionReportResult { IsValid = true, PrUrls = prUrls };
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

	/// <summary>Returns (content, null) on success or (null, errorMessage) on failure.</summary>
	private async Task<(string? Content, string? Error)> FetchReportUrlAsync(string url, Cancel ctx)
	{
		if (!IsAllowedUrl(url))
			return (null, $"Report URL must use HTTPS and target an allowed domain ({string.Join(", ", AllowedHosts)}): {url}");

		_logger.LogInformation("Fetching promotion report from URL: {Url}", url);
		var response = await HttpClient.GetAsync(url, ctx);

		if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location != null)
		{
			var redirectTarget = response.Headers.Location.IsAbsoluteUri
				? response.Headers.Location.ToString()
				: new Uri(new Uri(url), response.Headers.Location).ToString();

			if (!IsAllowedUrl(redirectTarget))
				return (null, $"Report URL redirected to a disallowed domain: {redirectTarget}");

			_logger.LogInformation("Following redirect to: {Url}", redirectTarget);
			response = await HttpClient.GetAsync(redirectTarget, ctx);
		}

		if (!response.IsSuccessStatusCode)
			return (null, $"Failed to fetch promotion report from URL: {response.StatusCode}");

		var content = await response.Content.ReadAsStringAsync(ctx);
		return (content, null);
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
