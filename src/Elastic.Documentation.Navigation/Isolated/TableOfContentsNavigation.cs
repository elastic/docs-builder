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
		Dictionary<Uri, INodeNavigationItem<IDocumentationFile, INavigationItem>> tocNodes
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
		_pathPrefix = pathPrefix;

		// Initialize _homeProvider to this - it will be updated in assembler builds if needed
		_homeProvider = this;

		// Create an identifier for this TOC
		Identifier = new Uri($"{git.RepositoryName}://{parentPath}");
		_ = tocNodes.TryAdd(Identifier, this);

		// FindIndex must be called after _homeProvider is set
		Index = this.FindIndex<IDocumentationFile>(new NotFoundModel($"{parentPath}/index.md"));
	}

	private readonly string _pathPrefix;

	/// <summary>
	/// Internal HomeProvider - defaults to this, but can be updated in assembler builds.
	/// </summary>
	private INavigationHomeProvider _homeProvider { get; set; }

	/// <summary>
	/// The composed path prefix for this TOC, which is the parent's prefix + this TOC's parent path.
	/// This is used by children to build their URLs.
	/// Implements INavigationHomeProvider.PathPrefix
	/// When HomeProvider is set (during assembler), this returns the external provider's PathPrefix.
	/// </summary>
	public string PathPrefix => _homeProvider == this ? _pathPrefix : _homeProvider.PathPrefix;

	/// <inheritdoc />
	public string Url => Index.Url;

	/// <inheritdoc />
	public string NavigationTitle => Index.NavigationTitle;

	/// <summary>
	/// TableOfContentsNavigation is its own NavigationRoot in isolated builds.
	/// In assembler builds, this can be overridden via HomeProvider.
	/// This satisfies both INavigationItem.NavigationRoot and INavigationHomeProvider.NavigationRoot.
	/// </summary>
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => _homeProvider == this ? this : _homeProvider.NavigationRoot;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }

	/// <summary>
	/// TableOfContentsNavigation implements INavigationHomeProvider and provides itself
	/// as the home provider for its children by default. This creates the scoped navigation context.
	/// The setter is used in assembler builds to rehome the navigation.
	/// </summary>
	public INavigationHomeProvider HomeProvider
	{
		get => _homeProvider;
		set => _homeProvider = value;
	}

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
	public ILeafNavigationItem<IDocumentationFile> Index { get; }

	/// <inheritdoc />
	public bool IsUsingNavigationDropdown { get; }

	public IDirectoryInfo TableOfContentsDirectory { get; }

	public Uri Identifier { get; }

	public IReadOnlyCollection<INavigationItem> NavigationItems { get; }
}
