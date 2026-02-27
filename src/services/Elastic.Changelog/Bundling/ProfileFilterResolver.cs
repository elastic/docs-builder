// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Changelog.Bundling;

/// <summary>
/// The resolved filter derived from a bundle profile — either a product list, a set of PR URLs, or a set of issue URLs.
/// Exactly one of <see cref="Products"/>, <see cref="Prs"/> and <see cref="Issues"/> will be non-null on a successful result.
/// </summary>
public record ProfileFilterResult
{
	/// <summary>Product/target/lifecycle filters parsed from the profile's products pattern.</summary>
	public IReadOnlyList<ProductArgument>? Products { get; init; }

	/// <summary>PR URLs extracted from a promotion report or URL list file supplied as the profile argument.</summary>
	public string[]? Prs { get; init; }

	/// <summary>Issue URLs extracted from a URL list file supplied as the profile argument.</summary>
	public string[]? Issues { get; init; }

	/// <summary>
	/// The resolved version string used for placeholder substitution.
	/// This is the profile argument itself for version-based invocations, or <c>"unknown"</c> when
	/// a promotion report or URL list file was parsed (because the actual version is not available from the file).
	/// When both a version and a report are provided (Phase 3.4), this is always the explicit version.
	/// </summary>
	public string Version { get; init; } = "unknown";
}

/// <summary>
/// Resolves a <see cref="BundleProfile"/> and a profile argument (version string, promotion
/// report URL/path, or URL list file) into a concrete filter that can be used by both
/// <see cref="ChangelogBundlingService"/> and <see cref="ChangelogRemoveService"/>.
/// </summary>
public static partial class ProfileFilterResolver
{
	[GeneratedRegex(@"^https?://github\.com/[^/]+/[^/]+/pull/\d+/?$", RegexOptions.IgnoreCase)]
	private static partial Regex GitHubPrUrlRegex();

	[GeneratedRegex(@"^https?://github\.com/[^/]+/[^/]+/issues/\d+/?$", RegexOptions.IgnoreCase)]
	private static partial Regex GitHubIssueUrlRegex();

	/// <summary>
	/// Resolves the profile into a <see cref="ProfileFilterResult"/>, or returns <c>null</c> and
	/// emits diagnostics errors on failure.
	/// </summary>
	/// <param name="profileReport">
	/// Optional third argument. When non-null, <paramref name="profileArgument"/> is treated as the
	/// version string for <c>{version}</c> substitution and this value is used as the PR/issue filter
	/// source (promotion report or URL list file).
	/// </param>
	public static async Task<ProfileFilterResult?> ResolveAsync(
		IDiagnosticsCollector collector,
		string profileName,
		string? profileArgument,
		ChangelogConfiguration? config,
		IFileSystem fileSystem,
		ILogger? logger,
		Cancel ctx,
		string? profileReport = null)
	{
		if (config?.Bundle?.Profiles == null || !config.Bundle.Profiles.TryGetValue(profileName, out var profile))
		{
			collector.EmitError(string.Empty, $"Profile '{profileName}' not found in bundle.profiles configuration");
			return null;
		}

		if (string.IsNullOrWhiteSpace(profileArgument))
		{
			collector.EmitError(string.Empty, $"Profile '{profileName}' requires a version number or promotion report URL as the second argument");
			return null;
		}

		// When a separate report argument is provided, profileArgument is always the version
		if (profileReport != null)
			return await ResolveWithSeparateReportAsync(collector, profileName, profileArgument, profileReport, profile, fileSystem, logger, ctx);

		// Auto-detect argument type
		var argType = PromotionReportParser.DetectArgumentType(profileArgument);

		// Treat an existing local file path as promotion report (HTML) or URL list file (anything else)
		if (argType == ProfileArgumentType.Version && fileSystem.File.Exists(profileArgument))
		{
			var ext = fileSystem.Path.GetExtension(profileArgument).ToLowerInvariant();
			argType = ext is ".html" or ".htm"
				? ProfileArgumentType.PromotionReportFile
				: ProfileArgumentType.UrlListFile;
		}

		string version;
		string[]? prsFromReport = null;
		string[]? issuesFromFile = null;

		switch (argType)
		{
			case ProfileArgumentType.PromotionReportUrl:
			case ProfileArgumentType.PromotionReportFile:
				{
					var parser = new PromotionReportParser(NullLoggerFactory.Instance, fileSystem);
					var reportResult = await parser.ParsePromotionReportAsync(profileArgument, ctx);

					if (!reportResult.IsValid)
					{
						collector.EmitError(string.Empty, reportResult.ErrorMessage ?? "Failed to parse promotion report");
						return null;
					}

					prsFromReport = reportResult.PrUrls.ToArray();
					logger?.LogInformation("Extracted {Count} PRs from promotion report", prsFromReport.Length);
					version = "unknown";
					break;
				}
			case ProfileArgumentType.UrlListFile:
				{
					var result = await ResolveUrlListFileAsync(collector, profileArgument, fileSystem, ctx);
					if (result == null)
						return null;

					prsFromReport = result.Value.Prs;
					issuesFromFile = result.Value.Issues;
					version = "unknown";
					break;
				}
			default:
				{
					version = profileArgument;
					break;
				}
		}

		// Substitute {version} and {lifecycle} in the products pattern
		var lifecycle = VersionLifecycleInference.InferLifecycle(version);
		var productsPattern = profile.Products?
			.Replace("{version}", version)
			.Replace("{lifecycle}", lifecycle);

		// If we have PRs or issues from a file/report, return those directly
		if (prsFromReport != null)
			return new ProfileFilterResult { Prs = prsFromReport, Version = version };

		if (issuesFromFile != null)
			return new ProfileFilterResult { Issues = issuesFromFile, Version = version };

		// Without a promotion report or URL list we need a products pattern to filter by
		if (string.IsNullOrWhiteSpace(productsPattern))
		{
			collector.EmitError(
				string.Empty,
				$"Profile '{profileName}' has no 'products' pattern configured and no promotion report was provided — cannot determine filter"
			);
			return null;
		}

		var products = ParseProfileProducts(productsPattern);
		return new ProfileFilterResult { Products = products, Version = version };
	}

	/// <summary>
	/// Handles the case where both a version string and a separate report/URL-list argument are provided.
	/// <paramref name="profileArgument"/> is the version; <paramref name="profileReport"/> is the filter source.
	/// </summary>
	private static async Task<ProfileFilterResult?> ResolveWithSeparateReportAsync(
		IDiagnosticsCollector collector,
		string profileName,
		string profileArgument,
		string profileReport,
		BundleProfile profile,
		IFileSystem fileSystem,
		ILogger? logger,
		Cancel ctx)
	{
		// profileArgument must be a version string, not a file/URL
		var argType = PromotionReportParser.DetectArgumentType(profileArgument);
		if (argType == ProfileArgumentType.PromotionReportUrl ||
			(argType == ProfileArgumentType.Version && fileSystem.File.Exists(profileArgument)))
		{
			collector.EmitError(
				string.Empty,
				"When two arguments are provided, the first must be a version string and the second must be a promotion report or URL list file. " +
				$"'{profileArgument}' looks like a report path or URL. " +
				$"Did you mean: {profileName} <version> {profileArgument}?"
			);
			return null;
		}

		// A products pattern is mutually exclusive with a report/URL-list filter
		if (!string.IsNullOrWhiteSpace(profile.Products))
		{
			collector.EmitError(
				string.Empty,
				$"Profile '{profileName}' has a 'products' pattern configured. " +
				"A promotion report or URL list file cannot be combined with a products pattern filter."
			);
			return null;
		}

		var version = profileArgument;

		// Detect the type of the report argument
		var reportArgType = PromotionReportParser.DetectArgumentType(profileReport);
		if (reportArgType == ProfileArgumentType.Version && fileSystem.File.Exists(profileReport))
		{
			var ext = fileSystem.Path.GetExtension(profileReport).ToLowerInvariant();
			reportArgType = ext is ".html" or ".htm"
				? ProfileArgumentType.PromotionReportFile
				: ProfileArgumentType.UrlListFile;
		}

		switch (reportArgType)
		{
			case ProfileArgumentType.PromotionReportUrl:
			case ProfileArgumentType.PromotionReportFile:
				{
					var parser = new PromotionReportParser(NullLoggerFactory.Instance, fileSystem);
					var reportResult = await parser.ParsePromotionReportAsync(profileReport, ctx);

					if (!reportResult.IsValid)
					{
						collector.EmitError(string.Empty, reportResult.ErrorMessage ?? "Failed to parse promotion report");
						return null;
					}

					var prs = reportResult.PrUrls.ToArray();
					logger?.LogInformation("Extracted {Count} PRs from promotion report", prs.Length);
					return new ProfileFilterResult { Prs = prs, Version = version };
				}
			case ProfileArgumentType.UrlListFile:
				{
					var result = await ResolveUrlListFileAsync(collector, profileReport, fileSystem, ctx);
					if (result == null)
						return null;

					return result.Value.Prs != null
						? new ProfileFilterResult { Prs = result.Value.Prs, Version = version }
						: new ProfileFilterResult { Issues = result.Value.Issues, Version = version };
				}
			default:
				collector.EmitError(
					string.Empty,
					$"The third argument '{profileReport}' must be a promotion report URL, a local HTML file, or a URL list file. " +
					"Use a URL (https://...), a local .html file, or a text file containing fully-qualified GitHub PR/issue URLs."
				);
				return null;
		}
	}

	/// <summary>
	/// Reads a newline-delimited URL list file and validates/classifies its contents as PR or issue URLs.
	/// Returns <c>null</c> and emits errors on failure.
	/// </summary>
	internal static async Task<(string[]? Prs, string[]? Issues)?> ResolveUrlListFileAsync(
		IDiagnosticsCollector collector,
		string filePath,
		IFileSystem fileSystem,
		Cancel ctx)
	{
		var content = await fileSystem.File.ReadAllTextAsync(filePath, ctx);
		var lines = content
			.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Where(l => !string.IsNullOrWhiteSpace(l))
			.ToArray();

		if (lines.Length == 0)
		{
			collector.EmitError(filePath, "URL list file is empty");
			return null;
		}

		var hasPrs = false;
		var hasIssues = false;

		foreach (var line in lines)
		{
			if (!line.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
				!line.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
			{
				collector.EmitError(
					filePath,
					$"File must contain fully-qualified GitHub URLs (e.g. https://github.com/owner/repo/pull/123). " +
					$"Numbers and short forms are not allowed. Found: {line}"
				);
				return null;
			}

			if (GitHubPrUrlRegex().IsMatch(line))
				hasPrs = true;
			else if (GitHubIssueUrlRegex().IsMatch(line))
				hasIssues = true;
			else
			{
				collector.EmitError(
					filePath,
					$"File must contain GitHub pull request or issue URLs " +
					$"(e.g. https://github.com/owner/repo/pull/123 or https://github.com/owner/repo/issues/456). " +
					$"Not a recognized URL: {line}"
				);
				return null;
			}
		}

		if (hasPrs && hasIssues)
		{
			collector.EmitError(filePath, "File must contain only pull request URLs or only issue URLs, not a mix.");
			return null;
		}

		return hasPrs ? (lines, null) : (null, lines);
	}

	/// <summary>
	/// Parses a products pattern like <c>"elasticsearch 9.2.0 ga"</c> or
	/// <c>"cloud-serverless {version} *"</c> (after placeholder substitution) into a
	/// <see cref="ProductArgument"/> list.
	/// </summary>
	internal static List<ProductArgument> ParseProfileProducts(string pattern)
	{
		var parts = pattern.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length < 1)
			return [];

		var productId = parts[0];
		var target = parts.Length > 1 ? parts[1] : "*";
		var lifecycle = parts.Length > 2 ? parts[2] : "*";

		return
		[
			new ProductArgument
			{
				Product = productId == "*" ? "*" : productId,
				Target = target == "*" ? "*" : target,
				Lifecycle = lifecycle == "*" ? "*" : lifecycle
			}
		];
	}
}
