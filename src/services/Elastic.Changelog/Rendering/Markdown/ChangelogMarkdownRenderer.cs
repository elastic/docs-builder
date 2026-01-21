// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.IO.Abstractions;
using System.Text;
using Elastic.Changelog.Configuration;
using Elastic.Documentation.Changelog;

namespace Elastic.Changelog.Rendering.Markdown;

/// <summary>
/// Renderer for changelog markdown output
/// </summary>
public class ChangelogMarkdownRenderer(IFileSystem fileSystem)
{
	public async Task RenderIndexMarkdown(
		string outputDir,
		string title,
		string titleSlug,
		string repo,
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
		var features = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Feature, []);
		var enhancements = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Enhancement, []);
		var security = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Security, []);
		var bugFixes = entriesByType.GetValueOrDefault(ChangelogEntryTypes.BugFix, []);
		var docs = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Docs, []);
		var regressions = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Regression, []);
		var other = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Other, []);

		var hasBreakingChanges = entriesByType.ContainsKey(ChangelogEntryTypes.BreakingChange);
		var hasDeprecations = entriesByType.ContainsKey(ChangelogEntryTypes.Deprecation);
		var hasKnownIssues = entriesByType.ContainsKey(ChangelogEntryTypes.KnownIssue);

		var otherLinks = new List<string>();
		if (hasKnownIssues)
		{
			otherLinks.Add($"[Known issues](/release-notes/known-issues.md#{repo}-{titleSlug}-known-issues)");
		}
		if (hasBreakingChanges)
		{
			otherLinks.Add($"[Breaking changes](/release-notes/breaking-changes.md#{repo}-{titleSlug}-breaking-changes)");
		}
		if (hasDeprecations)
		{
			otherLinks.Add($"[Deprecations](/release-notes/deprecations.md#{repo}-{titleSlug}-deprecations)");
		}

		var sb = new StringBuilder();
		_ = sb.AppendLine(CultureInfo.InvariantCulture, $"## {title} [{repo}-release-notes-{titleSlug}]");

		if (otherLinks.Count > 0)
		{
			var linksText = string.Join(" and ", otherLinks);
			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"_{linksText}._");
			_ = sb.AppendLine();
		}

		var hasAnyEntries = features.Count > 0 || enhancements.Count > 0 || security.Count > 0 || bugFixes.Count > 0 || docs.Count > 0 || regressions.Count > 0 || other.Count > 0;

		if (hasAnyEntries)
		{
			if (features.Count > 0 || enhancements.Count > 0)
			{
				_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Features and enhancements [{repo}-{titleSlug}-features-enhancements]");
				var combined = features.Concat(enhancements).ToList();
				RenderEntriesByArea(sb, combined, subsections, featureIdsToHide, renderBlockers, entryToBundleProducts, entryToRepo, entryToHideLinks);
			}

			if (security.Count > 0 || bugFixes.Count > 0)
			{
				_ = sb.AppendLine();
				_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Fixes [{repo}-{titleSlug}-fixes]");
				var combined = security.Concat(bugFixes).ToList();
				RenderEntriesByArea(sb, combined, subsections, featureIdsToHide, renderBlockers, entryToBundleProducts, entryToRepo, entryToHideLinks);
			}

			if (docs.Count > 0)
			{
				_ = sb.AppendLine();
				_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Documentation [{repo}-{titleSlug}-docs]");
				RenderEntriesByArea(sb, docs, subsections, featureIdsToHide, renderBlockers, entryToBundleProducts, entryToRepo, entryToHideLinks);
			}

			if (regressions.Count > 0)
			{
				_ = sb.AppendLine();
				_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Regressions [{repo}-{titleSlug}-regressions]");
				RenderEntriesByArea(sb, regressions, subsections, featureIdsToHide, renderBlockers, entryToBundleProducts, entryToRepo, entryToHideLinks);
			}

			if (other.Count > 0)
			{
				_ = sb.AppendLine();
				_ = sb.AppendLine(CultureInfo.InvariantCulture, $"### Other changes [{repo}-{titleSlug}-other]");
				RenderEntriesByArea(sb, other, subsections, featureIdsToHide, renderBlockers, entryToBundleProducts, entryToRepo, entryToHideLinks);
			}
		}
		else
		{
			_ = sb.AppendLine("_No new features, enhancements, or fixes._");
		}

		var indexPath = fileSystem.Path.Combine(outputDir, titleSlug, "index.md");
		var indexDir = fileSystem.Path.GetDirectoryName(indexPath);
		if (!string.IsNullOrWhiteSpace(indexDir) && !fileSystem.Directory.Exists(indexDir))
		{
			_ = fileSystem.Directory.CreateDirectory(indexDir);
		}

		await fileSystem.File.WriteAllTextAsync(indexPath, sb.ToString(), ctx);
	}

	public async Task RenderBreakingChangesMarkdown(
		string outputDir,
		string title,
		string titleSlug,
		string repo,
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
		var breakingChanges = entriesByType.GetValueOrDefault(ChangelogEntryTypes.BreakingChange, []);

		var sb = new StringBuilder();
		_ = sb.AppendLine(CultureInfo.InvariantCulture, $"## {title} [{repo}-{titleSlug}-breaking-changes]");

		if (breakingChanges.Count > 0)
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
					_ = sb.AppendLine();
					_ = sb.AppendLine(CultureInfo.InvariantCulture, $"**{header}**");
				}

				foreach (var entry in group)
				{
					var bundleProductIds = entryToBundleProducts.GetValueOrDefault(entry, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
					var entryRepo = entryToRepo.GetValueOrDefault(entry, repo);
					var entryHideLinks = entryToHideLinks.GetValueOrDefault(entry, false);
					var shouldHide = (!string.IsNullOrWhiteSpace(entry.FeatureId) && featureIdsToHide.Contains(entry.FeatureId)) ||
						ShouldBlockEntry(entry, bundleProductIds, renderBlockers, out _);

					_ = sb.AppendLine();
					if (shouldHide)
					{
						_ = sb.AppendLine("<!--");
					}
					_ = sb.AppendLine(CultureInfo.InvariantCulture, $"::::{{dropdown}} {ChangelogTextUtilities.Beautify(entry.Title)}");
					_ = sb.AppendLine(entry.Description ?? "% Describe the functionality that changed");
					_ = sb.AppendLine();
					RenderPrIssueLinks(sb, entry, entryRepo, entryHideLinks);

					_ = sb.AppendLine(!string.IsNullOrWhiteSpace(entry.Impact)
						? "**Impact**<br>" + entry.Impact
						: "% **Impact**<br>_Add a description of the impact_");

					_ = sb.AppendLine();

					_ = sb.AppendLine(!string.IsNullOrWhiteSpace(entry.Action)
						? "**Action**<br>" + entry.Action
						: "% **Action**<br>_Add a description of the what action to take_");

					_ = sb.AppendLine("::::");
					if (shouldHide)
					{
						_ = sb.AppendLine("-->");
					}
				}
			}
		}
		else
		{
			_ = sb.AppendLine("_No breaking changes._");
		}

		var breakingPath = fileSystem.Path.Combine(outputDir, titleSlug, "breaking-changes.md");
		var breakingDir = fileSystem.Path.GetDirectoryName(breakingPath);
		if (!string.IsNullOrWhiteSpace(breakingDir) && !fileSystem.Directory.Exists(breakingDir))
		{
			_ = fileSystem.Directory.CreateDirectory(breakingDir);
		}

		await fileSystem.File.WriteAllTextAsync(breakingPath, sb.ToString(), ctx);
	}

	public async Task RenderDeprecationsMarkdown(
		string outputDir,
		string title,
		string titleSlug,
		string repo,
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
		var deprecations = entriesByType.GetValueOrDefault(ChangelogEntryTypes.Deprecation, []);

		var sb = new StringBuilder();
		_ = sb.AppendLine(CultureInfo.InvariantCulture, $"## {title} [{repo}-{titleSlug}-deprecations]");

		if (deprecations.Count > 0)
		{
			var groupedByArea = subsections
				? deprecations.GroupBy(e => GetComponent(e)).OrderBy(g => g.Key).ToList()
				: deprecations.GroupBy(e => GetComponent(e)).ToList();
			foreach (var areaGroup in groupedByArea)
			{
				if (subsections && !string.IsNullOrWhiteSpace(areaGroup.Key))
				{
					var header = ChangelogTextUtilities.FormatAreaHeader(areaGroup.Key);
					_ = sb.AppendLine();
					_ = sb.AppendLine(CultureInfo.InvariantCulture, $"**{header}**");
				}

				foreach (var entry in areaGroup)
				{
					var bundleProductIds = entryToBundleProducts.GetValueOrDefault(entry, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
					var entryRepo = entryToRepo.GetValueOrDefault(entry, repo);
					var entryHideLinks = entryToHideLinks.GetValueOrDefault(entry, false);
					var shouldHide = (!string.IsNullOrWhiteSpace(entry.FeatureId) && featureIdsToHide.Contains(entry.FeatureId)) ||
						ShouldBlockEntry(entry, bundleProductIds, renderBlockers, out _);

					_ = sb.AppendLine();
					if (shouldHide)
					{
						_ = sb.AppendLine("<!--");
					}
					_ = sb.AppendLine(CultureInfo.InvariantCulture, $"::::{{dropdown}} {ChangelogTextUtilities.Beautify(entry.Title)}");
					_ = sb.AppendLine(entry.Description ?? "% Describe the functionality that was deprecated");
					_ = sb.AppendLine();
					RenderPrIssueLinks(sb, entry, entryRepo, entryHideLinks);

					_ = sb.AppendLine(!string.IsNullOrWhiteSpace(entry.Impact)
						? "**Impact**<br>" + entry.Impact
						: "% **Impact**<br>_Add a description of the impact_");

					_ = sb.AppendLine();

					_ = sb.AppendLine(!string.IsNullOrWhiteSpace(entry.Action)
						? "**Action**<br>" + entry.Action
						: "% **Action**<br>_Add a description of the what action to take_");

					_ = sb.AppendLine("::::");
					if (shouldHide)
					{
						_ = sb.AppendLine("-->");
					}
				}
			}
		}
		else
		{
			_ = sb.AppendLine("_No deprecations._");
		}

		var deprecationsPath = fileSystem.Path.Combine(outputDir, titleSlug, "deprecations.md");
		var deprecationsDir = fileSystem.Path.GetDirectoryName(deprecationsPath);
		if (!string.IsNullOrWhiteSpace(deprecationsDir) && !fileSystem.Directory.Exists(deprecationsDir))
		{
			_ = fileSystem.Directory.CreateDirectory(deprecationsDir);
		}

		await fileSystem.File.WriteAllTextAsync(deprecationsPath, sb.ToString(), ctx);
	}

	public async Task RenderKnownIssuesMarkdown(
		string outputDir,
		string title,
		string titleSlug,
		string repo,
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
		var knownIssues = entriesByType.GetValueOrDefault(ChangelogEntryTypes.KnownIssue, []);

		var sb = new StringBuilder();
		_ = sb.AppendLine(CultureInfo.InvariantCulture, $"## {title} [{repo}-{titleSlug}-known-issues]");

		if (knownIssues.Count > 0)
		{
			var groupedByArea = subsections
				? knownIssues.GroupBy(e => GetComponent(e)).OrderBy(g => g.Key).ToList()
				: knownIssues.GroupBy(e => GetComponent(e)).ToList();
			foreach (var areaGroup in groupedByArea)
			{
				if (subsections && !string.IsNullOrWhiteSpace(areaGroup.Key))
				{
					var header = ChangelogTextUtilities.FormatAreaHeader(areaGroup.Key);
					_ = sb.AppendLine();
					_ = sb.AppendLine(CultureInfo.InvariantCulture, $"**{header}**");
				}

				foreach (var entry in areaGroup)
				{
					var bundleProductIds = entryToBundleProducts.GetValueOrDefault(entry, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
					var entryRepo = entryToRepo.GetValueOrDefault(entry, repo);
					var entryHideLinks = entryToHideLinks.GetValueOrDefault(entry, false);
					var shouldHide = (!string.IsNullOrWhiteSpace(entry.FeatureId) && featureIdsToHide.Contains(entry.FeatureId)) ||
						ShouldBlockEntry(entry, bundleProductIds, renderBlockers, out _);

					_ = sb.AppendLine();
					if (shouldHide)
					{
						_ = sb.AppendLine("<!--");
					}
					_ = sb.AppendLine(CultureInfo.InvariantCulture, $"::::{{dropdown}} {ChangelogTextUtilities.Beautify(entry.Title)}");
					_ = sb.AppendLine(entry.Description ?? "% Describe the known issue");
					_ = sb.AppendLine();
					RenderPrIssueLinks(sb, entry, entryRepo, entryHideLinks);

					_ = sb.AppendLine(!string.IsNullOrWhiteSpace(entry.Impact)
						? "**Impact**<br>" + entry.Impact
						: "% **Impact**<br>_Add a description of the impact_");

					_ = sb.AppendLine();

					_ = sb.AppendLine(!string.IsNullOrWhiteSpace(entry.Action)
						? "**Action**<br>" + entry.Action
						: "% **Action**<br>_Add a description of the what action to take_");

					_ = sb.AppendLine("::::");
					if (shouldHide)
					{
						_ = sb.AppendLine("-->");
					}
				}
			}
		}
		else
		{
			_ = sb.AppendLine("_No known issues._");
		}

		var knownIssuesPath = fileSystem.Path.Combine(outputDir, titleSlug, "known-issues.md");
		var knownIssuesDir = fileSystem.Path.GetDirectoryName(knownIssuesPath);
		if (!string.IsNullOrWhiteSpace(knownIssuesDir) && !fileSystem.Directory.Exists(knownIssuesDir))
		{
			_ = fileSystem.Directory.CreateDirectory(knownIssuesDir);
		}

		await fileSystem.File.WriteAllTextAsync(knownIssuesPath, sb.ToString(), ctx);
	}

	private void RenderEntriesByArea(
		StringBuilder sb,
		List<ChangelogData> entries,
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
			if (subsections && !string.IsNullOrWhiteSpace(areaGroup.Key))
			{
				var header = ChangelogTextUtilities.FormatAreaHeader(areaGroup.Key);
				_ = sb.AppendLine();
				_ = sb.AppendLine(CultureInfo.InvariantCulture, $"**{header}**");
			}

			foreach (var entry in areaGroup)
			{
				var bundleProductIds = entryToBundleProducts.GetValueOrDefault(entry, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
				var entryRepo = entryToRepo.GetValueOrDefault(entry, "elastic");
				var entryHideLinks = entryToHideLinks.GetValueOrDefault(entry, false);
				var shouldHide = (!string.IsNullOrWhiteSpace(entry.FeatureId) && featureIdsToHide.Contains(entry.FeatureId)) ||
					ShouldBlockEntry(entry, bundleProductIds, renderBlockers, out _);

				if (shouldHide)
				{
					_ = sb.Append("% ");
				}
				_ = sb.Append("* ");
				_ = sb.Append(ChangelogTextUtilities.Beautify(entry.Title));

				var hasCommentedLinks = false;
				if (entryHideLinks)
				{
					// When hiding private links, put them on separate lines as comments with proper indentation
					if (!string.IsNullOrWhiteSpace(entry.Pr))
					{
						_ = sb.AppendLine();
						if (shouldHide)
						{
							_ = sb.Append("% ");
						}
						_ = sb.Append("  ");
						_ = sb.Append(ChangelogTextUtilities.FormatPrLink(entry.Pr, entryRepo, entryHideLinks));
						hasCommentedLinks = true;
					}

					if (entry.Issues != null && entry.Issues.Count > 0)
					{
						foreach (var issue in entry.Issues)
						{
							_ = sb.AppendLine();
							if (shouldHide)
							{
								_ = sb.Append("% ");
							}
							_ = sb.Append("  ");
							_ = sb.Append(ChangelogTextUtilities.FormatIssueLink(issue, entryRepo, entryHideLinks));
							hasCommentedLinks = true;
						}
					}

					// Add newline after the last link if there are commented links
					if (hasCommentedLinks)
					{
						_ = sb.AppendLine();
					}
				}
				else
				{
					_ = sb.Append(' ');
					if (!string.IsNullOrWhiteSpace(entry.Pr))
					{
						_ = sb.Append(ChangelogTextUtilities.FormatPrLink(entry.Pr, entryRepo, entryHideLinks));
						_ = sb.Append(' ');
					}

					if (entry.Issues != null && entry.Issues.Count > 0)
					{
						foreach (var issue in entry.Issues)
						{
							_ = sb.Append(ChangelogTextUtilities.FormatIssueLink(issue, entryRepo, entryHideLinks));
							_ = sb.Append(' ');
						}
					}
				}

				if (!string.IsNullOrWhiteSpace(entry.Description))
				{
					// Add blank line before description
					_ = sb.AppendLine(entryHideLinks && hasCommentedLinks ? "  " : "");
					var indented = ChangelogTextUtilities.Indent(entry.Description);
					if (shouldHide)
					{
						// Comment out each line of the description
						var indentedLines = indented.Split('\n');
						foreach (var line in indentedLines)
						{
							_ = sb.Append("% ");
							_ = sb.AppendLine(line);
						}
					}
					else
					{
						_ = sb.AppendLine(indented);
					}
				}
				else
				{
					_ = sb.AppendLine();
				}
			}
		}
	}

	private static void RenderPrIssueLinks(StringBuilder sb, ChangelogData entry, string entryRepo, bool entryHideLinks)
	{
		var hasPr = !string.IsNullOrWhiteSpace(entry.Pr);
		var hasIssues = entry.Issues != null && entry.Issues.Count > 0;
		if (hasPr || hasIssues)
		{
			if (entryHideLinks)
			{
				// When hiding private links, put them on separate lines as comments
				if (hasPr)
				{
					_ = sb.AppendLine(ChangelogTextUtilities.FormatPrLink(entry.Pr!, entryRepo, entryHideLinks));
				}
				if (hasIssues)
				{
					foreach (var issue in entry.Issues!)
					{
						_ = sb.AppendLine(ChangelogTextUtilities.FormatIssueLink(issue, entryRepo, entryHideLinks));
					}
				}
				_ = sb.AppendLine("For more information, check the pull request or issue above.");
			}
			else
			{
				_ = sb.Append("For more information, check ");
				if (hasPr)
				{
					_ = sb.Append(ChangelogTextUtilities.FormatPrLink(entry.Pr!, entryRepo, entryHideLinks));
				}
				if (hasIssues)
				{
					foreach (var issue in entry.Issues!)
					{
						_ = sb.Append(' ');
						_ = sb.Append(ChangelogTextUtilities.FormatIssueLink(issue, entryRepo, entryHideLinks));
					}
				}
				_ = sb.AppendLine(".");
			}
			_ = sb.AppendLine();
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

	internal static bool ShouldBlockEntry(ChangelogData entry, HashSet<string> bundleProductIds, IReadOnlyDictionary<string, RenderBlockersEntry>? renderBlockers, out List<string> reasons)
	{
		reasons = [];
		if (renderBlockers == null || renderBlockers.Count == 0)
		{
			return false;
		}

		// Bundle must have products to be blocked
		if (bundleProductIds == null || bundleProductIds.Count == 0)
		{
			return false;
		}

		// Extract area values from entry (case-insensitive comparison)
		var entryAreas = entry.Areas != null && entry.Areas.Count > 0
			? entry.Areas
				.Where(a => !string.IsNullOrWhiteSpace(a))
				.Select(a => a!)
				.ToHashSet(StringComparer.OrdinalIgnoreCase)
			: new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		// Extract type from entry (case-insensitive comparison)
		var entryType = !string.IsNullOrWhiteSpace(entry.Type)
			? entry.Type
			: null;

		// Check each render_blockers entry
		foreach (var (productKey, blockersEntry) in renderBlockers)
		{
			if (blockersEntry == null)
			{
				continue;
			}

			// Parse product key - can be comma-separated (e.g., "elasticsearch, cloud-serverless")
			var productKeys = productKey
				.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Where(p => !string.IsNullOrWhiteSpace(p))
				.ToHashSet(StringComparer.OrdinalIgnoreCase);

			// Check if any product in the bundle matches any product in the key
			var matchingProducts = bundleProductIds.Intersect(productKeys, StringComparer.OrdinalIgnoreCase).ToList();
			if (matchingProducts.Count == 0)
			{
				continue;
			}

			var isBlocked = false;
			var blockReasons = new List<string>();

			// Check areas if specified
			if (blockersEntry.Areas != null && blockersEntry.Areas.Count > 0 && entryAreas.Count > 0)
			{
				var matchingAreas = entryAreas.Intersect(blockersEntry.Areas, StringComparer.OrdinalIgnoreCase).ToList();
				if (matchingAreas.Count > 0)
				{
					isBlocked = true;
					var reasonsForProductsAndAreas = matchingProducts
						.SelectMany(product => matchingAreas
							.Select(area => $"product '{product}' with area '{area}'"))
						.Distinct();

					foreach (var reason in reasonsForProductsAndAreas.Where(reason => !blockReasons.Contains(reason)))
					{
						blockReasons.Add(reason);
					}
				}
			}

			// Check types if specified
			if (blockersEntry.Types != null && blockersEntry.Types.Count > 0 && !string.IsNullOrWhiteSpace(entryType))
			{
				var matchingTypes = blockersEntry.Types
					.Where(t => string.Equals(t, entryType, StringComparison.OrdinalIgnoreCase))
					.ToList();
				if (matchingTypes.Count > 0)
				{
					isBlocked = true;
					var reasonsForProducts = matchingProducts
						.SelectMany(product => matchingTypes
							.Select(type => $"product '{product}' with type '{type}'"))
						.Distinct();

					foreach (var reason in reasonsForProducts.Where(reason => !blockReasons.Contains(reason)))
					{
						blockReasons.Add(reason);
					}
				}
			}

			if (isBlocked)
			{
				reasons.AddRange(blockReasons);
				return true;
			}
		}

		return false;
	}
}
