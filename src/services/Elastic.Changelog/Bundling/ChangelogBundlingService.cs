// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Elastic.Changelog.Configuration;
using Elastic.Changelog.Rendering;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Arguments for the BundleChangelogs method
/// </summary>
public record BundleChangelogsArguments
{
	/// <summary>
	/// Directory containing changelog YAML files. null = use config default.
	/// </summary>
	public string? Directory { get; init; }
	public string? Output { get; init; }
	public bool All { get; init; }
	public IReadOnlyList<ProductArgument>? InputProducts { get; init; }
	public IReadOnlyList<ProductArgument>? OutputProducts { get; init; }
	/// <summary>
	/// Whether to resolve (copy contents of each changelog file into the entries array).
	/// null = use config default; true = --resolve; false = --no-resolve.
	/// </summary>
	public bool? Resolve { get; init; }
	public string[]? Prs { get; init; }
	public string[]? Issues { get; init; }
	public string? Owner { get; init; }
	public string? Repo { get; init; }

	/// <summary>
	/// Profile name to use (from bundle.profiles in config)
	/// </summary>
	public string? Profile { get; init; }

	/// <summary>
	/// Version number or promotion report URL/path for profile-based bundling
	/// </summary>
	public string? ProfileArgument { get; init; }

	/// <summary>
	/// Output directory for bundled changelog files (from config bundle.output_directory)
	/// </summary>
	public string? OutputDirectory { get; init; }

	/// <summary>
	/// Path to the changelog.yml configuration file
	/// </summary>
	public string? Config { get; init; }

	/// <summary>
	/// Feature IDs to mark as hidden in the bundle output.
	/// When the bundle is rendered (by CLI render or {changelog} directive),
	/// entries with matching feature-id values will be commented out.
	/// </summary>
	public string[]? HideFeatures { get; init; }
}

/// <summary>
/// Service for bundling changelog files
/// </summary>
public partial class ChangelogBundlingService(
	ILoggerFactory logFactory,
	IConfigurationContext? configurationContext = null,
	IFileSystem? fileSystem = null)
	: IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogBundlingService>();
	private readonly IFileSystem _fileSystem = fileSystem ?? new FileSystem();
	private readonly ChangelogConfigurationLoader? _configLoader = configurationContext != null
		? new ChangelogConfigurationLoader(logFactory, configurationContext, fileSystem ?? new FileSystem())
		: null;

	[GeneratedRegex(@"(\s+)version:", RegexOptions.Multiline)]
	internal static partial Regex VersionToTargetRegex();

	[GeneratedRegex(@"github\.com/([^/]+)/([^/]+)/pull/(\d+)", RegexOptions.IgnoreCase)]
	private static partial Regex GitHubPrUrlRegex();

	[GeneratedRegex(@"github\.com/([^/]+)/([^/]+)/issues/(\d+)", RegexOptions.IgnoreCase)]
	private static partial Regex GitHubIssueUrlRegex();

	public async Task<bool> BundleChangelogs(IDiagnosticsCollector collector, BundleChangelogsArguments input, Cancel ctx)
	{
		try
		{
			// Load changelog configuration if available
			ChangelogConfiguration? config = null;
			if (_configLoader != null)
				config = await _configLoader.LoadChangelogConfiguration(collector, input.Config, ctx);

			// Handle profile-based bundling
			if (!string.IsNullOrWhiteSpace(input.Profile))
			{
				var profileResult = await ProcessProfile(collector, input, config, ctx);
				if (profileResult == null)
					return false;
				input = profileResult;
			}

			// Apply config defaults if available
			input = ApplyConfigDefaults(input, config);

			// Validate input
			if (!ValidateInput(collector, input))
				return false;

			// Load PR or issue filter values
			var prsToMatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var issuesToMatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			if (input.Prs is { Length: > 0 })
			{
				var prFilterLoader = new PrFilterLoader(_fileSystem);
				var prFilterResult = await prFilterLoader.LoadPrsAsync(collector, input.Prs, input.Owner, input.Repo, ctx);
				if (!prFilterResult.IsValid)
					return false;
				prsToMatch = prFilterResult.PrsToMatch;
			}
			else if (input.Issues is { Length: > 0 })
			{
				var issueFilterLoader = new IssueFilterLoader(_fileSystem);
				var issueFilterResult = await issueFilterLoader.LoadIssuesAsync(collector, input.Issues, input.Owner, input.Repo, ctx);
				if (!issueFilterResult.IsValid)
					return false;
				issuesToMatch = issueFilterResult.IssuesToMatch;
			}

			// Directory is resolved by ApplyConfigDefaults (never null at this point)
			var directory = input.Directory!;

			// Determine output path
			var outputPath = input.Output ?? _fileSystem.Path.Combine(directory, "changelog-bundle.yaml");

			// Discover changelog files
			var fileDiscovery = new ChangelogFileDiscovery(_fileSystem, _logger);
			var yamlFiles = await fileDiscovery.DiscoverChangelogFilesAsync(directory, outputPath, ctx);

			if (yamlFiles.Count == 0)
			{
				collector.EmitError(directory, "No YAML files found in directory");
				return false;
			}

			_logger.LogInformation("Found {Count} YAML files in directory", yamlFiles.Count);

			// Build filter criteria
			var filterCriteria = BuildFilterCriteria(input, prsToMatch, issuesToMatch);

			// Match changelog entries
			var entryMatcher = new ChangelogEntryMatcher(_fileSystem, ReleaseNotesSerialization.GetEntryDeserializer(), _logger);
			var matchResult = await entryMatcher.MatchChangelogsAsync(collector, yamlFiles, filterCriteria, ctx);

			_logger.LogInformation("Found {Count} matching changelog entries", matchResult.Entries.Count);

			if (matchResult.Entries.Count == 0)
			{
				collector.EmitError(string.Empty, "No changelog entries matched the filter criteria");
				return false;
			}

			// Load feature IDs to hide
			var featureHidingLoader = new FeatureHidingLoader(_fileSystem);
			var featureHidingResult = await featureHidingLoader.LoadFeatureIdsAsync(collector, input.HideFeatures, ctx);
			if (!featureHidingResult.IsValid)
				return false;

			// Build bundle
			var bundleBuilder = new BundleBuilder();
			var buildResult = bundleBuilder.BuildBundle(
				collector,
				matchResult.Entries,
				input.OutputProducts,
				input.Resolve ?? false,
				input.Repo,
				featureHidingResult.FeatureIdsToHide
			);

			if (!buildResult.IsValid || buildResult.Data == null)
				return false;

			// Write bundle file
			await WriteBundleFileAsync(buildResult.Data, outputPath, ctx);

			return true;
		}
		catch (IOException ioEx)
		{
			collector.EmitError(string.Empty, $"IO error bundling changelogs: {ioEx.Message}", ioEx);
			return false;
		}
		catch (UnauthorizedAccessException uaEx)
		{
			collector.EmitError(string.Empty, $"Access denied bundling changelogs: {uaEx.Message}", uaEx);
			return false;
		}
	}

	private async Task<BundleChangelogsArguments?> ProcessProfile(IDiagnosticsCollector collector, BundleChangelogsArguments input, ChangelogConfiguration? config, Cancel ctx)
	{
		if (config?.Bundle?.Profiles == null || !config.Bundle.Profiles.TryGetValue(input.Profile!, out var profile))
		{
			collector.EmitError(string.Empty, $"Profile '{input.Profile}' not found in bundle.profiles configuration");
			return null;
		}

		if (string.IsNullOrWhiteSpace(input.ProfileArgument))
		{
			collector.EmitError(string.Empty, $"Profile '{input.Profile}' requires a version number or promotion report URL as the second argument");
			return null;
		}

		// Auto-detect argument type
		var argType = PromotionReportParser.DetectArgumentType(input.ProfileArgument);

		// Check if it's a local file
		if (argType == ProfileArgumentType.Version && _fileSystem.File.Exists(input.ProfileArgument))
			argType = ProfileArgumentType.PromotionReportFile;

		string version;
		string[]? prsFromReport = null;

		if (argType is ProfileArgumentType.PromotionReportUrl or ProfileArgumentType.PromotionReportFile)
		{
			// Parse promotion report to get PR list
			var parser = new PromotionReportParser(NullLoggerFactory.Instance, _fileSystem);
			var reportResult = await parser.ParsePromotionReportAsync(input.ProfileArgument, ctx);

			if (!reportResult.IsValid)
			{
				collector.EmitError(string.Empty, reportResult.ErrorMessage ?? "Failed to parse promotion report");
				return null;
			}

			prsFromReport = reportResult.PrUrls.ToArray();
			_logger.LogInformation("Extracted {Count} PRs from promotion report", prsFromReport.Length);

			// Extract version from PR URLs or use "unknown"
			// Try to extract from first PR URL path
			version = "unknown";
		}
		else
		{
			// Use the argument as the version number
			version = input.ProfileArgument;
		}

		// Substitute {version} and {lifecycle} in profile patterns
		var lifecycle = VersionLifecycleInference.InferLifecycle(version);
		var productsPattern = profile.Products?
			.Replace("{version}", version)
			.Replace("{lifecycle}", lifecycle);
		var outputPattern = profile.Output?.Replace("{version}", version);

		// Parse products pattern
		List<ProductArgument>? inputProducts = null;
		if (!string.IsNullOrWhiteSpace(productsPattern))
			inputProducts = ParseProfileProducts(productsPattern);

		// Determine output path
		string? outputPath = null;
		if (!string.IsNullOrWhiteSpace(outputPattern))
		{
			var outputDir = config.Bundle.OutputDirectory ?? input.OutputDirectory ?? input.Directory ?? _fileSystem.Directory.GetCurrentDirectory();
			outputPath = _fileSystem.Path.Combine(outputDir, outputPattern);
		}

		// Merge profile hide-features with any CLI-provided hide-features
		var mergedHideFeatures = MergeHideFeatures(input.HideFeatures, profile.HideFeatures);

		// If we have PRs from a promotion report, use those; otherwise use input products filter
		return input with
		{
			InputProducts = prsFromReport == null ? inputProducts : null,
			Prs = prsFromReport,
			All = false,
			Output = outputPath ?? input.Output,
			HideFeatures = mergedHideFeatures
		};
	}

	private static string[]? MergeHideFeatures(string[]? cliHideFeatures, IReadOnlyList<string>? profileHideFeatures)
	{
		if (cliHideFeatures is not { Length: > 0 } && profileHideFeatures is not { Count: > 0 })
			return null;

		var merged = new HashSet<string>(cliHideFeatures ?? [], StringComparer.OrdinalIgnoreCase);

		if (profileHideFeatures is { Count: > 0 })
			merged.UnionWith(profileHideFeatures);

		return merged.Count > 0 ? [.. merged] : null;
	}

	private static List<ProductArgument> ParseProfileProducts(string pattern)
	{
		// Parse pattern like "elasticsearch {version} ga" or "cloud-serverless {version} *"
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

	private static BundleChangelogsArguments ApplyConfigDefaults(BundleChangelogsArguments input, ChangelogConfiguration? config)
	{
		// Apply directory: CLI takes precedence. Only use config when --directory not specified.
		var directory = input.Directory ?? config?.Bundle?.Directory ?? Directory.GetCurrentDirectory();

		if (config?.Bundle == null)
			return input with { Directory = directory };

		// Apply output default when --output not specified: use bundle.output_directory if set
		var output = input.Output;
		if (string.IsNullOrWhiteSpace(output) && !string.IsNullOrWhiteSpace(config.Bundle.OutputDirectory))
			output = Path.Combine(config.Bundle.OutputDirectory, "changelog-bundle.yaml");

		// Apply resolve: CLI takes precedence over config. Only use config when CLI did not specify.
		var resolve = input.Resolve ?? config.Bundle.Resolve;

		return input with
		{
			Directory = directory,
			Output = output,
			Resolve = resolve
		};
	}

	private bool ValidateInput(IDiagnosticsCollector collector, BundleChangelogsArguments input)
	{
		if (string.IsNullOrWhiteSpace(input.Directory))
		{
			collector.EmitError(string.Empty, "Directory is required");
			return false;
		}

		if (!_fileSystem.Directory.Exists(input.Directory))
		{
			collector.EmitError(input.Directory, "Directory does not exist");
			return false;
		}

		// Validate filter options - exactly one of: --all, --input-products, --prs, --issues
		var specifiedFilters = new List<string>();
		if (input.All)
			specifiedFilters.Add("--all");
		if (input.InputProducts is { Count: > 0 })
			specifiedFilters.Add("--input-products");
		if (input.Prs is { Length: > 0 })
			specifiedFilters.Add("--prs");
		if (input.Issues is { Length: > 0 })
			specifiedFilters.Add("--issues");

		if (specifiedFilters.Count == 0)
		{
			collector.EmitError(string.Empty, "At least one filter option must be specified: --all, --input-products, --prs, or --issues");
			return false;
		}

		if (specifiedFilters.Count > 1)
		{
			collector.EmitError(string.Empty,
				$"Multiple filter options cannot be specified together. You specified: {string.Join(", ", specifiedFilters)}. Please use only one filter option: --all, --input-products, --prs, or --issues");
			return false;
		}

		return true;
	}

	private static ChangelogFilterCriteria BuildFilterCriteria(
		BundleChangelogsArguments input,
		HashSet<string> prsToMatch,
		HashSet<string> issuesToMatch)
	{
		var productFilters = new List<ProductFilter>();
		if (input.InputProducts is { Count: > 0 })
		{
			foreach (var product in input.InputProducts)
			{
				productFilters.Add(new ProductFilter
				{
					ProductPattern = product.Product == "*" ? null : product.Product,
					TargetPattern = product.Target == "*" ? null : product.Target,
					LifecyclePattern = product.Lifecycle == "*" ? null : product.Lifecycle
				});
			}
		}

		return new ChangelogFilterCriteria
		{
			IncludeAll = input.All,
			ProductFilters = productFilters,
			PrsToMatch = prsToMatch,
			IssuesToMatch = issuesToMatch,
			DefaultOwner = input.Owner,
			DefaultRepo = input.Repo
		};
	}

	private async Task WriteBundleFileAsync(Bundle bundledData, string outputPath, Cancel ctx)
	{
		// Generate bundled YAML
		var bundledYaml = ReleaseNotesSerialization.SerializeBundle(bundledData);

		// Ensure output directory exists
		var outputDir = _fileSystem.Path.GetDirectoryName(outputPath);
		if (!string.IsNullOrWhiteSpace(outputDir) && !_fileSystem.Directory.Exists(outputDir))
			_ = _fileSystem.Directory.CreateDirectory(outputDir);

		// If output file already exists, generate a unique filename
		if (_fileSystem.File.Exists(outputPath))
		{
			var directory = _fileSystem.Path.GetDirectoryName(outputPath) ?? string.Empty;
			var fileNameWithoutExtension = _fileSystem.Path.GetFileNameWithoutExtension(outputPath);
			var extension = _fileSystem.Path.GetExtension(outputPath);
			var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			var uniqueFileName = $"{fileNameWithoutExtension}-{timestamp}{extension}";
			outputPath = _fileSystem.Path.Combine(directory, uniqueFileName);
			_logger.LogInformation("Output file already exists, using unique filename: {OutputPath}", outputPath);
		}

		// Write bundled file with explicit UTF-8 encoding to ensure proper character handling
		await _fileSystem.File.WriteAllTextAsync(outputPath, bundledYaml, Encoding.UTF8, ctx);
		_logger.LogInformation("Created bundled changelog: {OutputPath}", outputPath);
	}

	/// <summary>
	/// Computes a SHA1 hash from the normalized YAML content (comments stripped, version→target).
	/// This ensures checksums represent semantic content, not formatting or comments.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5350:Do not use insecure cryptographic algorithm SHA1", Justification = "SHA1 is required for compatibility with existing changelog bundle format")]
	internal static string ComputeSha1(string content)
	{
		var normalized = ReleaseNotesSerialization.NormalizeYaml(content);
		var bytes = Encoding.UTF8.GetBytes(normalized);
		var hash = SHA1.HashData(bytes);
		return Convert.ToHexString(hash).ToLowerInvariant();
	}

	internal static string NormalizePrForComparison(string pr, string? defaultOwner, string? defaultRepo)
	{
		// Parse PR using the same logic as GitHubPrService.ParsePrUrl
		// Return a normalized format (owner/repo#number) for comparison

		// Trim whitespace first
		pr = pr.Trim();

		// Handle full URL: https://github.com/owner/repo/pull/123
		if (pr.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase) ||
			pr.StartsWith("http://github.com/", StringComparison.OrdinalIgnoreCase))
		{
			// Use regex to parse URL more reliably
			var match = GitHubPrUrlRegex().Match(pr);
			if (match.Success && match.Groups.Count >= 4)
			{
				var owner = match.Groups[1].Value.Trim();
				var repo = match.Groups[2].Value.Trim();
				var prPart = match.Groups[3].Value.Trim();
				if (!string.IsNullOrWhiteSpace(owner) && !string.IsNullOrWhiteSpace(repo) &&
					int.TryParse(prPart, out var prNum))
					return $"{owner}/{repo}#{prNum}".ToLowerInvariant();
			}

			// Fallback to URI parsing if regex fails
			try
			{
				var uri = new Uri(pr);
				var segments = uri.Segments;
				// segments[0] is "/", segments[1] is "owner/", segments[2] is "repo/", segments[3] is "pull/", segments[4] is "123"
				if (segments.Length >= 5 && segments[3].Equals("pull/", StringComparison.OrdinalIgnoreCase))
				{
					var owner = segments[1].TrimEnd('/').Trim();
					var repo = segments[2].TrimEnd('/').Trim();
					var prPart = segments[4].TrimEnd('/').Trim();
					if (!string.IsNullOrWhiteSpace(owner) && !string.IsNullOrWhiteSpace(repo) &&
						int.TryParse(prPart, out var prNum))
						return $"{owner}/{repo}#{prNum}".ToLowerInvariant();
				}
			}
			catch (UriFormatException)
			{
				// Invalid URI, fall through
			}
		}

		// Handle short format: owner/repo#123
		var hashIndex = pr.LastIndexOf('#');
		if (hashIndex > 0 && hashIndex < pr.Length - 1)
		{
			var repoPart = pr[..hashIndex].Trim();
			var prPart = pr[(hashIndex + 1)..].Trim();
			if (int.TryParse(prPart, out var prNum))
			{
				var repoParts = repoPart.Split('/');
				if (repoParts.Length == 2)
				{
					var owner = repoParts[0].Trim();
					var repo = repoParts[1].Trim();
					if (!string.IsNullOrWhiteSpace(owner) && !string.IsNullOrWhiteSpace(repo))
						return $"{owner}/{repo}#{prNum}".ToLowerInvariant();
				}
			}
		}

		// Handle just a PR number when owner/repo are provided
		if (int.TryParse(pr, out var prNumber) &&
			!string.IsNullOrWhiteSpace(defaultOwner) && !string.IsNullOrWhiteSpace(defaultRepo))
			return $"{defaultOwner}/{defaultRepo}#{prNumber}".ToLowerInvariant();

		// Return as-is for comparison (fallback)
		return pr.ToLowerInvariant();
	}

	internal static string NormalizeIssueForComparison(string issue, string? defaultOwner, string? defaultRepo)
	{
		issue = issue.Trim();

		if (issue.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase) ||
			issue.StartsWith("http://github.com/", StringComparison.OrdinalIgnoreCase))
		{
			var match = GitHubIssueUrlRegex().Match(issue);
			if (match.Success && match.Groups.Count >= 4)
			{
				var owner = match.Groups[1].Value.Trim();
				var repo = match.Groups[2].Value.Trim();
				var issuePart = match.Groups[3].Value.Trim();
				if (!string.IsNullOrWhiteSpace(owner) && !string.IsNullOrWhiteSpace(repo) &&
					int.TryParse(issuePart, out var issueNum))
					return $"{owner}/{repo}#{issueNum}".ToLowerInvariant();
			}

			try
			{
				var uri = new Uri(issue);
				var segments = uri.Segments;
				if (segments.Length >= 5 && segments[3].Equals("issues/", StringComparison.OrdinalIgnoreCase))
				{
					var owner = segments[1].TrimEnd('/').Trim();
					var repo = segments[2].TrimEnd('/').Trim();
					var issuePart = segments[4].TrimEnd('/').Trim();
					if (!string.IsNullOrWhiteSpace(owner) && !string.IsNullOrWhiteSpace(repo) &&
						int.TryParse(issuePart, out var issueNum))
						return $"{owner}/{repo}#{issueNum}".ToLowerInvariant();
				}
			}
			catch (UriFormatException)
			{
				// Fall through
			}
		}

		var hashIndex = issue.LastIndexOf('#');
		if (hashIndex > 0 && hashIndex < issue.Length - 1)
		{
			var repoPart = issue[..hashIndex].Trim();
			var issuePart = issue[(hashIndex + 1)..].Trim();
			if (int.TryParse(issuePart, out var issueNum))
			{
				var repoParts = repoPart.Split('/');
				if (repoParts.Length == 2)
				{
					var owner = repoParts[0].Trim();
					var repo = repoParts[1].Trim();
					if (!string.IsNullOrWhiteSpace(owner) && !string.IsNullOrWhiteSpace(repo))
						return $"{owner}/{repo}#{issueNum}".ToLowerInvariant();
				}
			}
		}

		if (int.TryParse(issue, out var issueNumber) &&
			!string.IsNullOrWhiteSpace(defaultOwner) && !string.IsNullOrWhiteSpace(defaultRepo))
			return $"{defaultOwner}/{defaultRepo}#{issueNumber}".ToLowerInvariant();

		return issue.ToLowerInvariant();
	}
}
