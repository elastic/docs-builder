// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Site.Navigation;

namespace Elastic.ApiExplorer.Landing;

public class ApiLanding(IGroupNavigationItem root, string url) : IPageInformation, IPageRenderer<ApiRenderContext>
{
	public IGroupNavigationItem NavigationRoot { get; } = root;
	public string Url { get; } = url;

	//TODO
	public string NavigationTitle { get; } = "API Documentation";
	public string CrossLink { get; } = string.Empty;

	public async Task RenderAsync(FileSystemStream stream, ApiRenderContext context, Cancel ctx = default)
	{
		var viewModel = new LandingViewModel
		{
			Landing = this,
			StaticFileContentHashProvider = context.StaticFileContentHashProvider,
			NavigationHtml = context.NavigationHtml,
			ApiInfo = context.Model.Info,
		};
		var slice = Landing.LandingView.Create(viewModel);
		await slice.RenderAsync(stream, cancellationToken: ctx);
	}
}

public class LandingNavigationItem : IGroupNavigationItem
{
	public IGroupNavigationItem NavigationRoot { get; }
	public string Id { get; }
	public IGroupNavigationItem? Parent { get; set; }
	public int Depth { get; }
	public IPageInformation Current { get; set; }
	public IPageInformation Index { get; set; }
	public ApiLanding Landing { get; set; }
	public IReadOnlyCollection<INavigationItem> NavigationItems { get; set; } = [];

	public LandingNavigationItem(string url)
	{
		Parent = null;
		Depth = 0;
		NavigationRoot = this;
		Id = ShortId.Create("root");

		var landing = new ApiLanding(this, url);

		Index = landing;
		Current = landing;
		Landing = landing;
	}
}
