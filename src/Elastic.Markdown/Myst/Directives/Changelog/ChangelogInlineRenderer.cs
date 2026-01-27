// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text;
using Elastic.Changelog;

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

		// Render each bundle as a version section (already sorted by semver descending)
		var isFirst = true;
		foreach (var bundle in block.LoadedBundles)
		{
			if (!isFirst)
				_ = sb.AppendLine();

			var bundleMarkdown = RenderSingleBundle(bundle, block.Subsections);
			_ = sb.Append(bundleMarkdown);

			isFirst = false;
		}

		return sb.ToString();
	}

	private static string RenderSingleBundle(LoadedBundle bundle, bool subsections)
	{
		var titleSlug = ChangelogTextUtilities.TitleToSlug(bundle.Version);

		// Group entries by type
		var entriesByType = bundle.Entries
			.GroupBy(e => e.Type)
			.ToDictionary(g => g.Key, g => g.ToList());

		return GenerateMarkdown(bundle.Version, titleSlug, bundle.Repo, entriesByType, subsections);
	}

	private static string GenerateMarkdown(
		string title,
		string titleSlug,
		string repo,
		Dictionary<ChangelogEntryType, List<ChangelogEntry>> entriesByType,
		bool subsections)
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

		// Build header with links to other sections if they exist
		_ = sb.AppendLine(CultureInfo.InvariantCulture, $"## {title}");

		var otherLinks = new List<string>();
		if (knownIssues.Count > 0)
			otherLinks.Add($"[Known issues](#{repo}-{titleSlug}-known-issues)");
		if (breakingChanges.Count > 0)
			otherLinks.Add($"[Breaking changes](#{repo}-{titleSlug}-breaking-changes)");
		if (deprecations.Count > 0)
			otherLinks.Add($"[Deprecations](#{repo}-{titleSlug}-deprecations)");

		if (otherLinks.Count > 0)
		{
			var linksText = string.Join(" and ", otherLinks);
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"_{linksText}._");
			_ = sb.AppendLine();
		}

		// Render main content sections
		var hasMainContent = features.Count > 0 || enhancements.Count > 0 || security.Count > 0 ||
							 bugFixes.Count > 0 || docs.Count > 0 || regressions.Count > 0 || other.Count > 0;

		if (hasMainContent)
		{
			if (features.Count > 0 || enhancements.Count > 0)
			{
				_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Features and enhancements [{repo}-{titleSlug}-features-enhancements]");
				var combined = features.Concat(enhancements).ToList();
				RenderEntriesByArea(sb, combined, repo, subsections);
			}

			if (security.Count > 0 || bugFixes.Count > 0)
			{
				_ = sb.AppendLine();
				_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Fixes [{repo}-{titleSlug}-fixes]");
				var combined = security.Concat(bugFixes).ToList();
				RenderEntriesByArea(sb, combined, repo, subsections);
			}

			if (docs.Count > 0)
			{
				_ = sb.AppendLine();
				_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Documentation [{repo}-{titleSlug}-docs]");
				RenderEntriesByArea(sb, docs, repo, subsections);
			}

			if (regressions.Count > 0)
			{
				_ = sb.AppendLine();
				_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Regressions [{repo}-{titleSlug}-regressions]");
				RenderEntriesByArea(sb, regressions, repo, subsections);
			}

			if (other.Count > 0)
			{
				_ = sb.AppendLine();
				_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Other changes [{repo}-{titleSlug}-other]");
				RenderEntriesByArea(sb, other, repo, subsections);
			}
		}
		else
		{
			_ = sb.AppendLine("_No new features, enhancements, or fixes._");
		}

		// Render special sections
		if (breakingChanges.Count > 0)
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Breaking changes [{repo}-{titleSlug}-breaking-changes]");
			RenderDetailedEntries(sb, breakingChanges, repo, groupBySubtype: true);
		}

		if (deprecations.Count > 0)
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Deprecations [{repo}-{titleSlug}-deprecations]");
			RenderDetailedEntries(sb, deprecations, repo, groupBySubtype: false);
		}

		if (knownIssues.Count > 0)
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Known issues [{repo}-{titleSlug}-known-issues]");
			RenderDetailedEntries(sb, knownIssues, repo, groupBySubtype: false);
		}

		return sb.ToString();
	}

	private static void RenderEntriesByArea(
		StringBuilder sb,
		List<ChangelogEntry> entries,
		string repo,
		bool subsections)
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
					RenderSingleEntry(sb, entry, repo);
			}
		}
		else
		{
			foreach (var entry in entries)
				RenderSingleEntry(sb, entry, repo);
		}
	}

	private static void RenderSingleEntry(StringBuilder sb, ChangelogEntry entry, string repo)
	{
		_ = sb.Append("* ");
		_ = sb.Append(ChangelogTextUtilities.Beautify(entry.Title));

		_ = sb.Append(' ');
		if (!string.IsNullOrWhiteSpace(entry.Pr))
		{
			_ = sb.Append(ChangelogTextUtilities.FormatPrLink(entry.Pr, repo, hidePrivateLinks: false));
			_ = sb.Append(' ');
		}

		if (entry.Issues is { Count: > 0 })
		{
			foreach (var issue in entry.Issues)
			{
				_ = sb.Append(ChangelogTextUtilities.FormatIssueLink(issue, repo, hidePrivateLinks: false));
				_ = sb.Append(' ');
			}
		}

		if (!string.IsNullOrWhiteSpace(entry.Description))
		{
			_ = sb.AppendLine();
			var indented = ChangelogTextUtilities.Indent(entry.Description);
			_ = sb.AppendLine(indented);
		}
		else
		{
			_ = sb.AppendLine();
		}
	}

	private static void RenderDetailedEntries(
		StringBuilder sb,
		List<ChangelogEntry> entries,
		string repo,
		bool groupBySubtype)
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
			{
				_ = sb.AppendLine();
				_ = sb.AppendLine(CultureInfo.InvariantCulture, $"::::{{dropdown}} {ChangelogTextUtilities.Beautify(entry.Title)}");
				_ = sb.AppendLine(entry.Description ?? "% Describe the change");
				_ = sb.AppendLine();

				// PR/Issue links
				var hasPr = !string.IsNullOrWhiteSpace(entry.Pr);
				var hasIssues = entry.Issues is { Count: > 0 };
				if (hasPr || hasIssues)
				{
					_ = sb.Append("For more information, check ");
					if (hasPr)
					{
						_ = sb.Append(ChangelogTextUtilities.FormatPrLink(entry.Pr!, repo, hidePrivateLinks: false));
					}
					if (hasIssues)
					{
						foreach (var issue in entry.Issues!)
						{
							_ = sb.Append(' ');
							_ = sb.Append(ChangelogTextUtilities.FormatIssueLink(issue, repo, hidePrivateLinks: false));
						}
					}
					_ = sb.AppendLine(".");
					_ = sb.AppendLine();
				}

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
		}
	}

	private static string GetComponent(ChangelogEntry entry) =>
		entry.Areas is { Count: > 0 } ? entry.Areas[0] : string.Empty;
}
