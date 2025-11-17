// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using Elastic.Documentation.Navigation;

namespace Elastic.Markdown.IO;

public interface IPositionalNavigation
{
	ConditionalWeakTable<MarkdownFile, INavigationItem> MarkdownNavigationLookup { get; }
	FrozenDictionary<int, INavigationItem> NavigationIndexedByOrder { get; }
	FrozenDictionary<string, ILeafNavigationItem<MarkdownFile>> NavigationIndexedByCrossLink { get; }

	INavigationItem? GetPrevious(MarkdownFile current)
	{
		var currentNavigation = GetCurrent(current);
		var index = currentNavigation.NavigationIndex;
		do
		{
			var previous = NavigationIndexedByOrder.GetValueOrDefault(index - 1);
			if (previous is not null && !previous.Hidden && previous.Url != currentNavigation.Url)
				return previous;
			index--;
		} while (index > 0);

		return null;
	}

	INavigationItem? GetNext(MarkdownFile current)
	{
		var currentNavigation = GetCurrent(current);
		var index = currentNavigation.NavigationIndex;
		do
		{
			var next = NavigationIndexedByOrder.GetValueOrDefault(index + 1);
			if (next is not null && !next.Hidden && next.Url != currentNavigation.Url)
				return next;
			index++;
		} while (index <= NavigationIndexedByOrder.Count - 1);

		return null;
	}

	INavigationItem GetCurrent(MarkdownFile file) =>
		MarkdownNavigationLookup.TryGetValue(file, out var navigation)
			? navigation : throw new InvalidOperationException($"Could not find {file.RelativePath} in navigation");

	INavigationItem[] GetParents(INavigationItem current)
	{
		var parents = new List<INavigationItem>();
		var parent = current.Parent;
		do
		{
			if (parent is null)
				continue;
			if (parents.All(i => i.Url != parent.Url))
				parents.Add(parent);

			parent = parent.Parent;
		} while (parent != null);

		return [.. parents];
	}

	INavigationItem[] GetParentsOfMarkdownFile(MarkdownFile file) =>
		MarkdownNavigationLookup.TryGetValue(file, out var navigation) ? GetParents(navigation) : [];
}
