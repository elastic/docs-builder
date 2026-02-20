// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text;
using Elastic.Documentation.ReleaseNotes;
using static System.Globalization.CultureInfo;
using static Elastic.Documentation.ChangelogEntryType;

namespace Elastic.Changelog.Rendering.Markdown;

/// <summary>
/// Renderer for the known-issues.md changelog file
/// </summary>
public class KnownIssuesMarkdownRenderer(IFileSystem fileSystem) : MarkdownRendererBase(fileSystem)
{
	/// <inheritdoc />
	public override string OutputFileName => "known-issues.md";

	/// <inheritdoc />
	public override async Task RenderAsync(ChangelogRenderContext context, Cancel ctx)
	{
		var knownIssues = context.EntriesByType.GetValueOrDefault(KnownIssue, []);

		var sb = new StringBuilder();
		_ = sb.AppendLine(InvariantCulture, $"## {context.Title} [{context.Repo}-{context.TitleSlug}-known-issues]");

		// Check if all entries are hidden
		var allEntriesHidden = knownIssues.Count > 0 && knownIssues.All(entry =>
			ChangelogRenderUtilities.ShouldHideEntry(entry, context.FeatureIdsToHide, context));

		if (knownIssues.Count > 0)
		{
			var groupedByArea = context.Subsections
				? knownIssues.GroupBy(e => ChangelogRenderUtilities.GetComponent(e, context)).OrderBy(g => g.Key).ToList()
				: knownIssues.GroupBy(e => ChangelogRenderUtilities.GetComponent(e, context)).ToList();
			foreach (var areaGroup in groupedByArea)
			{
				// Check if all entries in this area group are hidden
				var allGroupEntriesHidden = areaGroup.All(entry =>
					ChangelogRenderUtilities.ShouldHideEntry(entry, context.FeatureIdsToHide, context));

				if (context.Subsections && !string.IsNullOrWhiteSpace(areaGroup.Key))
				{
					var header = ChangelogTextUtilities.FormatAreaHeader(areaGroup.Key);
					_ = sb.AppendLine();
					if (allGroupEntriesHidden)
						_ = sb.AppendLine("<!--");
					_ = sb.AppendLine(InvariantCulture, $"**{header}**");
					if (allGroupEntriesHidden)
						_ = sb.AppendLine("-->");
				}

				foreach (var entry in areaGroup)
				{
					var (bundleProductIds, entryRepo, entryHideLinks) = GetEntryContext(entry, context);
					var shouldHide = ChangelogRenderUtilities.ShouldHideEntry(entry, context.FeatureIdsToHide, context);

					_ = sb.AppendLine();
					if (shouldHide)
						_ = sb.AppendLine("<!--");
					_ = sb.AppendLine(InvariantCulture, $"::::{{dropdown}} {ChangelogTextUtilities.Beautify(entry.Title)}");
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
						_ = sb.AppendLine("-->");
				}
			}

			// Add message if all entries are hidden
			if (allEntriesHidden)
			{
				_ = sb.AppendLine();
				_ = sb.AppendLine("_There are no known issues associated with this release._");
			}
		}
		else
		{
			_ = sb.AppendLine("_There are no known issues associated with this release._");
		}

		await WriteOutputFileAsync(context.OutputDir, context.TitleSlug, sb.ToString(), ctx);
	}
}
