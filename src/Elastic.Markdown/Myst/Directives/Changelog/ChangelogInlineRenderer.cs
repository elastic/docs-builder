// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text;
using Elastic.Documentation;
using Elastic.Documentation.ReleaseNotes;

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
		var typeFilter = block.TypeFilter;

		// Render each bundle as a version section (already sorted by semver descending)
		var isFirst = true;
		foreach (var bundle in block.LoadedBundles)
		{
			if (!isFirst)
				_ = sb.AppendLine();

			var bundleMarkdown = RenderSingleBundle(
				bundle,
				block.Subsections,
				block.PublishBlocker,
				block.PrivateRepositories,
				block.HideFeatures,
				typeFilter);
			_ = sb.Append(bundleMarkdown);

			isFirst = false;
		}

		return sb.ToString();
	}

	private static string RenderSingleBundle(
		LoadedBundle bundle,
		bool subsections,
		PublishBlocker? publishBlocker,
		HashSet<string> privateRepositories,
		HashSet<string> hideFeatures,
		ChangelogTypeFilter typeFilter)
	{
		var titleSlug = ChangelogTextUtilities.TitleToSlug(bundle.Version);

		// Filter entries based on publish blocker configuration
		var filteredEntries = FilterEntries(bundle.Entries, publishBlocker);

		// Filter entries based on hide-features (from bundle metadata)
		filteredEntries = FilterEntriesByHideFeatures(filteredEntries, hideFeatures);

		// Apply type filter
		filteredEntries = FilterEntriesByType(filteredEntries, typeFilter);

		// Group entries by type
		var entriesByType = filteredEntries
			.GroupBy(e => e.Type)
			.ToDictionary(g => g.Key, g => g.ToList());

		// Check if the bundle's repo (which may be merged like "elasticsearch+kibana")
		// contains any private repositories - if so, hide links for this bundle
		var hideLinks = ShouldHideLinksForRepo(bundle.Repo, privateRepositories);

		return GenerateMarkdown(bundle.Version, titleSlug, bundle.Repo, entriesByType, subsections, hideLinks, typeFilter);
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

		// Split on '+' to handle merged bundles (e.g., "elasticsearch+kibana+private-repo")
		var repos = bundleRepo.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		// Hide links if ANY component repo is private
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

	private static string GenerateMarkdown(
		string title,
		string titleSlug,
		string repo,
		Dictionary<ChangelogEntryType, List<ChangelogEntry>> entriesByType,
		bool subsections,
		bool hideLinks,
		ChangelogTypeFilter typeFilter)
	{
		var sb = new StringBuilder();

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

		_ = sb.AppendLine(CultureInfo.InvariantCulture, $"## {title}");

		// Check if we have any content at all
		var hasAnyContent = features.Count > 0 || enhancements.Count > 0 || security.Count > 0 ||
							bugFixes.Count > 0 || docs.Count > 0 || regressions.Count > 0 || other.Count > 0 ||
							breakingChanges.Count > 0 || deprecations.Count > 0 || knownIssues.Count > 0 ||
							highlights.Count > 0;

		if (!hasAnyContent)
		{
			_ = sb.AppendLine(GetEmptyMessage(typeFilter));
			return sb.ToString();
		}

		// Special case: When filtering by highlight, render only highlights without type-based sections
		if (typeFilter == ChangelogTypeFilter.Highlight)
		{
			if (highlights.Count > 0)
				RenderDetailedEntries(sb, highlights, repo, groupBySubtype: false, hideLinks);
			return sb.ToString();
		}

		if (breakingChanges.Count > 0)
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Breaking changes [{repo}-{titleSlug}-breaking-changes]");
			RenderDetailedEntries(sb, breakingChanges, repo, groupBySubtype: true, hideLinks);
		}

		if (highlights.Count > 0 && typeFilter == ChangelogTypeFilter.All)
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Highlights [{repo}-{titleSlug}-highlights]");
			RenderDetailedEntries(sb, highlights, repo, groupBySubtype: false, hideLinks);
		}

		if (security.Count > 0)
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Security [{repo}-{titleSlug}-security]");
			RenderEntriesByArea(sb, security, repo, subsections, hideLinks);
		}

		if (knownIssues.Count > 0)
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Known issues [{repo}-{titleSlug}-known-issues]");
			RenderDetailedEntries(sb, knownIssues, repo, groupBySubtype: false, hideLinks);
		}

		if (deprecations.Count > 0)
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Deprecations [{repo}-{titleSlug}-deprecations]");
			RenderDetailedEntries(sb, deprecations, repo, groupBySubtype: false, hideLinks);
		}

		if (features.Count > 0 || enhancements.Count > 0)
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Features and enhancements [{repo}-{titleSlug}-features-enhancements]");
			var combined = features.Concat(enhancements).ToList();
			RenderEntriesByArea(sb, combined, repo, subsections, hideLinks);
		}

		if (bugFixes.Count > 0)
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Fixes [{repo}-{titleSlug}-fixes]");
			RenderEntriesByArea(sb, bugFixes, repo, subsections, hideLinks);
		}

		if (docs.Count > 0)
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Documentation [{repo}-{titleSlug}-docs]");
			RenderEntriesByArea(sb, docs, repo, subsections, hideLinks);
		}

		if (regressions.Count > 0)
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Regressions [{repo}-{titleSlug}-regressions]");
			RenderEntriesByArea(sb, regressions, repo, subsections, hideLinks);
		}

		if (other.Count > 0)
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Other changes [{repo}-{titleSlug}-other]");
			RenderEntriesByArea(sb, other, repo, subsections, hideLinks);
		}

		return sb.ToString();
	}

	private static void RenderEntriesByArea(
		StringBuilder sb,
		List<ChangelogEntry> entries,
		string repo,
		bool subsections,
		bool hideLinks)
	{
		if (subsections)
		{
			// Group by area and sort when subsections is enabled
			var groupedByArea = entries.GroupBy(GetComponent).OrderBy(g => g.Key).ToList();

			foreach (var areaGroup in groupedByArea)
			{
				if (!string.IsNullOrWhiteSpace(areaGroup.Key))
				{
					var header = ChangelogTextUtilities.FormatAreaHeader(areaGroup.Key);
					_ = sb.AppendLine();
					_ = sb.AppendLine(CultureInfo.InvariantCulture, $"**{header}**");
				}

				foreach (var entry in areaGroup)
					RenderSingleEntry(sb, entry, repo, hideLinks);
			}
		}
		else
		{
			foreach (var entry in entries)
				RenderSingleEntry(sb, entry, repo, hideLinks);
		}
	}

	private static void RenderSingleEntry(StringBuilder sb, ChangelogEntry entry, string repo, bool hideLinks)
	{
		_ = sb.Append("* ");
		_ = sb.Append(ChangelogTextUtilities.Beautify(entry.Title));

		RenderEntryLinks(sb, entry, repo, hideLinks);

		if (!string.IsNullOrWhiteSpace(entry.Description))
		{
			var indented = ChangelogTextUtilities.Indent(entry.Description);
			_ = sb.AppendLine(indented);
		}
	}

	private static void RenderEntryLinks(StringBuilder sb, ChangelogEntry entry, string repo, bool hideLinks)
	{
		var hasPr = !string.IsNullOrWhiteSpace(entry.Pr);

		if (hideLinks)
		{
			// When hiding links, put them on separate lines as comments
			_ = sb.AppendLine();
			if (hasPr)
			{
				_ = sb.Append("  ");
				_ = sb.AppendLine(ChangelogTextUtilities.FormatPrLink(entry.Pr!, repo, hidePrivateLinks: true));
			}
			foreach (var issue in entry.Issues ?? [])
			{
				_ = sb.Append("  ");
				_ = sb.AppendLine(ChangelogTextUtilities.FormatIssueLink(issue, repo, hidePrivateLinks: true));
			}
			return;
		}

		// Default: render links inline
		_ = sb.Append(' ');
		if (hasPr)
		{
			_ = sb.Append(ChangelogTextUtilities.FormatPrLink(entry.Pr!, repo, hidePrivateLinks: false));
			_ = sb.Append(' ');
		}
		foreach (var issue in entry.Issues ?? [])
		{
			_ = sb.Append(ChangelogTextUtilities.FormatIssueLink(issue, repo, hidePrivateLinks: false));
			_ = sb.Append(' ');
		}
		_ = sb.AppendLine();
	}

	private static void RenderDetailedEntries(
		StringBuilder sb,
		List<ChangelogEntry> entries,
		string repo,
		bool groupBySubtype,
		bool hideLinks)
	{
		var grouped = groupBySubtype
			? entries.GroupBy(e => e.Subtype?.ToStringFast(true) ?? string.Empty).OrderBy(g => g.Key).ToList()
			: entries.GroupBy(GetComponent).OrderBy(g => g.Key).ToList();

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
				RenderDetailedEntry(sb, entry, repo, hideLinks);
		}
	}

	private static void RenderDetailedEntry(StringBuilder sb, ChangelogEntry entry, string repo, bool hideLinks)
	{
		_ = sb.AppendLine();
		_ = sb.AppendLine(CultureInfo.InvariantCulture, $"::::{{dropdown}} {ChangelogTextUtilities.Beautify(entry.Title)}");
		_ = sb.AppendLine(entry.Description ?? "% Describe the change");
		_ = sb.AppendLine();

		RenderDetailedEntryLinks(sb, entry, repo, hideLinks);

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

	private static void RenderDetailedEntryLinks(StringBuilder sb, ChangelogEntry entry, string repo, bool hideLinks)
	{
		var hasPr = !string.IsNullOrWhiteSpace(entry.Pr);
		var hasIssues = entry.Issues is { Count: > 0 };

		if (!hasPr && !hasIssues)
			return;

		if (hideLinks)
		{
			// When hiding links, put them on separate lines as comments
			if (hasPr)
				_ = sb.AppendLine(ChangelogTextUtilities.FormatPrLink(entry.Pr!, repo, hidePrivateLinks: true));
			foreach (var issue in entry.Issues ?? [])
				_ = sb.AppendLine(ChangelogTextUtilities.FormatIssueLink(issue, repo, hidePrivateLinks: true));
			_ = sb.AppendLine("For more information, check the pull request or issue above.");
			_ = sb.AppendLine();
			return;
		}

		// Default: render links inline
		_ = sb.Append("For more information, check ");
		if (hasPr)
			_ = sb.Append(ChangelogTextUtilities.FormatPrLink(entry.Pr!, repo, hidePrivateLinks: false));
		foreach (var issue in entry.Issues ?? [])
		{
			_ = sb.Append(' ');
			_ = sb.Append(ChangelogTextUtilities.FormatIssueLink(issue, repo, hidePrivateLinks: false));
		}
		_ = sb.AppendLine(".");
		_ = sb.AppendLine();
	}

	private static string GetComponent(ChangelogEntry entry) =>
		entry.Areas is { Count: > 0 } ? entry.Areas[0] : string.Empty;

	/// <summary>
	/// Gets the appropriate empty message based on the type filter.
	/// Matches messages used by CLI renderers for consistency.
	/// </summary>
	private static string GetEmptyMessage(ChangelogTypeFilter typeFilter) =>
		typeFilter switch
		{
			ChangelogTypeFilter.BreakingChange => "_There are no breaking changes associated with this release._",
			ChangelogTypeFilter.Deprecation => "_There are no deprecations associated with this release._",
			ChangelogTypeFilter.KnownIssue => "_There are no known issues associated with this release._",
			_ => "_No new features, enhancements, or fixes._"
		};
}
