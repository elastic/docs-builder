// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text;
using Elastic.Documentation;
using static System.Globalization.CultureInfo;
using static Elastic.Documentation.ChangelogEntryType;

namespace Elastic.Changelog.Rendering.Markdown;

/// <summary>
/// Renderer for the index.md changelog file containing features, enhancements, fixes, docs, regressions, and other changes
/// </summary>
public class IndexMarkdownRenderer(IFileSystem fileSystem) : MarkdownRendererBase(fileSystem)
{
	/// <inheritdoc />
	public override string OutputFileName => "index.md";

	/// <inheritdoc />
	public override async Task RenderAsync(ChangelogRenderContext context, Cancel ctx)
	{
		var entriesByType = context.EntriesByType;
		var features = entriesByType.GetValueOrDefault(Feature, []);
		var enhancements = entriesByType.GetValueOrDefault(Enhancement, []);
		var security = entriesByType.GetValueOrDefault(Security, []);
		var bugFixes = entriesByType.GetValueOrDefault(BugFix, []);
		var docs = entriesByType.GetValueOrDefault(Docs, []);
		var regressions = entriesByType.GetValueOrDefault(Regression, []);
		var other = entriesByType.GetValueOrDefault(Other, []);

		var hasBreakingChanges = entriesByType.ContainsKey(BreakingChange);
		var hasDeprecations = entriesByType.ContainsKey(Deprecation);
		var hasKnownIssues = entriesByType.ContainsKey(KnownIssue);

		var otherLinks = new List<string>();
		if (hasKnownIssues)
			otherLinks.Add($"[Known issues](/release-notes/known-issues.md#{context.Repo}-{context.TitleSlug}-known-issues)");
		if (hasBreakingChanges)
			otherLinks.Add($"[Breaking changes](/release-notes/breaking-changes.md#{context.Repo}-{context.TitleSlug}-breaking-changes)");
		if (hasDeprecations)
			otherLinks.Add($"[Deprecations](/release-notes/deprecations.md#{context.Repo}-{context.TitleSlug}-deprecations)");

		var sb = new StringBuilder();
		_ = sb.AppendLine(InvariantCulture, $"## {context.Title} [{context.Repo}-release-notes-{context.TitleSlug}]");

		if (otherLinks.Count > 0)
		{
			var linksText = string.Join(" and ", otherLinks);
			_ = sb.AppendLine(InvariantCulture, $"_{linksText}._");
			_ = sb.AppendLine();
		}

		var hasAnyEntries = features.Count > 0 || enhancements.Count > 0 || security.Count > 0 || bugFixes.Count > 0 || docs.Count > 0 || regressions.Count > 0 || other.Count > 0;

		// Helper to check if all entries in a collection are hidden
		bool AllEntriesHidden(IReadOnlyCollection<ChangelogEntry> entries) =>
			entries.Count > 0 && entries.All(entry =>
				ChangelogRenderUtilities.ShouldHideEntry(entry, context.FeatureIdsToHide, context));

		// Check if each category has visible entries
		var hasVisibleFeatures = (features.Count > 0 || enhancements.Count > 0) &&
			!(AllEntriesHidden(features) && AllEntriesHidden(enhancements));
		var hasVisibleFixes = (security.Count > 0 || bugFixes.Count > 0) &&
			!(AllEntriesHidden(security) && AllEntriesHidden(bugFixes));
		var hasVisibleDocs = docs.Count > 0 && !AllEntriesHidden(docs);
		var hasVisibleRegressions = regressions.Count > 0 && !AllEntriesHidden(regressions);
		var hasVisibleOther = other.Count > 0 && !AllEntriesHidden(other);

		var hasAnyVisibleEntries = hasVisibleFeatures || hasVisibleFixes || hasVisibleDocs || hasVisibleRegressions || hasVisibleOther;

		if (hasAnyEntries)
		{
			if (features.Count > 0 || enhancements.Count > 0)
			{
				var combined = features.Concat(enhancements).ToList();
				_ = sb.AppendLine(InvariantCulture, $"### Features and enhancements [{context.Repo}-{context.TitleSlug}-features-enhancements]");
				RenderEntriesByArea(sb, combined, context);
			}

			if (security.Count > 0 || bugFixes.Count > 0)
			{
				var combined = security.Concat(bugFixes).ToList();
				_ = sb.AppendLine();
				_ = sb.AppendLine(InvariantCulture, $"### Fixes [{context.Repo}-{context.TitleSlug}-fixes]");
				RenderEntriesByArea(sb, combined, context);
			}

			if (docs.Count > 0)
			{
				_ = sb.AppendLine();
				_ = sb.AppendLine(InvariantCulture, $"### Documentation [{context.Repo}-{context.TitleSlug}-docs]");
				RenderEntriesByArea(sb, docs, context);
			}

			if (regressions.Count > 0)
			{
				_ = sb.AppendLine();
				_ = sb.AppendLine(InvariantCulture, $"### Regressions [{context.Repo}-{context.TitleSlug}-regressions]");
				RenderEntriesByArea(sb, regressions, context);
			}

			if (other.Count > 0)
			{
				_ = sb.AppendLine();
				_ = sb.AppendLine(InvariantCulture, $"### Other changes [{context.Repo}-{context.TitleSlug}-other]");
				RenderEntriesByArea(sb, other, context);
			}

			// Add message if all entries are hidden
			if (!hasAnyVisibleEntries)
			{
				_ = sb.AppendLine();
				_ = sb.AppendLine("_There are no new features, enhancements, or fixes associated with this release._");
			}
		}
		else
		{
			_ = sb.AppendLine("_There are no new features, enhancements, or fixes associated with this release._");
		}

		await WriteOutputFileAsync(context.OutputDir, context.TitleSlug, sb.ToString(), ctx);
	}

	private static void RenderEntriesByArea(
		StringBuilder sb,
		IReadOnlyCollection<ChangelogEntry> entries,
		ChangelogRenderContext context)
	{
		var groupedByArea = context.Subsections
			? entries.GroupBy(ChangelogRenderUtilities.GetComponent).OrderBy(g => g.Key).ToList()
			: entries.GroupBy(ChangelogRenderUtilities.GetComponent).ToList();
		foreach (var areaGroup in groupedByArea)
		{
			// Check if all entries in this area group are hidden
			var allEntriesHidden = areaGroup.All(entry =>
				ChangelogRenderUtilities.ShouldHideEntry(entry, context.FeatureIdsToHide, context));

			if (context.Subsections && !string.IsNullOrWhiteSpace(areaGroup.Key))
			{
				var header = ChangelogTextUtilities.FormatAreaHeader(areaGroup.Key);
				_ = sb.AppendLine();
				if (allEntriesHidden)
					_ = sb.Append("% ");
				_ = sb.AppendLine(InvariantCulture, $"**{header}**");
			}

			foreach (var entry in areaGroup)
			{
				var (bundleProductIds, entryRepo, entryHideLinks) = GetEntryContext(entry, context);
				var shouldHide = ChangelogRenderUtilities.ShouldHideEntry(entry, context.FeatureIdsToHide, context);

				if (shouldHide)
					_ = sb.Append("% ");
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
							_ = sb.Append("% ");
						_ = sb.Append("  ");
						_ = sb.Append(ChangelogTextUtilities.FormatPrLink(entry.Pr, entryRepo, entryHideLinks));
						hasCommentedLinks = true;
					}

					if (entry.Issues is { Count: > 0 })
					{
						foreach (var issue in entry.Issues)
						{
							_ = sb.AppendLine();
							if (shouldHide)
								_ = sb.Append("% ");
							_ = sb.Append("  ");
							_ = sb.Append(ChangelogTextUtilities.FormatIssueLink(issue, entryRepo, entryHideLinks));
							hasCommentedLinks = true;
						}
					}

					// Add a newline after the last link if there are commented links
					if (hasCommentedLinks)
						_ = sb.AppendLine();
				}
				else
				{
					_ = sb.Append(' ');
					if (!string.IsNullOrWhiteSpace(entry.Pr))
					{
						_ = sb.Append(ChangelogTextUtilities.FormatPrLink(entry.Pr, entryRepo, entryHideLinks));
						_ = sb.Append(' ');
					}

					if (entry.Issues is { Count: > 0 })
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
						_ = sb.AppendLine(indented);
				}
				else
					_ = sb.AppendLine();
			}
		}
	}
}
