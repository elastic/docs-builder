// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.ReleaseNotes;
using Elastic.Markdown.Diagnostics;

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
	KnownIssue
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
	/// Product ID for product-specific publish blocker configuration.
	/// Resolution order:
	/// 1. Explicit :product: option if specified
	/// 2. Docset's single product ID (when exactly one product is configured)
	/// 3. Falls back to global block.publish if no product can be determined
	/// </summary>
	public string? ProductId { get; private set; }

	/// <summary>
	/// The loaded publish blocker configuration used to filter entries.
	/// If null, no publish filtering is applied.
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
	public HashSet<string> PrivateRepositories { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Feature IDs that should be hidden when rendering changelog entries.
	/// Combined from all loaded bundles' hide-features fields.
	/// Entries with matching feature-id values will be excluded from the output.
	/// </summary>
	public HashSet<string> HideFeatures { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

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
		ExtractBundlesFolderPath();
		Subsections = PropBool("subsections");
		ConfigPath = Prop("config");
		ProductId = Prop("product");
		TypeFilter = ParseTypeFilter();
		LoadConfiguration();
		LoadPrivateRepositories();
		if (Found)
			LoadAndCacheBundles();
	}

	/// <summary>
	/// Parses and validates the :type: option.
	/// Valid values: all, breaking-change, deprecation, known-issue.
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
			_ => EmitInvalidTypeFilterWarning(typeValue)
		};
	}

	private ChangelogTypeFilter EmitInvalidTypeFilterWarning(string typeValue)
	{
		this.EmitWarning($"Invalid :type: value '{typeValue}'. Valid values are: all, breaking-change, deprecation, known-issue. Using default behavior.");
		return ChangelogTypeFilter.Default;
	}

	private void ExtractBundlesFolderPath()
	{
		var folderPath = Arguments;

		if (string.IsNullOrWhiteSpace(folderPath))
			folderPath = DefaultBundlesFolder;

		BundlesFolderPath = Build.DocumentationSourceDirectory.ResolvePathFrom(folderPath);
		BundlesFolderRelativeToSource = Path.GetRelativePath(Build.DocumentationSourceDirectory.FullName, BundlesFolderPath);

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
	/// Loads the changelog configuration to extract publish blockers.
	/// Attempts to load from:
	/// 1. Explicit :config: path if specified
	/// 2. changelog.yml in the docset root
	/// 3. docs/changelog.yml relative to docset root
	/// </summary>
	private void LoadConfiguration()
	{
		var fileSystem = Build.ReadFileSystem;
		string? configPath = null;

		// Try explicit config path first
		if (!string.IsNullOrWhiteSpace(ConfigPath))
		{
			var explicitPath = Build.DocumentationSourceDirectory.ResolvePathFrom(ConfigPath);

			if (fileSystem.File.Exists(explicitPath))
				configPath = explicitPath;
			else
				this.EmitWarning($"Specified changelog config path '{ConfigPath}' not found.");
		}
		else
		{
			// Auto-discover: try changelog.yml first, then docs/changelog.yml
			var changelogYml = Build.DocumentationSourceDirectory.ResolvePathFrom("changelog.yml");
			var docsChangelogYml = Build.DocumentationSourceDirectory.ResolvePathFrom("docs/changelog.yml");

			if (fileSystem.File.Exists(changelogYml))
				configPath = changelogYml;
			else if (fileSystem.File.Exists(docsChangelogYml))
				configPath = docsChangelogYml;
		}

		if (string.IsNullOrWhiteSpace(configPath))
			return;

		// Resolve product ID: explicit option > single docset product > null (global fallback)
		var resolvedProductId = ResolveProductId();

		PublishBlocker = ReleaseNotesSerialization.LoadPublishBlocker(fileSystem, configPath, resolvedProductId);
	}

	/// <summary>
	/// Resolves the product ID for publish blocker lookup.
	/// Priority: explicit :product: option > single docset product > null.
	/// </summary>
	private string? ResolveProductId()
	{
		// Use explicit :product: option if specified
		if (!string.IsNullOrWhiteSpace(ProductId))
			return ProductId;

		// Fall back to docset's single product if available
		var docsetProducts = Context.Configuration.Products;
		return docsetProducts.Count == 1 ? docsetProducts.First().Id :
			// No product could be determined - will use global blocker
			null;
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
		var loadedBundles = loader.LoadBundles(
			BundlesFolderPath,
			msg => this.EmitWarning(msg));

		// Sort by version (descending - newest first)
		// Supports both semver (e.g., "9.3.0") and date-based (e.g., "2025-08-05") versions
		var sortedBundles = loadedBundles
			.OrderByDescending(b => VersionOrDate.Parse(b.Version))
			.ToList();

		// Always merge bundles with the same target version
		// (e.g., Cloud Serverless with multiple repos contributing to a single dated release)
		LoadedBundles = loader.MergeBundlesByTarget(sortedBundles);

		// Collect hide-features from all loaded bundles
		foreach (var bundle in LoadedBundles)
		{
			foreach (var featureId in bundle.HideFeatures)
				_ = HideFeatures.Add(featureId);
		}
	}

	private IEnumerable<string> ComputeGeneratedAnchors()
	{
		foreach (var bundle in LoadedBundles)
		{
			var titleSlug = ChangelogTextUtilities.TitleToSlug(bundle.Version);
			var repo = bundle.Repo;

			// Group entries by type to determine which sections will exist
			var entriesByType = bundle.Entries
				.GroupBy(e => e.Type)
				.ToDictionary(g => g.Key, g => g.Count());

			// Apply type filter to determine which sections to include
			var shouldInclude = CreateTypeFilterPredicate();

			// Critical sections
			if (shouldInclude(ChangelogEntryType.BreakingChange) && entriesByType.ContainsKey(ChangelogEntryType.BreakingChange))
				yield return $"{repo}-{titleSlug}-breaking-changes";

			if (shouldInclude(ChangelogEntryType.Security) && entriesByType.ContainsKey(ChangelogEntryType.Security))
				yield return $"{repo}-{titleSlug}-security";

			if (shouldInclude(ChangelogEntryType.KnownIssue) && entriesByType.ContainsKey(ChangelogEntryType.KnownIssue))
				yield return $"{repo}-{titleSlug}-known-issues";

			if (shouldInclude(ChangelogEntryType.Deprecation) && entriesByType.ContainsKey(ChangelogEntryType.Deprecation))
				yield return $"{repo}-{titleSlug}-deprecations";

			// Features and enhancements section
			if (shouldInclude(ChangelogEntryType.Feature) &&
				(entriesByType.ContainsKey(ChangelogEntryType.Feature) ||
				 entriesByType.ContainsKey(ChangelogEntryType.Enhancement)))
				yield return $"{repo}-{titleSlug}-features-enhancements";

			// Fixes section (bug fixes only, security is separate)
			if (shouldInclude(ChangelogEntryType.BugFix) && entriesByType.ContainsKey(ChangelogEntryType.BugFix))
				yield return $"{repo}-{titleSlug}-fixes";

			// Documentation section
			if (shouldInclude(ChangelogEntryType.Docs) && entriesByType.ContainsKey(ChangelogEntryType.Docs))
				yield return $"{repo}-{titleSlug}-docs";

			// Regressions section
			if (shouldInclude(ChangelogEntryType.Regression) && entriesByType.ContainsKey(ChangelogEntryType.Regression))
				yield return $"{repo}-{titleSlug}-regressions";

			// Other changes section
			if (shouldInclude(ChangelogEntryType.Other) && entriesByType.ContainsKey(ChangelogEntryType.Other))
				yield return $"{repo}-{titleSlug}-other";
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

	private IEnumerable<PageTocItem> ComputeTableOfContent()
	{
		foreach (var bundle in LoadedBundles)
		{
			var titleSlug = ChangelogTextUtilities.TitleToSlug(bundle.Version);
			var repo = bundle.Repo;

			// Version header
			yield return new PageTocItem
			{
				Heading = bundle.Version,
				Slug = titleSlug,
				Level = 2
			};

			// Group entries by type to determine which sections will exist
			var entriesByType = bundle.Entries
				.GroupBy(e => e.Type)
				.ToDictionary(g => g.Key, g => g.Count());

			// Apply type filter to determine which sections to include
			var shouldInclude = CreateTypeFilterPredicate();

			// Critical sections first (new ordering) - all at h3 level (children of version)
			if (shouldInclude(ChangelogEntryType.BreakingChange) && entriesByType.ContainsKey(ChangelogEntryType.BreakingChange))
				yield return new PageTocItem
				{
					Heading = "Breaking changes",
					Slug = $"{repo}-{titleSlug}-breaking-changes",
					Level = 3
				};

			if (shouldInclude(ChangelogEntryType.Security) && entriesByType.ContainsKey(ChangelogEntryType.Security))
				yield return new PageTocItem
				{
					Heading = "Security",
					Slug = $"{repo}-{titleSlug}-security",
					Level = 3
				};

			if (shouldInclude(ChangelogEntryType.KnownIssue) && entriesByType.ContainsKey(ChangelogEntryType.KnownIssue))
				yield return new PageTocItem
				{
					Heading = "Known issues",
					Slug = $"{repo}-{titleSlug}-known-issues",
					Level = 3
				};

			if (shouldInclude(ChangelogEntryType.Deprecation) && entriesByType.ContainsKey(ChangelogEntryType.Deprecation))
				yield return new PageTocItem
				{
					Heading = "Deprecations",
					Slug = $"{repo}-{titleSlug}-deprecations",
					Level = 3
				};

			// Features and enhancements section
			if (shouldInclude(ChangelogEntryType.Feature) &&
				(entriesByType.ContainsKey(ChangelogEntryType.Feature) ||
				 entriesByType.ContainsKey(ChangelogEntryType.Enhancement)))
				yield return new PageTocItem
				{
					Heading = "Features and enhancements",
					Slug = $"{repo}-{titleSlug}-features-enhancements",
					Level = 3
				};

			// Fixes section (bug fixes only, security is separate)
			if (shouldInclude(ChangelogEntryType.BugFix) && entriesByType.ContainsKey(ChangelogEntryType.BugFix))
				yield return new PageTocItem
				{
					Heading = "Fixes",
					Slug = $"{repo}-{titleSlug}-fixes",
					Level = 3
				};

			// Documentation section
			if (shouldInclude(ChangelogEntryType.Docs) && entriesByType.ContainsKey(ChangelogEntryType.Docs))
				yield return new PageTocItem
				{
					Heading = "Documentation",
					Slug = $"{repo}-{titleSlug}-docs",
					Level = 3
				};

			// Regressions section
			if (shouldInclude(ChangelogEntryType.Regression) && entriesByType.ContainsKey(ChangelogEntryType.Regression))
				yield return new PageTocItem
				{
					Heading = "Regressions",
					Slug = $"{repo}-{titleSlug}-regressions",
					Level = 3
				};

			// Other changes section
			if (shouldInclude(ChangelogEntryType.Other) && entriesByType.ContainsKey(ChangelogEntryType.Other))
				yield return new PageTocItem
				{
					Heading = "Other changes",
					Slug = $"{repo}-{titleSlug}-other",
					Level = 3
				};
		}
	}
}
