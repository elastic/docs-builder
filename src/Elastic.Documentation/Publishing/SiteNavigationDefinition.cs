// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;
using Generator.Equals;

namespace Elastic.Documentation.Publishing;

/// <summary>
/// The assembled site's navigation as declared in <c>navigation.yml</c> (or <c>navigation_preview.yml</c>): where each
/// documentation set's table of contents mounts, grouped into top-level sections, plus phantom sources that are
/// known but intentionally not mounted.
/// </summary>
[Equatable]
public sealed partial record SiteNavigationDefinition
{
	[OrderedEquality]
	public IReadOnlyList<PhantomRegistration> Phantoms { get; init; } = [];

	// A union compares its case value, and both cases are [Equatable]; the analyzer cannot see through the union.
#pragma warning disable GE003
	[OrderedEquality]
	public IReadOnlyList<SiteNavigationEntry> TableOfContents { get; init; } = [];
#pragma warning restore GE003

	/// <summary>Every table of contents the navigation mounts, at any depth.</summary>
	public static ImmutableHashSet<Uri> GetAllDeclaredSources(SiteNavigationDefinition siteNavigation)
	{
		var set = new HashSet<Uri>();
		foreach (var entry in siteNavigation.TableOfContents)
		{
			if (entry is SiteTableOfContentsRef tocRef)
				CollectSource(tocRef, set);
			else
				foreach (var child in entry.Children)
					CollectSource(child, set);
		}
		return set.ToImmutableHashSet();
	}

	private static void CollectSource(SiteTableOfContentsRef tocRef, HashSet<Uri> set)
	{
		_ = set.Add(tocRef.Source);
		foreach (var child in tocRef.Children)
			CollectSource(child, set);
	}

	/// <summary>Every <c>path_prefix</c> the navigation mounts at, as a <c>scheme://prefix/</c> URI.</summary>
	public static ImmutableHashSet<Uri> GetAllPathPrefixes(SiteNavigationDefinition siteNavigation)
	{
		var set = new HashSet<Uri>();
		foreach (var entry in siteNavigation.TableOfContents)
		{
			if (entry is SiteTableOfContentsRef tocRef)
				CollectPathPrefixes(tocRef, set);
			else
				foreach (var child in entry.Children)
					CollectPathPrefixes(child, set);
		}
		return set.ToImmutableHashSet();
	}

	private static void CollectPathPrefixes(SiteTableOfContentsRef tocRef, HashSet<Uri> set)
	{
		if (!string.IsNullOrEmpty(tocRef.PathPrefix))
		{
			var pathUri = new Uri($"{tocRef.Source.Scheme}://{tocRef.PathPrefix.TrimEnd('/')}/");
			_ = set.Add(pathUri);
		}

		foreach (var child in tocRef.Children)
			CollectPathPrefixes(child, set);
	}

	/// <summary>The phantom sources; a bare path is a table of contents in the narrative repository.</summary>
	public static ImmutableHashSet<Uri> GetPhantomPrefixes(SiteNavigationDefinition siteNavigation)
	{
		var set = new HashSet<Uri>();

		foreach (var phantom in siteNavigation.Phantoms)
		{
			var source = phantom.Source;
			if (!source.Contains("://"))
				source = ContentSourceMoniker.CreateString(NarrativeRepository.RepositoryName, source);

			_ = set.Add(new Uri(source));
		}

		return set.ToImmutableHashSet();
	}
}

/// <summary>A top-level <c>navigation.yml</c> entry: a mounted table of contents or a section grouping several.</summary>
public readonly union SiteNavigationEntry(SiteTableOfContentsRef, SiteSectionRef)
{
	/// <summary>The tables of contents directly beneath this entry.</summary>
	public IReadOnlyList<SiteTableOfContentsRef> Children => Value switch
	{
		SiteTableOfContentsRef toc => toc.Children,
		SiteSectionRef section => section.Children,
		_ => []
	};
}

public static class SiteNavigationEntryExtensions
{
	/// <summary>
	/// The entries whose case value is a <typeparamref name="T"/>. Use this, not <c>OfType</c>: a union is a struct
	/// wrapping its case, so <c>OfType</c> over unions is always empty.
	/// </summary>
	public static IEnumerable<T> OfCase<T>(this IEnumerable<SiteNavigationEntry> entries) => entries.Select(e => e.Value).OfType<T>();
}

/// <summary>A source known to the site but deliberately not mounted in its navigation.</summary>
public sealed record PhantomRegistration
{
	public string Source { get; init; } = null!;
}

public record NavigationTocMapping
{
	public required Uri Source { get; init; }
	public required string SourcePathPrefix { get; init; }
}

/// <summary>A link entry within a <c>dropdown:</c> section.</summary>
public record SiteDropdownLinkRef(string Title, string Url);

[Equatable]
public partial record SiteSectionRef(
	string Title,
	string? ExternalUrl,
	[property: OrderedEquality] IReadOnlyList<SiteTableOfContentsRef> Children,
	[property: OrderedEquality] IReadOnlyList<SiteDropdownLinkRef> DropdownLinks
)
{
	public bool IsExternal => ExternalUrl is not null;

	/// <summary>True when the section carries a dropdown list instead of tree children.</summary>
	public bool IsDropdown => DropdownLinks.Count > 0;
}

/// <param name="Island">
/// When <c>true</c>, the resolved navigation node is marked as an island from the assembler side.
/// OR-ed with any <c>island: true</c> the content set already declares — can only enable, never disable.
/// </param>
/// <param name="NavigationTitle">
/// Optional assembler-side label for this TOC root. When set, replaces the index page title
/// in the assembled navigation (dropdowns, back-links, sidebar root row). Does not change the page H1.
/// </param>
[Equatable]
public partial record SiteTableOfContentsRef(
	Uri Source,
	string PathPrefix,
	[property: OrderedEquality] IReadOnlyList<SiteTableOfContentsRef> Children,
	bool Island = false,
	string? NavigationTitle = null
)
{
	// For site-level TOC refs, the Path is the path prefix (where it will be mounted in the site)
	public string PathRelativeToDocumentationSet => PathPrefix;

	// For site-level TOC refs, PathRelativeToContainer is the same as PathRelativeToDocumentationSet
	// since they're all defined in the same navigation.yml file
	public string PathRelativeToContainer => PathPrefix;

	// For site-level TOC refs, the Context is the navigation.yml file path
	// This will be set during site navigation loading
	public string Context { get; init; } = "";
}
