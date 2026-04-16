// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Extensions;
using RazorSlices;

namespace Elastic.ApiExplorer.Landing;

/// <summary>
/// Template-based API landing page model.
/// </summary>
public class TemplateLanding(string templateContent) : IApiGroupingModel
{
	/// <summary>
	/// The processed template content as HTML.
	/// </summary>
	public string TemplateContent { get; } = templateContent;

	/// <summary>
	/// Renders the template-based landing page.
	/// </summary>
	public async Task RenderAsync(FileSystemStream stream, ApiRenderContext context, Cancel ctx = default)
	{
		var viewModel = new TemplateLandingViewModel(context)
		{
			Landing = this,
			TemplateContent = TemplateContent,
			ApiInfo = context.Model.Info
		};
		var slice = TemplateLandingView.Create(viewModel);
		await slice.RenderAsync(stream, cancellationToken: ctx);
	}
}
