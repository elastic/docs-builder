// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Changelog.Rendering.Asciidoc;

/// <summary>
/// Renderer for asciidoc sections that group entries by area (security, bug fixes, features, docs, regressions, other)
/// </summary>
public class EntriesByAreaAsciidocRenderer(StringBuilder sb) : AsciidocRendererBase
{
	/// <inheritdoc />
	public override void Render(IReadOnlyCollection<ChangelogEntry> entries, ChangelogRenderContext context)
	{
		// Group by area if subsections is enabled, otherwise use single group
		var groupedEntries = context.Subsections
			? entries.GroupBy(e => ChangelogRenderUtilities.GetComponent(e, context)).OrderBy(g => g.Key).ToList()
			: [entries.GroupBy(_ => string.Empty).First()];

		foreach (var group in groupedEntries)
		{
			// Check if all entries in this group are hidden
			var allEntriesHidden = group.All(entry =>
				ChangelogRenderUtilities.ShouldHideEntry(entry, context.FeatureIdsToHide, context));

			// Add nested section header when subsections are enabled and group has a name
			if (context.Subsections && !string.IsNullOrWhiteSpace(group.Key))
			{
				var componentName = group.Key != string.Empty ? group.Key : "General";
				var formattedComponent = ChangelogTextUtilities.FormatAreaHeader(componentName);

				if (allEntriesHidden)
				{
					_ = sb.AppendLine("// [float]");
					_ = sb.AppendLine(CultureInfo.InvariantCulture, $"// ==== {formattedComponent}");
				}
				else
				{
					_ = sb.AppendLine("[float]");
					_ = sb.AppendLine(CultureInfo.InvariantCulture, $"==== {formattedComponent}");
				}
				_ = sb.AppendLine();
			}

			foreach (var entry in group)
				RenderBasicEntry(sb, entry, context);
		}
	}
}
