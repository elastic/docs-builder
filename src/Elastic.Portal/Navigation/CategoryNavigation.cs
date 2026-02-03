// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation;

namespace Elastic.Portal.Navigation;

/// <summary>
/// Represents a category navigation node that groups documentation sets.
/// Categories provide one level of hierarchy under the portal root.
/// </summary>
[DebuggerDisplay("{Url}")]
public class CategoryNavigation(
	string categoryName,
	string displayTitle,
	string pathPrefix,
	INodeNavigationItem<INavigationModel, INavigationItem>? parent,
	IRootNavigationItem<INavigationModel, INavigationItem> navigationRoot
) : INodeNavigationItem<IDocumentationFile, INavigationItem>, IAssignableChildrenNavigation
{
	/// <summary>
	/// Gets the category name (used in URL path).
	/// </summary>
	public string CategoryName { get; } = categoryName;

	/// <summary>
	/// Gets the path prefix for documentation sets within this category.
	/// </summary>
	public string PathPrefix { get; } = pathPrefix.TrimEnd('/');

	/// <inheritdoc />
	public string Url => PathPrefix;

	/// <inheritdoc />
	public string NavigationTitle => Index.NavigationTitle;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; } = navigationRoot;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = parent;

	/// <inheritdoc />
	public bool Hidden => false;

	/// <inheritdoc />
	public int NavigationIndex { get; set; }

	/// <inheritdoc />
	public string Id { get; } = ShortId.Create($"category-{categoryName}");

	/// <inheritdoc />
	public ILeafNavigationItem<IDocumentationFile> Index { get; } = new CategoryIndexLeaf(
		new CategoryIndexPage(displayTitle),
		pathPrefix.TrimEnd('/'),
		navigationRoot);

	/// <inheritdoc />
	public IReadOnlyCollection<INavigationItem> NavigationItems { get; private set; } = [];

	/// <inheritdoc />
	void IAssignableChildrenNavigation.SetNavigationItems(IReadOnlyCollection<INavigationItem> navigationItems) =>
		NavigationItems = navigationItems;
}

/// <summary>
/// Represents a virtual index page for a category.
/// </summary>
public record CategoryIndexPage(string NavigationTitle) : IDocumentationFile;

/// <summary>
/// Represents the leaf navigation item for a category's index.
/// </summary>
[DebuggerDisplay("{Url}")]
public class CategoryIndexLeaf(
	CategoryIndexPage model,
	string url,
	IRootNavigationItem<INavigationModel, INavigationItem> navigationRoot
) : ILeafNavigationItem<IDocumentationFile>
{
	/// <inheritdoc />
	public IDocumentationFile Model { get; } = model;

	/// <inheritdoc />
	public string Url { get; } = url;

	/// <inheritdoc />
	public string NavigationTitle => Model.NavigationTitle;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; } = navigationRoot;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }

	/// <inheritdoc />
	public bool Hidden => false;

	/// <inheritdoc />
	public int NavigationIndex { get; set; }
}
