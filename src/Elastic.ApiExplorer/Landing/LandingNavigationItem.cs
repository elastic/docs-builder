// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Endpoints;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Site.Navigation;
using RazorSlices;

namespace Elastic.ApiExplorer.Landing;

public class ApiLanding : INavigationModel, IPageRenderer<ApiRenderContext>
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

public class LandingNavigationItem : INodeNavigationItem<ApiLanding, EndpointNavigationItem>
{
	public INodeNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; }
	public string Id { get; }
	public int Depth { get; }
	public ApiLanding Index { get; }
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
	public IReadOnlyCollection<EndpointNavigationItem> NavigationItems { get; set; } = [];
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
