// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;
using Elastic.Documentation.Site.FileProviders;
using Elastic.Documentation.Site.Navigation;
using Microsoft.OpenApi.Models;

namespace Elastic.ApiExplorer;

public record ApiRenderContext(
	BuildContext BuildContext,
	OpenApiDocument Model,
	StaticFileContentHashProvider StaticFileContentHashProvider
)
	: RenderContext<OpenApiDocument>(BuildContext, Model)
{
	public required string NavigationHtml { get; init; }
	public required INavigationItem CurrentNavigation { get; init; }
}
