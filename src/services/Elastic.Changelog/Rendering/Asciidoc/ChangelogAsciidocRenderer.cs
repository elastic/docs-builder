// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

#pragma warning disable IDE0060 // Remove unused parameter

using System.Globalization;
using System.IO.Abstractions;
using System.Text;
using Elastic.Changelog;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.Configuration;
using Elastic.Changelog.Rendering.Markdown;

namespace Elastic.Changelog.Rendering.Asciidoc;

/// <summary>
/// Renderer for changelog asciidoc output
/// </summary>
public class ChangelogAsciidocRenderer(IFileSystem fileSystem)
{
	public async Task RenderAsciidoc(
		string outputDir,
		string title,
		string titleSlug,
		string repo,
		List<ChangelogData> entries,
		Dictionary<string, List<ChangelogData>> entriesByType,
		bool subsections,
		HashSet<string> featureIdsToHide,
		IReadOnlyDictionary<string, RenderBlockersEntry>? renderBlockers,
		Dictionary<ChangelogData, HashSet<string>> entryToBundleProducts,
		Dictionary<ChangelogData, string> entryToRepo,
		Dictionary<ChangelogData, bool> entryToHideLinks,
		Cancel ctx
	)
	{
		var sb = new StringBuilder();

		// Add anchor
		_ = sb.AppendLine(CultureInfo.InvariantCulture, $"[[release-notes-{titleSlug}]]");
		_ = sb.AppendLine(CultureInfo.InvariantCulture, $"== {title}");
		_ = sb.AppendLine();

		// Group entries by type
		var security = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Security, []);
		var bugFixes = entriesByType.GetValueOrDefault(ChangelogEntryTypes.BugFix, []);
		var features = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Feature, []);
		var enhancements = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Enhancement, []);
		var breakingChanges = entriesByType.GetValueOrDefault(ChangelogEntryTypes.BreakingChange, []);
		var deprecations = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Deprecation, []);
		var knownIssues = entriesByType.GetValueOrDefault(ChangelogEntryTypes.KnownIssue, []);
		var docs = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Docs, []);
		var regressions = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Regression, []);
		var other = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Other, []);

		// Render security updates
		if (security.Count > 0)
		{
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"[[security-updates-{titleSlug}]]");
			_ = sb.AppendLine("[float]");
			_ = sb.AppendLine("=== Security updates");
			_ = sb.AppendLine();
			RenderEntriesByAreaAsciidoc(sb, security, repo, subsections, featureIdsToHide, renderBlockers, entryToBundleProducts, entryToRepo, entryToHideLinks);
			_ = sb.AppendLine();
		}

		// Render bug fixes
		if (bugFixes.Count > 0)
		{
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"[[bug-fixes-{titleSlug}]]");
			_ = sb.AppendLine("[float]");
			_ = sb.AppendLine("=== Bug fixes");
			_ = sb.AppendLine();
			RenderEntriesByAreaAsciidoc(sb, bugFixes, repo, subsections, featureIdsToHide, renderBlockers, entryToBundleProducts, entryToRepo, entryToHideLinks);
			_ = sb.AppendLine();
		}

		// Render features and enhancements
		if (features.Count > 0 || enhancements.Count > 0)
		{
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"[[features-enhancements-{titleSlug}]]");
			_ = sb.AppendLine("[float]");
			_ = sb.AppendLine("=== New features and enhancements");
			_ = sb.AppendLine();
			var combined = features.Concat(enhancements).ToList();
			RenderEntriesByAreaAsciidoc(sb, combined, repo, subsections, featureIdsToHide, renderBlockers, entryToBundleProducts, entryToRepo, entryToHideLinks);
			_ = sb.AppendLine();
		}

		// Render breaking changes
		if (breakingChanges.Count > 0)
		{
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"[[breaking-changes-{titleSlug}]]");
			_ = sb.AppendLine("[float]");
			_ = sb.AppendLine("=== Breaking changes");
			_ = sb.AppendLine();
			RenderBreakingChangesAsciidoc(sb, breakingChanges, repo, subsections, featureIdsToHide, renderBlockers, entryToBundleProducts, entryToRepo, entryToHideLinks);
			_ = sb.AppendLine();
		}

		// Render deprecations
		if (deprecations.Count > 0)
		{
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"[[deprecations-{titleSlug}]]");
			_ = sb.AppendLine("[float]");
			_ = sb.AppendLine("=== Deprecations");
			_ = sb.AppendLine();
			RenderDeprecationsAsciidoc(sb, deprecations, repo, subsections, featureIdsToHide, renderBlockers, entryToBundleProducts, entryToRepo, entryToHideLinks);
			_ = sb.AppendLine();
		}

		// Render known issues
		if (knownIssues.Count > 0)
		{
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"[[known-issues-{titleSlug}]]");
			_ = sb.AppendLine("[float]");
			_ = sb.AppendLine("=== Known issues");
			_ = sb.AppendLine();
			RenderKnownIssuesAsciidoc(sb, knownIssues, repo, subsections, featureIdsToHide, renderBlockers, entryToBundleProducts, entryToRepo, entryToHideLinks);
			_ = sb.AppendLine();
		}

		// Render documentation changes
		if (docs.Count > 0)
		{
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"[[docs-{titleSlug}]]");
			_ = sb.AppendLine("[float]");
			_ = sb.AppendLine("=== Documentation");
			_ = sb.AppendLine();
			RenderEntriesByAreaAsciidoc(sb, docs, repo, subsections, featureIdsToHide, renderBlockers, entryToBundleProducts, entryToRepo, entryToHideLinks);
			_ = sb.AppendLine();
		}

		// Render regressions
		if (regressions.Count > 0)
		{
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"[[regressions-{titleSlug}]]");
			_ = sb.AppendLine("[float]");
			_ = sb.AppendLine("=== Regressions");
			_ = sb.AppendLine();
			RenderEntriesByAreaAsciidoc(sb, regressions, repo, subsections, featureIdsToHide, renderBlockers, entryToBundleProducts, entryToRepo, entryToHideLinks);
			_ = sb.AppendLine();
		}

		// Render other changes
		if (other.Count > 0)
		{
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"[[other-{titleSlug}]]");
			_ = sb.AppendLine("[float]");
			_ = sb.AppendLine("=== Other changes");
			_ = sb.AppendLine();
			RenderEntriesByAreaAsciidoc(sb, other, repo, subsections, featureIdsToHide, renderBlockers, entryToBundleProducts, entryToRepo, entryToHideLinks);
			_ = sb.AppendLine();
		}

		// Write the asciidoc file
		var asciidocPath = fileSystem.Path.Combine(outputDir, $"{titleSlug}.asciidoc");
		var asciidocDir = fileSystem.Path.GetDirectoryName(asciidocPath);
		if (!string.IsNullOrWhiteSpace(asciidocDir) && !fileSystem.Directory.Exists(asciidocDir))
		{
			_ = fileSystem.Directory.CreateDirectory(asciidocDir);
		}

		await fileSystem.File.WriteAllTextAsync(asciidocPath, sb.ToString(), ctx);
	}

	private static void RenderEntriesByAreaAsciidoc(
		StringBuilder sb,
		List<ChangelogData> entries,
		string repo,
		bool subsections,
		HashSet<string> featureIdsToHide,
		IReadOnlyDictionary<string, RenderBlockersEntry>? renderBlockers,
		Dictionary<ChangelogData, HashSet<string>> entryToBundleProducts,
		Dictionary<ChangelogData, string> entryToRepo,
		Dictionary<ChangelogData, bool> entryToHideLinks
	)
	{
		var groupedByArea = subsections
			? entries.GroupBy(e => GetComponent(e)).OrderBy(g => g.Key).ToList()
			: entries.GroupBy(e => GetComponent(e)).ToList();
		foreach (var areaGroup in groupedByArea)
		{
			var componentName = !string.IsNullOrWhiteSpace(areaGroup.Key) ? areaGroup.Key : "General";

			// Format component name (capitalize first letter, replace hyphens with spaces)
			var formattedComponent = ChangelogTextUtilities.FormatAreaHeader(componentName);

			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"{formattedComponent}::");
			_ = sb.AppendLine();

			foreach (var entry in areaGroup)
			{
				var bundleProductIds = entryToBundleProducts.GetValueOrDefault(entry, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
				var shouldHide = (!string.IsNullOrWhiteSpace(entry.FeatureId) && featureIdsToHide.Contains(entry.FeatureId)) ||
					ChangelogMarkdownRenderer.ShouldBlockEntry(entry, bundleProductIds, renderBlockers, out _);

				if (shouldHide)
				{
					_ = sb.AppendLine("// ");
				}

				_ = sb.Append("* ");
				_ = sb.Append(ChangelogTextUtilities.Beautify(entry.Title));

				var hasPr = !string.IsNullOrWhiteSpace(entry.Pr);
				var hasIssues = entry.Issues != null && entry.Issues.Count > 0;

				if (hasPr || hasIssues)
				{
					var entryRepo = entryToRepo.GetValueOrDefault(entry, repo);
					var hideLinks = entryToHideLinks.GetValueOrDefault(entry, false);
					_ = sb.Append(' ');
					if (hasPr)
					{
						_ = sb.Append(ChangelogTextUtilities.FormatPrLinkAsciidoc(entry.Pr!, entryRepo, hideLinks));
						_ = sb.Append(' ');
					}
					if (hasIssues)
					{
						foreach (var issue in entry.Issues!)
						{
							_ = sb.Append(ChangelogTextUtilities.FormatIssueLinkAsciidoc(issue, entryRepo, hideLinks));
							_ = sb.Append(' ');
						}
					}
				}

				if (!string.IsNullOrWhiteSpace(entry.Description))
				{
					_ = sb.AppendLine();
					var indented = ChangelogTextUtilities.Indent(entry.Description);
					if (shouldHide)
					{
						var indentedLines = indented.Split('\n');
						foreach (var line in indentedLines)
						{
							_ = sb.AppendLine(CultureInfo.InvariantCulture, $"// {line}");
						}
					}
					else
					{
						_ = sb.AppendLine(indented);
					}
				}

				_ = sb.AppendLine();
			}
		}
	}

	private static void RenderBreakingChangesAsciidoc(
		StringBuilder sb,
		List<ChangelogData> breakingChanges,
		string repo,
		bool subsections,
		HashSet<string> featureIdsToHide,
		IReadOnlyDictionary<string, RenderBlockersEntry>? renderBlockers,
		Dictionary<ChangelogData, HashSet<string>> entryToBundleProducts,
		Dictionary<ChangelogData, string> entryToRepo,
		Dictionary<ChangelogData, bool> entryToHideLinks
	)
	{
		// Group by subtype if subsections is enabled, otherwise group by area
		var groupedEntries = subsections
			? breakingChanges.GroupBy(e => string.IsNullOrWhiteSpace(e.Subtype) ? string.Empty : e.Subtype).OrderBy(g => g.Key).ToList()
			: breakingChanges.GroupBy(e => GetComponent(e)).ToList();

		foreach (var group in groupedEntries)
		{
			if (subsections && !string.IsNullOrWhiteSpace(group.Key))
			{
				var header = ChangelogTextUtilities.FormatSubtypeHeader(group.Key);
				_ = sb.AppendLine(CultureInfo.InvariantCulture, $"**{header}**");
				_ = sb.AppendLine();
			}

			foreach (var entry in group)
			{
				var bundleProductIds = entryToBundleProducts.GetValueOrDefault(entry, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
				var shouldHide = (!string.IsNullOrWhiteSpace(entry.FeatureId) && featureIdsToHide.Contains(entry.FeatureId)) ||
					ChangelogMarkdownRenderer.ShouldBlockEntry(entry, bundleProductIds, renderBlockers, out _);

				if (shouldHide)
				{
					_ = sb.AppendLine("// ");
				}

				_ = sb.Append("* ");
				_ = sb.Append(ChangelogTextUtilities.Beautify(entry.Title));

				var hasPr = !string.IsNullOrWhiteSpace(entry.Pr);
				var hasIssues = entry.Issues != null && entry.Issues.Count > 0;

				if (hasPr || hasIssues)
				{
					var entryRepo = entryToRepo.GetValueOrDefault(entry, repo);
					var hideLinks = entryToHideLinks.GetValueOrDefault(entry, false);
					_ = sb.Append(' ');
					if (hasPr)
					{
						_ = sb.Append(ChangelogTextUtilities.FormatPrLinkAsciidoc(entry.Pr!, entryRepo, hideLinks));
						_ = sb.Append(' ');
					}
					if (hasIssues)
					{
						foreach (var issue in entry.Issues!)
						{
							_ = sb.Append(ChangelogTextUtilities.FormatIssueLinkAsciidoc(issue, entryRepo, hideLinks));
							_ = sb.Append(' ');
						}
					}
				}

				if (!string.IsNullOrWhiteSpace(entry.Description))
				{
					_ = sb.AppendLine();
					var indented = ChangelogTextUtilities.Indent(entry.Description);
					if (shouldHide)
					{
						var indentedLines = indented.Split('\n');
						foreach (var line in indentedLines)
						{
							_ = sb.AppendLine(CultureInfo.InvariantCulture, $"// {line}");
						}
					}
					else
					{
						_ = sb.AppendLine(indented);
					}
				}

				if (!string.IsNullOrWhiteSpace(entry.Impact))
				{
					_ = sb.AppendLine();
					_ = sb.AppendLine(CultureInfo.InvariantCulture, $"**Impact:** {entry.Impact}");
				}

				if (!string.IsNullOrWhiteSpace(entry.Action))
				{
					_ = sb.AppendLine();
					_ = sb.AppendLine(CultureInfo.InvariantCulture, $"**Action:** {entry.Action}");
				}

				_ = sb.AppendLine();
			}
		}
	}

	private static void RenderDeprecationsAsciidoc(
		StringBuilder sb,
		List<ChangelogData> deprecations,
		string repo,
		bool subsections,
		HashSet<string> featureIdsToHide,
		IReadOnlyDictionary<string, RenderBlockersEntry>? renderBlockers,
		Dictionary<ChangelogData, HashSet<string>> entryToBundleProducts,
		Dictionary<ChangelogData, string> entryToRepo,
		Dictionary<ChangelogData, bool> entryToHideLinks
	)
	{
		var groupedByArea = deprecations.GroupBy(e => GetComponent(e)).OrderBy(g => g.Key).ToList();
		foreach (var areaGroup in groupedByArea)
		{
			var componentName = !string.IsNullOrWhiteSpace(areaGroup.Key) ? areaGroup.Key : "General";
			var formattedComponent = ChangelogTextUtilities.FormatAreaHeader(componentName);

			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"{formattedComponent}::");
			_ = sb.AppendLine();

			foreach (var entry in areaGroup)
			{
				var bundleProductIds = entryToBundleProducts.GetValueOrDefault(entry, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
				var shouldHide = (!string.IsNullOrWhiteSpace(entry.FeatureId) && featureIdsToHide.Contains(entry.FeatureId)) ||
					ChangelogMarkdownRenderer.ShouldBlockEntry(entry, bundleProductIds, renderBlockers, out _);

				if (shouldHide)
				{
					_ = sb.AppendLine("// ");
				}

				_ = sb.Append("* ");
				_ = sb.Append(ChangelogTextUtilities.Beautify(entry.Title));

				var hasPr = !string.IsNullOrWhiteSpace(entry.Pr);
				var hasIssues = entry.Issues != null && entry.Issues.Count > 0;

				if (hasPr || hasIssues)
				{
					var entryRepo = entryToRepo.GetValueOrDefault(entry, repo);
					var hideLinks = entryToHideLinks.GetValueOrDefault(entry, false);
					_ = sb.Append(' ');
					if (hasPr)
					{
						_ = sb.Append(ChangelogTextUtilities.FormatPrLinkAsciidoc(entry.Pr!, entryRepo, hideLinks));
						_ = sb.Append(' ');
					}
					if (hasIssues)
					{
						foreach (var issue in entry.Issues!)
						{
							_ = sb.Append(ChangelogTextUtilities.FormatIssueLinkAsciidoc(issue, entryRepo, hideLinks));
							_ = sb.Append(' ');
						}
					}
				}

				if (!string.IsNullOrWhiteSpace(entry.Description))
				{
					_ = sb.AppendLine();
					var indented = ChangelogTextUtilities.Indent(entry.Description);
					if (shouldHide)
					{
						var indentedLines = indented.Split('\n');
						foreach (var line in indentedLines)
						{
							_ = sb.AppendLine(CultureInfo.InvariantCulture, $"// {line}");
						}
					}
					else
					{
						_ = sb.AppendLine(indented);
					}
				}

				if (!string.IsNullOrWhiteSpace(entry.Impact))
				{
					_ = sb.AppendLine();
					_ = sb.AppendLine(CultureInfo.InvariantCulture, $"**Impact:** {entry.Impact}");
				}

				if (!string.IsNullOrWhiteSpace(entry.Action))
				{
					_ = sb.AppendLine();
					_ = sb.AppendLine(CultureInfo.InvariantCulture, $"**Action:** {entry.Action}");
				}

				_ = sb.AppendLine();
			}
		}
	}

	private static void RenderKnownIssuesAsciidoc(
		StringBuilder sb,
		List<ChangelogData> knownIssues,
		string repo,
		bool subsections,
		HashSet<string> featureIdsToHide,
		IReadOnlyDictionary<string, RenderBlockersEntry>? renderBlockers,
		Dictionary<ChangelogData, HashSet<string>> entryToBundleProducts,
		Dictionary<ChangelogData, string> entryToRepo,
		Dictionary<ChangelogData, bool> entryToHideLinks
	)
	{
		var groupedByArea = knownIssues.GroupBy(e => GetComponent(e)).OrderBy(g => g.Key).ToList();
		foreach (var areaGroup in groupedByArea)
		{
			var componentName = !string.IsNullOrWhiteSpace(areaGroup.Key) ? areaGroup.Key : "General";
			var formattedComponent = ChangelogTextUtilities.FormatAreaHeader(componentName);

			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"{formattedComponent}::");
			_ = sb.AppendLine();

			foreach (var entry in areaGroup)
			{
				var bundleProductIds = entryToBundleProducts.GetValueOrDefault(entry, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
				var shouldHide = (!string.IsNullOrWhiteSpace(entry.FeatureId) && featureIdsToHide.Contains(entry.FeatureId)) ||
					ChangelogMarkdownRenderer.ShouldBlockEntry(entry, bundleProductIds, renderBlockers, out _);

				if (shouldHide)
				{
					_ = sb.AppendLine("// ");
				}

				_ = sb.Append("* ");
				_ = sb.Append(ChangelogTextUtilities.Beautify(entry.Title));

				var hasPr = !string.IsNullOrWhiteSpace(entry.Pr);
				var hasIssues = entry.Issues != null && entry.Issues.Count > 0;

				if (hasPr || hasIssues)
				{
					var entryRepo = entryToRepo.GetValueOrDefault(entry, repo);
					var hideLinks = entryToHideLinks.GetValueOrDefault(entry, false);
					_ = sb.Append(' ');
					if (hasPr)
					{
						_ = sb.Append(ChangelogTextUtilities.FormatPrLinkAsciidoc(entry.Pr!, entryRepo, hideLinks));
						_ = sb.Append(' ');
					}
					if (hasIssues)
					{
						foreach (var issue in entry.Issues!)
						{
							_ = sb.Append(ChangelogTextUtilities.FormatIssueLinkAsciidoc(issue, entryRepo, hideLinks));
							_ = sb.Append(' ');
						}
					}
				}

				if (!string.IsNullOrWhiteSpace(entry.Description))
				{
					_ = sb.AppendLine();
					var indented = ChangelogTextUtilities.Indent(entry.Description);
					if (shouldHide)
					{
						var indentedLines = indented.Split('\n');
						foreach (var line in indentedLines)
						{
							_ = sb.AppendLine(CultureInfo.InvariantCulture, $"// {line}");
						}
					}
					else
					{
						_ = sb.AppendLine(indented);
					}
				}

				if (!string.IsNullOrWhiteSpace(entry.Impact))
				{
					_ = sb.AppendLine();
					_ = sb.AppendLine(CultureInfo.InvariantCulture, $"**Impact:** {entry.Impact}");
				}

				if (!string.IsNullOrWhiteSpace(entry.Action))
				{
					_ = sb.AppendLine();
					_ = sb.AppendLine(CultureInfo.InvariantCulture, $"**Action:** {entry.Action}");
				}

				_ = sb.AppendLine();
			}
		}
	}

	private static string GetComponent(ChangelogData entry)
	{
		// Map areas (list) to component (string) - use first area or empty string
		if (entry.Areas != null && entry.Areas.Count > 0)
		{
			return entry.Areas[0];
		}
		return string.Empty;
	}
}
