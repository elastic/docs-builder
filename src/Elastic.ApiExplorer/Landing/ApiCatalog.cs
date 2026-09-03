// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Navigation;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation;
using RazorSlices;

namespace Elastic.ApiExplorer.Landing;

public sealed record ApiCatalogEntry(string Key, string Title, string Url);

public class ApiCatalog : IApiGroupingModel
{
	public required IReadOnlyList<ApiCatalogEntry> Entries { get; init; }

	public async Task RenderAsync(FileSystemStream stream, ApiRenderContext context, Cancel ctx = default)
	{
		var viewModel = new ApiCatalogViewModel(context) { Entries = Entries };
		await ApiCatalogView.Create(viewModel).RenderAsync(stream, cancellationToken: ctx);
	}

	public Task<string?> RenderCommonMarkAsync(ApiRenderContext context, Cancel ctx = default) =>
		Task.FromResult<string?>(LandingCommonMark.Catalog(Entries));
}

public class ApiCatalogNavigationItem : IRootNavigationItem<ApiCatalog, INavigationItem>, INavigationItem
{
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; }
	public string Id { get; }
	public ILeafNavigationItem<ApiCatalog> Index { get; }
	public IReadOnlyCollection<INavigationItem> NavigationItems { get; set; } = [];
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
	public int NavigationIndex { get; set; }
	public string Url => Index.Url;
	public bool Hidden => false;
	public Uri Identifier { get; } = new("about:blank");
	public string NavigationTitle => Index.NavigationTitle;

	public ApiCatalogNavigationItem(string url, IReadOnlyList<ApiCatalogEntry> entries)
	{
		NavigationRoot = this;
		Id = ShortId.Create("api-catalog");
		var catalog = new ApiCatalog { Entries = entries };
		Index = new ApiIndexLeafNavigation<ApiCatalog>(catalog, url, "API Explorer", this);
	}

	/// <inheritdoc />
	public bool IsUsingNavigationDropdown => false;

	void IAssignableChildrenNavigation.SetNavigationItems(IReadOnlyCollection<INavigationItem> navigationItems) =>
		throw new NotSupportedException(
			$"{nameof(IAssignableChildrenNavigation.SetNavigationItems)} is not supported on {nameof(ApiCatalogNavigationItem)}."
		);
}
