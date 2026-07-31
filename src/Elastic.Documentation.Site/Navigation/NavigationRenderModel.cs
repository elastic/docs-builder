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
	/// <summary>The root index page, surfaced as the first item only when primary nav is off.</summary>
	IndexLink,
	Link,
	Folder
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
	/// <summary>Only projected for folders, where it drives the expand/collapse checkbox and its persisted state.</summary>
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
	public required IReadOnlyList<NavigationRenderNode> Tree { get; init; }
	/// <summary>Hash of the preserved tree content only; the dropdown and search live outside the preserved element.</summary>
	public required string ContentHash { get; init; }

	public static NavigationRenderModel Create(NavigationViewModel model)
	{
		var topLevelItems = model.TopLevelItems.ToArray();
		var currentTopLevelItem = topLevelItems.FirstOrDefault(i => i.Id == model.Tree.Id) ?? model.Tree;
		var tree = CreateTree(model);
		return new NavigationRenderModel
		{
			IsUsingNavigationDropdown = model.IsUsingNavigationDropdown,
			CurrentTopLevelNavigationTitle = currentTopLevelItem.NavigationTitle,
			CurrentTopLevelUrl = currentTopLevelItem.Url,
			DropdownItems = model.IsUsingNavigationDropdown
				? [.. topLevelItems.Select(i => new NavigationDropdownItem(i.NavigationTitle, i.Url, i.NavigationRoot.Id == model.Tree.Id))]
				: [],
			Tree = tree,
			ContentHash = HashTree(tree)
		};
	}

	private static List<NavigationRenderNode> CreateTree(NavigationViewModel model)
	{
		var nodes = new List<NavigationRenderNode>();
		if (!model.IsGlobalAssemblyBuild && !model.IsPrimaryNavEnabled && !model.Tree.Index.Hidden)
		{
			nodes.Add(new NavigationRenderNode
			{
				Kind = NavigationRenderNodeKind.IndexLink,
				IsTopLevel = true,
				NavigationTitle = model.Tree.Index.NavigationTitle,
				Url = model.Tree.Index.Url
			});
		}
		nodes.AddRange(CreateNavigationItems(model.Tree, isTopLevel: true));
		return nodes;
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

			if (item is INodeNavigationItem<INavigationModel, INavigationItem> { NavigationItems.Count: > 0 } folder)
				yield return CreateFolder(folder, isTopLevel);
			else if (item is INodeNavigationItem<INavigationModel, INavigationItem> or ILeafNavigationItem<INavigationModel>)
				yield return CreateLink(item, isTopLevel);
		}
	}

	private static NavigationRenderNode CreateFolder(INodeNavigationItem<INavigationModel, INavigationItem> folder, bool isTopLevel)
	{
		var (badge, navigationTitle) = ParseNavTitle(folder.NavigationTitle);
		return new NavigationRenderNode
		{
			Kind = NavigationRenderNodeKind.Folder,
			IsTopLevel = isTopLevel,
			NavigationTitle = navigationTitle,
			Badge = badge,
			Url = folder.Url,
			Id = folder.Id,
			ShowToggle = !folder.NavigationItems.All(n => n.Hidden),
			NavigationItems = [.. CreateNavigationItems(folder, isTopLevel: false)]
		};
	}

	private static NavigationRenderNode CreateLink(INavigationItem item, bool isTopLevel)
	{
		var (badge, navigationTitle) = ParseNavTitle(item.NavigationTitle);
		return new NavigationRenderNode
		{
			Kind = NavigationRenderNodeKind.Link,
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

	private static string HashTree(IReadOnlyList<NavigationRenderNode> tree)
	{
		using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
		Append(hash, "navigation-tree-v1");
		AppendNodes(hash, tree);
		return Convert.ToHexStringLower(hash.GetHashAndReset().AsSpan(0, 8));
	}

	private static void AppendNodes(IncrementalHash hash, IReadOnlyList<NavigationRenderNode> nodes)
	{
		AppendInt(hash, nodes.Count);
		foreach (var node in nodes)
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
