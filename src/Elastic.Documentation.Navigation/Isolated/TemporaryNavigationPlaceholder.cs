// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;

namespace Elastic.Documentation.Navigation.Isolated;

/// <summary>
/// Temporary placeholder used during navigation construction when children need a parent reference
/// before the final navigation item is created with its children collection.
/// This placeholder should never appear in the final navigation tree.
/// </summary>
internal sealed class TemporaryNavigationPlaceholder(
	int depth,
	string id,
	INodeNavigationItem<INavigationModel, INavigationItem>? parent,
	IRootNavigationItem<INavigationModel, INavigationItem> navigationRoot,
	IPathPrefixProvider pathPrefixProvider,
	string parentPath,
	IDirectoryInfo? tocDirectory = null) : INodeNavigationItem<INavigationModel, INavigationItem>, IPathPrefixProvider
{
	public int Depth { get; } = depth;
	public string Id { get; } = id;
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = parent;
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; } = navigationRoot;

	/// <summary>
	/// The parent path used for constructing nested paths, matching TableOfContentsNavigation behavior.
	/// </summary>
	public string ParentPath { get; } = parentPath;

	/// <summary>
	/// When this placeholder represents a TOC, this contains the TOC directory.
	/// This is needed for resolving file paths relative to the TOC directory.
	/// </summary>
	public IDirectoryInfo? TableOfContentsDirectory { get; } = tocDirectory;

	/// <summary>
	/// Computes the path prefix for this placeholder, mimicking TableOfContentsNavigation behavior.
	/// </summary>
	public string PathPrefix
	{
		get
		{
			var parentPrefix = pathPrefixProvider.PathPrefix.TrimEnd('/');
			return string.IsNullOrEmpty(parentPrefix) ? $"/{ParentPath}" : $"{parentPrefix}/{ParentPath}";
		}
	}

	// Properties that should never be accessed on a placeholder
	public string Url => throw new InvalidOperationException("TemporaryNavigationPlaceholder should not appear in final navigation");
	public string NavigationTitle => throw new InvalidOperationException("TemporaryNavigationPlaceholder should not appear in final navigation");
	public bool Hidden => throw new InvalidOperationException("TemporaryNavigationPlaceholder should not appear in final navigation");
	public int NavigationIndex { get; set; }
	public bool IsCrossLink => throw new InvalidOperationException("TemporaryNavigationPlaceholder should not appear in final navigation");
	public ILeafNavigationItem<INavigationModel> Index => throw new InvalidOperationException("TemporaryNavigationPlaceholder should not appear in final navigation");
	public IReadOnlyCollection<INavigationItem> NavigationItems => throw new InvalidOperationException("TemporaryNavigationPlaceholder should not appear in final navigation");
}
