// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text;
using Elastic.Documentation.Changelog;

namespace Elastic.Changelog.Rendering.Asciidoc;

/// <summary>
/// Renderer for asciidoc sections that group entries by area (security, bug fixes, features, docs, regressions, other)
/// </summary>
public class EntriesByAreaAsciidocRenderer(StringBuilder sb) : AsciidocRendererBase
{
	/// <inheritdoc />
	public override void Render(IReadOnlyCollection<ChangelogEntry> entries, ChangelogRenderContext context)
	{
		var groupedByArea = context.Subsections
			? entries.GroupBy(ChangelogRenderUtilities.GetComponent).OrderBy(g => g.Key).ToList()
			: entries.GroupBy(ChangelogRenderUtilities.GetComponent).ToList();

		foreach (var areaGroup in groupedByArea)
		{
			var componentName = !string.IsNullOrWhiteSpace(areaGroup.Key) ? areaGroup.Key : "General";
			var formattedComponent = ChangelogTextUtilities.FormatAreaHeader(componentName);

			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"{formattedComponent}::");
			_ = sb.AppendLine();

			foreach (var entry in areaGroup)
				RenderBasicEntry(sb, entry, context);
		}
	}
}
