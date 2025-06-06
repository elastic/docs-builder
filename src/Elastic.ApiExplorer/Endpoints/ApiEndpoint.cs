// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Landing;
using Elastic.Documentation.Site.Navigation;
using Microsoft.OpenApi.Models.Interfaces;
using RazorSlices;

namespace Elastic.ApiExplorer.Endpoints;

public record ApiEndpoint : IPageInformation, IPageRenderer<ApiRenderContext>
{
	public ApiEndpoint(string url, string route, IOpenApiPathItem pathValue, INodeNavigationItem navigationRoot)
	{
		Route = route;
		PathValue = pathValue;
		NavigationRoot = navigationRoot;

		//TODO
		NavigationTitle = pathValue.Summary;
		CrossLink = pathValue.Summary;
		Url = url;
	}

	public string NavigationTitle { get; }
	public string CrossLink { get; }
	public string Url { get; }
	public string Route { get; }
	public IOpenApiPathItem PathValue { get; }
	public INodeNavigationItem NavigationRoot { get; }

	public async Task RenderAsync(FileSystemStream stream, ApiRenderContext context, Cancel ctx = default)
	{
		var viewModel = new IndexViewModel
		{
			ApiEndpoint = this,
			StaticFileContentHashProvider = context.StaticFileContentHashProvider,
			NavigationHtml = context.NavigationHtml
		};
		var slice = EndpointView.Create(viewModel);
		await slice.RenderAsync(stream, cancellationToken: ctx);
	}
}

public class EndpointNavigationItem : INodeNavigationItem
{
	public EndpointNavigationItem(int depth, ApiEndpoint apiEndpoint, INodeNavigationItem parent, LandingNavigationItem root)
	{
		Parent = parent;
		Depth = depth;
		//Current = group.Current;
		NavigationRoot = root;
		Id = NavigationRoot.Id;

		Index = apiEndpoint;
		Current = apiEndpoint;
		Endpoint = apiEndpoint;
	}

	public INodeNavigationItem NavigationRoot { get; }
	public string Id { get; }
	public INodeNavigationItem? Parent { get; set; }
	public int Depth { get; }
	public IPageInformation Current { get; }
	public IPageInformation Index { get; }
	public ApiEndpoint Endpoint { get; }
	public IReadOnlyCollection<INavigationItem> NavigationItems { get; set; } = [];
}
