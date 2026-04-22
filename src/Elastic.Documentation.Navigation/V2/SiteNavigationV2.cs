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
	private readonly Dictionary<string, NavigationSection> _urlToSection = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, NavigationIsland> _urlToIsland = new(StringComparer.OrdinalIgnoreCase);

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
		Sections = BuildSections(V2NavigationItems);
		Islands = BuildIslands(Sections);
		BuildUrlToSectionLookup();
		BuildUrlToIslandLookup();
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
	/// Resolves which island a page belongs to, if any. Islands take priority over sections.
	/// </summary>
	public NavigationIsland? GetIslandForUrl(string? pageUrl)
	{
		if (pageUrl is null)
			return null;
		var normalized = pageUrl.TrimEnd('/');
		if (_urlToIsland.TryGetValue(normalized, out var island))
			return island;
		return _urlToIsland.TryGetValue(normalized + "/", out island) ? island : null;
	}

	/// <summary>
	/// Resolves which section a page belongs to by its URL.
	/// Returns the first non-isolated section as fallback for unresolved URLs.
	/// </summary>
	public NavigationSection? GetSectionForUrl(string? pageUrl)
	{
		if (pageUrl is not null)
		{
			var normalized = pageUrl.TrimEnd('/');
			if (_urlToSection.TryGetValue(normalized, out var section))
				return section;
			if (_urlToSection.TryGetValue(normalized + "/", out section))
				return section;
		}
		return Sections.FirstOrDefault(s => !s.Isolated);
	}

	private static IReadOnlyList<NavigationSection> BuildSections(IReadOnlyList<INavigationItem> items) =>
		items
			.OfType<SectionNavigationNode>()
			.Select(s => new NavigationSection(s.Id, s.NavigationTitle, s.Url, s.Isolated, [.. s.NavigationItems]))
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
					parentSection,
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
			CollectUrlsForSection(section.NavigationItems, section);
	}

	private void CollectUrlsForSection(IEnumerable<INavigationItem> items, NavigationSection section)
	{
		foreach (var item in items)
		{
			// Skip island subtrees — they're handled by the island lookup
			if (item is IslandNavigationNode)
				continue;

			if (!string.IsNullOrEmpty(item.Url))
			{
				var normalized = item.Url.TrimEnd('/');
				_ = _urlToSection.TryAdd(normalized, section);
			}

			if (item is INodeNavigationItem<INavigationModel, INavigationItem> node)
				CollectUrlsForSection(node.NavigationItems, section);
		}
	}

	private void BuildUrlToIslandLookup()
	{
		foreach (var island in Islands)
		{
			// Register the island root URL (the toc entry point)
			if (!string.IsNullOrEmpty(island.Url))
			{
				var normalized = island.Url.TrimEnd('/');
				_ = _urlToIsland.TryAdd(normalized, island);
			}
			CollectUrlsForIsland(island.NavigationItems, island);
		}
	}

	private void CollectUrlsForIsland(IEnumerable<INavigationItem> items, NavigationIsland island)
	{
		foreach (var item in items)
		{
			if (!string.IsNullOrEmpty(item.Url))
			{
				var normalized = item.Url.TrimEnd('/');
				_ = _urlToIsland.TryAdd(normalized, island);
			}

			if (item is INodeNavigationItem<INavigationModel, INavigationItem> node)
				CollectUrlsForIsland(node.NavigationItems, island);
		}
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
		var placeholder = new SectionNavigationNode(section.Label, section.Url, section.Isolated, [], parent);
		var children = BuildV2Items(section.Children, nodes, placeholder, sitePrefix);
		return new SectionNavigationNode(section.Label, section.Url, section.Isolated, children, parent);
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
			var folderPlaceholder = new PageFolderNavigationNode(group.Title, group.Page, sitePrefix, [], parent);
			var folderChildren = BuildV2Items(group.Children, nodes, folderPlaceholder, sitePrefix);
			return new PageFolderNavigationNode(group.Title, group.Page, sitePrefix, folderChildren, parent);
		}
		var placeholder = new PlaceholderNavigationNode(group.Title, sitePrefix, [], parent);
		var children = BuildV2Items(group.Children, nodes, placeholder, sitePrefix);
		return new PlaceholderNavigationNode(group.Title, sitePrefix, children, parent);
	}
}
