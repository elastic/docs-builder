// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text;

namespace Elastic.Changelog.Rendering.Asciidoc;

/// <summary>
/// Renderer for asciidoc breaking changes section with subtype grouping
/// </summary>
public class BreakingChangesAsciidocRenderer : AsciidocRendererBase
{
	/// <inheritdoc />
	public override void Render(StringBuilder sb, List<ChangelogData> entries, ChangelogRenderContext context)
	{
		// Group by subtype if subsections is enabled, otherwise group by area
		var groupedEntries = context.Subsections
			? entries.GroupBy(e => string.IsNullOrWhiteSpace(e.Subtype) ? string.Empty : e.Subtype).OrderBy(g => g.Key).ToList()
			: entries.GroupBy(ChangelogRenderUtilities.GetComponent).ToList();

		foreach (var group in groupedEntries)
		{
			if (context.Subsections && !string.IsNullOrWhiteSpace(group.Key))
			{
				var header = ChangelogTextUtilities.FormatSubtypeHeader(group.Key);
				_ = sb.AppendLine(CultureInfo.InvariantCulture, $"**{header}**");
				_ = sb.AppendLine();
			}

			foreach (var entry in group)
				RenderEntryWithImpactAction(sb, entry, context);
		}
	}
}
