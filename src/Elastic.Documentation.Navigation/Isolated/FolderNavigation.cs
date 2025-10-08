// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Extensions;

namespace Elastic.Documentation.Navigation.Isolated;

public class FolderNavigation : INodeNavigationItem<IDocumentationFile, INavigationItem>
{
	private readonly string _folderPath;
	private readonly IPathPrefixProvider _pathPrefixProvider;

	public FolderNavigation(
		int depth,
		string parentPath,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		IRootNavigationItem<INavigationModel, INavigationItem> navigationRoot,
		IPathPrefixProvider pathPrefixProvider,
		IReadOnlyCollection<INavigationItem> navigationItems
	)
	{
		_folderPath = parentPath;
		_pathPrefixProvider = pathPrefixProvider;
		NavigationItems = navigationItems;
		Index = NavigationItems.OfType<ILeafNavigationItem<IDocumentationFile>>().First();
		NavigationRoot = navigationRoot;
		Parent = parent;
		Depth = depth;
		Hidden = false;
		IsCrossLink = false;
		Id = ShortId.Create(parentPath);
	}

	/// <inheritdoc />
	public string Url
	{
		get
		{
			// Check if there's an index file among the children
			var hasIndexChild = NavigationItems.Any(item =>
				item is ILeafNavigationItem<IDocumentationFile> &&
				item.NavigationTitle.Equals("index", StringComparison.OrdinalIgnoreCase));

			// If no index child exists, use the first child's URL
			if (!hasIndexChild && NavigationItems.Count > 0)
				return NavigationItems.First().Url;

			// Otherwise, use the folder path
			var rootUrl = _pathPrefixProvider.PathPrefix.TrimEnd('/');
			return string.IsNullOrEmpty(rootUrl) ? $"/{_folderPath}" : $"{rootUrl}/{_folderPath}";
		}
	}

	/// <inheritdoc />
	public string NavigationTitle => Index.NavigationTitle;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; }

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
	public ILeafNavigationItem<IDocumentationFile> Index { get; }

	public IReadOnlyCollection<INavigationItem> NavigationItems { get; }
}
