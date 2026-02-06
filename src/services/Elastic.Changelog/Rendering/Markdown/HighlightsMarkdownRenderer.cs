// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text;
using Elastic.Documentation.ReleaseNotes;
using static System.Globalization.CultureInfo;

namespace Elastic.Changelog.Rendering.Markdown;

/// <summary>
/// Renderer for the highlights.md changelog file
/// </summary>
public class HighlightsMarkdownRenderer(IFileSystem fileSystem) : MarkdownRendererBase(fileSystem)
{
	/// <inheritdoc />
	public override string OutputFileName => "highlights.md";

	/// <inheritdoc />
	public override async Task RenderAsync(ChangelogRenderContext context, Cancel ctx)
	{
		// Get all entries with highlight == true from all types
		var highlights = context.EntriesByType.Values
			.SelectMany(e => e)
			.Where(e => e.Highlight == true)
			.ToList();

		var sb = new StringBuilder();
		_ = sb.AppendLine(InvariantCulture, $"## {context.Title} [{context.Repo}-{context.TitleSlug}-highlights]");

		// Check if all entries are hidden
		var allEntriesHidden = highlights.Count > 0 && highlights.All(entry =>
			ChangelogRenderUtilities.ShouldHideEntry(entry, context.FeatureIdsToHide, context));

		if (highlights.Count > 0)
		{
			var groupedByArea = context.Subsections
				? highlights.GroupBy(ChangelogRenderUtilities.GetComponent).OrderBy(g => g.Key).ToList()
				: highlights.GroupBy(ChangelogRenderUtilities.GetComponent).ToList();
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
					_ = sb.AppendLine(entry.Description ?? "% Describe the highlight");
					_ = sb.AppendLine();
					RenderPrIssueLinks(sb, entry, entryRepo, entryHideLinks);
					_ = sb.AppendLine("::::");
					if (shouldHide)
						_ = sb.AppendLine("-->");
				}
			}

			// Add message if all entries are hidden
			if (allEntriesHidden)
			{
				_ = sb.AppendLine();
				_ = sb.AppendLine("_There are no highlights associated with this release._");
			}
		}
		else
		{
			_ = sb.AppendLine("_There are no highlights associated with this release._");
		}

		await WriteOutputFileAsync(context.OutputDir, context.TitleSlug, sb.ToString(), ctx);
	}
}
