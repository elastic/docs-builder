// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Nullean.ScopedFileSystem;
using Elastic.Changelog.GitHub;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;
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

	/// <summary>
	/// The lifecycle inferred from the raw release tag when using <c>source: github_release</c>.
	/// Set to the lifecycle derived from the full tag name (e.g. <c>"preview"</c> for <c>v1.0.0-preview.1</c>),
	/// <em>before</em> <see cref="ChangelogTextUtilities.ExtractBaseVersion"/> strips the pre-release suffix.
	/// <c>null</c> for all other profile types; in those cases, lifecycle is inferred from <see cref="Version"/>.
	/// </summary>
	public string? Lifecycle { get; init; }
}

/// <summary>
/// Resolves a <see cref="BundleProfile"/> and a profile argument (version string, promotion
/// report URL/path, or URL list file) into a concrete filter that can be used by both
/// <see cref="ChangelogBundlingService"/> and <see cref="ChangelogRemoveService"/>.
/// </summary>
/// <summary>Result of resolving a URL list file into PR or issue URLs.</summary>
internal record UrlListFileResult(string[]? Prs, string[]? Issues);

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
	/// <param name="releaseService">
	/// Optional GitHub release service. Required when the profile's <c>source</c> is <c>github_release</c>.
	/// </param>
	public static async Task<ProfileFilterResult?> ResolveAsync(
		IDiagnosticsCollector collector,
		string profileName,
		string? profileArgument,
		ChangelogConfiguration? config,
		ScopedFileSystem fileSystem,
		ILogger? logger,
		Cancel ctx,
		string? profileReport = null,
		IGitHubReleaseService? releaseService = null)
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

		// Handle github_release source before the generic argument-type detection
		if (string.Equals(profile.Source, "github_release", StringComparison.OrdinalIgnoreCase))
			return await ResolveFromGitHubReleaseAsync(collector, profileName, profileArgument, profileReport, profile, config, releaseService, logger, ctx);

		// When a separate report argument is provided, profileArgument is always the version
		if (profileReport != null)
			return await ResolveWithSeparateReportAsync(collector, profileName, profileArgument, profileReport, profile, fileSystem, logger, ctx);

		// Auto-detect argument type
		var argType = PromotionReportParser.DetectArgumentType(profileArgument);

		// Treat an existing local file path as promotion report (HTML) or URL list file (anything else)
		if (argType == ProfileArgumentType.Version && fileSystem.File.Exists(profileArgument))
		{
			argType = DetectLocalFileType(fileSystem, profileArgument);
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
					prsFromReport = await parser.ParseReportToPrUrlsAsync(collector, profileArgument, ctx);
					if (prsFromReport == null)
						return null;

					logger?.LogInformation("Extracted {Count} PRs from promotion report", prsFromReport.Length);
					version = "unknown";
					break;
				}
			case ProfileArgumentType.UrlListFile:
				{
					var result = await ResolveUrlListFileAsync(collector, profileArgument, fileSystem, ctx);
					if (result == null)
						return null;

					prsFromReport = result.Prs;
					issuesFromFile = result.Issues;
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

		if (!TryParseProfileProducts(productsPattern, out var products, out var productsParseError))
		{
			collector.EmitError(string.Empty, $"Profile '{profileName}': {productsParseError}");
			return null;
		}

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
		ScopedFileSystem fileSystem,
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
			reportArgType = DetectLocalFileType(fileSystem, profileReport);
		}

		switch (reportArgType)
		{
			case ProfileArgumentType.PromotionReportUrl:
			case ProfileArgumentType.PromotionReportFile:
				{
					var parser = new PromotionReportParser(NullLoggerFactory.Instance, fileSystem);
					var prs = await parser.ParseReportToPrUrlsAsync(collector, profileReport, ctx);
					if (prs == null)
						return null;

					logger?.LogInformation("Extracted {Count} PRs from promotion report", prs.Length);
					return new ProfileFilterResult { Prs = prs, Version = version };
				}
			case ProfileArgumentType.UrlListFile:
				{
					var result = await ResolveUrlListFileAsync(collector, profileReport, fileSystem, ctx);
					if (result == null)
						return null;

					return result.Prs != null
						? new ProfileFilterResult { Prs = result.Prs, Version = version }
						: new ProfileFilterResult { Issues = result.Issues, Version = version };
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
	internal static async Task<UrlListFileResult?> ResolveUrlListFileAsync(
		IDiagnosticsCollector collector,
		string filePath,
		ScopedFileSystem fileSystem,
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

		return hasPrs ? new UrlListFileResult(lines, null) : new UrlListFileResult(null, lines);
	}

	private static ProfileArgumentType DetectLocalFileType(ScopedFileSystem fileSystem, string path) =>
		fileSystem.Path.GetExtension(path).ToLowerInvariant() is ".html" or ".htm"
			? ProfileArgumentType.PromotionReportFile
			: ProfileArgumentType.UrlListFile;

	/// <summary>
	/// Parses a products pattern like <c>"elasticsearch 9.2.0 ga"</c> or
	/// <c>"cloud-serverless {version} *"</c> (after placeholder substitution) into a
	/// <see cref="ProductArgument"/> list.
	/// </summary>
	/// <returns>False when any comma-separated segment has more than three space-separated fields (product, target, lifecycle).</returns>
	internal static bool TryParseProfileProducts(
		string pattern,
		[NotNullWhen(true)] out List<ProductArgument>? products,
		[NotNullWhen(false)] out string? errorMessage)
	{
		products = null;
		errorMessage = null;

		if (string.IsNullOrWhiteSpace(pattern))
		{
			products = [];
			return true;
		}

		// Support both single and multi-product: "kibana 9.3.0 ga" or "kibana 9.3.0 ga, security 9.3.0 ga"
		var productEntries = pattern.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var list = new List<ProductArgument>();

		foreach (var entry in productEntries)
		{
			var parts = entry.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length < 1)
				continue;

			if (parts.Length > 3)
			{
				errorMessage =
					"Each product entry must have at most three space-separated fields (product, target, lifecycle). " +
					$"Too many values in segment: '{entry}'.";
				return false;
			}

			list.Add(new ProductArgument
			{
				Product = parts[0] == "*" ? "*" : parts[0],
				Target = parts.Length > 1 ? (parts[1] == "*" ? "*" : parts[1]) : "*",
				Lifecycle = parts.Length > 2 ? (parts[2] == "*" ? "*" : parts[2]) : "*"
			});
		}

		products = list.Count > 0 ? list : [];
		return true;
	}

	/// <summary>
	/// Handles profiles with <c>source: github_release</c>. Fetches PR references directly
	/// from the GitHub release identified by <paramref name="profileArgument"/> (version tag or
	/// <c>"latest"</c>) and returns them as the PR filter.
	/// </summary>
	private static async Task<ProfileFilterResult?> ResolveFromGitHubReleaseAsync(
		IDiagnosticsCollector collector,
		string profileName,
		string profileArgument,
		string? profileReport,
		BundleProfile profile,
		ChangelogConfiguration? config,
		IGitHubReleaseService? releaseService,
		ILogger? logger,
		Cancel ctx)
	{
		if (!string.IsNullOrWhiteSpace(profile.Products))
		{
			collector.EmitError(
				string.Empty,
				$"Profile '{profileName}': 'source: github_release' cannot be combined with a 'products' filter. " +
				"Remove the 'products' field or change the source."
			);
			return null;
		}

		if (!string.IsNullOrWhiteSpace(profileReport))
		{
			collector.EmitError(
				string.Empty,
				$"Profile '{profileName}': 'source: github_release' does not accept a third positional argument. " +
				"The PR list is sourced automatically from the GitHub release. " +
				"To override the lifecycle in 'output_products', hardcode the value instead of using {{lifecycle}} " +
				"(for example, output_products: \"apm-agent-dotnet {{version}} preview\")."
			);
			return null;
		}

		if (releaseService == null)
		{
			collector.EmitError(string.Empty, $"Profile '{profileName}': a GitHub release service is required for 'source: github_release'.");
			return null;
		}

		// Resolve repo and owner: profile-level overrides bundle-level defaults
		var repo = profile.Repo ?? config?.Bundle?.Repo;
		var owner = profile.Owner ?? config?.Bundle?.Owner ?? "elastic";

		if (string.IsNullOrWhiteSpace(repo))
		{
			collector.EmitError(
				string.Empty,
				$"Profile '{profileName}': 'source: github_release' requires a GitHub repository name. " +
				"Set 'repo' on the profile or on the top-level 'bundle' configuration."
			);
			return null;
		}

		logger?.LogInformation("Fetching GitHub release {Version} from {Owner}/{Repo}", profileArgument, owner, repo);

		var release = await releaseService.FetchReleaseAsync(owner, repo, profileArgument, ctx);
		if (release == null)
		{
			collector.EmitError(
				string.Empty,
				$"Profile '{profileName}': failed to fetch release '{profileArgument}' from {owner}/{repo}. " +
				"Ensure the repository exists and the version tag is valid."
			);
			return null;
		}

		logger?.LogInformation("Fetched release {Tag} from {Owner}/{Repo}", release.TagName, owner, repo);

		var parsed = ReleaseNoteParser.Parse(release.Body);
		logger?.LogInformation("Detected release note format: {Format}, found {Count} PR references", parsed.Format, parsed.PrReferences.Count);

		if (parsed.PrReferences.Count == 0)
		{
			collector.EmitWarning(string.Empty, $"Profile '{profileName}': no PR references found in release '{release.TagName}'. The bundle will be empty.");
			return null;
		}

		var prUrls = parsed.PrReferences
			.Select(pr => $"https://github.com/{owner}/{repo}/pull/{pr.PrNumber}")
			.ToArray();

		var version = ChangelogTextUtilities.ExtractBaseVersion(release.TagName);
		// Infer lifecycle from the raw tag before base-version extraction so that pre-release suffixes
		// (e.g. "-preview.1", "-beta.1") are preserved for {lifecycle} substitution in output_products/output.
		var lifecycle = VersionLifecycleInference.InferLifecycle(release.TagName);
		logger?.LogInformation("Resolved {Count} PR URLs from release {Tag} (version: {Version}, lifecycle: {Lifecycle})", prUrls.Length, release.TagName, version, lifecycle);

		return new ProfileFilterResult { Prs = prUrls, Version = version, Lifecycle = lifecycle };
	}
}
