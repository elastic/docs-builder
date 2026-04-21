// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Landing;

/// <summary>
/// View model for template-based API landing pages.
/// </summary>
public class TemplateLandingViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	/// <summary>
	/// The template landing model.
	/// </summary>
	public required TemplateLanding Landing { get; init; }

	/// <summary>
	/// The processed template content as HTML.
	/// </summary>
	public required string TemplateContent { get; init; }

	/// <summary>
	/// The API information from the OpenAPI specification.
	/// </summary>
	public required OpenApiInfo ApiInfo { get; init; }
}
