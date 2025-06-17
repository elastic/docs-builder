// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Site.Navigation;
using RazorSlices;

namespace Elastic.ApiExplorer.Landing;

public class ApiLanding : IApiGroupingModel, IPageRenderer<ApiRenderContext>
{
	public async Task RenderAsync(FileSystemStream stream, ApiRenderContext context, Cancel ctx = default)
	{
		var viewModel = new LandingViewModel
		{
			Landing = this,
			StaticFileContentHashProvider = context.StaticFileContentHashProvider,
			NavigationHtml = context.NavigationHtml,
			ApiInfo = context.Model.Info,
			CurrentNavigationItem = context.CurrentNavigation
		};
		var slice = LandingView.Create(viewModel);
		await slice.RenderAsync(stream, cancellationToken: ctx);
	}
}

public class LandingNavigationItem : IApiGroupingNavigationItem<ApiLanding, INavigationItem>
{
	public INodeNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; }
	public string Id { get; }
	public int Depth { get; }
	public ApiLanding Index { get; }
	public IReadOnlyCollection<INavigationItem> NavigationItems { get; set; } = [];
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
	public int NavigationIndex { get; set; }
	public string Url { get; }
	public bool Hidden => false;

	//TODO
	public string NavigationTitle { get; } = "API Documentation";

	public LandingNavigationItem(string url)
	{
		Depth = 0;
		NavigationRoot = this;
		Id = ShortId.Create("root");

		var landing = new ApiLanding();
		Url = url;

		Index = landing;
	}
}

public interface IApiGroupingNavigationItem<out TGroupingModel, out TNavigationItem> : INodeNavigationItem<TGroupingModel, TNavigationItem>
	where TGroupingModel : IApiGroupingModel
	where TNavigationItem : INavigationItem;

public abstract class ApiGroupingNavigationItem<TGroupingModel, TNavigationItem>(
	TGroupingModel groupingModel,
	LandingNavigationItem rootNavigation,
	INodeNavigationItem<INavigationModel, INavigationItem> parent)
	: IApiGroupingNavigationItem<TGroupingModel, TNavigationItem>
	where TGroupingModel : IApiGroupingModel
	where TNavigationItem : INavigationItem
{
	/// <inheritdoc />
	public string Url => NavigationItems.First().Url;

	/// <inheritdoc />
	public abstract string NavigationTitle { get; }

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; } = rootNavigation;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = parent;

	/// <inheritdoc />
	public bool Hidden => false;
	/// <inheritdoc />
	public int NavigationIndex { get; set; }

	/// <inheritdoc />
	public int Depth => 0;

	/// <inheritdoc />
	public abstract string Id { get; }
	/// <inheritdoc />
	public TGroupingModel Index { get; } = groupingModel;

	/// <inheritdoc />
	public IReadOnlyCollection<TNavigationItem> NavigationItems { get; set; } = [];
}

public class ClassificationNavigationItem(ApiClassification classification, LandingNavigationItem rootNavigation, LandingNavigationItem parent)
	: ApiGroupingNavigationItem<ApiClassification, INavigationItem>(classification, rootNavigation, parent)
{
	/// <inheritdoc />
	public override string NavigationTitle { get; } = classification.Name;

	/// <inheritdoc />
	public override string Id { get; } = ShortId.Create(classification.Name);
}

public class TagNavigationItem(ApiTag tag, LandingNavigationItem rootNavigation, INodeNavigationItem<INavigationModel, INavigationItem> parent)
	: ApiGroupingNavigationItem<ApiTag, IEndpointOrOperationNavigationItem>(tag, rootNavigation, parent)
{
	/// <inheritdoc />
	public override string NavigationTitle { get; } = tag.Name;

	/// <inheritdoc />
	public override string Id { get; } = ShortId.Create(tag.Name);
}

public interface IEndpointOrOperationNavigationItem : INavigationItem;

public class EndpointNavigationItem(ApiEndpoint endpoint, LandingNavigationItem rootNavigation, INodeNavigationItem<INavigationModel, INavigationItem> parent)
	: IApiGroupingNavigationItem<ApiEndpoint, OperationNavigationItem>, IEndpointOrOperationNavigationItem
{
	/// <inheritdoc />
	public string Url => "TODO ENDPOINT URL";

	/// <inheritdoc />
	public string NavigationTitle { get; } = endpoint.Route;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; } = rootNavigation;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = parent;

	/// <inheritdoc />
	public bool Hidden => false;

	/// <inheritdoc />
	public int NavigationIndex { get; set; }

	/// <inheritdoc />
	public int Depth => 0;

	/// <inheritdoc />
	public string Id { get; } = ShortId.Create(endpoint.Route);

	/// <inheritdoc />
	public ApiEndpoint Index { get; } = endpoint;

	/// <inheritdoc />
	public IReadOnlyCollection<OperationNavigationItem> NavigationItems { get; set; } = [];
}
public class OperationNavigationItem(
	ApiOperation apiOperation,
	LandingNavigationItem root,
	IApiGroupingNavigationItem<IApiGroupingModel, INavigationItem> parent)
	: ILeafNavigationItem<ApiOperation>, IEndpointOrOperationNavigationItem
{
	public INodeNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; } = root;
	//TODO enum to string
	public string Id { get; } = ShortId.Create(apiOperation.Operation.OperationId ?? apiOperation.OperationType.ToString());
	public int Depth { get; } = 1;
	public ApiOperation Model { get; } = apiOperation;
	public string Url { get; } = "TODO OPERATION URL";
	public bool Hidden => false;

	public string NavigationTitle { get; } = $"{apiOperation.OperationType.ToString().ToLowerInvariant()} {apiOperation.Operation.OperationId}";

	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = parent;

	public int NavigationIndex { get; set; }

}
