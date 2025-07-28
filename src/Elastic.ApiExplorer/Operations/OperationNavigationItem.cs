// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Landing;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Site.Navigation;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Models.Interfaces;
using RazorSlices;

namespace Elastic.ApiExplorer.Operations;

public interface IApiProperty
{

}

public record ApiObject
{
	public required string Name { get; init; }
	public IReadOnlyCollection<IApiProperty> Properties { get; init; } = [];
}



public record ApiOperation(OperationType OperationType, OpenApiOperation Operation, string Route, IOpenApiPathItem Path, string ApiName) : IApiModel
{
	public async Task RenderAsync(FileSystemStream stream, ApiRenderContext context, Cancel ctx = default)
	{
		var viewModel = new OperationViewModel(context)
		{
			Operation = this
		};
		var slice = OperationView.Create(viewModel);
		await slice.RenderAsync(stream, cancellationToken: ctx);
	}
}

public class OperationNavigationItem : ILeafNavigationItem<ApiOperation>, IEndpointOrOperationNavigationItem
{
	public OperationNavigationItem(
		string? urlPathPrefix,
		string apiUrlSuffix,
		ApiOperation apiOperation,
		IRootNavigationItem<IApiGroupingModel, INavigationItem> root,
		IApiGroupingNavigationItem<IApiGroupingModel, INavigationItem> parent
	)
	{
		NavigationRoot = root;
		Model = apiOperation;
		NavigationTitle = apiOperation.ApiName;
		Parent = parent;
		var moniker = apiOperation.Operation.OperationId ?? apiOperation.Route.Replace("}", "").Replace("{", "").Replace('/', '-');
		Url = $"{urlPathPrefix?.TrimEnd('/')}/api/{apiUrlSuffix}/{moniker}";
		Id = ShortId.Create(Url);
	}

	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; }
	//TODO enum to string
	public string Id { get; }
	public int Depth { get; } = 1;
	public ApiOperation Model { get; }
	public string Url { get; }
	public bool Hidden { get; set; }

	public string NavigationTitle { get; }

	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }

	public int NavigationIndex { get; set; }

}
