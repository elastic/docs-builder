// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.IO.Abstractions;
using System.Text;
using Elastic.Documentation.ReleaseNotes;
using Nullean.ScopedFileSystem;
using static System.Globalization.CultureInfo;
using static Elastic.Documentation.ReleaseNotes.ChangelogEntryType;

namespace Elastic.Changelog.Rendering.Markdown;

/// <summary>
/// Renderer for generating clean GitHub Flavored Markdown in a single changelog.md file
/// </summary>
public class ChangelogGfmRenderer(ScopedFileSystem fileSystem) : MarkdownRendererBase(fileSystem)
{
	/// <inheritdoc />
	public override string OutputFileName => "changelog.md";

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
		var breakingChanges = entriesByType.GetValueOrDefault(BreakingChange, []);
		var deprecations = entriesByType.GetValueOrDefault(Deprecation, []);
		var knownIssues = entriesByType.GetValueOrDefault(KnownIssue, []);

		// Check for highlights
		var highlights = entriesByType.Values
			.SelectMany(e => e)
			.Where(e => e.Highlight == true)
			.ToList();

		var sb = new StringBuilder();

		// Main heading - clean without anchors
		_ = sb.AppendLine(InvariantCulture, $"## {context.Title}");

		// Release date if present
		if (context.BundleReleaseDate is { } releaseDate)
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(InvariantCulture, $"_Released: {releaseDate.ToString("MMMM d, yyyy", InvariantCulture)}_");
		}

		// Add description if present
		if (!string.IsNullOrEmpty(context.BundleDescription))
		{
			_ = sb.AppendLine();
			_ = sb.AppendLine(context.BundleDescription);
		}

		_ = sb.AppendLine();

		// Helper to check if all entries in a collection are hidden
		bool AllEntriesHidden(IReadOnlyCollection<ChangelogEntry> entries) =>
			entries.Count > 0 && entries.All(entry =>
				ChangelogRenderUtilities.ShouldHideEntry(entry, context.FeatureIdsToHide, context));

		// Render highlights first if any exist
		if (highlights.Count > 0)
		{
			_ = sb.AppendLine("### Highlights");
			RenderEntriesByArea(sb, highlights, context);
			_ = sb.AppendLine();
		}

		// Features and enhancements
		if (features.Count > 0 || enhancements.Count > 0)
		{
			var combined = features.Concat(enhancements).ToList();
			if (!AllEntriesHidden(combined))
			{
				_ = sb.AppendLine("### Features and enhancements");
				RenderEntriesByArea(sb, combined, context);
				_ = sb.AppendLine();
			}
		}

		// Breaking changes
		if (breakingChanges.Count > 0 && !AllEntriesHidden(breakingChanges))
		{
			_ = sb.AppendLine("### Breaking changes");
			RenderEntriesByArea(sb, breakingChanges, context);
			_ = sb.AppendLine();
		}

		// Deprecations
		if (deprecations.Count > 0 && !AllEntriesHidden(deprecations))
		{
			_ = sb.AppendLine("### Deprecations");
			RenderEntriesByArea(sb, deprecations, context);
			_ = sb.AppendLine();
		}

		// Bug fixes and security updates
		if (security.Count > 0 || bugFixes.Count > 0)
		{
			var combined = security.Concat(bugFixes).ToList();
			if (!AllEntriesHidden(combined))
			{
				_ = sb.AppendLine("### Bug fixes");
				RenderEntriesByArea(sb, combined, context);
				_ = sb.AppendLine();
			}
		}

		// Known issues
		if (knownIssues.Count > 0 && !AllEntriesHidden(knownIssues))
		{
			_ = sb.AppendLine("### Known issues");
			RenderEntriesByArea(sb, knownIssues, context);
			_ = sb.AppendLine();
		}

		// Documentation
		if (docs.Count > 0 && !AllEntriesHidden(docs))
		{
			_ = sb.AppendLine("### Documentation");
			RenderEntriesByArea(sb, docs, context);
			_ = sb.AppendLine();
		}

		// Regressions
		if (regressions.Count > 0 && !AllEntriesHidden(regressions))
		{
			_ = sb.AppendLine("### Regressions");
			RenderEntriesByArea(sb, regressions, context);
			_ = sb.AppendLine();
		}

		// Other changes
		if (other.Count > 0 && !AllEntriesHidden(other))
		{
			_ = sb.AppendLine("### Other changes");
			RenderEntriesByArea(sb, other, context);
			_ = sb.AppendLine();
		}

		// Check if we have any visible content
		var hasAnyVisibleContent = highlights.Count > 0 ||
			(!AllEntriesHidden(features) && features.Count > 0) ||
			(!AllEntriesHidden(enhancements) && enhancements.Count > 0) ||
			(!AllEntriesHidden(breakingChanges) && breakingChanges.Count > 0) ||
			(!AllEntriesHidden(deprecations) && deprecations.Count > 0) ||
			(!AllEntriesHidden(security) && security.Count > 0) ||
			(!AllEntriesHidden(bugFixes) && bugFixes.Count > 0) ||
			(!AllEntriesHidden(knownIssues) && knownIssues.Count > 0) ||
			(!AllEntriesHidden(docs) && docs.Count > 0) ||
			(!AllEntriesHidden(regressions) && regressions.Count > 0) ||
			(!AllEntriesHidden(other) && other.Count > 0);

		if (!hasAnyVisibleContent)
		{
			_ = sb.AppendLine("_There are no new features, enhancements, or fixes associated with this release._");
			_ = sb.AppendLine();
		}

		await WriteOutputFileAsync(context.OutputDir, context.TitleSlug, sb.ToString(), ctx);
	}

	private static void RenderEntriesByArea(
		StringBuilder sb,
		IReadOnlyCollection<ChangelogEntry> entries,
		ChangelogRenderContext context)
	{
		var groupedByArea = context.Subsections
			? entries.GroupBy(e => ChangelogRenderUtilities.GetComponent(e, context)).OrderBy(g => g.Key).ToList()
			: entries.GroupBy(e => ChangelogRenderUtilities.GetComponent(e, context)).ToList();

		foreach (var areaGroup in groupedByArea)
		{
			// Check if all entries in this area group are hidden
			var allEntriesHidden = areaGroup.All(entry =>
				ChangelogRenderUtilities.ShouldHideEntry(entry, context.FeatureIdsToHide, context));

			if (context.Subsections && !string.IsNullOrWhiteSpace(areaGroup.Key))
			{
				var header = ChangelogTextUtilities.FormatAreaHeader(areaGroup.Key);
				if (allEntriesHidden)
					_ = sb.Append("% ");
				_ = sb.AppendLine(InvariantCulture, $"**{header}**");
				_ = sb.AppendLine();
			}

			foreach (var entry in areaGroup)
			{
				var (entryRepo, entryOwner, entryHideLinks, shouldHide) = ChangelogRenderUtilities.GetEntryContext(entry, context);

				if (shouldHide)
					_ = sb.Append("% ");
				_ = sb.Append("* ");
				_ = sb.Append(ChangelogTextUtilities.Beautify(entry.Title));

				var hasCommentedLinks = false;
				if (entryHideLinks)
				{
					foreach (var pr in entry.Prs ?? [])
					{
						var formatted = ChangelogTextUtilities.FormatPrLink(pr, entryRepo, entryHideLinks, entryOwner);
						if (string.IsNullOrEmpty(formatted))
							continue;

						_ = sb.AppendLine();
						if (shouldHide)
							_ = sb.Append("% ");
						_ = sb.Append("  ");
						_ = sb.Append(formatted);
						hasCommentedLinks = true;
					}

					foreach (var issue in entry.Issues ?? [])
					{
						var formatted = ChangelogTextUtilities.FormatIssueLink(issue, entryRepo, entryHideLinks, entryOwner);
						if (string.IsNullOrEmpty(formatted))
							continue;

						_ = sb.AppendLine();
						if (shouldHide)
							_ = sb.Append("% ");
						_ = sb.Append("  ");
						_ = sb.Append(formatted);
						hasCommentedLinks = true;
					}

					if (hasCommentedLinks)
						_ = sb.AppendLine();
				}
				else
				{
					var linkParts = new List<string>();
					foreach (var pr in entry.Prs ?? [])
					{
						var s = ChangelogTextUtilities.FormatPrLink(pr, entryRepo, entryHideLinks, entryOwner);
						if (!string.IsNullOrEmpty(s))
							linkParts.Add(s);
					}

					foreach (var issue in entry.Issues ?? [])
					{
						var s = ChangelogTextUtilities.FormatIssueLink(issue, entryRepo, entryHideLinks, entryOwner);
						if (!string.IsNullOrEmpty(s))
							linkParts.Add(s);
					}

					if (linkParts.Count > 0)
					{
						_ = sb.Append(' ');
						var first = true;
						foreach (var s in linkParts)
						{
							if (!first)
								_ = sb.Append(' ');
							_ = sb.Append(s);
							first = false;
						}
					}
				}

				if (!context.HideDescriptions && !string.IsNullOrWhiteSpace(entry.Description))
				{
					_ = sb.AppendLine(entryHideLinks && hasCommentedLinks ? "  " : "");
					_ = sb.AppendLine();
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
