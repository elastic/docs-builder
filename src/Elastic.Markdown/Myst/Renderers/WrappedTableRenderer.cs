// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Markdig.Extensions.Tables;
using Markdig.Renderers;

namespace Elastic.Markdown.Myst.Renderers;

public class WrappedTableRenderer : HtmlTableRenderer
{
	protected override void Write(HtmlRenderer renderer, Table table)
	{
		// Wrap the table in a div to allow for overflow scrolling
		_ = renderer.Write("<div class=\"table-wrapper\">");
		base.Write(renderer, table);
		_ = renderer.Write("</div>");
	}
}
