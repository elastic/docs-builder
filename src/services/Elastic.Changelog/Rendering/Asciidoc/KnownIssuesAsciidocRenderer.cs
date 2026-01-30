// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text;
using Elastic.Documentation;

namespace Elastic.Changelog.Rendering.Asciidoc;

/// <summary>
/// Renderer for asciidoc known issues section with area grouping and Impact/Action fields
/// </summary>
public class KnownIssuesAsciidocRenderer(StringBuilder sb) : AsciidocRendererBase
{
	/// <inheritdoc />
	public override void Render(IReadOnlyCollection<ChangelogEntry> entries, ChangelogRenderContext context)
	{
		var groupedByArea = entries.GroupBy(ChangelogRenderUtilities.GetComponent).OrderBy(g => g.Key).ToList();

		foreach (var areaGroup in groupedByArea)
		{
			// Check if all entries in this area group are hidden
			var allEntriesHidden = areaGroup.All(entry =>
				ChangelogRenderUtilities.ShouldHideEntry(entry, context.FeatureIdsToHide, context));

			var componentName = !string.IsNullOrWhiteSpace(areaGroup.Key) ? areaGroup.Key : "General";
			var formattedComponent = ChangelogTextUtilities.FormatAreaHeader(componentName);

			var headerLine = allEntriesHidden ? $"// {formattedComponent}::" : $"{formattedComponent}::";
			_ = sb.AppendLine(headerLine);
			_ = sb.AppendLine();

			foreach (var entry in areaGroup)
				RenderEntryWithImpactAction(sb, entry, context);
		}
	}
}
