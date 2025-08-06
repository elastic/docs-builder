// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using Elastic.Documentation.AppliesTo;
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

		// Skip rendering if appliesTo is null or All
		if (appliesTo is null || appliesTo == ApplicableTo.All)
			return;

		// Create the view model with the VersionsConfig from the role's BuildContext
		var viewModel = new Components.ApplicableToViewModel
		{
			AppliesTo = appliesTo,
			Inline = true,
			VersionsConfig = role.BuildContext.VersionsConfiguration
		};

		var slice = ApplicableToRole.Create(viewModel);
		var html = slice.RenderAsync().GetAwaiter().GetResult();
		_ = renderer.Write(html);
	}
}
