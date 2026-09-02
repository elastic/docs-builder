// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.Assembler;

namespace Elastic.Documentation.Site.Navigation;

public enum NavigationRenderNodeKind
{
	Leaf,
	Node,
	Island
}

/// <summary>A fully resolved navigation tree node; the only tree data the nav templates consume.</summary>
public sealed record NavigationRenderNode
{
	public required NavigationRenderNodeKind Kind { get; init; }
	public required bool IsTopLevel { get; init; }
	public required string NavigationTitle { get; init; }
	public required string Url { get; init; }
	/// <summary>Badge parsed from a <c>[ns]</c>/<c>[cmd]</c>/<c>[alias]</c> title prefix; doubles as its CSS class suffix.</summary>
	public string? Badge { get; init; }
	/// <summary>Only projected for nodes, where it drives the expand/collapse checkbox and its persisted state.</summary>
	public string? Id { get; init; }
	public bool ShowToggle { get; init; }
	public IReadOnlyList<NavigationRenderNode> NavigationItems { get; init; } = [];
}

public sealed record NavigationDropdownItem(string NavigationTitle, string Url, bool IsActive);

/// <summary>A single back-link in the island sidebar's breadcrumb trail.</summary>
public sealed record IslandBackLink(string Title, string Url);

/// <summary>
/// Everything <c>_TocTree.cshtml</c> renders, resolved from the domain navigation up front.
/// <see cref="ContentHash"/> identifies the tree content so same-island pages share markup.
/// The tree itself lives in <c>#pages-nav</c>, which is <c>hx-preserve</c>'d so
/// expanding folders survives same-tree navigations. JS still replaces the nav
/// when the island/section surface changes (heading + Overview).
/// </summary>
public sealed record NavigationRenderModel
{
	public required bool IsUsingNavigationDropdown { get; init; }
	public required string CurrentTopLevelNavigationTitle { get; init; }
	public required string CurrentTopLevelUrl { get; init; }
	public required IReadOnlyList<NavigationDropdownItem> DropdownItems { get; init; }
	/// <summary>
	/// Root-first trail of island ancestors out of a nested island.
	/// Empty when the dropdown or assembler Docs tab already covers the site root
	/// and the render root has no other island ancestors.
	/// </summary>
	public required IReadOnlyList<IslandBackLink> BackLinks { get; init; }
	/// <summary>
	/// Root index link as the first sidebar row when primary nav is off.
	/// Null when primary nav / global assembly already covers that role,
	/// or when the index is flattened to an Overview row under <see cref="TreeHeading"/>.
	/// </summary>
	public NavigationRenderNode? RootIndex { get; init; }
	/// <summary>
	/// Non-clickable label above an island/section tree (e.g. "Elasticsearch", "Reference").
	/// The clickable index sits in <see cref="Tree"/> as "Overview".
	/// </summary>
	public string? TreeHeading { get; init; }
	/// <summary>Slug for the heading icon (<c>reference</c>, <c>elasticsearch</c>, …); null when none maps.</summary>
	public string? TreeHeadingIcon { get; init; }
	public required IReadOnlyList<NavigationRenderNode> Tree { get; init; }
	/// <summary>Hash of the tree content; used as the <c>nav-tree-*</c> id so same-island pages share markup.</summary>
	public required string ContentHash { get; init; }
	/// <summary>Whether the NAVIGATION_PREVIEW feature flag is enabled; drives nav-v2 vs legacy tree rendering.</summary>
	public bool NavigationPreviewEnabled { get; init; }

	public static NavigationRenderModel Create(
		INodeNavigationItem<INavigationModel, INavigationItem> tree,
		IEnumerable<INodeNavigationItem<INavigationModel, INavigationItem>> topLevelItems,
		bool isUsingNavigationDropdown,
		bool isPrimaryNavEnabled,
		bool isGlobalAssemblyBuild,
		bool navigationPreviewEnabled = false
	)
	{
		var topLevel = topLevelItems.ToArray();
		// Resolve current top-level by walking self-then-ancestors so nested islands
		// still highlight the right dropdown entry (e.g. "Reference" when rendering
		// the Java client island nested 3 levels deep).
		var topLevelIds = topLevel.Select(i => i.Id).ToHashSet();
		var currentTopLevelItem = tree;
		for (var cursor = (INavigationItem)tree; cursor is not null; cursor = cursor.Parent)
		{
			if (cursor is INodeNavigationItem<INavigationModel, INavigationItem> n && topLevelIds.Contains(n.Id))
			{
				currentTopLevelItem = n;
				break;
			}
		}
		var rootIndex = CreateRootIndex(tree, isPrimaryNavEnabled, isGlobalAssemblyBuild);
		string? treeHeading = null;
		List<NavigationRenderNode> nodes;
		if (TryUnwrapSingleChildSection(tree, out var onlyChild, out var sectionTitle))
		{
			treeHeading = sectionTitle;
			nodes = CreateNavigationItems(onlyChild, isTopLevel: true).ToList();
			if (!tree.Index.Hidden)
				nodes = FlattenIslandOverview(SectionOverviewLeaf(tree.Url), nodes);
			rootIndex = null;
		}
		else
		{
			nodes = CreateNavigationItems(tree, isTopLevel: true).ToList();
			if (rootIndex is not null && IsIslandSidebar(tree, isPrimaryNavEnabled, isGlobalAssemblyBuild))
			{
				treeHeading = rootIndex.NavigationTitle;
				nodes = FlattenIslandOverview(rootIndex, nodes);
				rootIndex = null;
			}
		}
		var backLinks = CreateBackLinks(tree, isUsingNavigationDropdown, omitSiteRoot: isGlobalAssemblyBuild);
		var treeHeadingIcon = HeadingIconSlug(treeHeading);
		return new NavigationRenderModel
		{
			IsUsingNavigationDropdown = isUsingNavigationDropdown,
			CurrentTopLevelNavigationTitle = currentTopLevelItem.NavigationTitle,
			CurrentTopLevelUrl = currentTopLevelItem.Url,
			DropdownItems = isUsingNavigationDropdown
				? [.. topLevel.Select(i => new NavigationDropdownItem(i.NavigationTitle, i.Url, i.Id == currentTopLevelItem.Id))]
				: [],
			BackLinks = backLinks,
			RootIndex = rootIndex,
			TreeHeading = treeHeading,
			TreeHeadingIcon = treeHeadingIcon,
			Tree = nodes,
			ContentHash = HashContent(rootIndex, treeHeading, treeHeadingIcon, nodes),
			NavigationPreviewEnabled = navigationPreviewEnabled
		};
	}

	/// <summary>
	/// Builds the root-first back-link trail out of a nested island.
	/// Immediate parent is always included; further ancestors only if they render as
	/// islands, so nested books stay visible after the sidebar collapses to the current one.
	/// The site root is omitted when the dropdown or assembler Docs tab already links there.
	/// Ancestors that share the render root URL are omitted so a tab landing
	/// (Reference, Troubleshoot, Release notes) does not link back to itself.
	/// </summary>
	private static IReadOnlyList<IslandBackLink> CreateBackLinks(
		INavigationItem renderRoot,
		bool isUsingNavigationDropdown,
		bool omitSiteRoot
	)
	{
		var immediateParent = renderRoot.Parent;
		if (immediateParent is null)
			return [];

		var links = new List<IslandBackLink>();
		var seen = new HashSet<string>(StringComparer.Ordinal);
		for (var ancestor = immediateParent; ancestor is not null; ancestor = ancestor.Parent)
		{
			if ((isUsingNavigationDropdown || omitSiteRoot) && ancestor.Parent is null)
				continue;
			if (SameNavUrl(ancestor.Url, renderRoot.Url))
				continue;

			var include = ReferenceEquals(ancestor, immediateParent) || ancestor.Parent is null || ancestor.RendersAsIsland();
			if (!include || !seen.Add(ancestor.Url))
				continue;
			var (_, title) = ParseNavTitle(ancestor.NavigationTitle);
			links.Add(new IslandBackLink(title, ancestor.Url));
		}
		links.Reverse();
		return links;
	}

	private static bool SameNavUrl(string left, string right) =>
		string.Equals(TrimNavUrl(left), TrimNavUrl(right), StringComparison.Ordinal);

	private static string TrimNavUrl(string url) => url.Length > 1 ? url.TrimEnd('/') : url;

	private static NavigationRenderNode? CreateRootIndex(
		INodeNavigationItem<INavigationModel, INavigationItem> tree,
		bool isPrimaryNavEnabled,
		bool isGlobalAssemblyBuild
	)
	{
		if (tree.Index.Hidden)
			return null;

		if (isGlobalAssemblyBuild)
		{
			// Top-level sections in assembler builds: dropdown covers navigation, no root row.
			// Nested islands (parent exists AND grandparent exists, i.e. not direct child of SiteNavigation)
			// show their own title row.
			if (tree.Parent?.Parent is null)
				return null;

			var (_, title) = ParseNavTitle(tree.NavigationTitle);
			return new NavigationRenderNode
			{
				Kind = NavigationRenderNodeKind.Leaf,
				IsTopLevel = true,
				NavigationTitle = title,
				Url = tree.Url
			};
		}

		if (isPrimaryNavEnabled)
			return null;

		// Island roots in isolated builds (no primary nav) show their own title row using the node title
		if (tree.RendersAsIsland())
		{
			var (_, title) = ParseNavTitle(tree.NavigationTitle);
			return new NavigationRenderNode
			{
				Kind = NavigationRenderNodeKind.Leaf,
				IsTopLevel = true,
				NavigationTitle = title,
				Url = tree.Url
			};
		}

		return new NavigationRenderNode
		{
			Kind = NavigationRenderNodeKind.Leaf,
			IsTopLevel = true,
			NavigationTitle = tree.Index.NavigationTitle,
			Url = tree.Index.Url
		};
	}

	/// <summary>
	/// Nested island sidebars (assembler) and isolated island roots: heading + Overview leaf
	/// in the same list as the children, not a wrapping folder.
	/// </summary>
	private static bool IsIslandSidebar(
		INodeNavigationItem<INavigationModel, INavigationItem> tree,
		bool isPrimaryNavEnabled,
		bool isGlobalAssemblyBuild
	)
	{
		if (isGlobalAssemblyBuild)
			return tree.Parent?.Parent is not null;
		return !isPrimaryNavEnabled && tree.RendersAsIsland();
	}

	/// <summary>
	/// Reference / Troubleshoot: a section whose only child is a toc wrapper. Unwrap it so
	/// the sidebar is "Reference" (heading) + Overview + the toc's children, not a folder.
	/// </summary>
	private static bool TryUnwrapSingleChildSection(
		INodeNavigationItem<INavigationModel, INavigationItem> tree,
		out INodeNavigationItem<INavigationModel, INavigationItem> child,
		out string heading
	)
	{
		child = null!;
		heading = "";
		if (tree is not SectionNavigation || tree.Parent?.Parent is not null)
			return false;

		INodeNavigationItem<INavigationModel, INavigationItem>? only = null;
		foreach (var item in tree.NavigationItems)
		{
			if (item.Hidden)
				continue;
			if (only is not null)
				return false;
			if (item is not INodeNavigationItem<INavigationModel, INavigationItem> { NavigationItems.Count: > 0 } node)
				return false;
			only = node;
		}

		if (only is null)
			return false;

		child = only;
		(_, heading) = ParseNavTitle(tree.NavigationTitle);
		return true;
	}

	private static NavigationRenderNode SectionOverviewLeaf(string url) =>
		new() { Kind = NavigationRenderNodeKind.Leaf, IsTopLevel = true, NavigationTitle = "Overview", Url = url };

	private static List<NavigationRenderNode> FlattenIslandOverview(NavigationRenderNode overview, List<NavigationRenderNode> children)
	{
		var overviewLeaf = overview with
		{
			Kind = NavigationRenderNodeKind.Leaf,
			NavigationTitle = "Overview",
			Id = null,
			ShowToggle = false,
			NavigationItems = []
		};
		if (children.Count == 0)
			return [overviewLeaf];

		return [overviewLeaf, .. children];
	}

	private static IEnumerable<NavigationRenderNode> CreateNavigationItems(
		INodeNavigationItem<INavigationModel, INavigationItem> parent,
		bool isTopLevel
	)
	{
		foreach (var item in parent.NavigationItems)
		{
			if (item.Hidden)
				continue;
			if (item.Parent is not null && item.Parent.Index == item)
				continue;

			if (item is INodeNavigationItem<INavigationModel, INavigationItem> { NavigationItems.Count: > 0 } node)
				yield return CreateNode(node, isTopLevel);
			else if (item is INodeNavigationItem<INavigationModel, INavigationItem> or ILeafNavigationItem<INavigationModel>)
				yield return CreateLeaf(item, isTopLevel);
		}
	}

	private static NavigationRenderNode CreateNode(INodeNavigationItem<INavigationModel, INavigationItem> node, bool isTopLevel)
	{
		var (badge, navigationTitle) = ParseNavTitle(node.NavigationTitle);
		if (node.RendersAsIsland())
		{
			return new NavigationRenderNode
			{
				Kind = NavigationRenderNodeKind.Island,
				IsTopLevel = isTopLevel,
				NavigationTitle = navigationTitle,
				Badge = badge,
				Url = node.Url,
				Id = node.Id
			};
		}
		return new NavigationRenderNode
		{
			Kind = NavigationRenderNodeKind.Node,
			IsTopLevel = isTopLevel,
			NavigationTitle = navigationTitle,
			Badge = badge,
			Url = node.Url,
			Id = node.Id,
			ShowToggle = !node.NavigationItems.All(n => n.Hidden),
			NavigationItems = [.. CreateNavigationItems(node, isTopLevel: false)]
		};
	}

	private static NavigationRenderNode CreateLeaf(INavigationItem item, bool isTopLevel)
	{
		var (badge, navigationTitle) = ParseNavTitle(item.NavigationTitle);
		return new NavigationRenderNode
		{
			Kind = NavigationRenderNodeKind.Leaf,
			IsTopLevel = isTopLevel,
			NavigationTitle = navigationTitle,
			Badge = badge,
			Url = item.Url
		};
	}

	private static (string? Badge, string NavigationTitle) ParseNavTitle(string raw)
	{
		if (raw.StartsWith("[ns]", StringComparison.Ordinal))
			return ("ns", raw[4..]);
		if (raw.StartsWith("[cmd]", StringComparison.Ordinal))
			return ("cmd", raw[5..]);
		if (raw.StartsWith("[alias]", StringComparison.Ordinal))
			return ("alias", raw[7..]);
		return (null, raw);
	}

	/// <summary>Top-nav / product glyph that matches a flattened heading, if we ship one.</summary>
	internal static string? HeadingIconSlug(string? heading) => heading switch
	{
		"Guides" => "guides",
		"Reference" => "reference",
		"Troubleshoot" => "troubleshoot",
		"Products" => "products",
		"APIs" => "apis",
		"Release notes" => "release-notes",
		"Elasticsearch" => "elasticsearch",
		_ => null
	};

	private static string HashContent(
		NavigationRenderNode? rootIndex,
		string? treeHeading,
		string? treeHeadingIcon,
		IReadOnlyList<NavigationRenderNode> tree
	)
	{
		using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
		Append(hash, "navigation-tree-v4");
		AppendInt(hash, rootIndex is null ? 0 : 1);
		if (rootIndex is not null)
			AppendNode(hash, rootIndex);
		Append(hash, treeHeading ?? string.Empty);
		Append(hash, treeHeadingIcon ?? string.Empty);
		AppendNodes(hash, tree);
		return Convert.ToHexStringLower(hash.GetHashAndReset().AsSpan(0, 8));
	}

	private static void AppendNodes(IncrementalHash hash, IReadOnlyList<NavigationRenderNode> nodes)
	{
		AppendInt(hash, nodes.Count);
		foreach (var node in nodes)
			AppendNode(hash, node);
	}

	private static void AppendNode(IncrementalHash hash, NavigationRenderNode node)
	{
		AppendInt(hash, (int)node.Kind);
		AppendInt(hash, node.IsTopLevel ? 1 : 0);
		Append(hash, node.NavigationTitle);
		Append(hash, node.Badge ?? string.Empty);
		Append(hash, node.Url);
		Append(hash, node.Id ?? string.Empty);
		AppendInt(hash, node.ShowToggle ? 1 : 0);
		AppendNodes(hash, node.NavigationItems);
	}

	// Length-prefixed fields make the byte stream unambiguous without separator escaping
	private static void Append(IncrementalHash hash, string value)
	{
		var bytes = Encoding.UTF8.GetBytes(value);
		AppendInt(hash, bytes.Length);
		hash.AppendData(bytes);
	}

	private static void AppendInt(IncrementalHash hash, int value)
	{
		Span<byte> buffer = stackalloc byte[4];
		BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
		hash.AppendData(buffer);
	}
}
