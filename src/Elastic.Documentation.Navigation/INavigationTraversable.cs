// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Runtime.CompilerServices;

namespace Elastic.Documentation.Navigation;

public interface INavigationTraversable
{
	ConditionalWeakTable<IDocumentationFile, INavigationItem> NavigationDocumentationFileLookup { get; }
	FrozenDictionary<int, INavigationItem> NavigationIndexedByOrder { get; }

	IEnumerable<INavigationItem> YieldAll()
	{
		var current = NavigationIndexedByOrder.Values.First();
		yield return current;
		do
		{
			current = GetNext(current);
			if (current is not null)
				yield return current;

		} while (current is not null);
	}

	/// <summary>
	/// Type-safe helper to get navigation item for a specific documentation file type
	/// </summary>
	INavigationItem? GetNavigationItem<TFile>(TFile file) where TFile : IDocumentationFile =>
		NavigationDocumentationFileLookup.TryGetValue(file, out var navigation) ? navigation : null;

	INavigationItem? GetPrevious(IDocumentationFile current)
	{
		var currentNavigation = GetCurrent(current);
		return GetPrevious(currentNavigation);
	}

	private INavigationItem? GetPrevious(INavigationItem currentNavigation)
	{
		var index = currentNavigation.NavigationIndex;
		do
		{
			var previous = NavigationIndexedByOrder.GetValueOrDefault(index - 1);
			if (previous is not null && !previous.Hidden && previous.Url != currentNavigation.Url)
				return previous;
			index--;
		} while (index >= 0);

		return null;
	}

	INavigationItem? GetNext(IDocumentationFile current)
	{
		var currentNavigation = GetCurrent(current);
		return GetNext(currentNavigation);
	}

	private INavigationItem? GetNext(INavigationItem currentNavigation)
	{
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

	INavigationItem GetCurrent(IDocumentationFile file) =>
		NavigationDocumentationFileLookup.TryGetValue(file, out var navigation)
			? navigation : throw new InvalidOperationException($"Could not find {file.NavigationTitle} in navigation");

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

	INavigationItem[] GetParentsOfMarkdownFile(IDocumentationFile file) =>
		NavigationDocumentationFileLookup.TryGetValue(file, out var navigation) ? GetParents(navigation) : [];
}
