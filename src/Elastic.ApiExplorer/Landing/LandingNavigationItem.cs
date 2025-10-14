// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation;
using RazorSlices;

namespace Elastic.ApiExplorer.Landing;

public class ApiLanding : IApiGroupingModel
{
	public async Task RenderAsync(FileSystemStream stream, ApiRenderContext context, Cancel ctx = default)
	{
		var viewModel = new LandingViewModel(context)
		{
			Landing = this,
			ApiInfo = context.Model.Info
		};
		var slice = LandingView.Create(viewModel);
		await slice.RenderAsync(stream, cancellationToken: ctx);
	}
}

public class LandingNavigationItem : IApiGroupingNavigationItem<ApiLanding, INavigationItem>, IRootNavigationItem<ApiLanding, INavigationItem>
{
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; }
	public string Id { get; }
	public int Depth { get; }
	public ILeafNavigationItem<ApiLanding> Index { get; }
	public IReadOnlyCollection<INavigationItem> NavigationItems { get; set; } = [];
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
	public int NavigationIndex { get; set; }
	public bool IsCrossLink => false; // API landing items are never cross-links
	public string Url => Index.Url;
	public bool Hidden => false;

	public string NavigationTitle => Index.NavigationTitle;

	public LandingNavigationItem(string url)
	{
		Depth = 0;
		NavigationRoot = this;
		Id = ShortId.Create("root");
		var landing = new ApiLanding();
		Index = new ApiIndexLeafNavigation<ApiLanding>(landing, url, "Api Overview", this);
	}

	/// <inheritdoc />
	public bool IsUsingNavigationDropdown => false;
}

public interface IApiGroupingNavigationItem<out TGroupingModel, out TNavigationItem> : INodeNavigationItem<TGroupingModel, TNavigationItem>
	where TGroupingModel : IApiGroupingModel
	where TNavigationItem : INavigationItem;

public abstract class ApiGroupingNavigationItem<TGroupingModel, TNavigationItem>(
	TGroupingModel groupingModel,
	IRootNavigationItem<IApiGroupingModel, INavigationItem> rootNavigation,
	INodeNavigationItem<INavigationModel, INavigationItem> parent
)
	: IApiGroupingNavigationItem<TGroupingModel, TNavigationItem>
	where TGroupingModel : IApiGroupingModel
	where TNavigationItem : INavigationItem
{
	/// <inheritdoc />
	public string Url => NavigationItems.First().Url;

	/// <inheritdoc />
	public abstract string NavigationTitle { get; }

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; } = rootNavigation;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = parent;

	/// <inheritdoc />
	public bool Hidden => false;
	/// <inheritdoc />
	public int NavigationIndex { get; set; }
	public bool IsCrossLink => false; // API grouping items are never cross-links

	/// <inheritdoc />
	public int Depth => 0;

	/// <inheritdoc />
	public abstract string Id { get; }

	//TODO ensure Index is not newed everytime
	/// <inheritdoc />
	public ILeafNavigationItem<TGroupingModel> Index => new ApiIndexLeafNavigation<TGroupingModel>(groupingModel, Url, NavigationTitle, rootNavigation, Parent);

	/// <inheritdoc />
	public IReadOnlyCollection<TNavigationItem> NavigationItems { get; set; } = [];
}

public class ClassificationNavigationItem(ApiClassification classification, LandingNavigationItem rootNavigation, LandingNavigationItem parent)
	: ApiGroupingNavigationItem<ApiClassification, INavigationItem>(classification, rootNavigation, parent), IRootNavigationItem<ApiClassification, INavigationItem>
{
	/// <inheritdoc />
	public override string NavigationTitle { get; } = classification.Name;

	/// <inheritdoc />
	public override string Id { get; } = ShortId.Create(classification.Name);

	/// <inheritdoc />
	public bool IsUsingNavigationDropdown => false;
}

public class TagNavigationItem(ApiTag tag, IRootNavigationItem<IApiGroupingModel, INavigationItem> rootNavigation, INodeNavigationItem<INavigationModel, INavigationItem> parent)
	: ApiGroupingNavigationItem<ApiTag, IEndpointOrOperationNavigationItem>(tag, rootNavigation, parent)
{
	/// <inheritdoc />
	public override string NavigationTitle { get; } = tag.Name;

	/// <inheritdoc />
	public override string Id { get; } = ShortId.Create(tag.Name);
}

public interface IEndpointOrOperationNavigationItem : INavigationItem;

public class EndpointNavigationItem(ApiEndpoint endpoint, IRootNavigationItem<IApiGroupingModel, INavigationItem> rootNavigation, INodeNavigationItem<INavigationModel, INavigationItem> parent)
	: IApiGroupingNavigationItem<ApiEndpoint, OperationNavigationItem>, IEndpointOrOperationNavigationItem
{
	/// <inheritdoc />
	public string Url => NavigationItems.First().Url;

	/// <inheritdoc />
	public string NavigationTitle { get; } = endpoint.Operations.First().ApiName;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; } = rootNavigation;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = parent;

	/// <inheritdoc />
	public bool Hidden => false;

	/// <inheritdoc />
	public int NavigationIndex { get; set; }
	public bool IsCrossLink => false; // API endpoint items are never cross-links

	/// <inheritdoc />
	public int Depth => 0;

	/// <inheritdoc />
	public string Id { get; } = ShortId.Create(nameof(EndpointNavigationItem), endpoint.Operations.First().ApiName, endpoint.Operations.First().Route);

	//TODO ensure Index is not newed everytime
	/// <inheritdoc />
	public ILeafNavigationItem<ApiEndpoint> Index => new ApiIndexLeafNavigation<ApiEndpoint>(endpoint, Url, NavigationTitle, rootNavigation, Parent);

	/// <inheritdoc />
	public IReadOnlyCollection<OperationNavigationItem> NavigationItems { get; set; } = [];
}
