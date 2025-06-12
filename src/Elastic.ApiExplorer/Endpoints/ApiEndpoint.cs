// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Operations;
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

public class EndpointNavigationItem : INodeNavigationItem<ApiEndpoint, OperationNavigationItem>
{
	public EndpointNavigationItem(int depth, string url, ApiEndpoint apiEndpoint, LandingNavigationItem parent, LandingNavigationItem root)
	{
		Parent = parent;
		Depth = depth;
		NavigationRoot = root;
		Id = NavigationRoot.Id;

		Index = apiEndpoint;
		Url = url;
		//TODO
		NavigationTitle = apiEndpoint.OpenApiPath.Summary;
	}

	public string Id { get; }
	public int Depth { get; }
	public ApiEndpoint Index { get; }
	public string Url { get; }
	public string NavigationTitle { get; }
	public bool Hidden => false;

	public IReadOnlyCollection<OperationNavigationItem> NavigationItems { get; set; } = [];

	public INodeNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; }

	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }

	public int NavigationIndex { get; set; }
}
