// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.IO.Abstractions;
using Elastic.Documentation.Extensions;

namespace Elastic.Documentation.Navigation.Isolated;

public interface IDocumentationFile : INavigationModel
{
	string NavigationTitle { get; }
}

[DebuggerDisplay("{Url}")]
public class TableOfContentsNavigation : IRootNavigationItem<IDocumentationFile, INavigationItem>
	, INavigationHomeAccessor
	, INavigationHomeProvider
{
	public TableOfContentsNavigation(
		IDirectoryInfo tableOfContentsDirectory,
		int depth,
		string parentPath,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		string pathPrefix,
		IReadOnlyCollection<INavigationItem> navigationItems,
		GitCheckoutInformation git,
		Dictionary<Uri, IRootNavigationItem<IDocumentationFile, INavigationItem>> tocNodes,
		INavigationHomeProvider homeProvider
	)
	{
		TableOfContentsDirectory = tableOfContentsDirectory;
		NavigationItems = navigationItems;
		Parent = parent;
		Hidden = false;
		IsUsingNavigationDropdown = false;
		IsCrossLink = false;
		Id = ShortId.Create(parentPath);
		Depth = depth;
		ParentPath = parentPath;
		PathPrefix = pathPrefix;

		// Initialize _homeProvider from the provided homeProvider
		// According to url-building.md: "In isolated builds the NavigationRoot is always the DocumentationSetNavigation"
		HomeProvider = homeProvider;

		// Create an identifier for this TOC
		Identifier = new Uri($"{git.RepositoryName}://{parentPath}");
		_ = tocNodes.TryAdd(Identifier, this);

		// FindIndex must be called after _homeProvider is set
		Index = this.FindIndex<IDocumentationFile>(new NotFoundModel($"{parentPath}/index.md"));
	}

	/// <summary>
	/// The path prefix for this TOC - same as parent per url-building.md.
	/// Implements INavigationHomeProvider.PathPrefix.
	/// TOC doesn't change PathPrefix from parent.
	/// </summary>
	public string PathPrefix { get; }

	bool INavigationHomeProvider.RelativeToTableOfContents => false;

	/// <inheritdoc />
	public string Url => Index.Url;

	/// <inheritdoc />
	public string NavigationTitle => Index.NavigationTitle;

	/// <summary>
	/// TableOfContentsNavigation's NavigationRoot comes from its HomeProvider.
	/// According to url-building.md: "In isolated builds the NavigationRoot is always the DocumentationSetNavigation"
	/// This satisfies both INavigationItem.NavigationRoot and INavigationHomeProvider.NavigationRoot.
	/// </summary>
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => HomeProvider.NavigationRoot;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }

	/// <summary>
	/// TableOfContentsNavigation implements INavigationHomeProvider and provides itself
	/// as the home provider for its children by default. This creates the scoped navigation context.
	/// The setter is used in assembler builds to rehome the navigation.
	/// </summary>
	public INavigationHomeProvider HomeProvider { get; set; }

	/// <inheritdoc />
	public bool Hidden { get; }

	/// <inheritdoc />
	public int NavigationIndex { get; set; }

	/// <inheritdoc />
	public bool IsCrossLink { get; }

	/// <inheritdoc />
	public int Depth { get; }

	public string ParentPath { get; }

	/// <inheritdoc />
	public string Id { get; }

	/// <inheritdoc />
	public ILeafNavigationItem<IDocumentationFile> Index { get; private set; }

	/// <inheritdoc />
	public bool IsUsingNavigationDropdown { get; }

	public IDirectoryInfo TableOfContentsDirectory { get; }

	public Uri Identifier { get; }

	public IReadOnlyCollection<INavigationItem> NavigationItems { get; private set; }

	void IAssignableChildrenNavigation.SetNavigationItems(IReadOnlyCollection<INavigationItem> navigationItems) => SetNavigationItems(navigationItems);
	internal void SetNavigationItems(IReadOnlyCollection<INavigationItem> navigationItems)
	{
		NavigationItems = navigationItems;
		Index = this.FindIndex<IDocumentationFile>(new NotFoundModel($"{PathPrefix}/index.md"));
	}
}
