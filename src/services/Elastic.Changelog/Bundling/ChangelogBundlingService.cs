// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Elastic.Changelog.GitHub;
using Elastic.Changelog.Rendering;
using Elastic.Changelog.Utilities;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.ReleaseNotes;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

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
	public string[]? Prs { get; init; }
	public string[]? Issues { get; init; }

	/// <summary>
	/// Explicit changelog YAML paths (or a path-list file) for the <c>--files</c> filter.
	/// Mutually exclusive with other filter sources. Follows the standard entry-sourcing gate:
	/// when entries are sourced from the CDN the paths are matched to pool entries by file name,
	/// otherwise they must exist on the local filesystem.
	/// </summary>
	public string[]? Files { get; init; }

	/// <summary>
	/// When true, force local entry sourcing for this run (CLI <c>--force-local</c>),
	/// equivalent to <c>bundle.use_local_changelogs: true</c> without editing config.
	/// </summary>
	public bool ForceLocal { get; init; }

	public string? Owner { get; init; }
	public string? Repo { get; init; }

	/// <summary>
	/// Branch whose CDN changelog pool (<c>changelog/{org}/{repo}/{branch}/…</c>) entries are sourced from.
	/// null = use config <c>bundle.branch</c>, then the default branch (<c>main</c>).
	/// </summary>
	public string? Branch { get; init; }

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
	/// Optional bundle description with placeholder substitution.
	/// Supports {version}, {lifecycle}, {owner}, and {repo} placeholders.
	/// </summary>
	public string? Description { get; init; }

	/// <summary>
	/// Optional explicit release date for the bundle in YYYY-MM-DD format.
	/// When provided, overrides auto-population behavior.
	/// </summary>
	public string? ReleaseDate { get; init; }

	/// <summary>
	/// When true, skips auto-population of release date (respects --no-release-date).
	/// Existing dates in bundle YAML files are still preserved.
	/// </summary>
	public bool SuppressReleaseDate { get; init; }

	/// <summary>
	/// When non-null (including empty), PR/issue links are filtered to this <c>owner/repo</c> allowlist (from changelog.yml <c>bundle.link_allow_repos</c>).
	/// </summary>
	public IReadOnlyList<string>? LinkAllowRepos { get; init; }

	/// <summary>
	/// Start ref (exclusive) of a git commit range to bundle (<c>--start-git-ref</c>).
	/// Must be provided together with <see cref="EndGitRef"/>.
	/// </summary>
	public string? StartGitRef { get; init; }

	/// <summary>
	/// End ref (inclusive) of a git commit range to bundle (<c>--end-git-ref</c>); stored as the
	/// bundle's <c>git_ref</c> metadata. Must be provided together with <see cref="StartGitRef"/>.
	/// </summary>
	public string? EndGitRef { get; init; }

	/// <summary>
	/// When true, resolve the commit range and print the run report (resolved PR list with per-PR
	/// entry source) without writing a bundle. Only valid together with a git ref range.
	/// </summary>
	public bool DryRun { get; init; }
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

	/// <summary>
	/// Public CDN URL of the (scrubbed) bundle once uploaded: <c>{base}/bundle/{product}/{file}</c>.
	/// Null when no concrete product can be resolved to scope the URL (e.g. option-mode PR/issue-only
	/// filters). Consumed by the bundle-PR action to poll for and download the scrubbed copy.
	/// </summary>
	public string? CdnUrl { get; init; }
}

/// <summary>
/// Service for bundling changelog files
/// </summary>
public partial class ChangelogBundlingService(
	ILoggerFactory logFactory,
	IChangelogFileSystem fileSystem,
	IConfigurationContext? configurationContext = null,
	IGitHubReleaseService? releaseService = null,
	CdnChangelogEntryFetcher? entryFetcher = null,
	IGitHubPrService? prService = null,
	IGitHubCommitRangeService? commitRangeService = null)
	: IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogBundlingService>();
	private readonly IChangelogFileSystem _fileSystem = fileSystem;
	private readonly IGitHubReleaseService _releaseService = releaseService ?? new GitHubReleaseService(logFactory);
	private readonly CdnChangelogEntryFetcher _entryFetcher = entryFetcher ?? new CdnChangelogEntryFetcher(logFactory);
	private readonly IGitHubPrService _prService = prService ?? new GitHubPrService(logFactory);
	private readonly IGitHubCommitRangeService _commitRangeService = commitRangeService ?? new GitHubCommitRangeService(logFactory);
	private readonly ChangelogConfigurationLoader? _configLoader = configurationContext != null
		? new ChangelogConfigurationLoader(logFactory, configurationContext, fileSystem)
		: null;

	// Defaults applied when sourcing CDN entries and the org/branch are not otherwise resolvable.
	private const string DefaultOwner = "elastic";
	private const string DefaultBranch = "main";

	/// <summary>
	/// UTF-8 encoding without BOM for writing YAML files.
	/// </summary>
	private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

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
			if (!ValidateGitRefArguments(collector, input))
				return false;

			// Capture whether the caller explicitly pointed at a local folder before config defaults
			// fill it in. An explicit --directory always forces local sourcing.
			var explicitDirectory = !string.IsNullOrWhiteSpace(input.Directory);

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

			// Decide where the individual changelog entries come from. Under Option AD entries are scoped to
			// an org/repo/branch pool (changelog/{org}/{repo}/{branch}/...), so CDN sourcing keys off the
			// resolvable authoring repo (bundle.repo / --repo), with org and branch defaulting when unset —
			// not the bundle's target products. Fall back to the local folder when the user forces it
			// (bundle.use_local_changelogs / --force-local / --directory), the repo is unresolvable,
			// or no CDN base is configured. This stays in lockstep with PlanBundleAsync's needs_network decision.
			// The --files / path-list filter follows the same gate: in CDN mode the requested paths are
			// matched to pool entries by file name, so private repos whose entries exist only in S3 (with
			// PR/issue references scrubbed from the public copies) can still bundle by explicit selection.
			var useLocalChangelogs = (config?.Bundle?.UseLocalChangelogs ?? false)
				|| input.ForceLocal;
			var authoringRepo = ChangelogRepoOwnerResolver.NormalizeRepo(input.Repo);
			var authoringOwner = ChangelogRepoOwnerResolver.ResolveOwner(input.Owner, input.Repo, DefaultOwner);
			var authoringBranch = string.IsNullOrWhiteSpace(input.Branch) ? DefaultBranch : input.Branch;
			var useCdn = ShouldSourceFromCdn(authoringRepo, useLocalChangelogs: useLocalChangelogs, explicitDirectory: explicitDirectory);

			// Commit-range mode replaces the filter pipeline: the PR list is derived from git and
			// each PR's entry is sourced pool-first with PR-metadata fallback.
			if (!string.IsNullOrWhiteSpace(input.StartGitRef))
			{
				var sourcing = new GitRangeSourcingContext
				{
					UseCdn = useCdn,
					Owner = authoringOwner,
					Repo = authoringRepo,
					Branch = authoringBranch
				};
				return await BundleFromGitRange(collector, input, config, sourcing, ctx);
			}

			// Validate input. In CDN mode the local input directory is not read, so its existence
			// is not required.
			if (!ValidateInput(collector, input, requireDirectoryExists: !useCdn))
				return false;

			// --all, --input-products, and --issues require reading every entry body; that is only possible locally.
			// On the CDN path entries are probed by key (one per PR), so there is nothing to enumerate without a PR list.
			if (useCdn && (input.All || input.InputProducts is { Count: > 0 } || input.Issues is { Length: > 0 }))
			{
				var flag = input.All ? "--all" : input.Issues is { Length: > 0 } ? "--issues" : "--input-products";
				collector.EmitError(string.Empty,
					$"{flag} is not supported when sourcing changelog entries from the CDN, because entries are fetched by key (one per PR) and there is no pool enumeration. " +
					"Pass --force-local or --directory to bundle from a local checkout instead.");
				return false;
			}

			if (!ValidatePlaceholderUsage(collector, input))
				return false;

			// Load PR, issue, or file filter values
			var prsToMatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var issuesToMatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			IReadOnlyList<string>? explicitFilePaths = null;
			IReadOnlyList<string>? requestedEntryNames = null;

			if (input.Files is { Length: > 0 })
			{
				var fileFilterLoader = new FileFilterLoader(_fileSystem);
				if (useCdn)
				{
					// CDN mode: reduce the requested paths to entry file names (the pool is flat); the
					// entries do not need to exist locally.
					var namesResult = await fileFilterLoader.LoadFileNamesAsync(collector, input.Files, ctx);
					if (!namesResult.IsValid)
						return false;
					requestedEntryNames = namesResult.FilePaths;
				}
				else
				{
					var fileFilterResult = await fileFilterLoader.LoadFilesAsync(collector, input.Files, input.Directory, ctx);
					if (!fileFilterResult.IsValid)
						return false;
					explicitFilePaths = fileFilterResult.FilePaths;
				}
			}
			else if (input.Prs is { Length: > 0 })
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

			// Build filter criteria
			var filterCriteria = BuildFilterCriteria(input, prsToMatch, issuesToMatch);

			// Source and match changelog entries — from the CDN (default) or the local folder.
			// Explicit --files / path-list selection bypasses content filters (IncludeAll): locally it loads
			// the named paths, in CDN mode it selects pool entries by file name.
			var entryMatcher = new ChangelogEntryMatcher(_fileSystem, ReleaseNotesSerialization.GetEntryDeserializer(), _logger);
			ChangelogMatchResult matchResult;
			if (explicitFilePaths != null)
			{
				_logger.LogInformation("Matching {Count} explicitly selected changelog files", explicitFilePaths.Count);
				var filesCriteria = filterCriteria with { IncludeAll = true };
				matchResult = await entryMatcher.MatchChangelogsAsync(collector, explicitFilePaths, filesCriteria, ctx);
			}
			else if (useCdn)
			{
				if (requestedEntryNames is not null)
				{
					// --files on the CDN path: fetch each entry directly by key; no registry needed.
					var selected = await FetchCdnNamedEntriesAsync(collector, authoringOwner, authoringRepo, authoringBranch, requestedEntryNames, ctx);
					if (selected == null)
						return false;
					_logger.LogInformation("Matching {Count} explicitly selected changelog entries from the CDN", selected.Count);
					var filesCriteria = filterCriteria with { IncludeAll = true };
					matchResult = entryMatcher.MatchChangelogContents(collector, selected, filesCriteria, ctx);
				}
				else
				{
					// --prs / --report / --release-version on the CDN path: probe one key per PR number.
					// Each entry lives at changelog/{org}/{repo}/{branch}/{pr}.yaml; 404 = no entry for that PR.
					var probed = await FetchCdnProbedEntriesAsync(collector, authoringOwner, authoringRepo, authoringBranch, prsToMatch, ctx);
					if (probed == null)
						return false;
					_logger.LogInformation("Probed {Count} changelog entry(ies) for {Pool} from CDN",
						probed.Count, $"{authoringOwner}/{authoringRepo}/{authoringBranch}");
					// Entries are already selected by probe; pass IncludeAll so the matcher skips content re-filtering.
					var probeCriteria = filterCriteria with { IncludeAll = true };
					matchResult = entryMatcher.MatchChangelogContents(collector, probed, probeCriteria, ctx);
				}
			}
			else
			{
				// Discover changelog files
				var fileDiscovery = new ChangelogFileDiscovery(_fileSystem, _logger);
				var yamlFiles = await fileDiscovery.DiscoverChangelogFilesAsync(directory, outputPath, ctx);

				if (yamlFiles.Count == 0)
				{
					collector.EmitError(directory, "No YAML files found in directory");
					return false;
				}

				_logger.LogInformation("Found {Count} YAML files in directory", yamlFiles.Count);
				matchResult = await entryMatcher.MatchChangelogsAsync(collector, yamlFiles, filterCriteria, ctx);
			}

			_logger.LogInformation("Found {Count} matching changelog entries", matchResult.Entries.Count);

			// Refuse to write a bundle when any individual entry failed to parse; the result would be
			// silently incomplete and could ship a broken release bundle.
			if (collector.Errors > 0)
				return false;

			// Merge notes for the target when sourcing from CDN and an explicit target is available.
			// Notes are target-scoped by their index, so they are never filtered by PR/issue criteria.
			var allEntries = await MergeNotesAsync(collector, matchResult.Entries, useCdn, authoringOwner, authoringRepo, input, ctx);
			if (allEntries == null)
				return false;

			if (allEntries.Count == 0)
			{
				collector.EmitError(string.Empty, "No changelog entries matched the filter criteria");
				return false;
			}

			return await BuildAndWriteBundle(collector, input, config, allEntries, outputPath, ctx);
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

	/// <summary>
	/// Shared bundle tail: applies the <c>rules.bundle</c> secondary filter, builds the bundle,
	/// applies link allowlist/description/release-date/git-ref metadata, and writes the output file.
	/// </summary>
	private async Task<bool> BuildAndWriteBundle(
		IDiagnosticsCollector collector,
		BundleChangelogsArguments input,
		ChangelogConfiguration? config,
		IReadOnlyList<MatchedChangelogFile> entries,
		string outputPath,
		Cancel ctx)
	{
		// Apply rules.bundle secondary filter (three modes: none, global content, per-product context).
		// Input stage (--input-products, --prs, etc.) and bundle filtering stage are conceptually separate.
		var filteredEntries = entries;
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
			input.Repo,
			input.Owner,
			featureHidingResult.FeatureIdsToHide
		);

		if (!buildResult.IsValid || buildResult.Data == null)
			return false;

		var bundleData = buildResult.Data;
		if (input.LinkAllowRepos != null)
		{
			if (!LinkAllowlistSanitizer.TryApplyBundle(
				collector,
				bundleData,
				input.LinkAllowRepos,
				input.Owner ?? "elastic",
				input.Repo,
				out var sanitizedBundle,
				out _))
				return false;
			bundleData = sanitizedBundle;

			if (configurationContext != null && input.LinkAllowRepos.Count > 0)
			{
				try
				{
					var assemblyYaml = configurationContext.ConfigurationFileProvider.AssemblerFile.ReadToEnd();
					var assembly = AssemblyConfiguration.Deserialize(assemblyYaml, skipPrivateRepositories: false);
					LinkAllowlistSanitizer.EmitAssemblerDiagnostics(collector, input.LinkAllowRepos, assembly);
				}
				catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException))
				{
					collector.EmitWarning(
						string.Empty,
						$"Could not load assembler.yml for bundle.link_allow_repos diagnostics: {ex.Message}");
				}
			}
		}

		// Apply description with placeholder substitution
		if (!string.IsNullOrEmpty(input.Description))
		{
			var version = (input.OutputProducts?.Count > 0 ? input.OutputProducts[0].Target : null)
						  ?? (bundleData.Products.Count > 0 ? bundleData.Products[0].Target : null);
			var lifecycle = (input.OutputProducts?.Count > 0 ? input.OutputProducts[0].Lifecycle : null)
							?? (bundleData.Products.Count > 0 ? bundleData.Products[0].Lifecycle?.ToStringFast(true) : null);
			var owner = input.Owner ?? "elastic";
			var repo = input.Repo ?? (bundleData.Products.Count > 0 ? bundleData.Products[0].ProductId : null) ?? "unknown";

			try
			{
				var substitutedDescription = BundleDescriptionSubstitution.SubstitutePlaceholders(
					input.Description, version, lifecycle, owner, repo, validateResolvable: true);
				bundleData = bundleData with { Description = substitutedDescription };
			}
			catch (InvalidOperationException ex)
			{
				collector.EmitError(string.Empty, $"Description placeholder substitution failed: {ex.Message}");
				return false;
			}
		}

		// Apply release date: CLI override → existing bundle date → auto-populate (unless suppressed)
		var finalReleaseDate = bundleData.ReleaseDate; // Preserve existing date if present
		if (!string.IsNullOrEmpty(input.ReleaseDate))
		{
			// Explicit CLI override
			if (DateOnly.TryParseExact(input.ReleaseDate, "yyyy-MM-dd", out var parsedDate))
			{
				finalReleaseDate = parsedDate;
			}
			else
			{
				collector.EmitError(string.Empty, $"Invalid release date format '{input.ReleaseDate}'. Expected YYYY-MM-DD format.");
				return false;
			}
		}
		else if (finalReleaseDate == null && !input.SuppressReleaseDate)
		{
			// Auto-populate with today's date (UTC) if no existing date
			finalReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow);
		}

		bundleData = bundleData with { ReleaseDate = finalReleaseDate };

		// Commit-range bundles record the published endpoint ref (--end-git-ref) as metadata.
		if (!string.IsNullOrWhiteSpace(input.EndGitRef))
			bundleData = bundleData with { GitRef = input.EndGitRef };

		// Write bundle file
		await WriteBundleFileAsync(bundleData, outputPath, ctx);

		return true;
	}

	private async Task<BundleChangelogsArguments?> ProcessProfile(IDiagnosticsCollector collector, BundleChangelogsArguments input, ChangelogConfiguration? config, Cancel ctx)
	{
		// Commit-range mode derives its PR list from git; the profile only contributes output
		// metadata (output/output_products/repo/owner/branch/description), not a filter source.
		var filterResult = !string.IsNullOrWhiteSpace(input.StartGitRef)
			? ResolveGitRangeProfileFilter(collector, input, config)
			: await ProfileFilterResolver.ResolveAsync(
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

		// Resolve bundle-specific output path, output products, repo, owner, hide-features, and description from profile
		string? outputPath = null;
		IReadOnlyList<ProductArgument>? outputProducts = null;
		string? repo = null;
		string? owner = null;
		string? branch = null;
		string[]? mergedHideFeatures = null;
		string? profileDescription = null;
		var profileSuppressReleaseDate = false;

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
				outputPath = _fileSystem.Path.Join(outputDir, outputPattern).OptionalWindowsReplace();
			}
			else if (!string.IsNullOrWhiteSpace(input.StartGitRef))
			{
				// Commit-range bundles follow the standardized {product}-{version}.yaml naming
				// convention when the profile sets no explicit output pattern (explicit output:
				// patterns are being phased out — see elastic/docs-builder#3774).
				var primaryProduct = ResolvePrimaryProduct(profile, input);
				if (!string.IsNullOrWhiteSpace(primaryProduct))
				{
					var outputDir = config.Bundle.OutputDirectory
						?? input.OutputDirectory
						?? config.Bundle.Directory
						?? _fileSystem.Directory.GetCurrentDirectory();
					outputPath = _fileSystem.Path.Join(outputDir, $"{primaryProduct}-{filterResult.Version}.yaml").OptionalWindowsReplace();
				}
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

			// Profile-level repo/owner/branch takes precedence; fall back to bundle-level defaults
			repo = profile.Repo ?? config.Bundle.Repo;
			owner = profile.Owner ?? config.Bundle.Owner;
			branch = profile.Branch ?? config.Bundle.Branch;
			mergedHideFeatures = profile.HideFeatures?.Count > 0 ? [.. profile.HideFeatures] : null;
			profileSuppressReleaseDate = !(profile.ReleaseDates ?? config.Bundle.ReleaseDates ?? true);

			// Handle profile-specific description with placeholder substitution
			var descriptionTemplate = profile.Description ?? config.Bundle.Description;
			if (!string.IsNullOrEmpty(descriptionTemplate))
			{
				// Validate placeholder usage in profile mode
				var hasVersionPlaceholder = descriptionTemplate.Contains("{version}") || descriptionTemplate.Contains("{lifecycle}");
				var hasOwnerRepoPlaceholder = descriptionTemplate.Contains("{owner}") || descriptionTemplate.Contains("{repo}");

				if (hasVersionPlaceholder &&
					filterResult.Version == "unknown" &&
					string.IsNullOrEmpty(profile.OutputProducts))
				{
					collector.EmitError(string.Empty,
						$"Profile '{input.Profile}' uses {{version}} or {{lifecycle}} placeholders in description but no version is available for substitution. " +
						"Either provide a version argument, or add 'output_products' pattern to the profile configuration.");
					return null;
				}

				if (hasOwnerRepoPlaceholder &&
					(string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo)))
				{
					collector.EmitError(string.Empty,
						$"Profile '{input.Profile}' uses {{owner}} or {{repo}} placeholders in description but values are not resolvable. " +
						"Ensure repository metadata is available in the configuration.");
					return null;
				}

				profileDescription = BundleDescriptionSubstitution.SubstitutePlaceholders(
					descriptionTemplate, filterResult.Version, resolvedLifecycle, owner, repo);
			}
		}

		return input with
		{
			InputProducts = filterResult.Products,
			Prs = filterResult.Prs,
			Issues = filterResult.Issues,
			Files = filterResult.Files,
			All = false,
			Output = outputPath,
			OutputProducts = outputProducts,
			Repo = repo,
			Owner = owner,
			Branch = branch,
			HideFeatures = mergedHideFeatures,
			Description = profileDescription,
			SuppressReleaseDate = profileSuppressReleaseDate
		};
	}

	/// <summary>
	/// Validates the commit-range arguments: both refs together (the start ref is never inferred —
	/// an explicit RFC-review decision), no other filter source, and <c>--dry-run</c> only in range mode.
	/// </summary>
	private static bool ValidateGitRefArguments(IDiagnosticsCollector collector, BundleChangelogsArguments input)
	{
		var hasStart = !string.IsNullOrWhiteSpace(input.StartGitRef);
		var hasEnd = !string.IsNullOrWhiteSpace(input.EndGitRef);

		if (!hasStart && !hasEnd)
		{
			if (input.DryRun)
			{
				collector.EmitError(string.Empty, "--dry-run is only supported when bundling a git commit range (--start-git-ref/--end-git-ref).");
				return false;
			}

			return true;
		}

		if (hasStart != hasEnd)
		{
			collector.EmitError(string.Empty,
				"--start-git-ref and --end-git-ref must be provided together; the start ref is never inferred from previous bundles.");
			return false;
		}

		var conflicting = new List<string>();
		if (input.All)
			conflicting.Add("--all");
		if (input.InputProducts is { Count: > 0 })
			conflicting.Add("--input-products");
		if (input.Prs is { Length: > 0 })
			conflicting.Add("--prs");
		if (input.Issues is { Length: > 0 })
			conflicting.Add("--issues");
		if (input.Files is { Length: > 0 })
			conflicting.Add("--files");
		if (!string.IsNullOrWhiteSpace(input.Report))
			conflicting.Add("--report");
		if (!string.IsNullOrWhiteSpace(input.ProfileReport))
			conflicting.Add("a report/list positional argument");

		if (conflicting.Count > 0)
		{
			collector.EmitError(string.Empty,
				$"--start-git-ref/--end-git-ref cannot be combined with other filter sources: {string.Join(", ", conflicting)}. " +
				"The PR list is derived from the commit range itself.");
			return false;
		}

		return true;
	}

	/// <summary>
	/// Profile resolution for commit-range mode: the profile contributes output metadata only, so
	/// filter-producing profile shapes (<c>source: github_release</c>, a <c>products</c> pattern)
	/// are rejected and the version argument is carried through for placeholder substitution.
	/// </summary>
	private static ProfileFilterResult? ResolveGitRangeProfileFilter(
		IDiagnosticsCollector collector,
		BundleChangelogsArguments input,
		ChangelogConfiguration? config)
	{
		if (config?.Bundle?.Profiles == null || !config.Bundle.Profiles.TryGetValue(input.Profile!, out var profile))
		{
			collector.EmitError(string.Empty, $"Profile '{input.Profile}' not found in bundle.profiles configuration");
			return null;
		}

		if (string.IsNullOrWhiteSpace(input.ProfileArgument))
		{
			collector.EmitError(string.Empty,
				$"Profile '{input.Profile}' requires a version as the second argument when bundling a git commit range");
			return null;
		}

		if (string.Equals(profile.Source, "github_release", StringComparison.OrdinalIgnoreCase))
		{
			collector.EmitError(string.Empty,
				$"Profile '{input.Profile}': 'source: github_release' cannot be combined with --start-git-ref/--end-git-ref. " +
				"The PR list is derived from the commit range itself.");
			return null;
		}

		if (!string.IsNullOrWhiteSpace(profile.Products))
		{
			collector.EmitError(string.Empty,
				$"Profile '{input.Profile}' has a 'products' pattern configured. " +
				"A git commit range cannot be combined with a products pattern filter; use 'output_products' and 'rules' to shape the bundle.");
			return null;
		}

		return new ProfileFilterResult { Version = input.ProfileArgument };
	}

	/// <summary>How commit-range entries are sourced: the resolved authoring pool and the CDN/local gate.</summary>
	private sealed record GitRangeSourcingContext
	{
		public required bool UseCdn { get; init; }
		public required string? Owner { get; init; }
		public required string? Repo { get; init; }
		public required string? Branch { get; init; }
	}

	/// <summary>
	/// Bundles a git commit range: resolves the range to a PR list (compare API +
	/// <c>associatedPullRequests</c>), sources each PR's entry with the pool-first /
	/// inferred-from-PR-metadata precedence, and reports PRs and commits that produced no entry.
	/// In dry-run mode prints the run report instead of writing the bundle.
	/// </summary>
	private async Task<bool> BundleFromGitRange(
		IDiagnosticsCollector collector,
		BundleChangelogsArguments input,
		ChangelogConfiguration? config,
		GitRangeSourcingContext sourcing,
		Cancel ctx)
	{
		if (string.IsNullOrWhiteSpace(sourcing.Repo))
		{
			collector.EmitError(string.Empty,
				"Bundling a git commit range requires a resolvable authoring repository. " +
				"Set bundle.repo in changelog.yml (or pass --repo).");
			return false;
		}

		var owner = string.IsNullOrWhiteSpace(sourcing.Owner) ? DefaultOwner : sourcing.Owner;
		var resolution = await _commitRangeService.ResolvePullRequestsAsync(collector, new CommitRangeArguments
		{
			Owner = owner,
			Repo = sourcing.Repo,
			StartRef = input.StartGitRef!,
			EndRef = input.EndGitRef!
		}, ctx);
		if (resolution == null)
			return false;

		var directory = input.Directory!;
		var outputPath = input.Output ?? _fileSystem.Path.Join(directory, "changelog-bundle.yaml");

		var candidates = sourcing.UseCdn
			? await FetchCdnEntriesAsync(collector, owner, sourcing.Repo, sourcing.Branch, ctx)
			: await ReadLocalEntriesAsync(collector, directory, outputPath, ctx);
		if (candidates == null)
			return false;

		var resolver = new GitRangeEntryResolver(_prService, _logger);
		var result = await resolver.ResolveAsync(collector, resolution, candidates, config, new GitRangeEntryResolutionOptions
		{
			Owner = owner,
			Repo = sourcing.Repo,
			StartRef = input.StartGitRef!,
			EndRef = input.EndGitRef!,
			FallbackProducts = input.OutputProducts
		}, ctx);

		var report = result.Report.ToMarkdown();
		_logger.LogInformation("Commit-range bundle report:\n{Report}", report);

		if (input.DryRun)
		{
			// The report is the dry run's product: print it verbatim for release-PR bodies / job summaries.
			await Console.Out.WriteLineAsync(report);
			return result.Success && collector.Errors == 0;
		}

		if (!result.Success || collector.Errors > 0)
			return false;

		if (result.Entries.Count == 0)
		{
			collector.EmitError(string.Empty,
				$"No changelog entries could be resolved for commit range {input.StartGitRef}..{input.EndGitRef} of {owner}/{sourcing.Repo}.");
			return false;
		}

		return await BuildAndWriteBundle(collector, input, config, result.Entries, outputPath, ctx);
	}

	/// <summary>Reads the local changelog directory into (file name, content) pairs for range matching.</summary>
	private async Task<IReadOnlyList<(string FileName, string Content)>?> ReadLocalEntriesAsync(
		IDiagnosticsCollector collector,
		string directory,
		string outputPath,
		Cancel ctx)
	{
		if (!_fileSystem.Directory.Exists(directory))
		{
			collector.EmitError(directory, "Directory does not exist");
			return null;
		}

		var fileDiscovery = new ChangelogFileDiscovery(_fileSystem, _logger);
		var yamlFiles = await fileDiscovery.DiscoverChangelogFilesAsync(directory, outputPath, ctx);
		var entries = new List<(string FileName, string Content)>(yamlFiles.Count);
		foreach (var filePath in yamlFiles)
		{
			var content = await _fileSystem.File.ReadAllTextAsync(filePath, ctx);
			entries.Add((_fileSystem.Path.GetFileName(filePath), content));
		}

		return entries;
	}

	private BundleChangelogsArguments ApplyConfigDefaults(BundleChangelogsArguments input, ChangelogConfiguration? config)
	{
		// Apply directory: CLI takes precedence. Only use config when --directory not specified.
		var directory = input.Directory ?? config?.Bundle?.Directory ?? _fileSystem.Directory.GetCurrentDirectory();

		if (config?.Bundle == null)
			return input with { Directory = directory, LinkAllowRepos = null };

		// Apply output default when --output not specified: use bundle.output_directory if set
		var output = input.Output;
		if (string.IsNullOrWhiteSpace(output) && !string.IsNullOrWhiteSpace(config.Bundle.OutputDirectory))
			output = _fileSystem.Path.Join(config.Bundle.OutputDirectory, "changelog-bundle.yaml").OptionalWindowsReplace();

		// Apply repo/owner/branch: CLI takes precedence; fall back to bundle-level config defaults.
		var repo = input.Repo ?? config.Bundle.Repo;
		var owner = input.Owner ?? config.Bundle.Owner;
		var branch = input.Branch ?? config.Bundle.Branch;

		// Apply description: CLI takes precedence; fall back to bundle-level config default
		var description = input.Description ?? config.Bundle.Description;

		// Apply release date suppression: CLI takes precedence; config can enable suppression when CLI didn't
		// In profile mode, profile has already resolved inheritance, so skip bundle logic
		var suppressReleaseDate = !string.IsNullOrWhiteSpace(input.Profile)
			? input.SuppressReleaseDate
			: input.SuppressReleaseDate || !(config.Bundle.ReleaseDates ?? true);

		return input with
		{
			Directory = directory,
			Output = output,
			Repo = repo,
			Owner = owner,
			Branch = branch,
			Description = description,
			SuppressReleaseDate = suppressReleaseDate,
			LinkAllowRepos = config.Bundle.LinkAllowRepos
		};
	}

	/// <summary>
	/// Resolves a bundle plan from config and profile metadata without executing any network calls or
	/// file-scanning. Used by <c>--plan</c> mode to emit GitHub Actions step outputs
	/// (<c>needs_network</c>, <c>needs_github_token</c>, <c>output_path</c>) that CI actions consume.
	/// </summary>
	public async Task<BundlePlanResult?> PlanBundleAsync(
		IDiagnosticsCollector collector,
		BundleChangelogsArguments input,
		bool hasReleaseVersion,
		Cancel ctx)
	{
		var needsNetwork = hasReleaseVersion;
		var needsGithubToken = hasReleaseVersion;

		// Commit-range bundling always needs the GitHub API (compare + GraphQL + PR metadata fallback).
		if (!string.IsNullOrWhiteSpace(input.StartGitRef) || !string.IsNullOrWhiteSpace(input.EndGitRef))
		{
			needsNetwork = true;
			needsGithubToken = true;
		}

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

		// CDN entry sourcing needs network access for the Docker bundle run. Mirror the run-mode gate:
		// active when the authoring repo resolves (profile/config bundle.repo), the user has not forced
		// local sourcing, and a CDN base is configured.
		var useLocalChangelogs = (config?.Bundle?.UseLocalChangelogs ?? false)
			|| input.ForceLocal;
		var explicitDirectory = !string.IsNullOrWhiteSpace(input.Directory);
		var authoringRepo = ChangelogRepoOwnerResolver.NormalizeRepo(input.Repo ?? profileDef?.Repo ?? config?.Bundle?.Repo);
		if (ShouldSourceFromCdn(authoringRepo, useLocalChangelogs: useLocalChangelogs, explicitDirectory: explicitDirectory))
			needsNetwork = true;

		// Resolve output path — mirrors the logic in ProcessProfile + ApplyConfigDefaults.
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
			outputPath = _fileSystem.Path.Join(outputDir, outputPattern).OptionalWindowsReplace();
		}
		else if (string.IsNullOrWhiteSpace(outputPath) &&
			!string.IsNullOrWhiteSpace(input.StartGitRef) &&
			profileDef != null &&
			!string.IsNullOrWhiteSpace(input.ProfileArgument) &&
			ResolvePrimaryProduct(profileDef, input) is { } primaryProduct)
		{
			// Mirror ProcessProfile's commit-range convention: {product}-{version}.yaml when the
			// profile sets no explicit output pattern.
			var outputDir = config?.Bundle?.OutputDirectory
				?? config?.Bundle?.Directory
				?? _fileSystem.Directory.GetCurrentDirectory();
			outputPath = _fileSystem.Path.Join(outputDir, $"{primaryProduct}-{input.ProfileArgument}.yaml").OptionalWindowsReplace();
		}
		else if (string.IsNullOrWhiteSpace(outputPath) && config?.Bundle?.OutputDirectory != null)
			outputPath = _fileSystem.Path.Join(config.Bundle.OutputDirectory, "changelog-bundle.yaml").OptionalWindowsReplace();

		return new BundlePlanResult
		{
			NeedsNetwork = needsNetwork,
			NeedsGithubToken = needsGithubToken,
			OutputPath = outputPath,
			CdnUrl = ResolveCdnBundleUrl(profileDef, input, outputPath)
		};
	}

	/// <summary>Public CDN URL of the scrubbed bundle (<c>{base}/bundle/{product}/{file}</c>); null when product, output file name, or CDN base cannot be resolved.</summary>
	private string? ResolveCdnBundleUrl(BundleProfile? profileDef, BundleChangelogsArguments input, string? outputPath)
	{
		if (string.IsNullOrWhiteSpace(outputPath))
			return null;

		var product = ResolvePrimaryProduct(profileDef, input);
		if (string.IsNullOrWhiteSpace(product))
			return null;

		if (ChangelogCdn.ResolveBaseUri() is not { } baseUri)
			return null;

		var fileName = _fileSystem.Path.GetFileName(outputPath);
		if (string.IsNullOrWhiteSpace(fileName))
			return null;

		var basePath = baseUri.AbsoluteUri.TrimEnd('/');
		return $"{basePath}/bundle/{Uri.EscapeDataString(product)}/{Uri.EscapeDataString(fileName)}";
	}

	/// <summary>
	/// The first concrete (non-wildcard) product that scopes the bundle, used to build its CDN URL.
	/// From the profile <c>output_products</c>/<c>products</c> pattern, else the first explicit product argument.
	/// </summary>
	private static string? ResolvePrimaryProduct(BundleProfile? profileDef, BundleChangelogsArguments input)
	{
		var pattern = profileDef?.OutputProducts ?? profileDef?.Products;
		if (!string.IsNullOrWhiteSpace(pattern))
		{
			var firstGroup = pattern.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
			var id = firstGroup?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
			if (!string.IsNullOrWhiteSpace(id) && id != "*")
				return id;
		}

		foreach (var list in new[] { input.OutputProducts, input.InputProducts })
		{
			if (list is null)
				continue;
			foreach (var p in list)
			{
				if (!string.IsNullOrWhiteSpace(p.Product) && p.Product != "*")
					return p.Product;
			}
		}

		return null;
	}

	/// <summary>
	/// Fetches a named list of changelog entries directly from the CDN without consulting the pool registry.
	/// Used for the <c>--files</c> CDN path. Returns <c>null</c> after emitting an error on any failure.
	/// </summary>
	private async Task<IReadOnlyList<(string FileName, string Content)>?> FetchCdnNamedEntriesAsync(
		IDiagnosticsCollector collector,
		string? org,
		string? repo,
		string? branch,
		IReadOnlyList<string> fileNames,
		Cancel ctx)
	{
		if (string.IsNullOrWhiteSpace(repo))
		{
			collector.EmitError(string.Empty,
				"Sourcing changelog entries from the CDN requires a resolvable authoring repository. " +
				"Set bundle.repo in changelog.yml (or pass --repo), or pass --force-local / --directory to bundle local files.");
			return null;
		}

		var resolvedOrg = string.IsNullOrWhiteSpace(org) ? DefaultOwner : org;
		var resolvedBranch = string.IsNullOrWhiteSpace(branch) ? DefaultBranch : branch;

		var baseUri = ChangelogCdn.ResolveBaseUri();
		if (baseUri is null)
		{
			collector.EmitError(string.Empty,
				$"No valid changelog CDN base URL is configured. Set the {ChangelogCdn.BaseUrlEnvironmentVariable} environment variable to an absolute http(s) URL.");
			return null;
		}

		var entries = await _entryFetcher.FetchNamedAsync(
			baseUri,
			resolvedOrg,
			repo,
			resolvedBranch,
			fileNames,
			msg => collector.EmitError(string.Empty, msg),
			ctx);

		if (entries == null)
			return null;

		_logger.LogInformation("Fetched {Count} named changelog entry(ies) for {Pool} from CDN",
			entries.Count, $"{resolvedOrg}/{repo}/{resolvedBranch}");

		return entries.Select(e => (e.FileName, e.Content)).ToList();
	}

	/// <summary>Downloads the authoring <paramref name="org"/>/<paramref name="repo"/>/<paramref name="branch"/> pool's changelog entries from the CDN (<c>changelog/{org}/{repo}/{branch}/...</c>); returns null after emitting an error on any fatal fetch failure.</summary>
	private async Task<IReadOnlyList<(string FileName, string Content)>?> FetchCdnEntriesAsync(
		IDiagnosticsCollector collector,
		string? org,
		string? repo,
		string? branch,
		Cancel ctx)
	{
		if (string.IsNullOrWhiteSpace(repo))
		{
			collector.EmitError(string.Empty,
				"Sourcing changelog entries from the CDN requires a resolvable authoring repository. " +
				"Set bundle.repo in changelog.yml (or pass --repo), or set bundle.use_local_changelogs: true " +
				"in changelog.yml / pass --directory to bundle local changelog files.");
			return null;
		}

		// org/branch always resolve to a default at the call site, but guard anyway so the fetcher never
		// receives a blank pool segment.
		var resolvedOrg = string.IsNullOrWhiteSpace(org) ? DefaultOwner : org;
		var resolvedBranch = string.IsNullOrWhiteSpace(branch) ? DefaultBranch : branch;

		var baseUri = ChangelogCdn.ResolveBaseUri();
		if (baseUri is null)
		{
			collector.EmitError(string.Empty,
				$"No valid changelog CDN base URL is configured. Set the {ChangelogCdn.BaseUrlEnvironmentVariable} environment variable to an absolute http(s) URL.");
			return null;
		}

		var fatalFailure = false;
		var entries = await _entryFetcher.FetchAsync(
			baseUri,
			resolvedOrg,
			repo,
			resolvedBranch,
			msg => { fatalFailure = true; collector.EmitError(string.Empty, msg); },
			msg => collector.EmitWarning(string.Empty, msg),
			ctx);

		// The fetcher emits an error (via the callback above) for any fatal condition — a registry that
		// cannot be read, or a registry-listed entry still missing after its retry budget. Either would
		// silently drop entries and ship an incomplete bundle, so treat it as fatal.
		if (fatalFailure)
			return null;

		var byName = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (var entry in entries)
			byName[entry.FileName] = entry.Content;

		_logger.LogInformation("Sourced {Count} changelog entr(ies) from the CDN for {Pool}",
			byName.Count, $"{resolvedOrg}/{repo}/{resolvedBranch}");

		return byName.Select(kv => (kv.Key, kv.Value)).ToList();
	}

	/// <summary>
	/// Fetches notes for <paramref name="target"/> from the CDN and converts them to matched entries.
	/// An absent notes index is not an error (most targets have no notes). Returns <c>null</c> after
	/// emitting an error when the index exists but a listed note cannot be fetched.
	/// </summary>
	private async Task<IReadOnlyList<MatchedChangelogFile>?> FetchCdnNotesAsync(
		IDiagnosticsCollector collector,
		string? org,
		string? repo,
		string target,
		Cancel ctx)
	{
		if (string.IsNullOrWhiteSpace(repo))
			return [];

		var resolvedOrg = string.IsNullOrWhiteSpace(org) ? DefaultOwner : org;

		var baseUri = ChangelogCdn.ResolveBaseUri();
		if (baseUri is null)
			return [];

		var hadError = false;
		var cdnEntries = await _entryFetcher.FetchNotesAsync(
			baseUri, resolvedOrg, repo, target,
			msg => { hadError = true; collector.EmitError(string.Empty, msg); },
			ctx);

		if (hadError)
			return null;

		if (cdnEntries.Count == 0)
			return [];

		var matchedNotes = new List<MatchedChangelogFile>(cdnEntries.Count);
		foreach (var entry in cdnEntries)
		{
			try
			{
				var normalized = ReleaseNotesSerialization.NormalizeYaml(entry.Content);
				var dto = ReleaseNotesSerialization.GetEntryDeserializer().Deserialize<ChangelogEntryDto>(normalized);
				var data = ReleaseNotesSerialization.ConvertEntry(dto);
				var checksum = ComputeSha1(entry.Content);
				matchedNotes.Add(new MatchedChangelogFile
				{
					Data = data,
					FilePath = entry.FileName,
					FileName = entry.FileName,
					Checksum = checksum
				});
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				_logger.LogWarning(ex, "Failed to parse note '{FileName}' for {Repo}@{Target}; skipping", entry.FileName, repo, target);
				collector.EmitError(string.Empty, $"Note '{entry.FileName}' for {repo}@{target} could not be parsed: {ex.Message}");
				return null;
			}
		}

		_logger.LogInformation("Resolved {Count} note(s) for {Repo}@{Target} from CDN", matchedNotes.Count, repo, target);
		return matchedNotes;
	}

	/// <summary>
	/// Fetches notes from the CDN (when applicable) and appends them to <paramref name="entries"/>,
	/// deduplicating by checksum. Returns <c>null</c> after emitting an error on any fatal failure;
	/// returns the original list unchanged when CDN notes are not applicable.
	/// </summary>
	private async Task<IReadOnlyList<MatchedChangelogFile>?> MergeNotesAsync(
		IDiagnosticsCollector collector,
		IReadOnlyList<MatchedChangelogFile> entries,
		bool useCdn,
		string? org,
		string? repo,
		BundleChangelogsArguments input,
		Cancel ctx)
	{
		if (!useCdn)
			return entries;

		var noteTargets = ResolveNoteTargets(input);
		if (noteTargets.Count == 0)
			return entries;

		// Dedup by checksum: a note body identical to a PR entry (edge case) should appear once.
		var seen = new HashSet<string>(entries.Select(e => e.Checksum), StringComparer.OrdinalIgnoreCase);
		var combined = new List<MatchedChangelogFile>(entries);

		foreach (var noteTarget in noteTargets)
		{
			var noteEntries = await FetchCdnNotesAsync(collector, org, repo, noteTarget, ctx);
			if (noteEntries == null)
				return null;

			foreach (var note in noteEntries)
			{
				if (seen.Add(note.Checksum))
					combined.Add(note);
			}
		}
		return combined;
	}

	private async Task<IReadOnlyList<(string FileName, string Content)>?> FetchCdnProbedEntriesAsync(
		IDiagnosticsCollector collector,
		string? org,
		string? repo,
		string? branch,
		HashSet<string> prsToMatch,
		Cancel ctx)
	{
		if (string.IsNullOrWhiteSpace(repo))
		{
			collector.EmitError(string.Empty,
				"Sourcing changelog entries from the CDN requires a resolvable authoring repository. " +
				"Set bundle.repo in changelog.yml (or pass --repo), or pass --force-local / --directory to bundle local changelog files.");
			return null;
		}

		var resolvedOrg = string.IsNullOrWhiteSpace(org) ? DefaultOwner : org;
		var resolvedBranch = string.IsNullOrWhiteSpace(branch) ? DefaultBranch : branch;
		var poolLabel = $"{resolvedOrg}/{repo}/{resolvedBranch}";

		var baseUri = ChangelogCdn.ResolveBaseUri();
		if (baseUri is null)
		{
			collector.EmitError(string.Empty,
				$"No valid changelog CDN base URL is configured. Set the {ChangelogCdn.BaseUrlEnvironmentVariable} environment variable to an absolute http(s) URL.");
			return null;
		}

		var prNumbers = new List<int>();
		foreach (var pr in prsToMatch)
		{
			if (TryExtractPrNumber(pr, resolvedOrg, repo, out var prNumber))
				prNumbers.Add(prNumber);
		}

		if (prNumbers.Count == 0)
		{
			_logger.LogInformation("No PR numbers for {Pool} found in filter; returning empty probe result", poolLabel);
			return [];
		}

		var tasks = prNumbers.Select(pr =>
			_entryFetcher.FetchPrEntryAsync(baseUri, resolvedOrg, repo, resolvedBranch, pr, ctx)).ToArray();
		var results = await Task.WhenAll(tasks).ConfigureAwait(false);

		var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		var hasError = false;

		for (var i = 0; i < prNumbers.Count; i++)
		{
			var prNumber = prNumbers[i];
			var cdnEntry = results[i];

			if (cdnEntry is null)
			{
				_logger.LogWarning("No changelog entry found for PR {PrNumber} in {Pool}", prNumber, poolLabel);
				continue;
			}

			var entry = ReleaseNotesSerialization.DeserializeEntry(cdnEntry.Value.Content);
			if (!entry.IsMarker)
			{
				_ = entries.TryAdd(cdnEntry.Value.FileName, cdnEntry.Value.Content);
				continue;
			}

			if (!int.TryParse(entry.Link, out var parentPr) || parentPr <= 0)
			{
				collector.EmitError(string.Empty,
					$"Changelog entry '{cdnEntry.Value.FileName}' contains an invalid link: '{entry.Link}'. Expected a positive PR number.");
				hasError = true;
				continue;
			}

			var parentCdnEntry = await _entryFetcher.FetchPrEntryAsync(baseUri, resolvedOrg, repo, resolvedBranch, parentPr, ctx).ConfigureAwait(false);
			if (parentCdnEntry is null)
			{
				collector.EmitError(string.Empty,
					$"Changelog entry '{cdnEntry.Value.FileName}' is a marker pointing to PR {parentPr}, but that entry does not exist in {poolLabel}.");
				hasError = true;
				continue;
			}

			var parentEntry = ReleaseNotesSerialization.DeserializeEntry(parentCdnEntry.Value.Content);
			if (parentEntry.IsMarker)
			{
				collector.EmitError(string.Empty,
					$"Marker chain detected: '{cdnEntry.Value.FileName}' → '{parentCdnEntry.Value.FileName}' is also a marker. Marker chains are not allowed.");
				hasError = true;
				continue;
			}

			_ = entries.TryAdd(parentCdnEntry.Value.FileName, parentCdnEntry.Value.Content);
		}

		return hasError ? null : entries.Select(kv => (kv.Key, kv.Value)).ToList();
	}

	private static bool TryExtractPrNumber(string pr, string authoringOwner, string authoringRepo, out int prNumber)
	{
		prNumber = 0;
		// Normalize to {owner}/{repo}#{number} form.
		var normalized = NormalizePrForComparison(pr, authoringOwner, authoringRepo);
		var hashIndex = normalized.LastIndexOf('#');
		if (hashIndex < 0)
			return false;
		// Reject PRs whose repo does not match the authoring repo — a kibana PR number must not
		// probe the elasticsearch pool even when --owner is overridden (owner may differ, repo must not).
		var ownerRepo = normalized[..hashIndex];
		var slashIndex = ownerRepo.LastIndexOf('/');
		var prRepo = slashIndex >= 0 ? ownerRepo[(slashIndex + 1)..] : ownerRepo;
		if (!string.Equals(prRepo, authoringRepo, StringComparison.OrdinalIgnoreCase))
			return false;
		return int.TryParse(normalized[(hashIndex + 1)..], out prNumber) && prNumber > 0;
	}

	/// <summary>Gate for repo-scoped CDN entry sourcing: true when the authoring repo resolves, local sourcing is not forced (<c>bundle.use_local_changelogs</c>/<c>--force-local</c>/<c>--directory</c>), and a CDN base is configured.</summary>
	private static bool ShouldSourceFromCdn(string? authoringRepo, bool useLocalChangelogs, bool explicitDirectory)
	{
		if (useLocalChangelogs || explicitDirectory || string.IsNullOrWhiteSpace(authoringRepo))
			return false;
		return ChangelogCdn.ResolveBaseUri() is not null;
	}

	/// <summary>
	/// Selects the CDN-sourced entries whose file names were explicitly requested via <c>--files</c> / a
	/// path list. Every requested name must exist in the pool: the registry is the source of truth for
	/// what was uploaded, so a missing name means the entry never reached S3 (or the name is wrong) and
	/// silently shipping an incomplete bundle is worse than failing the run. Returns <c>null</c> after
	/// emitting an error when any requested name is missing.
	/// </summary>
	private IReadOnlyList<(string FileName, string Content)>? SelectRequestedCdnEntries(
		IDiagnosticsCollector collector,
		IReadOnlyList<(string FileName, string Content)> contents,
		IReadOnlyList<string> requestedEntryNames,
		string poolLabel)
	{
		var byName = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (var (fileName, content) in contents)
			byName[fileName] = content;

		var selected = new List<(string FileName, string Content)>();
		var missing = new List<string>();
		var seen = new HashSet<string>(StringComparer.Ordinal);
		foreach (var name in requestedEntryNames)
		{
			if (!seen.Add(name))
				continue;
			if (byName.TryGetValue(name, out var content))
				selected.Add((name, content));
			else
				missing.Add(name);
		}

		if (missing.Count > 0)
		{
			collector.EmitError(string.Empty,
				$"Changelog entr{(missing.Count == 1 ? "y" : "ies")} not found in the CDN pool '{poolLabel}': {string.Join(", ", missing)}. " +
				"Ensure the entries were uploaded (changelog upload), or pass --force-local / --directory to bundle local files instead.");
			return null;
		}

		_logger.LogInformation("Selected {Selected} of {Total} CDN entries by requested file name for {Pool}",
			selected.Count, contents.Count, poolLabel);
		return selected;
	}

	private bool ValidateInput(IDiagnosticsCollector collector, BundleChangelogsArguments input, bool requireDirectoryExists)
	{
		if (string.IsNullOrWhiteSpace(input.Directory))
		{
			collector.EmitError(string.Empty, "Directory is required");
			return false;
		}

		if (requireDirectoryExists && !_fileSystem.Directory.Exists(input.Directory))
		{
			collector.EmitError(input.Directory, "Directory does not exist");
			return false;
		}

		// Validate filter options - exactly one of: --all, --input-products, --prs, --issues, --files
		var specifiedFilters = new List<string>();
		if (input.All)
			specifiedFilters.Add("--all");
		if (input.InputProducts is { Count: > 0 })
			specifiedFilters.Add("--input-products");
		if (input.Prs is { Length: > 0 })
			specifiedFilters.Add("--prs");
		if (input.Issues is { Length: > 0 })
			specifiedFilters.Add("--issues");
		if (input.Files is { Length: > 0 })
			specifiedFilters.Add("--files");

		if (specifiedFilters.Count == 0)
		{
			collector.EmitError(string.Empty, "At least one filter option must be specified: --all, --input-products, --prs, --issues, or --files");
			return false;
		}

		if (specifiedFilters.Count > 1)
		{
			collector.EmitError(string.Empty,
				$"Multiple filter options cannot be specified together. You specified: {string.Join(", ", specifiedFilters)}. Please use only one filter option: --all, --input-products, --prs, --issues, or --files");
			return false;
		}

		return true;
	}

	private static bool ValidatePlaceholderUsage(IDiagnosticsCollector collector, BundleChangelogsArguments input)
	{
		if (!string.IsNullOrEmpty(input.Profile))
			return true;

		if (string.IsNullOrEmpty(input.Description))
			return true;

		var hasPlaceholders = input.Description.Contains("{version}") ||
							 input.Description.Contains("{lifecycle}") ||
							 input.Description.Contains("{owner}") ||
							 input.Description.Contains("{repo}");

		if (hasPlaceholders && (input.OutputProducts == null || input.OutputProducts.Count == 0))
		{
			collector.EmitError(string.Empty,
				"When using placeholders in bundle description in option-based mode, " +
				"--output-products must be explicitly specified to ensure predictable substitution values.");
			return false;
		}

		return true;
	}

	/// <summary>
	/// Returns all distinct, explicit, non-wildcard targets from <see cref="BundleChangelogsArguments.OutputProducts"/>.
	/// Notes are fetched for every resolved target so multi-target bundles are fully covered.
	/// Returns an empty list when no concrete targets are available.
	/// </summary>
	private static IReadOnlyList<string> ResolveNoteTargets(BundleChangelogsArguments input)
	{
		if (input.OutputProducts is not { Count: > 0 })
			return [];
		return input.OutputProducts
			.Where(p => !string.IsNullOrWhiteSpace(p.Target) && p.Target != "*")
			.Select(p => p.Target!)
			.Distinct(StringComparer.Ordinal)
			.ToList();
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
		// Strip any leading BOM to ensure clean UTF-8 output for tooling compatibility
		var normalizedYaml = ChangelogUtf8Normalization.StripLeadingUtf8BomChar(bundledYaml);
		await _fileSystem.File.WriteAllTextAsync(outputPath, normalizedYaml, Utf8NoBom, ctx);
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
							$"Refer to https://github.com/elastic/docs-builder/blob/main/docs/contribute/configure-changelogs-ref.md");
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
