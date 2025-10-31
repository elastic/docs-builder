// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Documentation.Extensions;

namespace Elastic.Documentation.Navigation.Isolated;

[DebuggerDisplay("{Url}")]
public class FolderNavigation<TModel>(
	string parentPath,
	INodeNavigationItem<INavigationModel, INavigationItem>? parent,
	INavigationHomeAccessor homeAccessor)
	: INodeNavigationItem<TModel, INavigationItem>, IAssignableChildrenNavigation
	where TModel : class, IDocumentationFile
{
	// Will be set by SetNavigationItems

	public string FolderPath { get; } = parentPath;

	/// <inheritdoc />
	public string Url => Index.Url;

	/// <inheritdoc />
	public string NavigationTitle => Index.NavigationTitle;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => homeAccessor.HomeProvider.NavigationRoot;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = parent;

	/// <inheritdoc />
	public bool Hidden { get; }

	/// <inheritdoc />
	public int NavigationIndex { get; set; }

	/// <inheritdoc />
	public string Id { get; } = ShortId.Create(parentPath);

	/// <inheritdoc />
	public ILeafNavigationItem<TModel> Index { get; private set; } = null!;

	public IReadOnlyCollection<INavigationItem> NavigationItems { get; private set; } = [];

	void IAssignableChildrenNavigation.SetNavigationItems(IReadOnlyCollection<INavigationItem> navigationItems) => SetNavigationItems(navigationItems);
	internal void SetNavigationItems(IReadOnlyCollection<INavigationItem> navigationItems)
	{
		var indexNavigation = navigationItems.QueryIndex<TModel>(this, $"{FolderPath}/index.md", out navigationItems);
		Index = indexNavigation;
		NavigationItems = navigationItems;
	}
}
