// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Extensions;

namespace Elastic.Documentation.Navigation.Isolated;

public interface IDocumentationFile : INavigationModel
{
	string NavigationTitle { get; }
}

public class TableOfContentsNavigation : IRootNavigationItem<IDocumentationFile, INavigationItem>
	, INavigationPathPrefixProvider
	, IPathPrefixProvider
{
	public TableOfContentsNavigation(
		IDirectoryInfo tableOfContentsDirectory,
		int depth,
		string parentPath,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		IPathPrefixProvider pathPrefixProvider,
		IReadOnlyCollection<INavigationItem> navigationItems,
		GitCheckoutInformation git,
		Dictionary<Uri, INodeNavigationItem<INavigationModel, INavigationItem>> tocNodes
	)
	{
		TableOfContentsDirectory = tableOfContentsDirectory;
		NavigationItems = navigationItems;
		Index = NavigationItems.OfType<ILeafNavigationItem<IDocumentationFile>>().First();
		Parent = parent;
		PathPrefixProvider = pathPrefixProvider;
		NavigationRoot = this;
		Hidden = false;
		IsUsingNavigationDropdown = false;
		IsCrossLink = false;
		Id = ShortId.Create(parentPath);
		Depth = depth;
		ParentPath = parentPath;

		// Create an identifier for this TOC
		Identifier = new Uri($"{git.RepositoryName}://{parentPath}");
		_ = tocNodes.TryAdd(Identifier, this);
	}

	/// <summary>
	/// The composed path prefix for this TOC, which is the parent's prefix + this TOC's parent path.
	/// This is used by children to build their URLs.
	/// </summary>
	public string PathPrefix
	{
		get
		{
			var parentPrefix = PathPrefixProvider.PathPrefix.TrimEnd('/');
			return string.IsNullOrEmpty(parentPrefix) ? $"/{ParentPath}" : $"{parentPrefix}/{ParentPath}";
		}
	}

	/// <inheritdoc />
	public string Url => PathPrefix;

	/// <inheritdoc />
	public string NavigationTitle => Index.NavigationTitle;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; }

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }

	public IPathPrefixProvider PathPrefixProvider { get; set; }

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
