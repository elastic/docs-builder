// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;

namespace Elastic.Documentation.Navigation.Tests;

/// <summary>
/// Like <c>Elastic.Documentation.Testing.NavigationDump</c>, but also prints each node's index page
/// (<c>index: Type  url  "title"</c>) before its children, and flags a child whose <c>Parent</c> is not the node it is listed under. The index page is where most of the interesting
/// URLs live, and <c>NavigationDump</c> leaves it out.
/// </summary>
public static class NavigationTreeDump
{
	public static string ToText(INavigationItem root)
	{
		var sb = new StringBuilder();
		Write(sb, root, 0, null, null);
		return sb.ToString();
	}

	private static void Write(StringBuilder sb, INavigationItem item, int depth, string? label, INavigationItem? container)
	{
		sb
			.Append(' ', depth * 2)
			.Append(label)
			.Append(item.GetType().Name.Split('`')[0])
			.Append("  ")
			.Append(item.Url)
			.Append("  \"")
			.Append(item.NavigationTitle)
			.Append('"');
		if (item.Hidden)
			sb.Append(" [hidden]");
		if (item.IsIsland)
			sb.Append(" [island]");
		// every child points back at the node it is listed under
		if (container is not null && !ReferenceEquals(item.Parent, container))
			sb.Append(" [wrong parent]");
		sb.Append('\n');

		if (item is not INodeNavigationItem<INavigationModel, INavigationItem> node)
			return;
		Write(sb, node.Index, depth + 1, "index: ", null);
		foreach (var child in node.NavigationItems)
			Write(sb, child, depth + 1, null, item);
	}
}
