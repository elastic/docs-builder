// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Extensions;

namespace Elastic.Documentation.Navigation.V2;

/// <summary>
/// A folder node in the V2 navigation sidebar that also carries a real URL.
/// Used when a <c>group:</c> entry in <c>navigation-v2.yml</c> has an associated <c>page:</c> key.
/// Renders as an expandable section whose header is a clickable link.
/// </summary>
public class PageFolderNavigationNode : INodeNavigationItem<INavigationModel, INavigationItem>
{
	private readonly PageFolderIndexLeaf _index;

	public PageFolderNavigationNode(
		string title,
		Uri page,
		string sitePrefix,
		IReadOnlyCollection<INavigationItem> children,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent
	)
	{
		Id = ShortId.Create("page-folder", title);
		NavigationTitle = title;
		Url = ResolveUrl(page, sitePrefix);
		NavigationItems = children;
		Parent = parent;
		NavigationRoot = parent?.NavigationRoot!;
		_index = new PageFolderIndexLeaf(this);
	}

	/// <inheritdoc />
	public string Id { get; }

	/// <inheritdoc />
	public string Url { get; }

	/// <inheritdoc />
	public string NavigationTitle { get; }

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; }

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }

	/// <inheritdoc />
	public bool Hidden => false;

	/// <inheritdoc />
	public int NavigationIndex { get; set; }

	/// <inheritdoc />
	public ILeafNavigationItem<INavigationModel> Index => _index;

	/// <inheritdoc />
	public IReadOnlyCollection<INavigationItem> NavigationItems { get; }

	private static string ResolveUrl(Uri page, string sitePrefix)
	{
		var path = (page.Host + page.AbsolutePath).Trim('/');
		if (path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
			path = path[..^3];
		if (path.EndsWith("/index", StringComparison.OrdinalIgnoreCase))
			path = path[..^6];
		var prefix = "/" + sitePrefix.Trim('/');
		return string.IsNullOrEmpty(path) ? prefix : $"{prefix}/{path}";
	}

	private sealed class PageFolderIndexLeaf(PageFolderNavigationNode owner)
		: ILeafNavigationItem<INavigationModel>, INavigationModel
	{
		public INavigationModel Model => this;
		public string Url => owner.Url;
		public string NavigationTitle => owner.NavigationTitle;
		public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => owner.NavigationRoot;
		public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = owner;
		public bool Hidden => true;
		public int NavigationIndex { get; set; }
	}
}
