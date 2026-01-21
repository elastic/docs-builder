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

namespace Elastic.Changelog.Rendering.Asciidoc;

/// <summary>
/// Renderer for changelog asciidoc output
/// </summary>
public class ChangelogAsciidocRenderer(IFileSystem fileSystem)
{
	public async Task RenderAsciidoc(
		ChangelogRenderContext context,
		List<ChangelogData> entries,
		Cancel ctx
	)
	{
		var sb = new StringBuilder();

		// Add anchor
		_ = sb.AppendLine(CultureInfo.InvariantCulture, $"[[release-notes-{context.TitleSlug}]]");
		_ = sb.AppendLine(CultureInfo.InvariantCulture, $"== {context.Title}");
		_ = sb.AppendLine();

		// Group entries by type
		var entriesByType = context.EntriesByType;
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
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"[[security-updates-{context.TitleSlug}]]");
			_ = sb.AppendLine("[float]");
			_ = sb.AppendLine("=== Security updates");
			_ = sb.AppendLine();
			RenderEntriesByAreaAsciidoc(sb, security, context);
			_ = sb.AppendLine();
		}

		// Render bug fixes
		if (bugFixes.Count > 0)
		{
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"[[bug-fixes-{context.TitleSlug}]]");
			_ = sb.AppendLine("[float]");
			_ = sb.AppendLine("=== Bug fixes");
			_ = sb.AppendLine();
			RenderEntriesByAreaAsciidoc(sb, bugFixes, context);
			_ = sb.AppendLine();
		}

		// Render features and enhancements
		if (features.Count > 0 || enhancements.Count > 0)
		{
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"[[features-enhancements-{context.TitleSlug}]]");
			_ = sb.AppendLine("[float]");
			_ = sb.AppendLine("=== New features and enhancements");
			_ = sb.AppendLine();
			var combined = features.Concat(enhancements).ToList();
			RenderEntriesByAreaAsciidoc(sb, combined, context);
			_ = sb.AppendLine();
		}

		// Render breaking changes
		if (breakingChanges.Count > 0)
		{
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"[[breaking-changes-{context.TitleSlug}]]");
			_ = sb.AppendLine("[float]");
			_ = sb.AppendLine("=== Breaking changes");
			_ = sb.AppendLine();
			RenderBreakingChangesAsciidoc(sb, breakingChanges, context);
			_ = sb.AppendLine();
		}

		// Render deprecations
		if (deprecations.Count > 0)
		{
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"[[deprecations-{context.TitleSlug}]]");
			_ = sb.AppendLine("[float]");
			_ = sb.AppendLine("=== Deprecations");
			_ = sb.AppendLine();
			RenderDeprecationsAsciidoc(sb, deprecations, context);
			_ = sb.AppendLine();
		}

		// Render known issues
		if (knownIssues.Count > 0)
		{
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"[[known-issues-{context.TitleSlug}]]");
			_ = sb.AppendLine("[float]");
			_ = sb.AppendLine("=== Known issues");
			_ = sb.AppendLine();
			RenderKnownIssuesAsciidoc(sb, knownIssues, context);
			_ = sb.AppendLine();
		}

		// Render documentation changes
		if (docs.Count > 0)
		{
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"[[docs-{context.TitleSlug}]]");
			_ = sb.AppendLine("[float]");
			_ = sb.AppendLine("=== Documentation");
			_ = sb.AppendLine();
			RenderEntriesByAreaAsciidoc(sb, docs, context);
			_ = sb.AppendLine();
		}

		// Render regressions
		if (regressions.Count > 0)
		{
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"[[regressions-{context.TitleSlug}]]");
			_ = sb.AppendLine("[float]");
			_ = sb.AppendLine("=== Regressions");
			_ = sb.AppendLine();
			RenderEntriesByAreaAsciidoc(sb, regressions, context);
			_ = sb.AppendLine();
		}

		// Render other changes
		if (other.Count > 0)
		{
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"[[other-{context.TitleSlug}]]");
			_ = sb.AppendLine("[float]");
			_ = sb.AppendLine("=== Other changes");
			_ = sb.AppendLine();
			RenderEntriesByAreaAsciidoc(sb, other, context);
			_ = sb.AppendLine();
		}

		// Write the asciidoc file
		var asciidocPath = fileSystem.Path.Combine(context.OutputDir, $"{context.TitleSlug}.asciidoc");
		var asciidocDir = fileSystem.Path.GetDirectoryName(asciidocPath);
		if (!string.IsNullOrWhiteSpace(asciidocDir) && !fileSystem.Directory.Exists(asciidocDir))
			_ = fileSystem.Directory.CreateDirectory(asciidocDir);

		await fileSystem.File.WriteAllTextAsync(asciidocPath, sb.ToString(), ctx);
	}

	private static void RenderEntriesByAreaAsciidoc(
		StringBuilder sb,
		List<ChangelogData> entries,
		ChangelogRenderContext context
	)
	{
		var groupedByArea = context.Subsections
			? entries.GroupBy(ChangelogRenderUtilities.GetComponent).OrderBy(g => g.Key).ToList()
			: entries.GroupBy(ChangelogRenderUtilities.GetComponent).ToList();
		foreach (var areaGroup in groupedByArea)
		{
			var componentName = !string.IsNullOrWhiteSpace(areaGroup.Key) ? areaGroup.Key : "General";

			// Format component name (capitalize first letter, replace hyphens with spaces)
			var formattedComponent = ChangelogTextUtilities.FormatAreaHeader(componentName);

			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"{formattedComponent}::");
			_ = sb.AppendLine();

			foreach (var entry in areaGroup)
			{
				var bundleProductIds = context.EntryToBundleProducts.GetValueOrDefault(entry, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
				var shouldHide = ChangelogRenderUtilities.ShouldHideEntry(entry, context.FeatureIdsToHide, bundleProductIds, context.RenderBlockers);

				if (shouldHide)
					_ = sb.AppendLine("// ");

				_ = sb.Append("* ");
				_ = sb.Append(ChangelogTextUtilities.Beautify(entry.Title));

				var hasPr = !string.IsNullOrWhiteSpace(entry.Pr);
				var hasIssues = entry.Issues != null && entry.Issues.Count > 0;

				if (hasPr || hasIssues)
				{
					var entryRepo = context.EntryToRepo.GetValueOrDefault(entry, context.Repo);
					var hideLinks = context.EntryToHideLinks.GetValueOrDefault(entry, false);
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
							_ = sb.AppendLine(CultureInfo.InvariantCulture, $"// {line}");
					}
					else
						_ = sb.AppendLine(indented);
				}

				_ = sb.AppendLine();
			}
		}
	}

	private static void RenderBreakingChangesAsciidoc(
		StringBuilder sb,
		List<ChangelogData> breakingChanges,
		ChangelogRenderContext context
	)
	{
		// Group by subtype if subsections is enabled, otherwise group by area
		var groupedEntries = context.Subsections
			? breakingChanges.GroupBy(e => string.IsNullOrWhiteSpace(e.Subtype) ? string.Empty : e.Subtype).OrderBy(g => g.Key).ToList()
			: breakingChanges.GroupBy(ChangelogRenderUtilities.GetComponent).ToList();

		foreach (var group in groupedEntries)
		{
			if (context.Subsections && !string.IsNullOrWhiteSpace(group.Key))
			{
				var header = ChangelogTextUtilities.FormatSubtypeHeader(group.Key);
				_ = sb.AppendLine(CultureInfo.InvariantCulture, $"**{header}**");
				_ = sb.AppendLine();
			}

			foreach (var entry in group)
			{
				var bundleProductIds = context.EntryToBundleProducts.GetValueOrDefault(entry, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
				var shouldHide = ChangelogRenderUtilities.ShouldHideEntry(entry, context.FeatureIdsToHide, bundleProductIds, context.RenderBlockers);

				if (shouldHide)
					_ = sb.AppendLine("// ");

				_ = sb.Append("* ");
				_ = sb.Append(ChangelogTextUtilities.Beautify(entry.Title));

				var hasPr = !string.IsNullOrWhiteSpace(entry.Pr);
				var hasIssues = entry.Issues != null && entry.Issues.Count > 0;

				if (hasPr || hasIssues)
				{
					var entryRepo = context.EntryToRepo.GetValueOrDefault(entry, context.Repo);
					var hideLinks = context.EntryToHideLinks.GetValueOrDefault(entry, false);
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
							_ = sb.AppendLine(CultureInfo.InvariantCulture, $"// {line}");
					}
					else
						_ = sb.AppendLine(indented);
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
		ChangelogRenderContext context
	)
	{
		var groupedByArea = deprecations.GroupBy(ChangelogRenderUtilities.GetComponent).OrderBy(g => g.Key).ToList();
		foreach (var areaGroup in groupedByArea)
		{
			var componentName = !string.IsNullOrWhiteSpace(areaGroup.Key) ? areaGroup.Key : "General";
			var formattedComponent = ChangelogTextUtilities.FormatAreaHeader(componentName);

			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"{formattedComponent}::");
			_ = sb.AppendLine();

			foreach (var entry in areaGroup)
			{
				var bundleProductIds = context.EntryToBundleProducts.GetValueOrDefault(entry, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
				var shouldHide = ChangelogRenderUtilities.ShouldHideEntry(entry, context.FeatureIdsToHide, bundleProductIds, context.RenderBlockers);

				if (shouldHide)
					_ = sb.AppendLine("// ");

				_ = sb.Append("* ");
				_ = sb.Append(ChangelogTextUtilities.Beautify(entry.Title));

				var hasPr = !string.IsNullOrWhiteSpace(entry.Pr);
				var hasIssues = entry.Issues != null && entry.Issues.Count > 0;

				if (hasPr || hasIssues)
				{
					var entryRepo = context.EntryToRepo.GetValueOrDefault(entry, context.Repo);
					var hideLinks = context.EntryToHideLinks.GetValueOrDefault(entry, false);
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
							_ = sb.AppendLine(CultureInfo.InvariantCulture, $"// {line}");
					}
					else
						_ = sb.AppendLine(indented);
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
		ChangelogRenderContext context
	)
	{
		var groupedByArea = knownIssues.GroupBy(ChangelogRenderUtilities.GetComponent).OrderBy(g => g.Key).ToList();
		foreach (var areaGroup in groupedByArea)
		{
			var componentName = !string.IsNullOrWhiteSpace(areaGroup.Key) ? areaGroup.Key : "General";
			var formattedComponent = ChangelogTextUtilities.FormatAreaHeader(componentName);

			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"{formattedComponent}::");
			_ = sb.AppendLine();

			foreach (var entry in areaGroup)
			{
				var bundleProductIds = context.EntryToBundleProducts.GetValueOrDefault(entry, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
				var shouldHide = ChangelogRenderUtilities.ShouldHideEntry(entry, context.FeatureIdsToHide, bundleProductIds, context.RenderBlockers);

				if (shouldHide)
					_ = sb.AppendLine("// ");

				_ = sb.Append("* ");
				_ = sb.Append(ChangelogTextUtilities.Beautify(entry.Title));

				var hasPr = !string.IsNullOrWhiteSpace(entry.Pr);
				var hasIssues = entry.Issues != null && entry.Issues.Count > 0;

				if (hasPr || hasIssues)
				{
					var entryRepo = context.EntryToRepo.GetValueOrDefault(entry, context.Repo);
					var hideLinks = context.EntryToHideLinks.GetValueOrDefault(entry, false);
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
							_ = sb.AppendLine(CultureInfo.InvariantCulture, $"// {line}");
					}
					else
						_ = sb.AppendLine(indented);
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
}
