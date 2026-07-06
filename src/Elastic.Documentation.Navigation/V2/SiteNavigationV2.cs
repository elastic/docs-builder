// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Documentation.Navigation.Isolated.Node;

namespace Elastic.Documentation.Navigation.V2;

/// <summary>
/// Extends <see cref="SiteNavigation"/> with a V2 section-structured sidebar derived from
/// <c>navigation-v2.yml</c>. Content is built at the same URL paths as V1 (the original
/// <paramref name="originalFile"/> is passed to the base constructor unchanged).
/// <para>
/// Top-level <c>section:</c> items become independent nav trees (<see cref="Sections"/>).
/// Each section drives a tab in the secondary nav bar and its own sidebar.
/// Sections marked <c>isolated: true</c> do not appear in the top bar and render with a back arrow.
/// </para>
/// </summary>
public class SiteNavigationV2 : SiteNavigation
{
	private readonly Dictionary<string, NavigationSection> _urlToSection = [with(StringComparer.OrdinalIgnoreCase)];
	private readonly Dictionary<string, NavigationIsland> _tocRootToIsland = [with(StringComparer.Ordinal)];

	public SiteNavigationV2(
		NavigationV2File v2File,
		SiteNavigationFile originalFile,
		IDocumentationContext context,
		IReadOnlyCollection<IDocumentationSetNavigation> documentationSetNavigations,
		string? sitePrefix
	) : base(originalFile, context, documentationSetNavigations, sitePrefix)
	{
		var prefix = sitePrefix ?? string.Empty;
		V2NavigationItems = BuildV2Items(v2File.Nav, Nodes, this, prefix);
		RegisterV2PageLookups();
		Sections = BuildSections(V2NavigationItems);
		Islands = BuildIslands(Sections);
		BuildUrlToSectionLookup();
		BuildTocRootToIslandLookup();
	}

	/// <summary>
	/// All V2 navigation items (flat list including sections, labels, etc.).
	/// Used for placeholder generation and full-tree traversal.
	/// </summary>
	public IReadOnlyList<INavigationItem> V2NavigationItems { get; }

	/// <summary>
	/// Top-level sections extracted from <see cref="V2NavigationItems"/>.
	/// Each section owns an independent sidebar nav tree.
	/// </summary>
	public IReadOnlyList<NavigationSection> Sections { get; }

	/// <summary>
	/// Nav islands nested within sections. When a page belongs to an island,
	/// the sidebar shows the island's tree with a back arrow to the parent section.
	/// </summary>
	public IReadOnlyList<NavigationIsland> Islands { get; }

	/// <summary>
	/// Resolves which island a page belongs to by walking up its parent chain.
	/// Returns the innermost registered toc root, so islands that wrap nested tocs
	/// (whose pages have an outer toc as their NavigationRoot) still resolve correctly.
	/// </summary>
	public NavigationIsland? GetIslandForNavigationItem(INavigationItem item)
	{
		var current = item;
		while (current is not null)
		{
			if (current is INodeNavigationItem<INavigationModel, INavigationItem> node
				&& _tocRootToIsland.TryGetValue(node.Id, out var island))
				return island;
			current = current.Parent;
		}
		return null;
	}

	/// <summary>
	/// Resolves which section a page belongs to by its URL.
	/// Returns the first non-isolated section as fallback for unresolved URLs.
	/// </summary>
	public NavigationSection? GetSectionForUrl(string? pageUrl)
	{
		if (pageUrl is not null && TryGetSectionForUrl(pageUrl, out var section))
			return section;
		return Sections.FirstOrDefault(s => !s.Isolated);
	}

	private bool TryGetSectionForUrl(string url, out NavigationSection section)
	{
		var normalized = url.TrimEnd('/');
		if (_urlToSection.TryGetValue(normalized, out section!))
			return true;
		if (_urlToSection.TryGetValue(normalized + "/", out section!))
			return true;

		var prefix = Url.TrimEnd('/');
		if (!string.IsNullOrEmpty(prefix) && normalized.StartsWith($"{prefix}/", StringComparison.OrdinalIgnoreCase))
		{
			var withoutPrefix = normalized[prefix.Length..];
			if (_urlToSection.TryGetValue(withoutPrefix, out section!))
				return true;
			if (_urlToSection.TryGetValue(withoutPrefix + "/", out section!))
				return true;
		}

		section = null!;
		return false;
	}
	private static IReadOnlyList<NavigationSection> BuildSections(IReadOnlyList<INavigationItem> items) =>
		items
			.OfType<SectionNavigationNode>()
			.Select(s => new NavigationSection(s.Id, s.NavigationTitle, s.Url, s.Isolated, s.Dropdown, [.. s.NavigationItems]))
			.ToList();

	private static IReadOnlyList<NavigationIsland> BuildIslands(IReadOnlyList<NavigationSection> sections)
	{
		var islands = new List<NavigationIsland>();
		foreach (var section in sections)
			CollectIslandsFromItems(section.NavigationItems, section, islands);
		return islands;
	}

	private static void CollectIslandsFromItems(
		IEnumerable<INavigationItem> items,
		NavigationSection parentSection,
		List<NavigationIsland> islands
	)
	{
		foreach (var item in items)
		{
			if (item is IslandNavigationNode islandNode)
			{
				islands.Add(new NavigationIsland(
					islandNode.Id,
					islandNode.NavigationTitle,
					islandNode.Url,
					islandNode.SourceTocRootId,
					parentSection,
					islandNode.ParentUrl,
					[.. islandNode.NavigationItems]
				));
			}
			else if (item is INodeNavigationItem<INavigationModel, INavigationItem> node)
			{
				CollectIslandsFromItems(node.NavigationItems, parentSection, islands);
			}
		}
	}

	private void BuildUrlToSectionLookup()
	{
		foreach (var section in Sections)
		{
			CollectUrlsForSection(section.NavigationItems, section);
			AddUrlToSection(section.Url, section, replaceExisting: true);
		}
	}

	private void CollectUrlsForSection(IEnumerable<INavigationItem> items, NavigationSection section)
	{
		foreach (var item in items)
		{
			// Skip island subtrees — they're handled by the island lookup
			if (item is IslandNavigationNode)
				continue;

			AddUrlToSection(item.Url, section);

			if (item is INodeNavigationItem<INavigationModel, INavigationItem> node)
				CollectUrlsForSection(node.NavigationItems, section);
		}
	}

	private void AddUrlToSection(string url, NavigationSection section, bool replaceExisting = false)
	{
		if (string.IsNullOrEmpty(url))
			return;
		AddNormalizedUrlToSection(url, section, replaceExisting);
		if (IsExternalUrl(url))
			return;
		var prefix = Url.TrimEnd('/');
		var path = url.TrimStart('/');
		var prefixed = string.IsNullOrEmpty(path) ? prefix : $"{prefix}/{path}";
		if (!url.Equals(prefixed, StringComparison.OrdinalIgnoreCase))
			AddNormalizedUrlToSection(prefixed, section, replaceExisting);
	}

	private void AddNormalizedUrlToSection(string url, NavigationSection section, bool replaceExisting)
	{
		var normalized = url.TrimEnd('/');
		if (replaceExisting)
			_urlToSection[normalized] = section;
		else
			_ = _urlToSection.TryAdd(normalized, section);
	}

	private static bool IsExternalUrl(string url) =>
		url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
		url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

	private void BuildTocRootToIslandLookup()
	{
		foreach (var island in Islands)
			_ = _tocRootToIsland.TryAdd(island.SourceTocRootId, island);
	}

	private void RegisterV2PageLookups()
	{
		var urlToFile = new Dictionary<string, IDocumentationFile>(StringComparer.OrdinalIgnoreCase);
		foreach (var node in Nodes.Values)
			CollectDocumentationFilesByUrl(node, urlToFile, Url);

		RegisterV2PageLookups(V2NavigationItems, urlToFile);
	}

	private void RegisterV2PageLookups(
		IEnumerable<INavigationItem> items,
		IReadOnlyDictionary<string, IDocumentationFile> urlToFile
	)
	{
		foreach (var item in items)
		{
			switch (item)
			{
				case PageCrossLinkLeaf pageCrossLink:
					RegisterV2PageLookup(pageCrossLink.Page, pageCrossLink, urlToFile);
					break;
				case PageFolderNavigationNode pageFolder:
					RegisterV2PageLookup(pageFolder.Page, pageFolder, urlToFile);
					break;
			}

			if (item is INodeNavigationItem<INavigationModel, INavigationItem> node)
				RegisterV2PageLookups(node.NavigationItems, urlToFile);
		}
	}

	private void RegisterV2PageLookup(
		Uri page,
		INavigationItem item,
		IReadOnlyDictionary<string, IDocumentationFile> urlToFile
	)
	{
		if (TryResolvePageSource(page, out var file) || urlToFile.TryGetValue(item.Url, out file))
		{
			_ = NavigationDocumentationFileLookup.Remove(file);
			NavigationDocumentationFileLookup.Add(file, item);
		}
	}

	private bool TryResolvePageSource(Uri page, out IDocumentationFile file)
	{
		file = null!;
		var pagePath = GetUriPath(page);
		if (string.IsNullOrEmpty(pagePath))
			return false;

		var segments = pagePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
		for (var length = segments.Length; length > 0; length--)
		{
			var tocPath = string.Join('/', segments.Take(length));
			if (!Nodes.TryGetValue(CreateTocUri(page.Scheme, tocPath), out var node))
				continue;

			var remainingPath = string.Join('/', segments.Skip(length));
			if (string.IsNullOrEmpty(remainingPath))
			{
				file = node.Index.Model;
				return true;
			}

			if (TryFindDocumentationFile(node, remainingPath, out file))
				return true;
		}

		return false;
	}

	private static bool TryFindDocumentationFile(
		INavigationItem item,
		string remainingPath,
		out IDocumentationFile file
	)
	{
		switch (item)
		{
			case ILeafNavigationItem<IDocumentationFile> leaf when MatchesRemainingPath(leaf.Url, remainingPath):
				file = leaf.Model;
				return true;
			case INodeNavigationItem<IDocumentationFile, INavigationItem> node:
				if (MatchesRemainingPath(node.Index.Url, remainingPath))
				{
					file = node.Index.Model;
					return true;
				}
				foreach (var child in node.NavigationItems)
				{
					if (TryFindDocumentationFile(child, remainingPath, out file))
						return true;
				}
				break;
			case INodeNavigationItem<INavigationModel, INavigationItem> node:
				foreach (var child in node.NavigationItems)
				{
					if (TryFindDocumentationFile(child, remainingPath, out file))
						return true;
				}
				break;
		}

		file = null!;
		return false;
	}

	private static bool MatchesRemainingPath(string url, string remainingPath)
	{
		var normalizedPath = NormalizePagePath(remainingPath);
		return url.TrimEnd('/').EndsWith($"/{normalizedPath}", StringComparison.OrdinalIgnoreCase);
	}

	private static string NormalizePagePath(string path)
	{
		var normalized = path.Trim('/');
		if (normalized.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
			normalized = normalized[..^3];
		if (normalized.EndsWith("/index", StringComparison.OrdinalIgnoreCase))
			normalized = normalized[..^6];
		return normalized;
	}

	private static Uri CreateTocUri(string scheme, string path)
	{
		var slash = path.IndexOf('/');
		return slash < 0
			? new Uri($"{scheme}://{path}")
			: new Uri($"{scheme}://{path[..slash]}/{path[(slash + 1)..]}");
	}

	private static string GetUriPath(Uri uri) => (uri.Host + uri.AbsolutePath).Trim('/');

	private static void CollectDocumentationFilesByUrl(
		INavigationItem item,
		Dictionary<string, IDocumentationFile> urlToFile,
		string sitePrefix
	)
	{
		switch (item)
		{
			case ILeafNavigationItem<IDocumentationFile> leaf:
				AddDocumentationFileUrl(urlToFile, leaf.Url, leaf.Model, sitePrefix);
				break;
			case INodeNavigationItem<IDocumentationFile, INavigationItem> node:
				AddDocumentationFileUrl(urlToFile, node.Url, node.Index.Model, sitePrefix);
				AddDocumentationFileUrl(urlToFile, node.Index.Url, node.Index.Model, sitePrefix);
				foreach (var child in node.NavigationItems)
					CollectDocumentationFilesByUrl(child, urlToFile, sitePrefix);
				break;
			case INodeNavigationItem<INavigationModel, INavigationItem> node:
				foreach (var child in node.NavigationItems)
					CollectDocumentationFilesByUrl(child, urlToFile, sitePrefix);
				break;
		}
	}

	private static void AddDocumentationFileUrl(
		Dictionary<string, IDocumentationFile> urlToFile,
		string url,
		IDocumentationFile file,
		string sitePrefix
	)
	{
		_ = urlToFile.TryAdd(url, file);

		if (sitePrefix == "/" || string.IsNullOrEmpty(url) || !url.StartsWith('/'))
			return;

		_ = urlToFile.TryAdd($"{sitePrefix.TrimEnd('/')}{url}", file);
	}

	private static IReadOnlyList<INavigationItem> BuildV2Items(
		IReadOnlyList<INavV2Item> v2Items,
		IReadOnlyDictionary<Uri, IRootNavigationItem<IDocumentationFile, INavigationItem>> nodes,
		INodeNavigationItem<INavigationModel, INavigationItem> parent,
		string sitePrefix
	) =>
		v2Items
			.Select(item => CreateV2NavigationItem(item, nodes, parent, sitePrefix))
			.Where(navItem => navItem is not null)
			.Cast<INavigationItem>()
			.ToList();

	private static INavigationItem? CreateV2NavigationItem(
		INavV2Item item,
		IReadOnlyDictionary<Uri, IRootNavigationItem<IDocumentationFile, INavigationItem>> nodes,
		INodeNavigationItem<INavigationModel, INavigationItem> parent,
		string sitePrefix
	) =>
		item switch
		{
			SectionNavV2Item section => CreateSection(section, nodes, parent, sitePrefix),
			IslandNavV2Item island => CreateIsland(island, nodes, parent),
			LabelNavV2Item label => CreateLabel(label, nodes, parent, sitePrefix),
			GroupNavV2Item group => CreateGroup(group, nodes, parent, sitePrefix),
			TocNavV2Item toc => CreateToc(toc, nodes, parent, sitePrefix),
			PageNavV2Item { Page: null, Title: var title } => new PlaceholderNavigationLeaf(title ?? "Untitled", sitePrefix, parent),
			PageNavV2Item { Page: var page, Title: var title } => new PageCrossLinkLeaf(page, title ?? page.ToString(), sitePrefix, parent),
			_ => null
		};

	private static INavigationItem? CreateToc(
		TocNavV2Item toc,
		IReadOnlyDictionary<Uri, IRootNavigationItem<IDocumentationFile, INavigationItem>> nodes,
		INodeNavigationItem<INavigationModel, INavigationItem> parent,
		string sitePrefix
	)
	{
		if (!nodes.TryGetValue(toc.Source, out var node))
			return null;
		if (toc.Children.Count == 0)
			return node;
		var children = BuildV2Items(toc.Children, nodes, parent, sitePrefix);
		return new TocOverrideNode(node, children, parent.NavigationRoot, parent);
	}

	/// <summary>
	/// Wraps an existing toc node but replaces its children with a V2-specified subset.
	/// Used when a <c>toc:</c> entry in <c>navigation-v2.yml</c> declares explicit children,
	/// so we can show fewer items without mutating the shared V1 node.
	/// </summary>
	private sealed class TocOverrideNode(
		IRootNavigationItem<IDocumentationFile, INavigationItem> source,
		IReadOnlyList<INavigationItem> children,
		IRootNavigationItem<INavigationModel, INavigationItem> navigationRoot,
		INodeNavigationItem<INavigationModel, INavigationItem> parent
	) : INodeNavigationItem<INavigationModel, INavigationItem>
	{
		public string Id => source.Id;
		public string Url => source.Url;
		public string NavigationTitle => source.NavigationTitle;
		public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => navigationRoot;
		public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = parent;
		public bool Hidden => source.Hidden;
		public int NavigationIndex { get; set; }
		public ILeafNavigationItem<INavigationModel> Index => source.Index;
		public IReadOnlyCollection<INavigationItem> NavigationItems => children;
	}

	private static SectionNavigationNode CreateSection(
		SectionNavV2Item section,
		IReadOnlyDictionary<Uri, IRootNavigationItem<IDocumentationFile, INavigationItem>> nodes,
		INodeNavigationItem<INavigationModel, INavigationItem> parent,
		string sitePrefix
	)
	{
		var placeholder = new SectionNavigationNode(section.Label, section.Url, section.Isolated, section.Dropdown, [], parent);
		var children = BuildV2Items(section.Children, nodes, placeholder, sitePrefix);
		return new SectionNavigationNode(section.Label, section.Url, section.Isolated, section.Dropdown, children, parent);
	}

	private static INavigationItem? CreateIsland(
		IslandNavV2Item island,
		IReadOnlyDictionary<Uri, IRootNavigationItem<IDocumentationFile, INavigationItem>> nodes,
		INodeNavigationItem<INavigationModel, INavigationItem> parent
	)
	{
		if (!nodes.TryGetValue(island.Source, out var node))
			return null;
		return new IslandNavigationNode(island.Label, node, parent);
	}

	private static LabelNavigationNode CreateLabel(
		LabelNavV2Item label,
		IReadOnlyDictionary<Uri, IRootNavigationItem<IDocumentationFile, INavigationItem>> nodes,
		INodeNavigationItem<INavigationModel, INavigationItem> parent,
		string sitePrefix
	)
	{
		var placeholder = new LabelNavigationNode(label.Label, label.Expanded, [], parent);
		var children = BuildV2Items(label.Children, nodes, placeholder, sitePrefix);
		return new LabelNavigationNode(label.Label, label.Expanded, children, parent);
	}

	private static INodeNavigationItem<INavigationModel, INavigationItem> CreateGroup(
		GroupNavV2Item group,
		IReadOnlyDictionary<Uri, IRootNavigationItem<IDocumentationFile, INavigationItem>> nodes,
		INodeNavigationItem<INavigationModel, INavigationItem> parent,
		string sitePrefix
	)
	{
		if (group.Page is not null)
		{
			var folderPlaceholder = new PageFolderNavigationNode(group.Title, group.Page, sitePrefix, group.Expanded, [], parent);
			var folderChildren = BuildV2Items(group.Children, nodes, folderPlaceholder, sitePrefix);
			return new PageFolderNavigationNode(group.Title, group.Page, sitePrefix, group.Expanded, folderChildren, parent);
		}
		var placeholder = new PlaceholderNavigationNode(group.Title, sitePrefix, group.Expanded, [], parent);
		var children = BuildV2Items(group.Children, nodes, placeholder, sitePrefix);
		return new PlaceholderNavigationNode(group.Title, sitePrefix, group.Expanded, children, parent);
	}
}
