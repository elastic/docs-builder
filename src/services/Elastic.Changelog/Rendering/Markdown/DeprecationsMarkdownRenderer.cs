// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text;
using Elastic.Documentation.Changelog;
using static System.Globalization.CultureInfo;
using static Elastic.Documentation.Changelog.ChangelogEntryType;

namespace Elastic.Changelog.Rendering.Markdown;

/// <summary>
/// Renderer for the deprecations.md changelog file
/// </summary>
public class DeprecationsMarkdownRenderer(IFileSystem fileSystem) : MarkdownRendererBase(fileSystem)
{
	/// <inheritdoc />
	public override string OutputFileName => "deprecations.md";

	/// <inheritdoc />
	public override async Task RenderAsync(ChangelogRenderContext context, Cancel ctx)
	{
		var deprecations = context.EntriesByType.GetValueOrDefault(Deprecation, []);

		var sb = new StringBuilder();
		_ = sb.AppendLine(InvariantCulture, $"## {context.Title} [{context.Repo}-{context.TitleSlug}-deprecations]");

		if (deprecations.Count > 0)
		{
			var groupedByArea = context.Subsections
				? deprecations.GroupBy(ChangelogRenderUtilities.GetComponent).OrderBy(g => g.Key).ToList()
				: deprecations.GroupBy(ChangelogRenderUtilities.GetComponent).ToList();
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
					var shouldHide = ChangelogRenderUtilities.ShouldHideEntry(entry, context.FeatureIdsToHide);

					_ = sb.AppendLine();
					if (shouldHide)
						_ = sb.AppendLine("<!--");
					_ = sb.AppendLine(InvariantCulture, $"::::{{dropdown}} {ChangelogTextUtilities.Beautify(entry.Title)}");
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
						_ = sb.AppendLine("-->");
				}
			}
		}
		else
			_ = sb.AppendLine("_No deprecations._");

		await WriteOutputFileAsync(context.OutputDir, context.TitleSlug, sb.ToString(), ctx);
	}
}
