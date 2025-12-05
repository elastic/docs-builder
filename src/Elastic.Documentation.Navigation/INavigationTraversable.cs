// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Runtime.CompilerServices;

namespace Elastic.Documentation.Navigation;

public static class NavigationExtensions
{
	extension(INavigationItem navigationItem)
	{
		public INavigationItem[] GetParents()
		{
			var parents = new List<INavigationItem>();
			var parent = navigationItem.Parent;
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

		public int NavigationDepth => navigationItem.GetParents().Length;

		public string? NavigationSection
		{
			get
			{
				var parents = navigationItem.GetParents();
				if (parents.Length <= 1)
					return navigationItem.NavigationTitle.ToLowerInvariant();
				return parents.Reverse().Skip(1).FirstOrDefault()?.NavigationTitle.ToLowerInvariant();
			}
		}
	}
}

public interface INavigationTraversable
{
	ConditionalWeakTable<IDocumentationFile, INavigationItem> NavigationDocumentationFileLookup { get; }
	FrozenDictionary<int, INavigationItem> NavigationIndexedByOrder { get; }

	IEnumerable<INavigationItem> YieldAll()
	{
		if (NavigationIndexedByOrder.Count == 0)
			yield break;
		var current = NavigationIndexedByOrder.Values.First();
		yield return current;
		do
		{
			current = GetNext(current);
			if (current is not null)
				yield return current;

		} while (current is not null);
	}

	INavigationItem? GetPrevious(IDocumentationFile current)
	{
		var currentNavigation = GetNavigationFor(current);
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
		var currentNavigation = GetNavigationFor(current);
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

	INavigationItem GetNavigationFor(IDocumentationFile file) =>
		NavigationDocumentationFileLookup.TryGetValue(file, out var navigation)
			? navigation : throw new InvalidOperationException($"Could not find {file.NavigationTitle} in navigation");

	INavigationItem[] GetParents(INavigationItem current) => current.GetParents();

	INavigationItem[] GetParentsOfMarkdownFile(IDocumentationFile file) =>
		NavigationDocumentationFileLookup.TryGetValue(file, out var navigation) ? GetParents(navigation) : [];
}
