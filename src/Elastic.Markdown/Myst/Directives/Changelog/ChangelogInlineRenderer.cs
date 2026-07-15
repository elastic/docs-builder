// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text;
using Elastic.Documentation;
using Elastic.Documentation.ReleaseNotes;
using Elastic.Documentation.Versions;

namespace Elastic.Markdown.Myst.Directives.Changelog;

/// <summary>
/// Renders changelog bundles as inline markdown content for the {changelog} directive.
/// Uses pre-loaded and cached bundle data from <see cref="ChangelogBlock"/>.
/// </summary>
public static class ChangelogInlineRenderer
{
	public static string? RenderChangelogMarkdown(ChangelogBlock block)
	{
		if (!block.Found || block.LoadedBundles.Count == 0)
			return "_No changelog entries._";

		var sb = new StringBuilder();
		var options = new ChangelogRenderOptions
		{
			Subsections = block.Subsections,
			DropdownsEnabled = block.DropdownsEnabled,
			ReleaseDatesEnabled = block.ReleaseDatesEnabled,
			TypeFilter = block.TypeFilter,
			LinkVisibility = block.LinkVisibility,
			DescriptionVisibility = block.DescriptionVisibility,
			PrivateRepositories = block.PrivateRepositories,
			HideFeatures = block.HideFeatures,
			PublishBlocker = block.PublishBlocker
		};

		// Render each bundle as a version section (already sorted by semver descending)
		var isFirst = true;
		foreach (var bundle in block.LoadedBundles)
		{
			var bundleMarkdown = RenderSingleBundle(bundle, options);

			if (string.IsNullOrWhiteSpace(bundleMarkdown))
				continue;

			if (!isFirst)
				_ = sb.AppendLine();

			_ = sb.Append(bundleMarkdown);
			isFirst = false;
		}

		return sb.Length == 0 ? null : sb.ToString();
	}

	/// <summary>
	/// True when the directive filters to a single separated type page (deprecations, breaking changes, etc.).
	/// </summary>
	public static bool IsDedicatedSeparatedTypePage(ChangelogTypeFilter typeFilter) =>
		typeFilter is ChangelogTypeFilter.BreakingChange
			or ChangelogTypeFilter.Deprecation
			or ChangelogTypeFilter.KnownIssue
			or ChangelogTypeFilter.Highlight;

	/// <summary>
	/// True when <paramref name="typeFilter"/> is <see cref="ChangelogTypeFilter.All"/> or default.
	/// </summary>
	public static bool IsGeneralReleaseNotesPage(ChangelogTypeFilter typeFilter) =>
		typeFilter is ChangelogTypeFilter.All or ChangelogTypeFilter.Default;

	/// <summary>
	/// Returns filtered changelog entries for a bundle using the same filters as rendering.
	/// </summary>
	public static IReadOnlyList<ChangelogEntry> GetFilteredEntries(
		LoadedBundle bundle,
		PublishBlocker? publishBlocker,
		HashSet<string> hideFeatures,
		ChangelogTypeFilter typeFilter)
	{
		var entries = FilterEntries(bundle.Entries, publishBlocker);
		entries = FilterEntriesByHideFeatures(entries, hideFeatures);
		return FilterEntriesByType(entries, typeFilter);
	}

	/// <summary>
	/// True when the bundle would produce visible entry content after filtering.
	/// </summary>
	public static bool BundleHasRenderableEntries(
		LoadedBundle bundle,
		PublishBlocker? publishBlocker,
		HashSet<string> hideFeatures,
		ChangelogTypeFilter typeFilter) =>
		GetFilteredEntries(bundle, publishBlocker, hideFeatures, typeFilter).Count > 0;

	/// <summary>
	/// True when an empty bundle should still render a version block for bundle-level <c>description</c> only.
	/// <c>release-date</c> alone does not preserve an otherwise empty release.
	/// </summary>
	public static bool ShouldRenderEmptyBundleMetadata(ChangelogTypeFilter typeFilter, string? description) =>
		IsGeneralReleaseNotesPage(typeFilter) && !string.IsNullOrEmpty(description);

	private static string RenderSingleBundle(LoadedBundle bundle, ChangelogRenderOptions options)
	{
		var titleSlug = ChangelogTextUtilities.TitleToSlug(bundle.Version);

		// Filter entries based on publish blocker configuration
		var filteredEntries = FilterEntries(bundle.Entries, options.PublishBlocker);

		// Filter entries based on hide-features (from bundle metadata)
		filteredEntries = FilterEntriesByHideFeatures(filteredEntries, options.HideFeatures);

		// Apply type filter
		filteredEntries = FilterEntriesByType(filteredEntries, options.TypeFilter);

		// Group entries by type
		var entriesByType = filteredEntries
			.GroupBy(e => e.Type)
			.ToDictionary(g => g.Key, g => g.ToList());

		var hideLinks = options.LinkVisibility switch
		{
			ChangelogLinkVisibility.KeepLinks => false,
			ChangelogLinkVisibility.HideLinks => true,
			_ => ShouldHideLinksForRepo(bundle.Repo, options.PrivateRepositories)
		};

		var hideEntryDescriptions = ShouldHideEntryDescriptionsForRepo(bundle.Repo, options.PrivateRepositories, options.DescriptionVisibility);

		var model = new BundleRenderModel
		{
			Title = VersionOrDate.FormatDisplayVersion(bundle.Version),
			TitleSlug = titleSlug,
			Repo = bundle.Repo,
			Owner = bundle.Owner,
			EntriesByType = entriesByType,
			HideLinks = hideLinks,
			HideEntryDescriptions = hideEntryDescriptions,
			Description = bundle.Data?.Description,
			ReleaseDate = options.ReleaseDatesEnabled ? bundle.Data?.ReleaseDate : null
		};

		return GenerateMarkdown(model, options);
	}

	/// <summary>
	/// Filters entries based on the type filter.
	/// </summary>
	private static IReadOnlyList<ChangelogEntry> FilterEntriesByType(
		IReadOnlyList<ChangelogEntry> entries,
		ChangelogTypeFilter typeFilter) => typeFilter switch
		{
			ChangelogTypeFilter.All => entries,
			ChangelogTypeFilter.BreakingChange => entries.Where(e => e.Type == ChangelogEntryType.BreakingChange).ToList(),
			ChangelogTypeFilter.Deprecation => entries.Where(e => e.Type == ChangelogEntryType.Deprecation).ToList(),
			ChangelogTypeFilter.KnownIssue => entries.Where(e => e.Type == ChangelogEntryType.KnownIssue).ToList(),
			ChangelogTypeFilter.Highlight => entries.Where(e => e.Highlight == true).ToList(),
			_ => entries.Where(e => !ChangelogBlock.SeparatedTypes.Contains(e.Type)).ToList() // Default: exclude separated types
		};

	/// <summary>
	/// Filters entries based on hide-features configuration from bundle metadata.
	/// Entries with matching feature-id values are excluded from the output.
	/// </summary>
	private static IReadOnlyList<ChangelogEntry> FilterEntriesByHideFeatures(
		IReadOnlyList<ChangelogEntry> entries,
		HashSet<string> hideFeatures)
	{
		if (hideFeatures.Count == 0)
			return entries;

		return entries
			.Where(e => string.IsNullOrWhiteSpace(e.FeatureId) || !hideFeatures.Contains(e.FeatureId))
			.ToList();
	}

	/// <summary>
	/// Determines if links should be hidden for a bundle based on its repository.
	/// For merged bundles (e.g., "elasticsearch+kibana+private-repo"), returns true
	/// if ANY component repository is in the private repositories set.
	/// </summary>
	public static bool ShouldHideLinksForRepo(string bundleRepo, HashSet<string> privateRepositories)
	{
		if (privateRepositories.Count == 0)
			return false;

		return HasAnyPrivateRepoConstituent(bundleRepo, privateRepositories);
	}

	/// <summary>
	/// When true, changelog entry YAML <c>description</c> bodies (bullet text and dropdown intro) must not be rendered.
	/// </summary>
	public static bool ShouldHideEntryDescriptionsForRepo(
		string bundleRepo,
		HashSet<string> privateRepositories,
		ChangelogDescriptionVisibility visibility) =>
		visibility switch
		{
			ChangelogDescriptionVisibility.HideDescriptions => true,
			ChangelogDescriptionVisibility.KeepDescriptions => false,
			ChangelogDescriptionVisibility.Auto => !HasAnyPrivateRepoConstituent(bundleRepo, privateRepositories),
			_ => !HasAnyPrivateRepoConstituent(bundleRepo, privateRepositories)
		};

	/// <summary>
	/// True when merged <paramref name="bundleRepo"/> (<c>elasticsearch+kibana</c>-style) has at least one
	/// component listed as private for the build.
	/// </summary>
	public static bool HasAnyPrivateRepoConstituent(string bundleRepo, HashSet<string> privateRepositories)
	{
		if (privateRepositories.Count == 0)
			return false;

		var repos = bundleRepo.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		return repos.Any(privateRepositories.Contains);
	}

	/// <summary>
	/// Filters entries based on publish blocker configuration.
	/// </summary>
	private static IReadOnlyList<ChangelogEntry> FilterEntries(
		IReadOnlyList<ChangelogEntry> entries,
		PublishBlocker? publishBlocker)
	{
		if (publishBlocker is not { HasBlockingRules: true })
			return entries;

		return entries.Where(e => !publishBlocker.ShouldBlock(e)).ToList();
	}

	private static string GenerateMarkdown(BundleRenderModel model, ChangelogRenderOptions options)
	{
		var title = model.Title;
		var titleSlug = model.TitleSlug;
		var repo = model.Repo;
		var owner = model.Owner;
		var entriesByType = model.EntriesByType;
		var subsections = options.Subsections;
		var hideLinks = model.HideLinks;
		var hideEntryDescriptions = model.HideEntryDescriptions;
		var dropdownsEnabled = options.DropdownsEnabled;
		var typeFilter = options.TypeFilter;
		var publishBlocker = options.PublishBlocker;
		var description = model.Description;
		var releaseDate = model.ReleaseDate;

		var sb = new StringBuilder();
		var dedicatedPage = IsDedicatedSeparatedTypePage(typeFilter);

		// Get entries by category
		var features = entriesByType.GetValueOrDefault(ChangelogEntryType.Feature, []);
		var enhancements = entriesByType.GetValueOrDefault(ChangelogEntryType.Enhancement, []);
		var security = entriesByType.GetValueOrDefault(ChangelogEntryType.Security, []);
		var bugFixes = entriesByType.GetValueOrDefault(ChangelogEntryType.BugFix, []);
		var docs = entriesByType.GetValueOrDefault(ChangelogEntryType.Docs, []);
		var regressions = entriesByType.GetValueOrDefault(ChangelogEntryType.Regression, []);
		var other = entriesByType.GetValueOrDefault(ChangelogEntryType.Other, []);
		var breakingChanges = entriesByType.GetValueOrDefault(ChangelogEntryType.BreakingChange, []);
		var deprecations = entriesByType.GetValueOrDefault(ChangelogEntryType.Deprecation, []);
		var knownIssues = entriesByType.GetValueOrDefault(ChangelogEntryType.KnownIssue, []);

		// Get highlighted entries from all types
		var highlights = entriesByType.Values
			.SelectMany(e => e)
			.Where(e => e.Highlight == true)
			.ToList();

		// Check if we have any content at all
		var hasAnyContent = features.Count > 0 || enhancements.Count > 0 || security.Count > 0 ||
							bugFixes.Count > 0 || docs.Count > 0 || regressions.Count > 0 || other.Count > 0 ||
							breakingChanges.Count > 0 || deprecations.Count > 0 || knownIssues.Count > 0 ||
							highlights.Count > 0;

		if (!hasAnyContent)
		{
			if (ShouldRenderEmptyBundleMetadata(typeFilter, description))
			{
				AppendVersionHeader(sb, title, description, releaseDate);
				return sb.ToString();
			}

			return string.Empty;
		}

		AppendVersionHeader(sb, title, description, releaseDate);

		// Special case: When filtering by highlight, render only highlights without type-based sections
		if (typeFilter == ChangelogTypeFilter.Highlight)
		{
			if (highlights.Count > 0)
			{
				_ = sb.AppendLine();
				RenderSeparatedTypeEntries(
					sb, highlights, repo, owner, subsections, dropdownsEnabled, groupBySubtype: false,
					hideLinks, hideEntryDescriptions, publishBlocker);
			}
			return sb.ToString();
		}

		if (breakingChanges.Count > 0)
		{
			AppendSectionHeader(sb, dedicatedPage, $"### Breaking changes [{repo}-{titleSlug}-breaking-changes]");
			if (dropdownsEnabled)
				RenderDetailedEntries(sb, breakingChanges, repo, owner, groupBySubtype: true, hideLinks, hideEntryDescriptions, publishBlocker);
			else
				RenderDetailedEntriesFlattened(sb, breakingChanges, repo, owner, groupBySubtype: true, hideLinks, hideEntryDescriptions);
		}

		if (highlights.Count > 0 && typeFilter == ChangelogTypeFilter.All)
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Highlights [{repo}-{titleSlug}-highlights]");
			if (dropdownsEnabled)
				RenderDetailedEntries(sb, highlights, repo, owner, groupBySubtype: false, hideLinks, hideEntryDescriptions, publishBlocker);
			else
				RenderDetailedEntriesFlattened(sb, highlights, repo, owner, groupBySubtype: false, hideLinks, hideEntryDescriptions);
		}

		if (security.Count > 0)
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Security [{repo}-{titleSlug}-security]");
			RenderEntriesByArea(sb, security, repo, owner, subsections, hideLinks, hideEntryDescriptions, publishBlocker);
		}

		if (knownIssues.Count > 0)
		{
			AppendSectionHeader(sb, dedicatedPage, $"### Known issues [{repo}-{titleSlug}-known-issues]");
			RenderSeparatedTypeEntries(
				sb, knownIssues, repo, owner, subsections, dropdownsEnabled, groupBySubtype: false,
				hideLinks, hideEntryDescriptions, publishBlocker);
		}

		if (deprecations.Count > 0)
		{
			AppendSectionHeader(sb, dedicatedPage, $"### Deprecations [{repo}-{titleSlug}-deprecations]");
			RenderSeparatedTypeEntries(
				sb, deprecations, repo, owner, subsections, dropdownsEnabled, groupBySubtype: false,
				hideLinks, hideEntryDescriptions, publishBlocker);
		}

		if (features.Count > 0 || enhancements.Count > 0)
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Features and enhancements [{repo}-{titleSlug}-features-enhancements]");
			var combined = features.Concat(enhancements).ToList();
			RenderEntriesByArea(sb, combined, repo, owner, subsections, hideLinks, hideEntryDescriptions, publishBlocker);
		}

		if (bugFixes.Count > 0)
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Fixes [{repo}-{titleSlug}-fixes]");
			RenderEntriesByArea(sb, bugFixes, repo, owner, subsections, hideLinks, hideEntryDescriptions, publishBlocker);
		}

		if (docs.Count > 0)
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Documentation [{repo}-{titleSlug}-docs]");
			RenderEntriesByArea(sb, docs, repo, owner, subsections, hideLinks, hideEntryDescriptions, publishBlocker);
		}

		if (regressions.Count > 0)
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Regressions [{repo}-{titleSlug}-regressions]");
			RenderEntriesByArea(sb, regressions, repo, owner, subsections, hideLinks, hideEntryDescriptions, publishBlocker);
		}

		if (other.Count > 0)
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Other changes [{repo}-{titleSlug}-other]");
			RenderEntriesByArea(sb, other, repo, owner, subsections, hideLinks, hideEntryDescriptions, publishBlocker);
		}

		return sb.ToString();
	}

	private static void RenderEntriesByArea(
		StringBuilder sb,
		List<ChangelogEntry> entries,
		string repo,
		string owner,
		bool subsections,
		bool hideLinks,
		bool hideEntryDescriptions,
		PublishBlocker? publishBlocker)
	{
		if (subsections)
		{
			// Group by area and sort when subsections is enabled
			var groupedByArea = entries.GroupBy(e => publishBlocker.GetPreferredArea(e)).OrderBy(g => g.Key).ToList();

			foreach (var areaGroup in groupedByArea)
			{
				if (!string.IsNullOrWhiteSpace(areaGroup.Key))
				{
					var header = ChangelogTextUtilities.FormatAreaHeader(areaGroup.Key);
					_ = sb.AppendLine();
					_ = sb.AppendLine(CultureInfo.InvariantCulture, $"**{header}**");
				}

				foreach (var entry in areaGroup)
					RenderSingleEntry(sb, entry, repo, owner, hideLinks, hideEntryDescriptions);
			}
		}
		else
		{
			foreach (var entry in entries)
				RenderSingleEntry(sb, entry, repo, owner, hideLinks, hideEntryDescriptions);
		}
	}

	private static void RenderSingleEntry(StringBuilder sb, ChangelogEntry entry, string repo, string owner, bool hideLinks, bool hideEntryDescriptions)
	{
		_ = sb.Append("* ");
		_ = sb.Append(ChangelogTextUtilities.Beautify(entry.Title));

		RenderEntryLinks(sb, entry, repo, owner, hideLinks);

		if (hideEntryDescriptions || string.IsNullOrWhiteSpace(entry.Description))
			return;

		_ = sb.AppendLine();
		var indented = ChangelogTextUtilities.Indent(entry.Description);
		_ = sb.AppendLine(indented);
	}

	private static void RenderEntryLinks(StringBuilder sb, ChangelogEntry entry, string repo, string owner, bool hideLinks)
	{
		if (hideLinks)
		{
			_ = sb.AppendLine();
			foreach (var pr in entry.Prs ?? [])
			{
				_ = sb.Append("  ");
				_ = sb.AppendLine(ChangelogTextUtilities.FormatPrLink(pr, repo, hidePrivateLinks: true, owner));
			}
			foreach (var issue in entry.Issues ?? [])
			{
				_ = sb.Append("  ");
				_ = sb.AppendLine(ChangelogTextUtilities.FormatIssueLink(issue, repo, hidePrivateLinks: true, owner));
			}
			return;
		}

		_ = sb.Append(' ');
		foreach (var pr in entry.Prs ?? [])
		{
			_ = sb.Append(ChangelogTextUtilities.FormatPrLink(pr, repo, hidePrivateLinks: false, owner));
			_ = sb.Append(' ');
		}
		foreach (var issue in entry.Issues ?? [])
		{
			_ = sb.Append(ChangelogTextUtilities.FormatIssueLink(issue, repo, hidePrivateLinks: false, owner));
			_ = sb.Append(' ');
		}
		_ = sb.AppendLine();
	}

	private static void RenderDetailedEntries(
		StringBuilder sb,
		List<ChangelogEntry> entries,
		string repo,
		string owner,
		bool groupBySubtype,
		bool hideLinks,
		bool hideEntryDescriptions,
		PublishBlocker? publishBlocker)
	{
		var grouped = groupBySubtype
			? entries.GroupBy(e => e.Subtype?.ToStringFast(true) ?? string.Empty).OrderBy(g => g.Key).ToList()
			: entries.GroupBy(e => publishBlocker.GetPreferredArea(e)).OrderBy(g => g.Key).ToList();

		foreach (var group in grouped)
		{
			if (!string.IsNullOrWhiteSpace(group.Key))
			{
				var header = groupBySubtype
					? ChangelogTextUtilities.FormatSubtypeHeader(group.Key)
					: ChangelogTextUtilities.FormatAreaHeader(group.Key);
				_ = sb.AppendLine();
				_ = sb.AppendLine(CultureInfo.InvariantCulture, $"**{header}**");
			}

			foreach (var entry in group)
				RenderDetailedEntry(sb, entry, repo, owner, hideLinks, hideEntryDescriptions);
		}
	}

	private static void RenderDetailedEntriesFlattened(
		StringBuilder sb,
		List<ChangelogEntry> entries,
		string repo,
		string owner,
		bool groupBySubtype,
		bool hideLinks,
		bool hideEntryDescriptions)
	{
		if (groupBySubtype)
		{
			// Group by subtype and sort - same logic as RenderDetailedEntries
			var groupedBySubtype = entries.GroupBy(e => e.Subtype?.ToStringFast(true) ?? string.Empty).OrderBy(g => g.Key).ToList();

			foreach (var group in groupedBySubtype)
			{
				// Add subtype header if group has a non-empty key (same logic as RenderDetailedEntries)
				if (!string.IsNullOrWhiteSpace(group.Key))
				{
					var header = ChangelogTextUtilities.FormatSubtypeHeader(group.Key);
					_ = sb.AppendLine();
					_ = sb.AppendLine(header);
					_ = sb.AppendLine();
				}

				foreach (var entry in group)
					RenderDetailedEntryFlattened(sb, entry, repo, owner, hideLinks, hideEntryDescriptions);
			}
		}
		else
		{
			foreach (var entry in entries)
				RenderDetailedEntryFlattened(sb, entry, repo, owner, hideLinks, hideEntryDescriptions);
		}
	}

	private static void RenderDetailedEntriesFlattenedByArea(
		StringBuilder sb,
		List<ChangelogEntry> entries,
		string repo,
		string owner,
		bool hideLinks,
		bool hideEntryDescriptions,
		PublishBlocker? publishBlocker)
	{
		var groupedByArea = entries.GroupBy(e => publishBlocker.GetPreferredArea(e)).OrderBy(g => g.Key).ToList();

		foreach (var areaGroup in groupedByArea)
		{
			if (!string.IsNullOrWhiteSpace(areaGroup.Key))
			{
				var header = ChangelogTextUtilities.FormatAreaHeader(areaGroup.Key);
				_ = sb.AppendLine();
				_ = sb.AppendLine(CultureInfo.InvariantCulture, $"**{header}**");
			}

			foreach (var entry in areaGroup)
				RenderDetailedEntryFlattened(sb, entry, repo, owner, hideLinks, hideEntryDescriptions);
		}
	}

	private static void RenderDetailedEntryFlattened(StringBuilder sb, ChangelogEntry entry, string repo, string owner, bool hideLinks, bool hideEntryDescriptions)
	{
		// Start with bullet point and title (no bold, matching regular entries)
		_ = sb.Append("* ");
		_ = sb.Append(ChangelogTextUtilities.Beautify(entry.Title));

		// Add PR/Issue links on the same line if available
		var linksText = GetLinksText(entry, repo, owner, hideLinks);
		if (!string.IsNullOrEmpty(linksText))
			_ = sb.Append($" {linksText}");

		_ = sb.AppendLine();

		// Add description if not hidden
		if (!hideEntryDescriptions && !string.IsNullOrWhiteSpace(entry.Description))
		{
			_ = sb.AppendLine(ChangelogTextUtilities.Indent(entry.Description));
		}

		// Add Impact section
		if (!string.IsNullOrWhiteSpace(entry.Impact))
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(ChangelogTextUtilities.Indent($"**Impact:** {entry.Impact}"));
		}

		// Add Action section
		if (!string.IsNullOrWhiteSpace(entry.Action))
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(ChangelogTextUtilities.Indent($"**Action:** {entry.Action}"));
		}
	}

	private static string GetLinksText(ChangelogEntry entry, string repo, string owner, bool hideLinks)
	{
		if (!ChangelogTextUtilities.HasVisibleLinks(entry, repo, hideLinks, owner))
			return string.Empty;

		var linksParts = new List<string>();

		if (entry.Prs != null)
		{
			foreach (var pr in entry.Prs)
			{
				var formatted = ChangelogTextUtilities.FormatPrLink(pr, repo, hideLinks, owner);
				if (!string.IsNullOrEmpty(formatted))
					linksParts.Add(formatted);
			}
		}

		if (entry.Issues != null)
		{
			foreach (var issue in entry.Issues)
			{
				var formatted = ChangelogTextUtilities.FormatIssueLink(issue, repo, hideLinks, owner);
				if (!string.IsNullOrEmpty(formatted))
					linksParts.Add(formatted);
			}
		}

		return linksParts.Count > 0 ? string.Join(" ", linksParts) : string.Empty;
	}

	private static void RenderDetailedEntry(StringBuilder sb, ChangelogEntry entry, string repo, string owner, bool hideLinks, bool hideEntryDescriptions)
	{
		_ = sb.AppendLine();
		_ = sb.AppendLine(CultureInfo.InvariantCulture, $"::::{{dropdown}} {ChangelogTextUtilities.Beautify(entry.Title)}");
		if (!hideEntryDescriptions)
		{
			_ = sb.AppendLine(entry.Description ?? "% Describe the change");
			_ = sb.AppendLine();
		}
		else
			_ = sb.AppendLine();

		RenderDetailedEntryLinks(sb, entry, repo, owner, hideLinks);

		// Impact section
		_ = sb.AppendLine(!string.IsNullOrWhiteSpace(entry.Impact)
			? "**Impact**<br>" + entry.Impact
			: "% **Impact**<br>_Add a description of the impact_");
		_ = sb.AppendLine();

		// Action section
		_ = sb.AppendLine(!string.IsNullOrWhiteSpace(entry.Action)
			? "**Action**<br>" + entry.Action
			: "% **Action**<br>_Add a description of what action to take_");

		_ = sb.AppendLine("::::");
	}

	private static void RenderDetailedEntryLinks(StringBuilder sb, ChangelogEntry entry, string repo, string owner, bool hideLinks)
	{
		// Check if the entry has any visible links after formatting
		// This handles cases where all links are sanitized with PRIVATE prefix
		if (!ChangelogTextUtilities.HasVisibleLinks(entry, repo, hideLinks, owner))
			return;

		if (hideLinks)
		{
			var hasVisibleContent = false;
			foreach (var pr in entry.Prs ?? [])
			{
				var formatted = ChangelogTextUtilities.FormatPrLink(pr, repo, hidePrivateLinks: true, owner);
				if (!string.IsNullOrEmpty(formatted))
				{
					_ = sb.AppendLine(formatted);
					hasVisibleContent = true;
				}
			}
			foreach (var issue in entry.Issues ?? [])
			{
				var formatted = ChangelogTextUtilities.FormatIssueLink(issue, repo, hidePrivateLinks: true, owner);
				if (!string.IsNullOrEmpty(formatted))
				{
					_ = sb.AppendLine(formatted);
					hasVisibleContent = true;
				}
			}

			// Only show the reference text if we actually rendered some links
			if (hasVisibleContent)
			{
				_ = sb.AppendLine("For more information, check the pull request or issue above.");
			}
			_ = sb.AppendLine();
			return;
		}

		_ = sb.Append("For more information, check ");
		var first = true;
		foreach (var pr in entry.Prs ?? [])
		{
			var formatted = ChangelogTextUtilities.FormatPrLink(pr, repo, hidePrivateLinks: false, owner);
			if (!string.IsNullOrEmpty(formatted))
			{
				if (!first)
					_ = sb.Append(' ');
				_ = sb.Append(formatted);
				first = false;
			}
		}
		foreach (var issue in entry.Issues ?? [])
		{
			var formatted = ChangelogTextUtilities.FormatIssueLink(issue, repo, hidePrivateLinks: false, owner);
			if (!string.IsNullOrEmpty(formatted))
			{
				if (!first)
					_ = sb.Append(' ');
				_ = sb.Append(formatted);
				first = false;
			}
		}
		_ = sb.AppendLine(".");
		_ = sb.AppendLine();
	}

	private static void AppendVersionHeader(StringBuilder sb, string title, string? description, DateOnly? releaseDate)
	{
		_ = sb.AppendLine(CultureInfo.InvariantCulture, $"## {title}");

		if (releaseDate is { } date)
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"_Released: {date.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture)}_");
		}

		if (!string.IsNullOrEmpty(description))
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(description);
		}
	}

	private static void AppendSectionHeader(StringBuilder sb, bool dedicatedPage, string sectionHeading)
	{
		if (dedicatedPage)
			_ = sb.AppendLine();
		else
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(sectionHeading);
		}
	}

	private static void RenderSeparatedTypeEntries(
		StringBuilder sb,
		List<ChangelogEntry> entries,
		string repo,
		string owner,
		bool subsections,
		bool dropdownsEnabled,
		bool groupBySubtype,
		bool hideLinks,
		bool hideEntryDescriptions,
		PublishBlocker? publishBlocker)
	{
		if (dropdownsEnabled)
		{
			RenderDetailedEntries(sb, entries, repo, owner, groupBySubtype, hideLinks, hideEntryDescriptions, publishBlocker);
			return;
		}

		if (subsections && !groupBySubtype)
			RenderDetailedEntriesFlattenedByArea(sb, entries, repo, owner, hideLinks, hideEntryDescriptions, publishBlocker);
		else
			RenderDetailedEntriesFlattened(sb, entries, repo, owner, groupBySubtype, hideLinks, hideEntryDescriptions);
	}

	/// <summary>
	/// Per-bundle values computed by <see cref="RenderSingleBundle"/> and consumed by <see cref="GenerateMarkdown"/>.
	/// Groups the title, owner/repo identity, filtered entries, and resolved visibility flags for a single bundle.
	/// </summary>
	private sealed record BundleRenderModel
	{
		public required string Title { get; init; }
		public required string TitleSlug { get; init; }
		public required string Repo { get; init; }
		public required string Owner { get; init; }
		public required Dictionary<ChangelogEntryType, List<ChangelogEntry>> EntriesByType { get; init; }
		public required bool HideLinks { get; init; }
		public required bool HideEntryDescriptions { get; init; }
		public string? Description { get; init; }
		public DateOnly? ReleaseDate { get; init; }
	}
}
