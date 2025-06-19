// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Helpers;
using Markdig.Syntax;
using Microsoft.AspNetCore.Html;

namespace Elastic.Markdown.Myst.Directives;

public abstract class DirectiveViewModel
{
	public required ContainerBlock DirectiveBlock { get; set; }
	public HtmlString RenderBlock()
	{
		var subscription = DocumentationObjectPoolProvider.HtmlRendererPool.Get();
		subscription.HtmlRenderer.WriteChildren(DirectiveBlock);

		var result = subscription.RentedStringBuilder?.ToString();
		DocumentationObjectPoolProvider.HtmlRendererPool.Return(subscription);
		return new HtmlString(result);
	}
}
