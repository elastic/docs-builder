// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.Documentation.Navigation;

namespace Elastic.Documentation.Testing;

public static class NavigationDump
{
	/// <summary>
	/// The navigation tree as indented text, one node per line: <c>Type  url  "title"</c>, with <c>[hidden]</c> and
	/// <c>[island]</c> flags. A node lists its index page first (<c>index: Type  url  "title"</c>), then its children. The
	/// index page is where most of the interesting URLs live. A child whose <c>Parent</c> is not the node it is listed
	/// under is flagged <c>[wrong parent]</c>; index pages are not checked, because a virtual file's index has a
	/// different parent.
	/// </summary>
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
