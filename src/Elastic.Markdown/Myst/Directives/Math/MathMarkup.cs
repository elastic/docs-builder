// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Markdig.Renderers;

namespace Elastic.Markdown.Myst.Directives.Math;

/// <summary>
/// Shared HTML markup for math content, used by both the block `math` directive and the
/// inline `{math}` role. Output is inert HTML that KaTeX renders client-side.
/// </summary>
internal static class MathMarkup
{
	public static void WriteHtml(HtmlRenderer renderer, string? content, bool isDisplay, string? label = null)
	{
		var labelAttr = !string.IsNullOrEmpty(label) ? $" id=\"{label}\"" : "";
		var tag = isDisplay ? "div" : "span";
		_ = renderer.Write($"<{tag} class=\"math\"{labelAttr}>");
		_ = renderer.WriteEscape(content ?? "");
		_ = renderer.Write($"</{tag}>");
	}
}
