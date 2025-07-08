// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Markdig;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Renderers.Html.Inlines;

namespace Elastic.Markdown.Myst.Roles.Kbd;

public class KbdRoleHtmlRenderer : HtmlObjectRenderer<KbdRole>
{
	protected override void Write(HtmlRenderer renderer, KbdRole role)
	{
		var output = KeyboardShortcut.Render(role.KeyboardShortcut);
		_ = renderer.Write(output);
	}
}

public static class InlineKbdExtensions
{
	public static MarkdownPipelineBuilder UseInlineKbd(this MarkdownPipelineBuilder pipeline)
	{
		pipeline.Extensions.AddIfNotAlready<InlineKbdExtension>();
		return pipeline;
	}
}

public class InlineKbdExtension : IMarkdownExtension
{
	public void Setup(MarkdownPipelineBuilder pipeline) => _ = pipeline.InlineParsers.InsertBefore<CodeInlineParser>(new KbdParser());

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer) =>
		renderer.ObjectRenderers.InsertBefore<CodeInlineRenderer>(new KbdRoleHtmlRenderer());
}
