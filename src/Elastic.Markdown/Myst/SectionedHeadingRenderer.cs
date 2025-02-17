// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using Elastic.Markdown.Helpers;
using Elastic.Markdown.Myst.InlineParsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst;

public class SectionedHeadingRenderer : HtmlObjectRenderer<HeadingBlock>
{
	private static readonly string[] HeadingTexts =
	[
		"h1",
		"h2",
		"h3",
		"h4",
		"h5",
		"h6"
	];

	protected override void Write(HtmlRenderer renderer, HeadingBlock obj)
	{
		var index = obj.Level - 1;
		var headings = HeadingTexts;
		var headingText = ((uint)index < (uint)headings.Length)
			? headings[index]
			: $"h{obj.Level}";

		var header = obj.GetData("header") as string;
		var anchor = obj.GetData("anchor") as string;

		var slugTarget = (anchor ?? header) ?? string.Empty;
		if (slugTarget.IndexOf('$') >= 0)
			slugTarget = HeadingAnchorParser.InlineAnchors().Replace(slugTarget, "");

		var slug = slugTarget.Slugify();

		renderer.Write(@"<div class=""heading-wrapper"" id=""");
		renderer.Write(slug);
		renderer.Write(@""">");
		renderer.Write('<');
		renderer.Write(headingText);
		renderer.WriteAttributes(obj);
		renderer.Write('>');
		renderer.Write($"""<a class="headerlink" href="#{slug}">""");
		renderer.WriteLeafInline(obj);
		renderer.Write("</a>");
		renderer.Write("</");
		renderer.Write(headingText);
		renderer.WriteLine('>');
		renderer.Write("</div>");
		renderer.EnsureLine();
	}
}
