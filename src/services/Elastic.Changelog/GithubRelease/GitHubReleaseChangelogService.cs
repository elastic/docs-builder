// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.IO.Abstractions;
using System.Text;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.GitHub;
using Elastic.Changelog.Utilities;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Changelog;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.ReleaseNotes;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.GithubRelease;

/// <summary>
/// Arguments for the CreateChangelogsFromRelease method
/// </summary>
public record CreateChangelogsFromReleaseArguments
{
	/// <summary>
	/// Repository in owner/repo format (e.g., "elastic/elasticsearch")
	/// </summary>
	public required string Repository { get; init; }

	/// <summary>
	/// Version tag or "latest" (defaults to "latest")
	/// </summary>
	public string Version { get; init; } = "latest";

	/// <summary>
	/// Path to changelog.yml configuration file (optional)
	/// </summary>
	public string? Config { get; init; }

	/// <summary>
	/// Output directory for changelog files (optional, defaults to ./changelogs)
	/// </summary>
	public string? Output { get; init; }

	/// <summary>
	/// Whether to strip [prefix] from PR titles
	/// </summary>
	public bool? StripTitlePrefix { get; init; }

	/// <summary>
	/// Optional bundle description text with placeholder support.
	/// Supports {version}, {lifecycle}, {owner}, and {repo} placeholders.
	/// </summary>
	public string? Description { get; init; }

	/// <summary>
	/// Optional explicit release date for the bundle in YYYY-MM-DD format.
	/// When provided, overrides the GitHub release published_at date.
	/// </summary>
	public string? ReleaseDate { get; init; }

	/// <summary>
	/// Whether to create a bundle file after creating individual changelog files. Defaults to true.
	/// Set to false when called from 'changelog add --release-version' to skip bundle creation.
	/// </summary>
	public bool CreateBundle { get; init; } = true;
}

/// <summary>
/// Service for creating changelogs from GitHub releases
/// </summary>
public class GitHubReleaseChangelogService(
	ILoggerFactory logFactory,
	IConfigurationContext configurationContext,
	IChangelogFileSystem fileSystem,
	IGitHubReleaseService? releaseService = null,
	IGitHubPrService? prService = null,
	ChangelogBundlingService? bundlingService = null,
	CdnChangelogEntryFetcher? entryFetcher = null,
	IGitHubCommitRangeService? commitRangeService = null
) : IService
{
	/// <summary>
	/// UTF-8 encoding without BOM for writing YAML files.
	/// </summary>
	private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

	private readonly ILogger _logger = logFactory.CreateLogger<GitHubReleaseChangelogService>();
	private readonly IChangelogFileSystem _fileSystem = fileSystem;
	private readonly ChangelogConfigurationLoader _configLoader = new(logFactory, configurationContext, fileSystem);
	private readonly IGitHubReleaseService _releaseService = releaseService ?? new GitHubReleaseService(logFactory);
	private readonly IGitHubPrService _prService = prService ?? new GitHubPrService(logFactory);
	private readonly ChangelogBundlingService _bundlingService = bundlingService
		?? new ChangelogBundlingService(logFactory, fileSystem, configurationContext);
	private readonly CdnChangelogEntryFetcher _entryFetcher = entryFetcher ?? new CdnChangelogEntryFetcher(logFactory);
	private readonly IGitHubCommitRangeService _commitRangeService = commitRangeService ?? new GitHubCommitRangeService(logFactory);

	public async Task<bool> CreateChangelogsFromRelease(
		IDiagnosticsCollector collector,
		CreateChangelogsFromReleaseArguments input,
		Cancel ctx
	)
	{
		try
		{
			// 1. Parse owner/repo from input
			var (owner, repo) = ChangelogTextUtilities.ParseRepository(input.Repository);
			if (string.IsNullOrWhiteSpace(owner))
			{
				// If no owner, assume "elastic" as default
				owner = "elastic";
				repo = input.Repository;
			}

			_logger.LogInformation("Processing GitHub release from {Owner}/{Repo}", owner, repo);

			// 2. Resolve product(s) from repo name via products.yml
			var products = configurationContext.ProductsConfiguration.GetProductsByRepositoryName(repo);
			if (products.Count == 0)
			{
				collector.EmitError(
					string.Empty,
					$"Could not find a product for repository '{repo}' in products.yml. " +
						$"Add a product to config/products.yml whose ID is '{repo}', " +
						$"or set 'repository: {repo}' on one or more existing products."
				);
				return false;
			}

			// Use the first product for versioning context; multiple products (e.g., cloud) share the same versioning system.
			var product = products[0];
			_logger.LogInformation(
				"Resolved {Count} product(s) for '{Repo}': {Products}",
				products.Count,
				repo,
				string.Join(", ", products.Select(p => p.Id))
			);

			// 3. Load changelog configuration
			var config = await _configLoader.LoadChangelogConfiguration(collector, input.Config, ctx);
			if (config == null)
			{
				collector.EmitError(string.Empty, "Failed to load changelog configuration");
				return false;
			}

			// Resolve StripTitlePrefix from input or config default
			var stripTitlePrefix = input.StripTitlePrefix ?? config.Extract.StripTitlePrefix;

			// 4. Fetch GitHub release
			var release = await _releaseService.FetchReleaseAsync(owner, repo, input.Version, ctx);
			if (release == null)
			{
				collector.EmitError(
					string.Empty,
					$"Failed to fetch release for {owner}/{repo}@{input.Version}. " +
						"Ensure the repository exists and the version tag is valid."
				);
				return false;
			}

			_logger.LogInformation("Fetched release: {TagName} ({Name})", release.TagName, release.Name);

			// 5. Resolve PRs via GitHub commit-range API (previous tag → current tag)
			var pullRequests = await ResolvePrsFromRelease(collector, owner, repo, release.TagName, ctx);
			if (pullRequests == null)
				return false;

			if (pullRequests.Count == 0)
			{
				collector.EmitWarning(string.Empty, "No PRs found in commit range for this release. No changelogs will be created.");
				return true;
			}

			_logger.LogInformation("Processing {Count} PR(s) from commit range for release {Tag}", pullRequests.Count, release.TagName);

			// 6. Infer lifecycle and target version from release tag
			var lifecycle = ChangelogTextUtilities.InferLifecycleFromVersion(release.TagName);
			var targetVersion = ChangelogTextUtilities.ExtractBaseVersion(release.TagName);

			_logger.LogInformation("Inferred lifecycle: {Lifecycle}, target version: {Target}", lifecycle, targetVersion);

			// Create product filter with inferred values
			var productInfo = new ProductArgument { Product = product.Id, Target = targetVersion, Lifecycle = lifecycle };

			// 7. Resolve CDN base URI for per-PR entry probing. Pool entries uploaded via
			// changelog-upload take precedence over anything synthesized from PR metadata.
			// We probe per-PR (GET {pr}.yaml) rather than fetching a registry upfront —
			// the pool registry was retired and is no longer written to S3.
			var cdnBaseUri = config.Bundle?.UseLocalChangelogs == true ? null : ChangelogCdn.ResolveBaseUri();
#pragma warning disable CS0618
			var cdnOwner = config.Bundle?.Owner ?? owner;
#pragma warning restore CS0618
			var cdnBranch = config.Bundle?.Branch ?? "main";

			// 8. Process each PR and create changelog files
			var outputDir = input.Output ?? _fileSystem.Path.Join(_fileSystem.Directory.GetCurrentDirectory(), "changelogs");
			if (!_fileSystem.Directory.Exists(outputDir))
				_ = _fileSystem.Directory.CreateDirectory(outputDir);

			var createdFiles = new List<string>();
			var successCount = 0;
			var entryContext = new GhReleaseEntryContext
			{
				Config = config,
				Owner = owner,
				CdnOwner = cdnOwner,
				Repo = repo,
				ProductInfo = productInfo,
				StripTitlePrefix = stripTitlePrefix,
				OutputDir = outputDir,
				CdnBaseUri = cdnBaseUri,
				CdnBranch = cdnBranch
			};

			foreach (var pr in pullRequests)
			{
				var success = await ProcessPr(collector, entryContext, pr, createdFiles, ctx);
				if (success)
					successCount++;
			}

			_logger.LogInformation("Included {Count}/{Total} PR(s) from release {Tag}", successCount, pullRequests.Count, release.TagName);
			if (successCount == 0 && pullRequests.Count > 0)
				collector.EmitWarning(
					string.Empty,
					$"All {pullRequests.Count} PR(s) in the commit range for {release.TagName} were excluded by label rules; no changelog files created."
				);

			// 9. Optionally create bundle file if changelogs were created
			if (input.CreateBundle && createdFiles.Count > 0)
			{
				var bundlePath = await CreateBundleViaService(
					collector,
					outputDir,
					createdFiles,
					productInfo,
					owner,
					repo,
					input,
					release,
					ctx
				);
				if (bundlePath != null)
					_logger.LogInformation("Created bundle file: {BundlePath}", bundlePath);
			}

			// successCount == 0 here means all PRs were intentionally skipped by label rules
			// (ProcessPr returns false only on explicit label-rule exclusions, never on errors).
			// Intentional skips are not failures; the warnings per-PR are already emitted above.
			return true;
		}
		catch (IOException ioEx)
		{
			collector.EmitError(string.Empty, $"IO error creating changelog: {ioEx.Message}", ioEx);
			return false;
		}
		catch (UnauthorizedAccessException uaEx)
		{
			collector.EmitError(string.Empty, $"Access denied creating changelog: {uaEx.Message}", uaEx);
			return false;
		}
	}

	/// <summary>Per-release state shared by every PR while creating entry files.</summary>
	private sealed record GhReleaseEntryContext
	{
		public required ChangelogConfiguration Config { get; init; }
		public required string Owner { get; init; }
		public required string CdnOwner { get; init; }
		public required string Repo { get; init; }
		public required ProductArgument ProductInfo { get; init; }
		public required bool StripTitlePrefix { get; init; }
		public required string OutputDir { get; init; }
		public Uri? CdnBaseUri { get; init; }
		public required string CdnBranch { get; init; }
	}

	private async Task<IReadOnlyList<CommitRangePullRequest>?> ResolvePrsFromRelease(
		IDiagnosticsCollector collector,
		string owner,
		string repo,
		string currentTag,
		Cancel ctx
	)
	{
		var previousTag = await _releaseService.FetchPreviousTagAsync(owner, repo, currentTag, ctx);

		if (previousTag == null)
		{
			collector.EmitError(
				string.Empty,
				$"GitHub could not determine the previous release before '{currentTag}' in {owner}/{repo}. " +
					"Cannot derive PR list from commit range. Ensure at least one prior release exists in the same major version line."
			);
			return null;
		}

		_logger.LogInformation(
			"Resolving PRs via commit range {PrevTag}..{CurrentTag} for {Owner}/{Repo}",
			previousTag,
			currentTag,
			owner,
			repo
		);

		var resolution = await _commitRangeService.ResolvePullRequestsAsync(
			collector,
			new CommitRangeArguments { Owner = owner, Repo = repo, StartRef = previousTag, EndRef = currentTag },
			ctx
		);

		if (resolution == null)
			return null;

		_logger.LogInformation(
			"Commit range {PrevTag}..{CurrentTag}: {TotalCommits} commit(s), {PrCount} PR(s), {UnattributedCount} commit(s) without a PR",
			previousTag,
			currentTag,
			resolution.TotalCommits,
			resolution.PullRequests.Count,
			resolution.CommitsWithoutPullRequest.Count
		);

		if (resolution.CommitsWithoutPullRequest.Count > 0)
			_logger.LogInformation("Commits without an associated PR: {Shas}", string.Join(", ", resolution.CommitsWithoutPullRequest));

		return resolution.PullRequests;
	}

	private async Task<bool> ProcessPr(
		IDiagnosticsCollector collector,
		GhReleaseEntryContext context,
		CommitRangePullRequest pr,
		List<string> createdFiles,
		Cancel ctx
	)
	{
		_logger.LogInformation("PR #{PrNumber} ({PrUrl}): evaluating", pr.Number, pr.Url);

		// A checked-in entry from the pool wins over anything synthesized from PR metadata.
		if (await TryWritePoolEntries(collector, context, pr, createdFiles, ctx))
		{
			_logger.LogInformation("PR #{PrNumber}: included — using checked-in pool entry", pr.Number);
			return true;
		}

		var config = context.Config;

		// Fetch PR metadata (labels, body)
		var prInfo = await _prService.FetchPrInfoAsync(pr.Url, context.Owner, context.Repo, ctx);
		if (prInfo == null)
		{
			_logger.LogInformation("PR #{PrNumber}: included — PR info unavailable, defaulting to type 'other'", pr.Number);
			collector.EmitWarning(pr.Url, $"Failed to fetch PR info for #{pr.Number}; type will default to 'other'.");
		}

		// Check rules.create — skip PRs with blocking labels
		if (prInfo != null && ShouldSkipPrDueToLabelBlockers(prInfo.Labels.ToArray(), context.ProductInfo, config, collector, pr.Url))
		{
			_logger.LogInformation("PR #{PrNumber}: excluded — matched label block rule", pr.Number);
			return false;
		}

		// Derive type and areas from labels
		string? labelDerivedType = null;
		List<string>? labelDerivedAreas = null;

		if (prInfo != null)
		{
			if (config.LabelToType != null && config.LabelToType.Count > 0)
				labelDerivedType = MapLabelsToType(prInfo.Labels.ToArray(), config.LabelToType);

			if (config.LabelToAreas != null && config.LabelToAreas.Count > 0)
				labelDerivedAreas = MapLabelsToAreas(prInfo.Labels.ToArray(), config.LabelToAreas);
		}

		var finalTypeString = labelDerivedType ?? ChangelogEntryType.Other.ToStringFast(true);
		var finalType = ChangelogEntryTypeExtensions.TryParse(
			finalTypeString,
			out var parsed,
			ignoreCase: true,
			allowMatchingMetadataAttribute: true
		) ? parsed : ChangelogEntryType.Other;

		var typeSource = labelDerivedType != null
			? $"label '{string.Join(", ", prInfo?.Labels ?? [])}'"
			: "no matching type label — defaulting to 'other'";
		_logger.LogInformation(
			"PR #{PrNumber}: included — type '{Type}' from {TypeSource}",
			pr.Number,
			finalType.ToStringFast(true),
			typeSource
		);

		var title = prInfo?.Title ?? $"PR #{pr.Number}";
		if (context.StripTitlePrefix)
			title = ChangelogTextUtilities.StripSquareBracketPrefix(title);

		var description = config.Extract.ReleaseNotes ? ReleaseNotesExtractor.FindReleaseNote(prInfo?.Body) : null;
		var issues = config.Extract.Issues && prInfo?.LinkedIssues is { Count: > 0 } linkedIssues ? linkedIssues.ToList() : null;

		var changelogData = new ChangelogEntry
		{
			Title = title,
			Type = finalType,
			Description = description,
			Products =
			[
				new ProductReference
				{
					ProductId = context.ProductInfo.Product ?? "",
#pragma warning disable CS0618
					Versions = context.ProductInfo.Versions is { Count: > 0 }
						? context.ProductInfo.Versions
						: context.ProductInfo.Target is not null ? [context.ProductInfo.Target] : [],
#pragma warning restore CS0618
					Lifecycle = !string.IsNullOrWhiteSpace(context.ProductInfo.Lifecycle)
						? (LifecycleExtensions.TryParse(
							context.ProductInfo.Lifecycle,
							out var lc,
							ignoreCase: true,
							allowMatchingMetadataAttribute: true
						) ? lc : null)
						: null
				}
			],
			Areas = labelDerivedAreas,
			Prs = [pr.Url],
			Issues = issues
		};

		var yamlContent = GenerateYaml(changelogData);
		var slug = ChangelogTextUtilities.GenerateSlug(title);
		var filename = $"{pr.Number}-{finalType.ToStringFast(true)}-{slug}.yaml";
		var filePath = _fileSystem.Path.Join(context.OutputDir, filename);
		var normalizedContent = ChangelogUtf8Normalization.StripLeadingUtf8BomChar(yamlContent);
		await _fileSystem.File.WriteAllTextAsync(filePath, normalizedContent, Utf8NoBom, ctx);

		createdFiles.Add(filename);
		return true;
	}

	/// <summary>
	/// Probes the CDN for a per-PR changelog entry and writes it verbatim when found.
	/// Returns false when the PR has no uploaded entry (404) — synthesis from PR metadata follows.
	/// </summary>
	private async Task<bool> TryWritePoolEntries(
		IDiagnosticsCollector collector,
		GhReleaseEntryContext context,
		CommitRangePullRequest pr,
		List<string> createdFiles,
		Cancel ctx
	)
	{
		if (context.CdnBaseUri is null)
			return false;

		CdnChangelogEntry? entry;
		try
		{
			entry = await _entryFetcher.FetchPrEntryAsync(
				context.CdnBaseUri,
				context.CdnOwner,
				context.Repo,
				context.CdnBranch,
				pr.Number,
				ctx
			).ConfigureAwait(false);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			collector.EmitWarning(
				string.Empty,
				$"PR #{pr.Number}: could not fetch CDN changelog entry; will synthesize from PR metadata. {ex.Message}"
			);
			return false;
		}

		if (entry is not { } cdnEntry)
			return false;

		// Validate the fetched YAML before writing — an unparseable entry must surface as an error
		// and fall through to synthesis rather than writing a malformed file verbatim.
		var candidate = GitRangeEntryResolver.ParseCandidate(cdnEntry.FileName, cdnEntry.Content);
		if (candidate.Entry is null)
		{
			collector.EmitError(
				cdnEntry.FileName,
				$"Checked-in changelog entry '{cdnEntry.FileName}' matches PR #{pr.Number} but could not be parsed: {candidate.ParseError}"
			);
			return false;
		}

		var filePath = _fileSystem.Path.Join(context.OutputDir, cdnEntry.FileName);
		var normalizedContent = ChangelogUtf8Normalization.StripLeadingUtf8BomChar(cdnEntry.Content);
		await _fileSystem.File.WriteAllTextAsync(filePath, normalizedContent, Utf8NoBom, ctx);
		createdFiles.Add(cdnEntry.FileName);
		_logger.LogInformation("PR #{PrNumber}: pool entry '{FileName}' written verbatim from CDN", pr.Number, cdnEntry.FileName);
		return true;
	}

	private static string GenerateYaml(ChangelogEntry data) => ReleaseNotesSerialization.SerializeEntry(data);

	private async Task<string?> CreateBundleViaService(
		IDiagnosticsCollector collector,
		string outputDir,
		List<string> createdFileNames,
		ProductArgument productInfo,
		string owner,
		string repo,
		CreateChangelogsFromReleaseArguments input,
		GitHubReleaseInfo release,
		Cancel ctx
	)
	{
		// Build the bundles subfolder path (mirrors the previous CreateBundleFile convention)
		var bundlesDir = _fileSystem.Path.Join(outputDir, "bundles");
		if (!_fileSystem.Directory.Exists(bundlesDir))
			_ = _fileSystem.Directory.CreateDirectory(bundlesDir);

		// Name format: <version>-<product>-bundle.yml
		var bundleFilename = $"{productInfo.Target}-{productInfo.Product}-bundle.yml";
		var bundlePath = _fileSystem.Path.Join(bundlesDir, bundleFilename);

		// Select exactly the files this run created. A PR-URL filter would miss checked-in pool
		// entries whose prs references were scrubbed from the public copies.
		var files = createdFileNames
			.Distinct(StringComparer.Ordinal)
			.Select(filename => _fileSystem.Path.Join(outputDir, filename))
			.ToArray();

		// Use explicit release date if provided, otherwise GitHub release published date, otherwise fall back to auto-population
		var releaseDate = input.ReleaseDate;
		if (string.IsNullOrEmpty(releaseDate) && release.PublishedAt.HasValue)
		{
			releaseDate = DateOnly.FromDateTime(release.PublishedAt.Value.UtcDateTime).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}

		var bundleArgs = new BundleChangelogsArguments
		{
			Directory = outputDir,
			Output = bundlePath,
			Files = files,
			Owner = owner,
			Repo = repo,
			Config = input.Config,
			OutputProducts = [productInfo],
			Description = input.Description,
			ReleaseDate = releaseDate
		};

		var success = await _bundlingService.BundleChangelogs(collector, bundleArgs, ctx);
		return success ? bundlePath : null;
	}

	private static string? MapLabelsToType(string[] labels, IReadOnlyDictionary<string, string> labelToTypeMapping) =>
		labels.Select(label => labelToTypeMapping.TryGetValue(label, out var mappedType) ? mappedType : null).FirstOrDefault(
			mappedType => mappedType != null
		);

	private static List<string> MapLabelsToAreas(string[] labels, IReadOnlyDictionary<string, IReadOnlyList<string>> labelToAreasMapping)
	{
		var areas = new HashSet<string>();
		foreach (var label in labels)
		{
			if (!labelToAreasMapping.TryGetValue(label, out var mappedAreas))
				continue;

			foreach (var area in mappedAreas)
				_ = areas.Add(area);
		}
		return areas.ToList();
	}

	private bool ShouldSkipPrDueToLabelBlockers(
		string[] prLabels,
		ProductArgument productInfo,
		ChangelogConfiguration config,
		IDiagnosticsCollector collector,
		string prUrl
	)
	{
		var createRules = config.Rules?.Create;
		if (createRules == null)
			return false;

		var normalizedProductId = productInfo.Product?.Replace('_', '-') ?? string.Empty;

		// Check product-specific overrides first
		if (createRules.ByProduct is { Count: > 0 } && createRules.ByProduct.TryGetValue(normalizedProductId, out var productRules))
			return ShouldSkipByCreateRules(prLabels, productRules, collector, prUrl, productInfo.Product);

		// Fall back to global rules
		return ShouldSkipByCreateRules(prLabels, createRules, collector, prUrl, null);
	}

	private static bool ShouldSkipByCreateRules(
		string[] prLabels,
		CreateRules rules,
		IDiagnosticsCollector collector,
		string prUrl,
		string? productContext
	)
	{
		if (rules.Labels == null || rules.Labels.Count == 0)
			return false;

		var mode = rules.Mode;
		var match = rules.Match;
		var prefix = mode == FieldMode.Include ? "[+include]" : "[-exclude]";
		var productSuffix = productContext != null ? $" for product '{productContext}'" : "";

		if (mode == FieldMode.Exclude)
		{
			var matchingLabel = rules.Labels.FirstOrDefault(
				blockerLabel => prLabels.Contains(blockerLabel, StringComparer.OrdinalIgnoreCase)
			);
			if (matchingLabel != null)
			{
				collector.EmitWarning(
					prUrl,
					$"{prefix} Skipping changelog creation for PR {prUrl} due to blocking label '{matchingLabel}'{productSuffix} (match: {match.ToString().ToLowerInvariant()})."
				);
				return true;
			}
		}
		else
		{
			var hasMatch = prLabels.Any(label => rules.Labels.Contains(label, StringComparer.OrdinalIgnoreCase));
			if (!hasMatch)
			{
				var labelsList = string.Join(", ", rules.Labels);
				collector.EmitWarning(
					prUrl,
					$"{prefix} Skipping changelog creation for PR {prUrl}, no labels match rules.create.include [{labelsList}]{productSuffix} (match: {match.ToString().ToLowerInvariant()})."
				);
				return true;
			}
		}

		return false;
	}
}
