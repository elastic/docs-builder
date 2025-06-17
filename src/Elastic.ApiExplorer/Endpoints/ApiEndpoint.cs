// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Site.Navigation;
using Microsoft.OpenApi.Models.Interfaces;
using RazorSlices;

namespace Elastic.ApiExplorer.Endpoints;

public record ApiEndpoint : INavigationModel, IPageRenderer<ApiRenderContext>
{
	public ApiEndpoint(string route, IOpenApiPathItem openApiPath)
	{
		Route = route;
		OpenApiPath = openApiPath;
	}

	public string Route { get; }
	public IOpenApiPathItem OpenApiPath { get; }

	public async Task RenderAsync(FileSystemStream stream, ApiRenderContext context, Cancel ctx = default)
	{
		var viewModel = new IndexViewModel
		{
			ApiEndpoint = this,
			StaticFileContentHashProvider = context.StaticFileContentHashProvider,
			NavigationHtml = context.NavigationHtml,
			CurrentNavigationItem = context.CurrentNavigation

		};
		var slice = EndpointView.Create(viewModel);
		await slice.RenderAsync(stream, cancellationToken: ctx);
	}
}
