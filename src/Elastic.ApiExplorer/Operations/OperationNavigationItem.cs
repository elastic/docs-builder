// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation;
using Microsoft.OpenApi;
using RazorSlices;

namespace Elastic.ApiExplorer.Operations;

public record ApiOperation(
	HttpMethod OperationType,
	OpenApiOperation Operation,
	string Route,
	IOpenApiPathItem Path,
	string ApiName
) : IApiModel<OperationPageModel>, IHttpMethodNavigationModel
{
	string IHttpMethodNavigationModel.HttpMethod => OperationType.Method.ToLowerInvariant();

	public OperationPageModel? CreatePageModel(ApiRenderContext context) => OperationPageModel.Create(this, context);

	public async Task RenderAsync(FileSystemStream stream, ApiRenderContext context, OperationPageModel? pageModel, Cancel ctx = default)
	{
		var page = pageModel ?? OperationPageModel.Create(this, context);
		var viewModel = new OperationViewModel(context) { Operation = this, Page = page };
		var slice = OperationView.Create(viewModel);
		await slice.RenderAsync(stream, cancellationToken: ctx);
	}

	public Task<string?> RenderCommonMarkAsync(ApiRenderContext context, OperationPageModel? pageModel, Cancel ctx = default)
	{
		var page = pageModel ?? OperationPageModel.Create(this, context);
		var prerequisites = OpenApiXReqAuthParser.TryGetPrerequisiteLines(Operation, context.ApiExplorerLog, Route, Operation.OperationId);
		return Task.FromResult<string?>(OperationCommonMark.Write(this, page, prerequisites, context));
	}
}

public class OperationNavigationItem : ILeafNavigationItem<ApiOperation>, IEndpointOrOperationNavigationItem
{
	public OperationNavigationItem(
		string? urlPathPrefix,
		string apiUrlSuffix,
		ApiOperation apiOperation,
		IRootNavigationItem<IApiGroupingModel, INavigationItem> root,
		IApiGroupingNavigationItem<IApiGroupingModel, INavigationItem> parent,
		string? moniker = null
	)
	{
		NavigationRoot = root;
		Model = apiOperation;
		NavigationTitle = apiOperation.ApiName;
		Parent = parent;
		Url = ApiUrlBuilder.OperationUrl(
			urlPathPrefix,
			apiUrlSuffix,
			moniker ?? ApiUrlBuilder.OperationMoniker(apiOperation.Operation.OperationId, apiOperation.Route)
		);
		Id = ShortId.Create(Url);
	}

	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; }
	//TODO enum to string
	public string Id { get; }
	public ApiOperation Model { get; }
	public string Url { get; }
	public bool Hidden { get; set; }

	/// <summary>Other operations of the same API collapsed onto this page (other methods and routes).</summary>
	public IReadOnlyList<ApiOperation> Siblings { get; init; } = [];

	/// <summary>Former per-operation URLs of <see cref="Siblings"/> that now redirect to <see cref="Url"/>.</summary>
	public IReadOnlyList<string> AliasUrls { get; init; } = [];

	public string NavigationTitle { get; }

	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }

	public int NavigationIndex { get; set; }
}
