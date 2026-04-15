// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Templates;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.Navigation;

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

	public async Task RenderAsync(FileSystemStream stream, ApiRenderContext context, CancellationToken ctx = default)
	{
		string content;

		if (_apiConfig.HasCustomTemplate)
		{
			// Use template-based content
			content = await _templateProcessor.ProcessTemplateAsync(_apiConfig, _urlPathPrefix, ctx);
		}
		else
		{
			// Use fallback (auto-generated) content
			content = _fallbackContent;
		}

		// Write the content to the stream
		await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(content), ctx);
	}
}

/// <summary>
/// Navigation item for template-based landing pages.
/// </summary>
public class TemplateLandingNavigationItem(string url, TemplateLandingModel landingModel) : LandingNavigationItem(url)
{
	public TemplateLandingModel TemplateLandingModel { get; } = landingModel;
}
