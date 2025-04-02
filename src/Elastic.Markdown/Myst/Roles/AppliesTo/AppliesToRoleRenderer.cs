// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using Elastic.Markdown.Slices.Roles;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using RazorSlices;

namespace Elastic.Markdown.Myst.Roles.AppliesTo;

public class AppliesToRoleHtmlRenderer : HtmlObjectRenderer<AppliesToRole>
{
	[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly")]
	protected override void Write(HtmlRenderer renderer, AppliesToRole role)
	{
		var appliesTo = role.AppliesTo;
		var slice = ApplicableToRole.Create(appliesTo);
		if (appliesTo is null || appliesTo == FrontMatter.ApplicableTo.All)
			return;
		var html = slice.RenderAsync().GetAwaiter().GetResult();
		_ = renderer.Write(html);
	}
}
