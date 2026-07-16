// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.ReleaseNotes;
using Elastic.Documentation.Versions;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Helpers;

namespace Elastic.Markdown.Myst.Directives.Changelog;

/// <summary>
/// Specifies which changelog entry types to display in the directive output.
/// </summary>
public enum ChangelogTypeFilter
{
	/// <summary>
	/// Default behavior: show all types EXCEPT known issues, breaking changes, and deprecations.
	/// These "separated types" are typically shown on their own dedicated pages.
	/// </summary>
	Default,

	/// <summary>
	/// Show all entry types including known issues, breaking changes, and deprecations.
	/// </summary>
	All,

	/// <summary>
	/// Show only breaking change entries.
	/// </summary>
	BreakingChange,

	/// <summary>
	/// Show only deprecation entries.
	/// </summary>
	Deprecation,

	/// <summary>
	/// Show only known issue entries.
	/// </summary>
	KnownIssue,

	/// <summary>
	/// Show only highlighted entries (where highlight == true).
	/// </summary>
	Highlight
}

/// <summary>
/// A directive block that reads all changelog bundles from a folder and renders them inline,
/// ordered by version (descending). Supports both semver (e.g., "9.3.0") and date-based
/// versions (e.g., "2025-08-05") for Serverless and similar release strategies.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// :::{changelog}
/// :::
/// </code>
///
/// Or with a custom bundles folder:
/// <code>
/// :::{changelog} /path/to/bundles
/// :::
/// </code>
///
/// Default bundles folder is <c>changelog/bundles/</c> relative to the docset root.
/// </remarks>
public class ChangelogBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	/// <summary>
	/// Default folder for changelog bundles, relative to the documentation source directory.
	/// </summary>
	private const string DefaultBundlesFolder = "changelog/bundles";

	public override string Directive => "changelog";

	public ParserContext Context { get; } = context;

	/// <summary>
	/// The resolved absolute path to the bundles folder.
	/// </summary>
	public string? BundlesFolderPath { get; private set; }

	/// <summary>
	/// The path relative to the documentation source directory.
	/// </summary>
	private string? BundlesFolderRelativeToSource { get; set; }

	/// <summary>
	/// Whether the bundles folder was found and contains bundle files.
	/// </summary>
	public bool Found { get; private set; }

	/// <summary>
	/// Product to source bundles for from the public CDN (the <c>:cdn:</c> option). When set, the
	/// directive fetches <c>{cdnBase}/bundle/{product}/registry.json</c> and the bundles it lists
	/// instead of reading a local folder; any folder argument is ignored.
	/// </summary>
	public string? CdnProduct { get; private set; }

	/// <summary>
	/// Optional single target to render (the <c>:version:</c> option, e.g. <c>9.4.0</c> or a release
	/// date). When set, only the bundle whose target/file matches is rendered; applies to both local
	/// folder and CDN sources. In CDN mode it also limits which bundles are downloaded.
	/// </summary>
	public string? VersionFilter { get; private set; }

	/// <summary>
	/// Loaded and parsed bundles, sorted by version (semver descending).
	/// </summary>
	public IReadOnlyList<LoadedBundle> LoadedBundles { get; private set; } = [];

	/// <summary>
	/// Whether to group entries by area/component within each section.
	/// Defaults to false in order to match CLI behavior.
	/// </summary>
	public bool Subsections { get; private set; }

	/// <summary>
	/// Explicit path to the changelog configuration file, parsed from the :config: option.
	/// If not specified, auto-discovers from docs/changelog.yml or changelog.yml relative to docset root.
	/// </summary>
	public string? ConfigPath { get; private set; }

	/// <summary>
	/// Deprecated. The :product: option is ignored. Product-specific filtering must be done at bundle time via rules.bundle.
	/// </summary>
	public string? ProductId { get; private set; }

	/// <summary>
	/// Always null. The directive no longer applies rules.publish; filtering must be done at bundle time via rules.bundle.
	/// </summary>
	public PublishBlocker? PublishBlocker { get; private set; }

	/// <summary>
	/// The type filter to apply when rendering changelog entries.
	/// Default behavior excludes known issues, breaking changes, and deprecations.
	/// </summary>
	public ChangelogTypeFilter TypeFilter { get; private set; }

	/// <summary>
	/// The entry types that are considered "separated" and excluded by default.
	/// These types are typically shown on their own dedicated pages (e.g., known issues page, breaking changes page).
	/// </summary>
	public static readonly HashSet<ChangelogEntryType> SeparatedTypes =
	[
		ChangelogEntryType.KnownIssue,
		ChangelogEntryType.BreakingChange,
		ChangelogEntryType.Deprecation
	];

	/// <summary>
	/// Repository names that are marked as private in assembler.yml.
	/// Links to these repositories will be hidden (commented out) in the rendered output.
	/// Auto-detected from assembler configuration when available.
	/// </summary>
	public HashSet<string> PrivateRepositories { get; private set; } = [with(StringComparer.OrdinalIgnoreCase)];

	/// <summary>
	/// Feature IDs that should be hidden when rendering changelog entries.
	/// Combined from all loaded bundles' hide-features fields.
	/// Entries with matching feature-id values will be excluded from the output.
	/// </summary>
	public HashSet<string> HideFeatures { get; private set; } = [with(StringComparer.OrdinalIgnoreCase)];

	/// <summary>
	/// How to handle PR/issue links relative to private bundle repos (see :link-visibility: option).
	/// </summary>
	public ChangelogLinkVisibility LinkVisibility { get; private set; }

	/// <summary>
	/// Visibility of changelog record <c>description</c> body text (see :description-visibility: option).
	/// </summary>
	public ChangelogDescriptionVisibility DescriptionVisibility { get; private set; }

	/// <summary>
	/// Whether to render separated types (breaking changes, deprecations, known issues, highlights) as
	/// Myst dropdown sections. When false (default), these types render as flattened bulleted lists.
	/// </summary>
	public bool DropdownsEnabled { get; private set; }

	/// <summary>
	/// When true, renders the bundle <c>release-date</c> field as <c>_Released: …_</c> after the version heading
	/// (see <c>:release-dates:</c> option). Defaults to false, matching <see cref="Subsections"/> and
	/// <see cref="DropdownsEnabled"/>.
	/// </summary>
	public bool ReleaseDatesEnabled { get; private set; }

	/// <summary>
	/// Returns all anchors that will be generated by this directive during rendering.
	/// </summary>
	public override IEnumerable<string> GeneratedAnchors => ComputeGeneratedAnchors();

	/// <summary>
	/// Returns table of contents items for the right-hand navigation.
	/// </summary>
	public IEnumerable<PageTocItem> GeneratedTableOfContent => ComputeTableOfContent();

	public override void FinalizeAndValidate(ParserContext context)
	{
		Subsections = PropBool("subsections");
		ConfigPath = Prop("config");
		var productOpt = Prop("product");
		if (!string.IsNullOrWhiteSpace(productOpt))
			this.EmitWarning("The :product: option is deprecated and has no effect. The directive does not apply rules.publish. Move type/area filtering to rules.bundle so it applies at bundle time.");
		ProductId = productOpt;
		TypeFilter = ParseTypeFilter();
		LoadConfiguration();
		LoadPrivateRepositories();
		LinkVisibility = ParseLinkVisibility();
		DescriptionVisibility = ParseDescriptionVisibility();
		DropdownsEnabled = PropBool("dropdowns");
		ReleaseDatesEnabled = PropBool("release-dates");
		VersionFilter = Prop("version") is { Length: > 0 } v ? v.Trim() : null;

		if (Properties?.ContainsKey("cdn") == true)
		{
			// :cdn: takes an explicit product, or may be valueless to infer the product from the
			// repository that holds the doc (the common case where the repo name is the product id).
			var product = Prop("cdn") is { Length: > 0 } explicitProduct
				? explicitProduct.Trim()
				: InferCdnProductFromRepository();

			if (string.IsNullOrWhiteSpace(product))
			{
				this.EmitError(
					"The :cdn: product could not be inferred from the repository; specify it explicitly, e.g. ':cdn: elasticsearch'.");
				return;
			}

			// Validate before assigning so an invalid product name is never stored on the block.
			if (!IsValidCdnProduct(product))
			{
				this.EmitError($"Invalid :cdn: product '{product}'. Product names must match [a-zA-Z0-9_-]+.");
				return;
			}

			CdnProduct = product;
			LoadCdnBundles(product);
			return;
		}

		ExtractBundlesFolderPath();
		if (Found)
			LoadAndCacheBundles();
	}

	private ChangelogLinkVisibility ParseLinkVisibility()
	{
		var value = Prop("link-visibility");
		if (string.IsNullOrWhiteSpace(value))
			return ChangelogLinkVisibility.Auto;

		return value.ToLowerInvariant() switch
		{
			"auto" => ChangelogLinkVisibility.Auto,
			"keep-links" => ChangelogLinkVisibility.KeepLinks,
			"hide-links" => ChangelogLinkVisibility.HideLinks,
			_ => EmitInvalidLinkVisibilityWarning(value)
		};
	}

	private ChangelogLinkVisibility EmitInvalidLinkVisibilityWarning(string value)
	{
		this.EmitWarning(
			$"Invalid :link-visibility: value '{value}'. Valid values are: auto, keep-links, hide-links. Using auto.");
		return ChangelogLinkVisibility.Auto;
	}

	private ChangelogDescriptionVisibility ParseDescriptionVisibility()
	{
		var value = Prop("description-visibility");
		if (string.IsNullOrWhiteSpace(value))
			return ChangelogDescriptionVisibility.Auto;

		return value.ToLowerInvariant() switch
		{
			"auto" => ChangelogDescriptionVisibility.Auto,
			"keep-descriptions" => ChangelogDescriptionVisibility.KeepDescriptions,
			"hide-descriptions" => ChangelogDescriptionVisibility.HideDescriptions,
			_ => EmitInvalidDescriptionVisibilityWarning(value)
		};
	}

	private ChangelogDescriptionVisibility EmitInvalidDescriptionVisibilityWarning(string value)
	{
		this.EmitWarning(
			$"Invalid :description-visibility: value '{value}'. Valid values are: auto, keep-descriptions, hide-descriptions. Using auto.");
		return ChangelogDescriptionVisibility.Auto;
	}

	/// <summary>
	/// Parses and validates the :type: option.
	/// Valid values: all, breaking-change, deprecation, known-issue, highlight.
	/// If not specified, returns Default (excludes separated types).
	/// </summary>
	private ChangelogTypeFilter ParseTypeFilter()
	{
		var typeValue = Prop("type");
		if (string.IsNullOrWhiteSpace(typeValue))
			return ChangelogTypeFilter.Default;

		return typeValue.ToLowerInvariant() switch
		{
			"all" => ChangelogTypeFilter.All,
			"breaking-change" => ChangelogTypeFilter.BreakingChange,
			"deprecation" => ChangelogTypeFilter.Deprecation,
			"known-issue" => ChangelogTypeFilter.KnownIssue,
			"highlight" => ChangelogTypeFilter.Highlight,
			_ => EmitInvalidTypeFilterWarning(typeValue)
		};
	}

	private ChangelogTypeFilter EmitInvalidTypeFilterWarning(string typeValue)
	{
		this.EmitWarning($"Invalid :type: value '{typeValue}'. Valid values are: all, breaking-change, deprecation, known-issue, highlight. Using default behavior.");
		return ChangelogTypeFilter.Default;
	}

	private void ExtractBundlesFolderPath()
	{
		var folderPath = Arguments;

		if (string.IsNullOrWhiteSpace(folderPath))
			folderPath = DefaultBundlesFolder;

		var trimmedPath = folderPath.TrimStart('/');
		if (Path.IsPathRooted(trimmedPath))
		{
			this.EmitError("Changelog bundles path must not be an absolute path.");
			return;
		}

		BundlesFolderPath = Path.GetFullPath(Build.DocumentationSourceDirectory.ResolvePathFrom(folderPath));
		BundlesFolderRelativeToSource = Path.GetRelativePath(Build.DocumentationSourceDirectory.FullName, BundlesFolderPath);

		var dir = Build.ReadFileSystem.DirectoryInfo.New(BundlesFolderPath);
		if (!dir.IsSubPathOf(Build.DocumentationSourceDirectory))
		{
			this.EmitError("Changelog bundles path must resolve within the documentation source directory.");
			return;
		}

		if (SymlinkValidator.ValidateDirectoryAccess(dir, Build.DocumentationSourceDirectory) is { } accessError)
		{
			this.EmitError(accessError);
			return;
		}

		if (!Build.ReadFileSystem.Directory.Exists(BundlesFolderPath))
		{
			this.EmitError($"Changelog bundles folder `{BundlesFolderRelativeToSource}` does not exist.");
			return;
		}

		var bundles = Build.ReadFileSystem.Directory
			.EnumerateFiles(BundlesFolderPath, "*.yaml")
			.Concat(Build.ReadFileSystem.Directory.EnumerateFiles(BundlesFolderPath, "*.yml"))
			.ToList();

		if (bundles.Count == 0)
		{
			this.EmitError($"Changelog bundles folder `{BundlesFolderRelativeToSource}` contains no YAML files.");
			return;
		}

		Found = true;
	}

	/// <summary>
	/// Loads changelog configuration settings from the config file.
	/// Uses the explicit :config: path if specified, otherwise auto-discovers changelog.yml.
	/// Reserved for future directive-relevant settings.
	/// </summary>
	private void LoadConfiguration() =>
		// Config file resolution is kept so the path validation infrastructure
		// stays exercised; settings are currently handled at bundle time.
		_ = ResolveConfigPath();

	/// <summary>
	/// The trust boundary for changelog config file resolution: checkout (git) root
	/// when available, otherwise the documentation source directory.
	/// Both explicit <c>:config:</c> paths and auto-discovered candidates are validated
	/// against this same root.
	/// </summary>
	private IDirectoryInfo ConfigTrustRoot =>
		Build.DocumentationCheckoutDirectory ?? Build.DocumentationSourceDirectory;

	private string? ResolveConfigPath()
	{
		if (!string.IsNullOrWhiteSpace(ConfigPath))
		{
			// A leading '/' or '\' is treated as relative to docset root
			var trimmedPath = ConfigPath.TrimStart('/', '\\');
			if (Path.IsPathRooted(trimmedPath))
			{
				this.EmitError("Changelog config path must not be an absolute path.");
				return null;
			}

			var explicitPath = Path.GetFullPath(Build.DocumentationSourceDirectory.ResolvePathFrom(trimmedPath));
			return ValidateConfigCandidate(explicitPath, emitDiagnostics: true);
		}

		// Auto-discover: try .yml and .yaml in each candidate location.
		string[] relativePaths =
		[
			"changelog.yml", "changelog.yaml",
			"../changelog.yml", "../changelog.yaml"
		];

		return relativePaths
			.Select(rel => Path.GetFullPath(Build.DocumentationSourceDirectory.ResolvePathFrom(rel)))
			.Select(abs => ValidateConfigCandidate(abs, emitDiagnostics: false))
			.FirstOrDefault(p => p != null);
	}

	/// <summary>
	/// Validates a config file candidate against the shared trust rules:
	/// must be within <see cref="ConfigTrustRoot"/>, must not be/traverse symlinks,
	/// and must exist on the (scoped) filesystem.
	/// </summary>
	private string? ValidateConfigCandidate(string fullPath, bool emitDiagnostics)
	{
		try
		{
			var file = Build.ReadFileSystem.FileInfo.New(fullPath);

			if (!file.IsSubPathOf(ConfigTrustRoot))
			{
				if (emitDiagnostics)
					this.EmitError("Changelog config path must resolve within the documentation directory.");
				return null;
			}

			if (SymlinkValidator.ValidateFileAccess(file, ConfigTrustRoot) is { } accessError)
			{
				if (emitDiagnostics)
					this.EmitError(accessError);
				return null;
			}

			if (!Build.ReadFileSystem.File.Exists(fullPath))
			{
				if (emitDiagnostics)
					this.EmitWarning($"Specified changelog config path '{ConfigPath}' not found.");
				return null;
			}

			return fullPath;
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	/// Loads private repository names from assembler configuration.
	/// Links to private repositories will be hidden in the rendered output.
	/// </summary>
	private void LoadPrivateRepositories()
	{
		try
		{
			// Try to load assembler configuration to get private repositories
			var assemblerConfig = AssemblyConfiguration.Create(Build.ConfigurationFileProvider);
			foreach (var repoName in assemblerConfig.PrivateRepositories.Keys)
				_ = PrivateRepositories.Add(repoName);
		}
		catch
		{
			// If assembler.yml is not available (standalone builds), no repos are private
			// This is expected behavior - we silently continue with empty private repos
		}
	}

	private void LoadAndCacheBundles()
	{
		if (BundlesFolderPath is null)
			return;

		var loader = new BundleLoader(Build.ReadFileSystem);

		// Load bundles using the BundleLoader service
		// Emit errors (not warnings) for missing file references so the build fails fast
		// rather than silently omitting entries from the rendered output.
		var loadedBundles = loader.LoadBundles(
			BundlesFolderPath,
			msg => this.EmitError(msg));

		ApplyLoadedBundles(loadedBundles);
	}

	private void LoadCdnBundles(string product)
	{
		// Product validity is checked by the caller before CdnProduct is assigned.
		if (!string.IsNullOrWhiteSpace(Arguments))
			this.EmitWarning("The bundles folder argument is ignored when :cdn: is set; bundles are sourced from the CDN.");

		// :cdn: is a selector over release notes prefetched at build startup. A product must be declared
		// under `release_notes` in docset.yml; otherwise its bundles were never fetched.
		if (!Context.ReleaseNotesResolver.IsDeclared(product))
		{
			this.EmitError(
				$"The :cdn: product '{product}' is not declared in docset.yml. Add it under 'release_notes:', for example:\n  release_notes:\n    - product: {product}");
			return;
		}

		_ = Context.ReleaseNotesResolver.TryGetBundles(product, out var loadedBundles);
		ApplyLoadedBundles(loadedBundles);
		Found = LoadedBundles.Count > 0;
	}

	private void ApplyLoadedBundles(IReadOnlyList<LoadedBundle> loadedBundles)
	{
		var filteredBundles = FilterByVersion(loadedBundles);

		// Sort by version (descending - newest first)
		// Supports both semver (e.g., "9.3.0") and date-based (e.g., "2025-08-05") versions
		var sortedBundles = filteredBundles
			.OrderByDescending(b => VersionOrDate.Parse(b.Version))
			.ToList();

		// Always merge bundles with the same target version
		// (e.g., Cloud Serverless with multiple repos contributing to a single dated release)
		LoadedBundles = BundleLoader.MergeBundlesByTarget(sortedBundles);

		// Collect hide-features from all loaded bundles
		foreach (var bundle in LoadedBundles)
		{
			foreach (var featureId in bundle.HideFeatures)
				_ = HideFeatures.Add(featureId);
		}
	}

	/// <summary>Filters bundles by the optional <c>:version:</c> value; warns and renders empty when nothing matches.</summary>
	private IReadOnlyList<LoadedBundle> FilterByVersion(IReadOnlyList<LoadedBundle> bundles)
	{
		if (VersionFilter is not { Length: > 0 } version)
			return bundles;

		var matched = bundles
			.Where(b => ChangelogVersionMatch.Matches(version, b.Version, b.FilePath))
			.ToList();

		if (matched.Count == 0 && bundles.Count > 0)
			this.EmitWarning($"No changelog bundle matches :version: '{version}'.");

		return matched;
	}

	private static bool IsValidCdnProduct(string product) =>
		product.Length > 0 && product.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-');

	/// <summary>Infers the CDN product for a valueless <c>:cdn:</c> from the repo, mapped to its canonical id via products.yml.</summary>
	private string? InferCdnProductFromRepository()
	{
		if (!Build.Git.IsAvailable)
			return null;

		var repository = Build.Git.RepositoryName;
		return Build.ProductsConfiguration.GetProductByRepositoryName(repository)?.Id ?? repository;
	}

	private IEnumerable<string> ComputeGeneratedAnchors()
	{
		var dedicatedPage = ChangelogInlineRenderer.IsDedicatedSeparatedTypePage(TypeFilter);

		foreach (var bundle in LoadedBundles)
		{
			if (!BundleContributesToNavigation(bundle))
				continue;

			var titleSlug = ChangelogTextUtilities.TitleToSlug(bundle.Version);
			var anchorSlug = titleSlug.Slugify();
			var repo = bundle.Repo;
			var entriesByType = GetFilteredEntryCounts(bundle);
			var shouldInclude = CreateTypeFilterPredicate();

			if (!dedicatedPage && shouldInclude(ChangelogEntryType.BreakingChange) && entriesByType.ContainsKey(ChangelogEntryType.BreakingChange))
				yield return $"{repo}-{anchorSlug}-breaking-changes";

			if (!dedicatedPage && shouldInclude(ChangelogEntryType.Security) && entriesByType.ContainsKey(ChangelogEntryType.Security))
				yield return $"{repo}-{anchorSlug}-security";

			if (!dedicatedPage && shouldInclude(ChangelogEntryType.KnownIssue) && entriesByType.ContainsKey(ChangelogEntryType.KnownIssue))
				yield return $"{repo}-{anchorSlug}-known-issues";

			if (!dedicatedPage && shouldInclude(ChangelogEntryType.Deprecation) && entriesByType.ContainsKey(ChangelogEntryType.Deprecation))
				yield return $"{repo}-{anchorSlug}-deprecations";

			if (!dedicatedPage && shouldInclude(ChangelogEntryType.Feature) &&
				(entriesByType.ContainsKey(ChangelogEntryType.Feature) ||
				 entriesByType.ContainsKey(ChangelogEntryType.Enhancement)))
				yield return $"{repo}-{anchorSlug}-features-enhancements";

			if (!dedicatedPage && shouldInclude(ChangelogEntryType.BugFix) && entriesByType.ContainsKey(ChangelogEntryType.BugFix))
				yield return $"{repo}-{anchorSlug}-fixes";

			if (!dedicatedPage && shouldInclude(ChangelogEntryType.Docs) && entriesByType.ContainsKey(ChangelogEntryType.Docs))
				yield return $"{repo}-{anchorSlug}-docs";

			if (!dedicatedPage && shouldInclude(ChangelogEntryType.Regression) && entriesByType.ContainsKey(ChangelogEntryType.Regression))
				yield return $"{repo}-{anchorSlug}-regressions";

			if (!dedicatedPage && shouldInclude(ChangelogEntryType.Other) && entriesByType.ContainsKey(ChangelogEntryType.Other))
				yield return $"{repo}-{anchorSlug}-other";
		}
	}

	/// <summary>
	/// Creates a predicate that returns true if the given entry type should be included based on the TypeFilter.
	/// </summary>
	private Func<ChangelogEntryType, bool> CreateTypeFilterPredicate() =>
		TypeFilter switch
		{
			ChangelogTypeFilter.All => _ => true,
			ChangelogTypeFilter.BreakingChange => type => type == ChangelogEntryType.BreakingChange,
			ChangelogTypeFilter.Deprecation => type => type == ChangelogEntryType.Deprecation,
			ChangelogTypeFilter.KnownIssue => type => type == ChangelogEntryType.KnownIssue,
			_ => type => !SeparatedTypes.Contains(type) // Default: exclude separated types
		};

	/// <summary>
	/// Returns entry counts by type after applying publish blocker, hide-features, and type filters.
	/// This ensures the TOC and generated anchors match what the renderer actually outputs.
	/// </summary>
	private Dictionary<ChangelogEntryType, int> GetFilteredEntryCounts(LoadedBundle bundle) =>
		ChangelogInlineRenderer.GetFilteredEntries(bundle, PublishBlocker, HideFeatures, TypeFilter)
			.GroupBy(e => e.Type)
			.ToDictionary(g => g.Key, g => g.Count());

	private bool BundleContributesToNavigation(LoadedBundle bundle) =>
		ChangelogInlineRenderer.BundleHasRenderableEntries(bundle, PublishBlocker, HideFeatures, TypeFilter)
		|| ChangelogInlineRenderer.ShouldRenderEmptyBundleMetadata(TypeFilter, bundle.Data?.Description);

	private IEnumerable<PageTocItem> ComputeTableOfContent()
	{
		var dedicatedPage = ChangelogInlineRenderer.IsDedicatedSeparatedTypePage(TypeFilter);

		foreach (var bundle in LoadedBundles)
		{
			if (!BundleContributesToNavigation(bundle))
				continue;

			var titleSlug = ChangelogTextUtilities.TitleToSlug(bundle.Version);
			var anchorSlug = titleSlug.Slugify();
			var repo = bundle.Repo;
			var displayVersion = VersionOrDate.FormatDisplayVersion(bundle.Version);

			yield return new PageTocItem
			{
				Heading = displayVersion,
				Slug = displayVersion.Slugify(),
				Level = 2
			};

			if (dedicatedPage || TypeFilter == ChangelogTypeFilter.Highlight)
				continue;

			var entriesByType = GetFilteredEntryCounts(bundle);
			var shouldInclude = CreateTypeFilterPredicate();

			if (shouldInclude(ChangelogEntryType.BreakingChange) && entriesByType.ContainsKey(ChangelogEntryType.BreakingChange))
				yield return new PageTocItem
				{
					Heading = "Breaking changes",
					Slug = $"{repo}-{anchorSlug}-breaking-changes",
					Level = 3
				};

			var hasHighlights = ChangelogInlineRenderer.GetFilteredEntries(bundle, PublishBlocker, HideFeatures, TypeFilter)
				.Any(e => e.Highlight == true);
			if (hasHighlights && TypeFilter == ChangelogTypeFilter.All)
				yield return new PageTocItem
				{
					Heading = "Highlights",
					Slug = $"{repo}-{anchorSlug}-highlights",
					Level = 3
				};

			if (shouldInclude(ChangelogEntryType.Security) && entriesByType.ContainsKey(ChangelogEntryType.Security))
				yield return new PageTocItem
				{
					Heading = "Security",
					Slug = $"{repo}-{anchorSlug}-security",
					Level = 3
				};

			if (shouldInclude(ChangelogEntryType.KnownIssue) && entriesByType.ContainsKey(ChangelogEntryType.KnownIssue))
				yield return new PageTocItem
				{
					Heading = "Known issues",
					Slug = $"{repo}-{anchorSlug}-known-issues",
					Level = 3
				};

			if (shouldInclude(ChangelogEntryType.Deprecation) && entriesByType.ContainsKey(ChangelogEntryType.Deprecation))
				yield return new PageTocItem
				{
					Heading = "Deprecations",
					Slug = $"{repo}-{anchorSlug}-deprecations",
					Level = 3
				};

			if (shouldInclude(ChangelogEntryType.Feature) &&
				(entriesByType.ContainsKey(ChangelogEntryType.Feature) ||
				 entriesByType.ContainsKey(ChangelogEntryType.Enhancement)))
				yield return new PageTocItem
				{
					Heading = "Features and enhancements",
					Slug = $"{repo}-{anchorSlug}-features-enhancements",
					Level = 3
				};

			if (shouldInclude(ChangelogEntryType.BugFix) && entriesByType.ContainsKey(ChangelogEntryType.BugFix))
				yield return new PageTocItem
				{
					Heading = "Fixes",
					Slug = $"{repo}-{anchorSlug}-fixes",
					Level = 3
				};

			if (shouldInclude(ChangelogEntryType.Docs) && entriesByType.ContainsKey(ChangelogEntryType.Docs))
				yield return new PageTocItem
				{
					Heading = "Documentation",
					Slug = $"{repo}-{anchorSlug}-docs",
					Level = 3
				};

			if (shouldInclude(ChangelogEntryType.Regression) && entriesByType.ContainsKey(ChangelogEntryType.Regression))
				yield return new PageTocItem
				{
					Heading = "Regressions",
					Slug = $"{repo}-{anchorSlug}-regressions",
					Level = 3
				};

			if (shouldInclude(ChangelogEntryType.Other) && entriesByType.ContainsKey(ChangelogEntryType.Other))
				yield return new PageTocItem
				{
					Heading = "Other changes",
					Slug = $"{repo}-{anchorSlug}-other",
					Level = 3
				};
		}
	}
}
