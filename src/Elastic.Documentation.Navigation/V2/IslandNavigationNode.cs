// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Extensions;

namespace Elastic.Documentation.Navigation.V2;

/// <summary>
/// A nav island that wraps an existing toc node. When a page belongs to this island,
/// the sidebar shows only the island's tree with a back arrow to the parent section.
/// In the parent section's sidebar, the island renders as a normal folder link.
/// </summary>
public class IslandNavigationNode(
	string label,
	IRootNavigationItem<IDocumentationFile, INavigationItem> source,
	INodeNavigationItem<INavigationModel, INavigationItem> parent
) : INodeNavigationItem<INavigationModel, INavigationItem>
{
	/// <summary>The Id of the wrapped toc root, used for island lookup by nav ownership.</summary>
	public string SourceTocRootId { get; } = source.Id;

	/// <summary>
	/// The URL to use for the back arrow when this island is active.
	/// Walks up the parent chain to find the nearest ancestor with a meaningful URL,
	/// falling back to "/" if none is found.
	/// </summary>
	public string ParentUrl { get; } = ResolveParentUrl(parent);

	private static string ResolveParentUrl(INodeNavigationItem<INavigationModel, INavigationItem>? parent)
	{
		var current = parent;
		while (current is not null)
		{
			if (!string.IsNullOrEmpty(current.Url) && current.Url != "/")
				return current.Url;
			current = current.Parent;
		}
		return "/";
	}

	/// <inheritdoc />
	public string Id { get; } = ShortId.Create("island", label);

	/// <inheritdoc />
	public string Url => source.Url;

	/// <inheritdoc />
	public string NavigationTitle { get; } = label;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; } = parent.NavigationRoot;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = parent;

	/// <inheritdoc />
	public bool Hidden => source.Hidden;

	/// <inheritdoc />
	public int NavigationIndex { get; set; }

	/// <inheritdoc />
	public ILeafNavigationItem<INavigationModel> Index => source.Index;

	/// <inheritdoc />
	public IReadOnlyCollection<INavigationItem> NavigationItems => source.NavigationItems;
}
