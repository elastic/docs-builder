// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Templates;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.Navigation;
using Microsoft.AspNetCore.Html;
using RazorSlices;

namespace Elastic.ApiExplorer.Landing;

/// <summary>
/// Landing page model that supports both template-based and auto-generated content.
/// </summary>
public class TemplateLandingModel(
	ResolvedApiConfiguration apiConfig,
	TemplateProcessor templateProcessor,
	string urlPathPrefix,
	string fallbackContent = "") : IApiGroupingModel
{
	private readonly ResolvedApiConfiguration _apiConfig = apiConfig;
	private readonly TemplateProcessor _templateProcessor = templateProcessor;
	private readonly string _urlPathPrefix = urlPathPrefix;
	private readonly string _fallbackContent = fallbackContent;

	public bool HasCustomTemplate => _apiConfig.HasCustomTemplate;

	public async Task RenderAsync(FileSystemStream stream, ApiRenderContext context, Cancel ctx = default)
	{
		if (_apiConfig.HasCustomTemplate)
		{
			var bodyHtml = await _templateProcessor.ProcessTemplateAsync(_apiConfig, _urlPathPrefix, ctx);
			var viewModel = new TemplateLandingViewModel(context)
			{
				BodyHtml = new HtmlString(bodyHtml)
			};
			var slice = TemplateLandingView.Create(viewModel);
			await slice.RenderAsync(stream, cancellationToken: ctx);
			return;
		}

		await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(_fallbackContent), ctx);
	}
}

/// <summary>
/// Navigation item for template-based landing pages.
/// </summary>
public class TemplateLandingNavigationItem(string url, TemplateLandingModel landingModel) : LandingNavigationItem(url)
{
	public TemplateLandingModel TemplateLandingModel { get; } = landingModel;
}
