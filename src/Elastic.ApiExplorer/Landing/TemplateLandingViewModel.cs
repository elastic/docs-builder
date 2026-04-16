// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Html;

namespace Elastic.ApiExplorer.Landing;

public class TemplateLandingViewModel(ApiRenderContext context) : ApiViewModel(context)
{
	public required HtmlString BodyHtml { get; init; }
}
