// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Navigation;

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

	/// <summary>
	/// Gets or sets the parent navigation item.
	/// </summary>
	/// <remarks>
	/// TODO: This should be read-only however currently needs the setter in assembler.
	/// </remarks>
	INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }

	bool Hidden { get; }

	int NavigationIndex { get; set; }

	/// Gets whether this navigation item is a cross-link to another repository.
	bool IsCrossLink { get; }
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
	/// Gets the depth level in the navigation hierarchy.
	int Depth { get; }

	/// Gets the unique identifier for this node.
	string Id { get; }

	/// Gets the index model associated with this node.
	TIndex Index { get; }

	/// <summary>
	/// Gets the collection of child navigation items.
	/// </summary>
	IReadOnlyCollection<TChildNavigation> NavigationItems { get; }
}

public interface IRootNavigationItem<out TIndex, out TChildNavigation> : INodeNavigationItem<TIndex, TChildNavigation>
	where TIndex : INavigationModel
	where TChildNavigation : INavigationItem
{
	bool IsUsingNavigationDropdown { get; }
}
