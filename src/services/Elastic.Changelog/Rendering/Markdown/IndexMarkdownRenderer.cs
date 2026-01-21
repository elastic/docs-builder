// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text;
using static System.Globalization.CultureInfo;

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

		if (hasAnyEntries)
		{
			if (features.Count > 0 || enhancements.Count > 0)
			{
				_ = sb.AppendLine(InvariantCulture, $"### Features and enhancements [{context.Repo}-{context.TitleSlug}-features-enhancements]");
				var combined = features.Concat(enhancements).ToList();
				RenderEntriesByArea(sb, combined, context);
			}

			if (security.Count > 0 || bugFixes.Count > 0)
			{
				_ = sb.AppendLine();
				_ = sb.AppendLine(InvariantCulture, $"### Fixes [{context.Repo}-{context.TitleSlug}-fixes]");
				var combined = security.Concat(bugFixes).ToList();
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
		}
		else
			_ = sb.AppendLine("_No new features, enhancements, or fixes._");

		await WriteOutputFileAsync(context.OutputDir, context.TitleSlug, sb.ToString(), ctx);
	}

	private static void RenderEntriesByArea(
		StringBuilder sb,
		IReadOnlyCollection<ChangelogData> entries,
		ChangelogRenderContext context)
	{
		var groupedByArea = context.Subsections
			? entries.GroupBy(ChangelogRenderUtilities.GetComponent).OrderBy(g => g.Key).ToList()
			: entries.GroupBy(ChangelogRenderUtilities.GetComponent).ToList();
		foreach (var areaGroup in groupedByArea)
		{
			if (context.Subsections && !string.IsNullOrWhiteSpace(areaGroup.Key))
			{
				var header = ChangelogTextUtilities.FormatAreaHeader(areaGroup.Key);
				_ = sb.AppendLine();
				_ = sb.AppendLine(InvariantCulture, $"**{header}**");
			}

			foreach (var entry in areaGroup)
			{
				var (bundleProductIds, entryRepo, entryHideLinks) = GetEntryContext(entry, context);
				var shouldHide = ChangelogRenderUtilities.ShouldHideEntry(entry, context.FeatureIdsToHide, bundleProductIds, context.RenderBlockers);

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
