// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Elastic.Documentation.Navigation;

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

/// <summary>
/// Everything <c>_TocTree.cshtml</c> renders, resolved from the domain navigation up front.
/// <see cref="ContentHash"/> identifies the preserved tree content: pages whose trees are identical
/// share a <c>nav-tree-*</c> id so htmx keeps the sidebar DOM (and its expand/collapse state) alive,
/// while any visible change produces a new id and swaps in fresh HTML.
/// </summary>
public sealed record NavigationRenderModel
{
	public required bool IsUsingNavigationDropdown { get; init; }
	public required string CurrentTopLevelNavigationTitle { get; init; }
	public required string CurrentTopLevelUrl { get; init; }
	public required IReadOnlyList<NavigationDropdownItem> DropdownItems { get; init; }
	/// <summary>
	/// Root index link as the first sidebar row when primary nav is off.
	/// Null when primary nav / global assembly already covers that role.
	/// </summary>
	public NavigationRenderNode? RootIndex { get; init; }
	public required IReadOnlyList<NavigationRenderNode> Tree { get; init; }
	/// <summary>Hash of the preserved tree content only; the dropdown and search live outside the preserved element.</summary>
	public required string ContentHash { get; init; }

	public static NavigationRenderModel Create(
		INodeNavigationItem<INavigationModel, INavigationItem> tree,
		IEnumerable<INodeNavigationItem<INavigationModel, INavigationItem>> topLevelItems,
		bool isUsingNavigationDropdown,
		bool isPrimaryNavEnabled,
		bool isGlobalAssemblyBuild)
	{
		var topLevel = topLevelItems.ToArray();
		var currentTopLevelItem = topLevel.FirstOrDefault(i => i.Id == tree.Id) ?? tree;
		var rootIndex = CreateRootIndex(tree, isPrimaryNavEnabled, isGlobalAssemblyBuild);
		var nodes = CreateNavigationItems(tree, isTopLevel: true).ToList();
		return new NavigationRenderModel
		{
			IsUsingNavigationDropdown = isUsingNavigationDropdown,
			CurrentTopLevelNavigationTitle = currentTopLevelItem.NavigationTitle,
			CurrentTopLevelUrl = currentTopLevelItem.Url,
			DropdownItems = isUsingNavigationDropdown
				? [.. topLevel.Select(i => new NavigationDropdownItem(i.NavigationTitle, i.Url, i.NavigationRoot.Id == tree.Id))]
				: [],
			RootIndex = rootIndex,
			Tree = nodes,
			ContentHash = HashContent(rootIndex, nodes)
		};
	}

	private static NavigationRenderNode? CreateRootIndex(
		INodeNavigationItem<INavigationModel, INavigationItem> tree,
		bool isPrimaryNavEnabled,
		bool isGlobalAssemblyBuild)
	{
		if (isGlobalAssemblyBuild || isPrimaryNavEnabled || tree.Index.Hidden)
			return null;

		return new NavigationRenderNode
		{
			Kind = NavigationRenderNodeKind.Leaf,
			IsTopLevel = true,
			NavigationTitle = tree.Index.NavigationTitle,
			Url = tree.Index.Url
		};
	}

	private static IEnumerable<NavigationRenderNode> CreateNavigationItems(
		INodeNavigationItem<INavigationModel, INavigationItem> parent,
		bool isTopLevel)
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
		if (node.IsIslandListing)
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

	private static string HashContent(NavigationRenderNode? rootIndex, IReadOnlyList<NavigationRenderNode> tree)
	{
		using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
		Append(hash, "navigation-tree-v2");
		AppendInt(hash, rootIndex is null ? 0 : 1);
		if (rootIndex is not null)
			AppendNode(hash, rootIndex);
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
