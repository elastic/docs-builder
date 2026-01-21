// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text;

namespace Elastic.Changelog.Rendering.Asciidoc;

/// <summary>
/// Renderer for asciidoc deprecations section with area grouping and Impact/Action fields
/// </summary>
public class DeprecationsAsciidocRenderer : AsciidocRendererBase
{
	/// <inheritdoc />
	public override void Render(StringBuilder sb, List<ChangelogData> entries, ChangelogRenderContext context)
	{
		var groupedByArea = entries.GroupBy(ChangelogRenderUtilities.GetComponent).OrderBy(g => g.Key).ToList();

		foreach (var areaGroup in groupedByArea)
		{
			var componentName = !string.IsNullOrWhiteSpace(areaGroup.Key) ? areaGroup.Key : "General";
			var formattedComponent = ChangelogTextUtilities.FormatAreaHeader(componentName);

			_ = sb.AppendLine(CultureInfo.InvariantCulture, $"{formattedComponent}::");
			_ = sb.AppendLine();

			foreach (var entry in areaGroup)
				RenderEntryWithImpactAction(sb, entry, context);
		}
	}
}
