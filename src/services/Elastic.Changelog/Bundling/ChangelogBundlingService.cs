// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Elastic.Changelog.Configuration;
using Elastic.Changelog.GitHub;
using Elastic.Changelog.Rendering;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.ReleaseNotes;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.ScopedFileSystem;

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
	/// Optional third profile argument: a promotion report URL/path or URL list file to use as the
	/// PR/issue filter source when <see cref="ProfileArgument"/> is the version string.
	/// </summary>
	public string? ProfileReport { get; init; }

	/// <summary>
	/// Promotion report URL or file path for option-based bundling (<c>--report</c>).
	/// When set, the report is parsed and the extracted PR URLs become the effective PR filter.
	/// </summary>
	public string? Report { get; init; }

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

	/// <summary>
	/// Effective flag after merging CLI, profile, and config (see <see cref="SanitizePrivateLinksCli"/>).
	/// </summary>
	public bool SanitizePrivateLinks { get; init; }

	/// <summary>
	/// CLI override for option-based bundling only. null = use changelog.yml bundle default.
	/// </summary>
	public bool? SanitizePrivateLinksCli { get; init; }

	/// <summary>
	/// When true, forces sanitization off (overrides other sources).
	/// </summary>
	public bool NoSanitizePrivateLinks { get; init; }
}

/// <summary>
/// Structured plan output for CI actions. Describes what Docker flags and output path to expect
/// without actually executing the bundle.
/// </summary>
public record BundlePlanResult
{
	public bool NeedsNetwork { get; init; }
	public bool NeedsGithubToken { get; init; }
	public string? OutputPath { get; init; }
}

/// <summary>
/// Service for bundling changelog files
/// </summary>
public partial class ChangelogBundlingService(
	ILoggerFactory logFactory,
	IConfigurationContext? configurationContext = null,
	ScopedFileSystem? fileSystem = null,
	IGitHubReleaseService? releaseService = null)
	: IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogBundlingService>();
	private readonly ScopedFileSystem _fileSystem = fileSystem ?? FileSystemFactory.RealRead;
	private readonly IGitHubReleaseService _releaseService = releaseService ?? new GitHubReleaseService(logFactory);
	private readonly ChangelogConfigurationLoader? _configLoader = configurationContext != null
		? new ChangelogConfigurationLoader(logFactory, configurationContext, fileSystem ?? FileSystemFactory.RealRead)
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
			// Load changelog configuration
			ChangelogConfiguration? config = null;
			if (!string.IsNullOrWhiteSpace(input.Profile))
			{
				// Profile mode requires the config file to exist — no fallback to defaults.
				if (_configLoader == null)
				{
					collector.EmitError(string.Empty, "Changelog configuration loader is required for profile-based bundling.");
					return false;
				}
				// When an explicit config path is provided, load it (required, no fallback).
				// Otherwise, discover from CWD: ./changelog.yml then ./docs/changelog.yml.
				config = string.IsNullOrWhiteSpace(input.Config)
					? await _configLoader.LoadChangelogConfigurationForProfileMode(collector, ctx)
					: await _configLoader.LoadChangelogConfigurationRequired(collector, input.Config, ctx);
				if (config == null)
					return false;
			}
			else if (_configLoader != null)
				config = await _configLoader.LoadChangelogConfiguration(collector, input.Config, ctx);

			// Handle profile-based bundling
			if (!string.IsNullOrWhiteSpace(input.Profile))
			{
				var profileResult = await ProcessProfile(collector, input, config, ctx);
				if (profileResult == null)
					return false;
				input = profileResult;
			}
			else if (!string.IsNullOrWhiteSpace(input.Report))
			{
				// Option-based mode with --report: parse report and populate Prs
				var parser = new PromotionReportParser(logFactory, _fileSystem);
				var prs = await parser.ParseReportToPrUrlsAsync(collector, input.Report, ctx);
				if (prs == null)
					return false;
				input = input with { Prs = prs };
			}

			// Apply config defaults if available
			input = ApplyConfigDefaults(input, config);

			// Validate input
			if (!ValidateInput(collector, input))
				return false;

			if (!ValidateSanitizePrivateLinks(collector, input))
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
			var outputPath = input.Output ?? _fileSystem.Path.Join(directory, "changelog-bundle.yaml");

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

			// Refuse to write a bundle when any individual entry failed to parse; the result would be
			// silently incomplete and could ship a broken release bundle.
			if (collector.Errors > 0)
				return false;

			if (matchResult.Entries.Count == 0)
			{
				collector.EmitError(string.Empty, "No changelog entries matched the filter criteria");
				return false;
			}

			// Apply rules.bundle secondary filter (three modes: none, global content, per-product context).
			// Input stage (--input-products, --prs, etc.) and bundle filtering stage are conceptually separate.
			var filteredEntries = matchResult.Entries;
			if (config?.Rules?.Bundle != null)
			{
				var outputProductIds = input.OutputProducts
					?.Select(p => p.Product)
					.Where(p => !string.IsNullOrWhiteSpace(p))
					.Select(p => p!)
					.ToList();
				var mode = config.Rules.Bundle.DetermineFilterMode();
				filteredEntries = mode switch
				{
					BundleFilterMode.NoFiltering => filteredEntries,
					BundleFilterMode.GlobalContent => ApplyGlobalContentBundleFilter(collector, filteredEntries, config.Rules.Bundle),
					BundleFilterMode.PerProductContext => ApplyPerProductContextBundleFilter(
						collector,
						filteredEntries,
						config.Rules.Bundle,
						outputProductIds),
					_ => filteredEntries
				};
			}

			if (filteredEntries.Count == 0)
			{
				collector.EmitError(string.Empty, "No changelog entries remained after applying rules.bundle filter");
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
				filteredEntries,
				input.OutputProducts,
				input.Resolve ?? false,
				input.Repo,
				input.Owner,
				featureHidingResult.FeatureIdsToHide
			);

			if (!buildResult.IsValid || buildResult.Data == null)
				return false;

			var bundleData = buildResult.Data;
			if (input.SanitizePrivateLinks)
			{
				ArgumentNullException.ThrowIfNull(configurationContext);
				var assemblyYaml = configurationContext.ConfigurationFileProvider.AssemblerFile.ReadToEnd();
				var assembly = AssemblyConfiguration.Deserialize(assemblyYaml, skipPrivateRepositories: false);
				if (!PrivateChangelogLinkSanitizer.TrySanitizeBundle(
					collector,
					bundleData,
					assembly,
					input.Owner ?? "elastic",
					input.Repo,
					out var sanitizedBundle,
					out _))
					return false;
				bundleData = sanitizedBundle;
			}

			// Write bundle file
			await WriteBundleFileAsync(bundleData, outputPath, ctx);

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
		var filterResult = await ProfileFilterResolver.ResolveAsync(
			collector,
			input.Profile!,
			input.ProfileArgument,
			config,
			_fileSystem,
			_logger,
			ctx,
			input.ProfileReport,
			_releaseService
		);

		if (filterResult == null)
			return null;

		// Resolve bundle-specific output path, output products, repo, owner, and hide-features from profile
		string? outputPath = null;
		IReadOnlyList<ProductArgument>? outputProducts = null;
		string? repo = null;
		string? owner = null;
		string[]? mergedHideFeatures = null;
		var sanitizePrivateLinks = false;

		if (config?.Bundle?.Profiles != null && config.Bundle.Profiles.TryGetValue(input.Profile!, out var profile))
		{
			// For github_release profiles, lifecycle is carried from the raw tag (pre-release suffix preserved).
			// For all other profile types, infer it from the base version string.
			var resolvedLifecycle = filterResult.Lifecycle ?? VersionLifecycleInference.InferLifecycle(filterResult.Version);

			var outputPattern = profile.Output?
				.Replace("{version}", filterResult.Version)
				.Replace("{lifecycle}", resolvedLifecycle);
			if (!string.IsNullOrWhiteSpace(outputPattern))
			{
				// Resolution order: bundle.output_directory → input.OutputDirectory (programmatic override)
				// → bundle.directory → CWD
				var outputDir = config.Bundle.OutputDirectory
					?? input.OutputDirectory
					?? config.Bundle.Directory
					?? _fileSystem.Directory.GetCurrentDirectory();
				outputPath = _fileSystem.Path.Join(outputDir, outputPattern);
			}

			// Parse output_products pattern with version/lifecycle substitution
			if (!string.IsNullOrWhiteSpace(profile.OutputProducts))
			{
				var outputProductsPattern = profile.OutputProducts
					.Replace("{version}", filterResult.Version)
					.Replace("{lifecycle}", resolvedLifecycle);
				if (!ProfileFilterResolver.TryParseProfileProducts(outputProductsPattern, out var parsedOutputProducts, out var outputProductsParseError))
				{
					collector.EmitError(string.Empty,
						$"Profile '{input.Profile}': bundle.output_products could not be parsed: {outputProductsParseError}");
					return null;
				}

				outputProducts = parsedOutputProducts;
			}

			// Profile-level repo/owner takes precedence; fall back to bundle-level defaults
			repo = profile.Repo ?? config.Bundle.Repo;
			owner = profile.Owner ?? config.Bundle.Owner;
			mergedHideFeatures = profile.HideFeatures?.Count > 0 ? [.. profile.HideFeatures] : null;
			sanitizePrivateLinks = profile.SanitizePrivateLinks ?? config.Bundle.SanitizePrivateLinks;
		}

		return input with
		{
			InputProducts = filterResult.Products,
			Prs = filterResult.Prs,
			Issues = filterResult.Issues,
			All = false,
			Output = outputPath,
			OutputProducts = outputProducts,
			Repo = repo,
			Owner = owner,
			HideFeatures = mergedHideFeatures,
			SanitizePrivateLinks = sanitizePrivateLinks
		};
	}

	private static BundleChangelogsArguments ApplyConfigDefaults(BundleChangelogsArguments input, ChangelogConfiguration? config)
	{
		// Apply directory: CLI takes precedence. Only use config when --directory not specified.
		var directory = input.Directory ?? config?.Bundle?.Directory ?? Directory.GetCurrentDirectory();

		if (config?.Bundle == null)
		{
			var sanitizeNoConfig = !input.NoSanitizePrivateLinks &&
				(string.IsNullOrWhiteSpace(input.Profile)
					? input.SanitizePrivateLinksCli ?? false
					: input.SanitizePrivateLinks);
			return input with { Directory = directory, SanitizePrivateLinks = sanitizeNoConfig };
		}

		// Apply output default when --output not specified: use bundle.output_directory if set
		var output = input.Output;
		if (string.IsNullOrWhiteSpace(output) && !string.IsNullOrWhiteSpace(config.Bundle.OutputDirectory))
			output = Path.Join(config.Bundle.OutputDirectory, "changelog-bundle.yaml");

		// Apply resolve: CLI takes precedence over config. Only use config when CLI did not specify.
		var resolve = input.Resolve ?? config.Bundle.Resolve;

		// Apply repo/owner: CLI takes precedence; fall back to bundle-level config defaults.
		var repo = input.Repo ?? config.Bundle.Repo;
		var owner = input.Owner ?? config.Bundle.Owner;

		// Profile mode forbids --sanitize-private-links on the CLI; SanitizePrivateLinksCli is only set for option-based bundle.
		var sanitizePrivateLinks = !input.NoSanitizePrivateLinks &&
			(!string.IsNullOrWhiteSpace(input.Profile)
				? input.SanitizePrivateLinks
				: input.SanitizePrivateLinksCli ?? config.Bundle.SanitizePrivateLinks);

		return input with
		{
			Directory = directory,
			Output = output,
			Resolve = resolve,
			Repo = repo,
			Owner = owner,
			SanitizePrivateLinks = sanitizePrivateLinks
		};
	}

	/// <summary>
	/// Resolves a bundle plan from config and profile metadata without executing any network calls or
	/// file-scanning. Used by <c>--plan</c> mode to emit structured JSON that CI actions consume.
	/// </summary>
	public async Task<BundlePlanResult?> PlanBundleAsync(
		IDiagnosticsCollector collector,
		BundleChangelogsArguments input,
		bool hasReleaseVersion,
		Cancel ctx)
	{
		var needsNetwork = hasReleaseVersion;
		var needsGithubToken = hasReleaseVersion;

		ChangelogConfiguration? config = null;
		if (!string.IsNullOrWhiteSpace(input.Profile))
		{
			if (_configLoader == null)
			{
				collector.EmitError(string.Empty, "Changelog configuration loader is required for profile-based bundling.");
				return null;
			}
			config = string.IsNullOrWhiteSpace(input.Config)
				? await _configLoader.LoadChangelogConfigurationForProfileMode(collector, ctx)
				: await _configLoader.LoadChangelogConfigurationRequired(collector, input.Config, ctx);
			if (config == null)
				return null;
		}
		else if (_configLoader != null)
			config = await _configLoader.LoadChangelogConfiguration(collector, input.Config, ctx);

		BundleProfile? profileDef = null;
		if (!string.IsNullOrWhiteSpace(input.Profile) &&
			config?.Bundle?.Profiles?.TryGetValue(input.Profile, out profileDef) == true)
		{
			if (string.Equals(profileDef.Source, "github_release", StringComparison.OrdinalIgnoreCase))
			{
				needsNetwork = true;
				needsGithubToken = true;
			}
		}

		// Resolve output path — mirrors the logic in ProcessProfile + ApplyConfigDefaults.
		// Uses UrlPath.Join so the result always has forward slashes (CI runners expect this).
		var outputPath = input.Output;
		if (string.IsNullOrWhiteSpace(outputPath) && profileDef?.Output != null)
		{
			var version = input.ProfileArgument ?? "unknown";
			var lifecycle = VersionLifecycleInference.InferLifecycle(version);
			var outputPattern = profileDef.Output
				.Replace("{version}", version)
				.Replace("{lifecycle}", lifecycle);
			var outputDir = config?.Bundle?.OutputDirectory
				?? config?.Bundle?.Directory
				?? _fileSystem.Directory.GetCurrentDirectory();
			outputPath = UrlPath.Join(outputDir, outputPattern);
		}
		else if (string.IsNullOrWhiteSpace(outputPath) && config?.Bundle?.OutputDirectory != null)
			outputPath = UrlPath.Join(config.Bundle.OutputDirectory, "changelog-bundle.yaml");

		return new BundlePlanResult
		{
			NeedsNetwork = needsNetwork,
			NeedsGithubToken = needsGithubToken,
			OutputPath = outputPath
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

	private bool ValidateSanitizePrivateLinks(IDiagnosticsCollector collector, BundleChangelogsArguments input)
	{
		if (!input.SanitizePrivateLinks)
			return true;

		if (!(input.Resolve ?? false))
		{
			collector.EmitError(
				string.Empty,
				"Private link sanitization requires resolved bundle content. " +
				"Use --resolve or set bundle.resolve: true in changelog.yml, or disable sanitization " +
				"(bundle.sanitize_private_links / --sanitize-private-links)."
			);
			return false;
		}

		if (configurationContext == null)
		{
			collector.EmitError(
				string.Empty,
				"Private link sanitization requires assembler configuration (assembler.yml). " +
				"Ensure docs-builder runs with a valid configuration source."
			);
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
			outputPath = _fileSystem.Path.Join(directory, uniqueFileName);
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
			if (match is { Success: true, Groups.Count: >= 4 })
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
			if (match is { Success: true, Groups.Count: >= 4 })
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

	private static IReadOnlyList<MatchedChangelogFile> ApplyGlobalContentBundleFilter(
		IDiagnosticsCollector collector,
		IReadOnlyList<MatchedChangelogFile> entries,
		BundleRules bundleRules)
	{
		var filtered = new List<MatchedChangelogFile>();
		var warnedMissingProducts = false;

		foreach (var entry in entries)
		{
			var entryProducts = entry.Data.Products?.Select(p => p.ProductId).ToList() ?? [];

			if (entryProducts.Count == 0)
			{
				if (!warnedMissingProducts)
				{
					collector.EmitWarning(entry.FilePath,
						"[-bundle-global] Changelog has no products declared; product filters are skipped for this entry. See documentation for rules.bundle global mode.");
					warnedMissingProducts = true;
				}
				else
					collector.EmitWarning(entry.FilePath, "[-bundle-global] Changelog has no products declared; product filters are skipped for this entry.");

				if (bundleRules.Blocker != null && bundleRules.Blocker.ShouldBlock(entry.Data))
				{
					collector.EmitWarning(entry.FilePath, $"[-bundle-type-area] Excluding '{entry.FileName}' from bundle (global type/area filter).");
					continue;
				}

				filtered.Add(entry);
				continue;
			}

			if (ShouldExcludeByProductFilter(entryProducts, bundleRules, out var productReason))
			{
				collector.EmitWarning(entry.FilePath, $"[-bundle-{productReason}] Excluding '{entry.FileName}' from bundle (global product filter).");
				continue;
			}

			if (bundleRules.Blocker != null && bundleRules.Blocker.ShouldBlock(entry.Data))
			{
				collector.EmitWarning(entry.FilePath, $"[-bundle-type-area] Excluding '{entry.FileName}' from bundle (global type/area filter).");
				continue;
			}

			filtered.Add(entry);
		}

		return filtered;
	}

	private static IReadOnlyList<MatchedChangelogFile> ApplyPerProductContextBundleFilter(
		IDiagnosticsCollector collector,
		IReadOnlyList<MatchedChangelogFile> entries,
		BundleRules bundleRules,
		IReadOnlyList<string>? outputProductIds = null)
	{
		// Early validation: validate bundle has some product context
		if ((outputProductIds == null || outputProductIds.Count == 0) &&
			!entries.Any(e => e.Data.Products?.Any() == true))
		{
			collector.EmitError(string.Empty,
				"Bundle has no product context - specify output_products or ensure changelogs declare products");
			return [];
		}

		// BUNDLE-LEVEL: Determine rule context product once for entire bundle
		// Always use alphabetical first for consistency, regardless of source
		var ruleContextProduct = outputProductIds?.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).FirstOrDefault()
			?? entries
				.SelectMany(e => e.Data.Products?.Select(p => p.ProductId) ?? [])
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
				.FirstOrDefault();

		var filtered = new List<MatchedChangelogFile>();
		var ruleStats = new Dictionary<string, int>(); // For bundle summary

		foreach (var entry in entries)
		{
			var entryProducts = entry.Data.Products?.Select(p => p.ProductId).ToList() ?? [];

			// Single resolver call handles all cases explicitly
			var resolveResult = ResolvePerProductBundleRule(entryProducts, bundleRules, ruleContextProduct);

			switch (resolveResult.Result)
			{
				case ResolveResult.ExcludeMissingProducts:
					collector.EmitWarning(entry.FilePath, $"[-bundle-missing-products] Excluding '{entry.FileName}' from bundle (no products declared).");
					ruleStats["excluded_no_products"] = ruleStats.GetValueOrDefault("excluded_no_products") + 1;
					continue;

				case ResolveResult.ExcludeDisjoint:
					collector.EmitHint(entry.FilePath, $"[-bundle-disjoint] Excluding '{entry.FileName}' from bundle (disjoint from rule context '{ruleContextProduct}').");
					ruleStats["excluded_disjoint"] = ruleStats.GetValueOrDefault("excluded_disjoint") + 1;
					continue;

				case ResolveResult.UsePerProduct when resolveResult.Rule != null:
					// Apply per-product rule
					ruleStats[ruleContextProduct ?? "unknown"] = ruleStats.GetValueOrDefault(ruleContextProduct ?? "unknown") + 1;

					// Emit hint about ineffective pattern usage (once per bundle, not per entry)
					if (resolveResult.Rule.MatchProducts == MatchMode.Any &&
						resolveResult.Rule.IncludeProducts?.Count > 0 &&
						!ruleStats.ContainsKey("ineffective_pattern_warned"))
					{
						var wouldIncludeAll = resolveResult.Rule.IncludeProducts.Contains(ruleContextProduct ?? "", StringComparer.OrdinalIgnoreCase);
						collector.EmitHint(string.Empty,
							$"Note: Per-product rule '{ruleContextProduct}' uses 'match_products: any' with 'include_products' which acts as " +
							$"{(wouldIncludeAll ? "include-all" : "exclude-all")} for this context. " +
							$"See: https://elastic.github.io/docs-builder/contribute/changelog/#ineffective-configuration-patterns");
						ruleStats["ineffective_pattern_warned"] = 1;
					}

					// 1 — Product filter: use per-product rule
					if (ShouldExcludeByResolvedProductRule(entryProducts, resolveResult.Rule, out var productReason))
					{
						collector.EmitWarning(entry.FilePath, $"[-bundle-{productReason}] Excluding '{entry.FileName}' from bundle (per-product filter).");
						continue;
					}

					// 2 — Type/area filter: use per-product blocker
					if (resolveResult.Rule.Blocker != null && resolveResult.Rule.Blocker.ShouldBlock(entry.Data))
					{
						collector.EmitWarning(entry.FilePath, $"[-bundle-type-area] Excluding '{entry.FileName}' from bundle (per-product type/area filter).");
						continue;
					}
					break;

				case ResolveResult.PassThrough:
					ruleStats["pass_through"] = ruleStats.GetValueOrDefault("pass_through") + 1;
					break;
			}

			filtered.Add(entry);
		}

		// Bundle-level summary with guidance message
		if (ruleStats.Count > 0)
		{
			var message = $"Applied rules - {string.Join(", ", ruleStats.Select(kvp => $"{kvp.Key}: {kvp.Value} entries"))}";
			if (ruleStats.Count > 2) // More than one rule type being used
			{
				message += ". Review rules.bundle configuration and documentation if this distribution seems unexpected.";
			}
			collector.EmitHint(string.Empty, message);
		}

		return filtered;
	}

	// match_products semantics (mirrors MatchesArea in PublishBlockerExtensions):
	//   any         — matched if ANY entry product is in the list
	//   all         — matched if ALL entry products are in the list (subset)
	//   conjunction — matched if EVERY configured product appears on the entry
	private static bool EntryMatchesProductList(
		IReadOnlyList<string> entryProducts,
		IReadOnlyList<string> list,
		MatchMode matchProducts) =>
		matchProducts switch
		{
			MatchMode.All => entryProducts.All(p => list.Contains(p, StringComparer.OrdinalIgnoreCase)),
			MatchMode.Conjunction => list.All(id => entryProducts.Contains(id, StringComparer.OrdinalIgnoreCase)),
			_ => entryProducts.Any(p => list.Contains(p, StringComparer.OrdinalIgnoreCase))
		};

	private static bool ShouldExcludeByProductFilter(IReadOnlyList<string> entryProducts, BundleRules bundleRules, out string reason)
	{
		if (bundleRules.ExcludeProducts is { Count: > 0 } excludeList)
		{
			var matches = EntryMatchesProductList(entryProducts, excludeList, bundleRules.MatchProducts);
			reason = "exclude";
			return matches;
		}

		if (bundleRules.IncludeProducts is { Count: > 0 } includeList)
		{
			var matchesSome = EntryMatchesProductList(entryProducts, includeList, bundleRules.MatchProducts);
			reason = "include";
			return !matchesSome;
		}

		reason = string.Empty;
		return false;
	}

	private static bool ShouldExcludeByResolvedProductRule(IReadOnlyList<string> entryProducts, BundlePerProductRule rule, out string reason)
	{
		if (rule.ExcludeProducts is { Count: > 0 } excludeList)
		{
			var matches = EntryMatchesProductList(entryProducts, excludeList, rule.MatchProducts);
			reason = "context-exclude";
			return matches;
		}

		if (rule.IncludeProducts is { Count: > 0 } includeList)
		{
			var matchesSome = EntryMatchesProductList(entryProducts, includeList, rule.MatchProducts);
			reason = "context-include";
			return !matchesSome;
		}

		reason = string.Empty;
		return false;
	}



	private static ResolveResultWithRule ResolvePerProductBundleRule(
		IReadOnlyList<string> entryProducts,
		BundleRules bundleRules,
		string? ruleContextProduct)
	{
		if (bundleRules.ByProduct is not { Count: > 0 } byProduct)
			return ResolveResultWithRule.PassThrough();

		// Edge case: changelog has no products → exclude with warning
		if (entryProducts.Count == 0)
			return ResolveResultWithRule.ExcludeMissingProducts();

		// Edge case: no rule context available → include without per-product rules (global not applied in this mode)
		if (string.IsNullOrEmpty(ruleContextProduct))
			return ResolveResultWithRule.PassThrough();

		// Disjoint check: exclude if changelog doesn't contain rule context product
		if (!entryProducts.Contains(ruleContextProduct, StringComparer.OrdinalIgnoreCase))
			return ResolveResultWithRule.ExcludeDisjoint();

		// Direct rule lookup — no per-product block for context product: pass through (global rules.bundle ignored in this mode)
		return byProduct.TryGetValue(ruleContextProduct, out var rule)
			? ResolveResultWithRule.UsePerProduct(rule)
			: ResolveResultWithRule.PassThrough();
	}
}
