// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Navigation;

/// Represents navigation model data for documentation elements.
public interface INavigationModel
{
	// This interface serves as a marker interface for navigation models
	// It's used as a constraint in other navigation-related interfaces
}

/// Represents an item in the navigation hierarchy.
public interface INavigationItem
{
	/// Gets the URL for this navigation item.
	string Url { get; }

	/// Gets the title displayed in navigation.
	string NavigationTitle { get; }

	/// Gets the root navigation item.
	IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; }

	/// Gets or sets the parent navigation item.
	INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }

	/// <summary>
	/// Whether this item is hidden from the rendered navigation tree.
	/// Used by <c>_TocTreeNav.cshtml</c> to skip rendering.
	/// </summary>
	bool Hidden { get; }

	/// <summary>
	/// Whether this item should be excluded from search indexing and the HTML <c>noindex</c> directive.
	/// Defaults to <see cref="Hidden"/> so existing behavior is preserved.
	/// Listing pages override this to <c>false</c> — they are hidden from the nav tree but should remain
	/// indexed and searchable.
	/// </summary>
	bool ExcludeFromIndexing => Hidden;

	/// <summary>
	/// When <c>true</c>, this node roots an island: the parent nav renders it as a <c>≫</c> stub
	/// and its subtree renders only inside a dedicated island sidebar.
	/// Combined with <see cref="NavigationItemExtensions.RendersAsIsland"/> (which also checks
	/// <see cref="Parent"/> is non-null) to suppress island behaviour on a docset root in isolated builds.
	/// </summary>
	bool IsIsland => false;

	int NavigationIndex { get; set; }
}

/// Represents a leaf node in the navigation tree with associated model data.
/// <typeparam name="TModel">The type attached to the navigation model.</typeparam>
public interface ILeafNavigationItem<out TModel> : INavigationItem
	where TModel : INavigationModel
{
	/// Gets the navigation model associated with this navigation item.
	TModel Model { get; }
}


/// Represents a node in the navigation tree that can contain child items.
/// <typeparam name="TIndex">The type of the index model.</typeparam>
/// <typeparam name="TChildNavigation">The type of child navigation items.</typeparam>
public interface INodeNavigationItem<out TIndex, out TChildNavigation> : INavigationItem
	where TIndex : INavigationModel
	where TChildNavigation : INavigationItem
{
	/// Gets the unique identifier for this node.
	string Id { get; }

	/// Gets the index model associated with this node.
	ILeafNavigationItem<TIndex> Index { get; }

	/// Gets the collection of child navigation items.
	IReadOnlyCollection<TChildNavigation> NavigationItems { get; }
}

public interface IAssignableChildrenNavigation
{
	void SetNavigationItems(IReadOnlyCollection<INavigationItem> navigationItems);
}

/// <summary>
/// Allows external code (e.g. <c>SiteNavigation</c>) to mark a resolved navigation node as an island
/// after the content set's own isolated build has already finished.
/// Implemented by every node type that supports <see cref="INavigationItem.IsIsland"/>.
/// </summary>
public interface IAssignableIslandNavigation
{
	bool IsIsland { get; set; }
}

public interface IRootNavigationItem<out TIndex, out TChildNavigation> : INodeNavigationItem<TIndex, TChildNavigation>, IAssignableChildrenNavigation
	where TIndex : INavigationModel
	where TChildNavigation : INavigationItem
{
	bool IsUsingNavigationDropdown { get; }

	Uri Identifier { get; }
}
