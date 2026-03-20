// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Navigation.V2;

/// <summary>
/// A real cross-link leaf in the V2 sidebar — has a URL derived from the source
/// <c>page:</c> URI and the site prefix. Renders as a normal clickable link.
/// </summary>
public class PageCrossLinkLeaf(
	Uri page,
	string title,
	string sitePrefix,
	INodeNavigationItem<INavigationModel, INavigationItem>? parent
) : ILeafNavigationItem<INavigationModel>, INavigationModel
{
	/// <inheritdoc />
	public INavigationModel Model => this;

	/// <inheritdoc />
	public string Url { get; } = ResolveUrl(page, sitePrefix);

	/// <inheritdoc />
	public string NavigationTitle { get; } = title;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; } = parent?.NavigationRoot!;

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = parent;

	/// <inheritdoc />
	public bool Hidden => false;

	/// <inheritdoc />
	public int NavigationIndex { get; set; }

	private static string ResolveUrl(Uri page, string sitePrefix)
	{
		// URI host carries the first path segment (e.g. docs-content://manage-data/ingest.md
		// → host="manage-data", absolutePath="/ingest.md"), so combine both.
		var path = (page.Host + page.AbsolutePath).Trim('/');
		if (path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
			path = path[..^3];
		if (path.EndsWith("/index", StringComparison.OrdinalIgnoreCase))
			path = path[..^6];
		var prefix = "/" + sitePrefix.Trim('/');
		return string.IsNullOrEmpty(path) ? prefix : $"{prefix}/{path}";
	}
}
