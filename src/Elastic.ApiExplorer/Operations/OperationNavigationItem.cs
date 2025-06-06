// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Endpoints;
using Elastic.ApiExplorer.Landing;
using Elastic.Documentation.Site.Navigation;
using Microsoft.OpenApi.Models;
using RazorSlices;

namespace Elastic.ApiExplorer.Operations;

public record ApiOperation : IPageInformation, IPageRenderer<ApiRenderContext>
{
	public ApiOperation(OperationType operationType, OpenApiOperation operation, LandingNavigationItem navigationRoot)
	{
		OperationType = operationType;
		Operation = operation;
		NavigationRoot = navigationRoot;

	}

	public OperationType OperationType { get; }
	public OpenApiOperation Operation { get; }
	public INodeNavigationItem<IPageInformation, INavigationItem> NavigationRoot { get; }

	public async Task RenderAsync(FileSystemStream stream, ApiRenderContext context, CancellationToken ctx = default)
	{
		var viewModel = new OperationViewModel
		{
			Operation = this,
			StaticFileContentHashProvider = context.StaticFileContentHashProvider,
			NavigationHtml = context.NavigationHtml,
			CurrentNavigationItem = context.CurrentNavigation
		};
		var slice = OperationView.Create(viewModel);
		await slice.RenderAsync(stream, cancellationToken: ctx);
	}
}

public class OperationNavigationItem : ILeafNavigationItem<ApiOperation>
{
	public OperationNavigationItem(int depth, string url, ApiOperation apiOperation, EndpointNavigationItem parent, LandingNavigationItem root)
	{
		Parent = parent;
		Depth = depth;
		//Current = group.Current;
		NavigationRoot = root;
		Id = NavigationRoot.Id;
		Model = apiOperation;
		Url = url;
		//TODO
		NavigationTitle = $"{apiOperation.OperationType.ToString().ToLowerInvariant()} {apiOperation.Operation.OperationId}";
	}

	public INodeNavigationItem<IPageInformation, INavigationItem> NavigationRoot { get; }
	public string Id { get; }
	public int Depth { get; }
	public ApiOperation Model { get; }
	public string Url { get; }

	public string NavigationTitle { get; }

	public INodeNavigationItem<IPageInformation, INavigationItem>? Parent { get; set; }
}
