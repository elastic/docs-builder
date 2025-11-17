// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.IO.Abstractions;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation.Isolated.Leaf;

namespace Elastic.Documentation.Navigation.Isolated.Node;

/// Represents a file navigation item that defines children which are not part of the file tree.
[DebuggerDisplay("{Url}")]
public class VirtualFileNavigation<TModel>(TModel model, IFileInfo fileInfo, VirtualFileNavigationArgs args)
	: INodeNavigationItem<TModel, INavigationItem>, IAssignableChildrenNavigation
	where TModel : IDocumentationFile
{
	/// <inheritdoc />
	public string Url => Index.Url;

	/// <inheritdoc />
	public string NavigationTitle => Index.NavigationTitle;

	/// <inheritdoc />
	public string? NavigationTooltip => Index.NavigationTooltip;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => args.HomeAccessor.HomeProvider.NavigationRoot;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = args.Parent;

	/// <inheritdoc />
	public bool Hidden { get; } = args.Hidden;

	/// <inheritdoc />
	public int NavigationIndex { get; set; }

	/// <inheritdoc />
	public string Id { get; } = ShortId.Create(args.RelativePathToDocumentationSet);

	/// <inheritdoc />
	public ILeafNavigationItem<TModel> Index { get; } =
		new FileNavigationLeaf<TModel>(model, fileInfo, new FileNavigationArgs(args.RelativePathToDocumentationSet, args.RelativePathToTableOfContents, args.Hidden, args.NavigationIndex, args.Parent, args.HomeAccessor));

	public IReadOnlyCollection<INavigationItem> NavigationItems { get; private set; } = [];

	void IAssignableChildrenNavigation.SetNavigationItems(IReadOnlyCollection<INavigationItem> navigationItems) => SetNavigationItems(navigationItems);
	internal void SetNavigationItems(IReadOnlyCollection<INavigationItem> navigationItems) => NavigationItems = navigationItems;
}
