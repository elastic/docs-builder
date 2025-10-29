// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Documentation.Extensions;

namespace Elastic.Documentation.Navigation.Isolated;

[DebuggerDisplay("{Url}")]
public class FolderNavigation : INodeNavigationItem<IDocumentationFile, INavigationItem>, IAssignableChildrenNavigation
{
	private readonly INavigationHomeAccessor _homeAccessor;

	public FolderNavigation(
		int depth,
		string parentPath,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		INavigationHomeAccessor homeAccessor,
		IReadOnlyCollection<INavigationItem> navigationItems
	)
	{
		_homeAccessor = homeAccessor;
		FolderPath = parentPath;
		NavigationItems = navigationItems;
		Parent = parent;
		Depth = depth;
		Hidden = false;
		IsCrossLink = false;
		Id = ShortId.Create(parentPath);
		var indexNavigation = navigationItems.QueryIndex(this, new NotFoundModel($"{FolderPath}/index.md"), out navigationItems);
		Index = indexNavigation;
		NavigationItems = navigationItems;
	}

	public string FolderPath { get; }

	/// <inheritdoc />
	public string Url => Index.Url;

	/// <inheritdoc />
	public string NavigationTitle => Index.NavigationTitle;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => _homeAccessor.HomeProvider.NavigationRoot;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }

	/// <inheritdoc />
	public bool Hidden { get; }

	/// <inheritdoc />
	public int NavigationIndex { get; set; }

	/// <inheritdoc />
	public bool IsCrossLink { get; }

	/// <inheritdoc />
	public int Depth { get; }

	/// <inheritdoc />
	public string Id { get; }

	/// <inheritdoc />
	public ILeafNavigationItem<IDocumentationFile> Index { get; private set; }

	public IReadOnlyCollection<INavigationItem> NavigationItems { get; private set; }

	void IAssignableChildrenNavigation.SetNavigationItems(IReadOnlyCollection<INavigationItem> navigationItems) => SetNavigationItems(navigationItems);
	internal void SetNavigationItems(IReadOnlyCollection<INavigationItem> navigationItems)
	{
		var indexNavigation = navigationItems.QueryIndex(this, new NotFoundModel($"{FolderPath}/index.md"), out navigationItems);
		Index = indexNavigation;
		NavigationItems = navigationItems;
	}
}
