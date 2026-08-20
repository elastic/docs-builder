// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using Elastic.Markdown.Page;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using RazorSlices;

namespace Elastic.Markdown.Myst.RelatedLearning;

public sealed class RelatedLearningHtmlRenderer : HtmlObjectRenderer<RelatedLearningBlock>
{
	[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly")]
	protected override void Write(HtmlRenderer renderer, RelatedLearningBlock obj)
	{
		var slice = RelatedLearningView.Create(new RelatedLearningViewModel { Links = obj.Links });
		var html = slice.RenderAsync().GetAwaiter().GetResult();
		_ = renderer.Write(html);
	}
}
