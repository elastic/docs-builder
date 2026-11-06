// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.Documentation.Navigation;

namespace Elastic.ApiExplorer.Tests;

/// <summary>Prints a navigation tree as text, one node per line, so a snapshot shows where pages sit and how they are flagged.</summary>
public static class NavigationTreePrinter
{
	public static string Print(INavigationItem root, int maxDepth = int.MaxValue, int maxChildren = int.MaxValue)
	{
		var builder = new StringBuilder();
		Append(builder, root, 0, maxDepth, maxChildren);
		return builder.ToString().TrimEnd();
	}

	private static void Append(StringBuilder builder, INavigationItem item, int depth, int maxDepth, int maxChildren)
	{
		_ = builder.Append(' ', depth * 2).Append(item.Url).Append(Flags(item)).AppendLine();
		if (item is not INodeNavigationItem<INavigationModel, INavigationItem> node)
			return;
		if (depth >= maxDepth)
			return;
		foreach (var child in node.NavigationItems.Take(maxChildren))
			Append(builder, child, depth + 1, maxDepth, maxChildren);
		var omitted = node.NavigationItems.Count - maxChildren;
		if (omitted > 0)
			_ = builder.Append(' ', (depth + 1) * 2).Append("… ").Append(omitted).AppendLine(" more");
	}

	private static string Flags(INavigationItem item)
	{
		var flags = new List<string>();
		if (item.IsIsland)
			flags.Add("island");
		if (item.Hidden)
			flags.Add("hidden");
		return flags.Count == 0 ? "" : $" [{string.Join(", ", flags)}]";
	}
}
