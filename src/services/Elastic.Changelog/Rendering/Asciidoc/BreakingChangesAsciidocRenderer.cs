// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.Documentation;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Changelog.Rendering.Asciidoc;

/// <summary>
/// Renderer for asciidoc breaking changes section with subtype grouping
/// </summary>
public class BreakingChangesAsciidocRenderer(StringBuilder sb) : AsciidocRendererBase
{
	/// <inheritdoc />
	public override void Render(IReadOnlyCollection<ChangelogEntry> entries, ChangelogRenderContext context)
	{
		// Group by subtype if subsections is enabled, otherwise group by area
		var groupedEntries = context.Subsections
			? entries.GroupBy(e => e.Subtype?.ToStringFast(true) ?? string.Empty).OrderBy(g => g.Key).ToList()
			: entries.GroupBy(e => ChangelogRenderUtilities.GetComponent(e, context)).ToList();

		foreach (var group in groupedEntries)
		{
			// Check if all entries in this group are hidden
			var allEntriesHidden = group.All(entry =>
				ChangelogRenderUtilities.ShouldHideEntry(entry, context.FeatureIdsToHide, context));

			if (context.Subsections && !string.IsNullOrWhiteSpace(group.Key))
			{
				var header = ChangelogTextUtilities.FormatSubtypeHeader(group.Key);
				var headerLine = allEntriesHidden ? $"// **{header}**" : $"**{header}**";
				_ = sb.AppendLine(headerLine);
				_ = sb.AppendLine();
			}

			foreach (var entry in group)
				RenderEntryWithImpactAction(sb, entry, context);
		}
	}
}
