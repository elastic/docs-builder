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
	// The page header leads with the dominant method, which can come from the description's method listing
	// rather than the spec; the sidebar badge must agree with it.
	string IHttpMethodNavigationModel.HttpMethod => OperationEndpoint.Build(this, Operation.Description, []).Rows[0].Method;

	/// <summary>The title shown for the operation; an operationId used as the grouping key is shown as readable words.</summary>
	public string DisplayName =>
		!string.IsNullOrWhiteSpace(Operation.OperationId) && string.Equals(ApiName, Operation.OperationId, StringComparison.Ordinal)
			? ApiName.TitleCaseFromSlug()
			: ApiName;

	/// <summary>
	/// The operation's parameters and the path item's, which apply to every operation on the path unless the operation
	/// declares one of the same name and location itself.
	/// </summary>
	public IOpenApiParameter[] Parameters { get; } = MergeParameters(Operation, Path);

	private static IOpenApiParameter[] MergeParameters(OpenApiOperation operation, IOpenApiPathItem path)
	{
		var own = operation.Parameters ?? [];
		var inherited = (path.Parameters ?? []).Where(
			p => !own.Any(o => o.In == p.In && string.Equals(o.Name, p.Name, StringComparison.Ordinal))
		);
		return [.. own, .. inherited];
	}

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
		return Task.FromResult<string?>(OperationCommonMark.Write(this, page, context));
	}
}

public class OperationNavigationItem(
	string? urlPathPrefix,
	string apiUrlSuffix,
	ApiOperation apiOperation,
	IRootNavigationItem<IApiGroupingModel, INavigationItem> root,
	IApiGroupingNavigationItem<IApiGroupingModel, INavigationItem> parent,
	string? moniker = null
) : ApiPageNavigationItem<ApiOperation>(
	ApiUrlBuilder.OperationUrl(
		urlPathPrefix,
		apiUrlSuffix,
		moniker ?? ApiUrlBuilder.OperationMoniker(apiOperation.Operation.OperationId, apiOperation.Route)
	),
	apiOperation.DisplayName,
	apiOperation,
	root,
	parent
), IEndpointOrOperationNavigationItem
{
	/// <summary>Other operations of the same API collapsed onto this page (other methods and routes).</summary>
	public IReadOnlyList<ApiOperation> Siblings { get; init; } = [];

	/// <summary>Former per-operation URLs of <see cref="Siblings"/> that now redirect to <see cref="Url"/>.</summary>
	public IReadOnlyList<string> AliasUrls { get; init; } = [];
}
