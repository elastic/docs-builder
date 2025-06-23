// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Elastic.Markdown.Myst.InlineParsers.Icon;

public class IconRenderer : HtmlObjectRenderer<IconLeaf>
{
	private static readonly IReadOnlyDictionary<string, string> IconMap;

	static IconRenderer()
	{
		var assembly = typeof(IconRenderer).Assembly;
		var iconFolder = $"{assembly.GetName().Name}.Myst.InlineParsers.Icon.svgs.";
		IconMap = assembly.GetManifestResourceNames()
			.Where(r => r.StartsWith(iconFolder) && r.EndsWith(".svg"))
			.ToDictionary(
				r => r[iconFolder.Length..].Replace(".svg", string.Empty),
				r =>
				{
					using var stream = assembly.GetManifestResourceStream(r);
					if (stream is null)
						return string.Empty;
					using var reader = new StreamReader(stream);
					return reader.ReadToEnd();
				}
			);
	}

	protected override void Write(HtmlRenderer renderer, IconLeaf obj)
	{
		if (IconMap.TryGetValue(obj.IconName, out var svg))
		{
			_ = renderer.Write($"<span aria-label=\"Icon for {obj.IconName}\" class=\"icon icon-{obj.IconName}\">");
			_ = renderer.Write(svg);
			_ = renderer.Write("</span>");
		}
		else
			_ = renderer.Write(obj.Content);
	}
}
