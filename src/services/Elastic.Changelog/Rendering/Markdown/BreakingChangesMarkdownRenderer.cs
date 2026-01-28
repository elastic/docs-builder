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
/// Renderer for the breaking-changes.md changelog file
/// </summary>
public class BreakingChangesMarkdownRenderer(IFileSystem fileSystem) : MarkdownRendererBase(fileSystem)
{
	/// <inheritdoc />
	public override string OutputFileName => "breaking-changes.md";

	/// <inheritdoc />
	public override async Task RenderAsync(ChangelogRenderContext context, Cancel ctx)
	{
		var breakingChanges = context.EntriesByType.GetValueOrDefault(BreakingChange, []);

		var sb = new StringBuilder();
		_ = sb.AppendLine(InvariantCulture, $"## {context.Title} [{context.Repo}-{context.TitleSlug}-breaking-changes]");

		// Check if all entries are hidden
		var allEntriesHidden = breakingChanges.Count > 0 && breakingChanges.All(entry =>
			ChangelogRenderUtilities.ShouldHideEntry(entry, context.FeatureIdsToHide, context));

		if (breakingChanges.Count > 0)
		{
			// Group by subtype if subsections are enabled, otherwise group by area
			var groupedEntries = context.Subsections
				? breakingChanges.GroupBy(e => e.Subtype?.ToStringFast(true) ?? string.Empty).OrderBy(g => g.Key).ToList()
				: breakingChanges.GroupBy(ChangelogRenderUtilities.GetComponent).ToList();

			foreach (var group in groupedEntries)
			{
				// Check if all entries in this group are hidden
				var allGroupEntriesHidden = group.All(entry =>
					ChangelogRenderUtilities.ShouldHideEntry(entry, context.FeatureIdsToHide, context));

				if (context.Subsections && !string.IsNullOrWhiteSpace(group.Key))
				{
					var header = ChangelogTextUtilities.FormatSubtypeHeader(group.Key);
					_ = sb.AppendLine();
					if (allGroupEntriesHidden)
						_ = sb.AppendLine("<!--");
					_ = sb.AppendLine(InvariantCulture, $"**{header}**");
					if (allGroupEntriesHidden)
						_ = sb.AppendLine("-->");
				}

				foreach (var entry in group)
				{
					var (bundleProductIds, entryRepo, entryHideLinks) = GetEntryContext(entry, context);
					var shouldHide = ChangelogRenderUtilities.ShouldHideEntry(entry, context.FeatureIdsToHide, context);

					_ = sb.AppendLine();
					if (shouldHide)
						_ = sb.AppendLine("<!--");
					_ = sb.AppendLine(InvariantCulture, $"::::{{dropdown}} {ChangelogTextUtilities.Beautify(entry.Title)}");
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
						_ = sb.AppendLine("-->");
				}
			}

			// Add message if all entries are hidden
			if (allEntriesHidden)
			{
				_ = sb.AppendLine();
				_ = sb.AppendLine("_There are no breaking changes associated with this release._");
			}
		}
		else
		{
			_ = sb.AppendLine("_There are no breaking changes associated with this release._");
		}

		await WriteOutputFileAsync(context.OutputDir, context.TitleSlug, sb.ToString(), ctx);
	}
}
