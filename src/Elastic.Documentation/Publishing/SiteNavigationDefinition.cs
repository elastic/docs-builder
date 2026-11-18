// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;
using Elastic.Documentation.DocumentationSets;
using Generator.Equals;
using Nullean.Entities;

namespace Elastic.Documentation.Publishing;

/// <summary>
/// The assembled site's navigation as declared in <c>navigation.yml</c> (or <c>navigation_preview.yml</c>): where each
/// documentation set's table of contents mounts, grouped into top-level sections, plus phantom sources that are
/// known but intentionally not mounted.
/// </summary>
/// <remarks>
/// Constructed with <c>SiteNavigationLoader</c>. Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
[Equatable]
public sealed partial record SiteNavigationDefinition : IHaveConstraints<SiteNavigationDefinition>
{
	public static ConstraintSet<SiteNavigationDefinition> Constraints => SiteNavigationConstraints.All;

	[OrderedEquality]
	public IReadOnlyList<PhantomRegistration> Phantoms { get; internal init; } = [];

	// A union compares its case value, and both cases are [Equatable]; the analyzer cannot see through the union.
#pragma warning disable GE003
	[OrderedEquality]
	public IReadOnlyList<SiteNavigationEntry> TableOfContents { get; internal init; } = [];
#pragma warning restore GE003

	/// <summary>
	/// The navigation without the tables of contents of <paramref name="repositories"/>: each such mount is dropped and
	/// its children take its place, and phantoms from those repositories are dropped too. Children promoted this way
	/// are kept as they are, even if they belong to one of the repositories.
	/// </summary>
	/// <remarks>
	/// Filtering can break a rule the whole navigation kept (a promoted child now shares a prefix with a sibling), so a caller checks what is
	/// returned: <c>SiteNavigationDefinition.Constraints.Apply(navigation, n => n.WithoutRepositories(...))</c>.
	/// </remarks>
	public SiteNavigationDefinition WithoutRepositories(IReadOnlySet<string> repositories) =>
		repositories.Count == 0
			? this
			: this with
			{
				Phantoms = [.. Phantoms.Where(p => !IsFrom(p.Source, repositories))],
				TableOfContents = [.. TableOfContents.SelectMany(entry => Without(entry, repositories))]
			};

	/// <summary>The navigation with <paramref name="entries"/> mounted after the rest (a local checkout of a repository the build adds back).</summary>
	public SiteNavigationDefinition WithAppended(IReadOnlyList<SiteNavigationEntry> entries) =>
		entries.Count == 0 ? this : this with { TableOfContents = [.. TableOfContents, .. entries] };

	private static IEnumerable<SiteNavigationEntry> Without(
		SiteNavigationEntry entry,
		IReadOnlySet<string> repositories
	) => entry.Value switch
	{
		SiteTableOfContentsRef toc => Without(toc, repositories).Select(t => new SiteNavigationEntry(t)),
		SiteSectionRef section =>
			[new SiteNavigationEntry(section with { Children = [.. section.Children.SelectMany(c => Without(c, repositories))] })],
		_ => []
	};

	private static IEnumerable<SiteTableOfContentsRef> Without(SiteTableOfContentsRef toc, IReadOnlySet<string> repositories) =>
		repositories.Contains(toc.Source.Scheme)
			? toc.Children
			: [toc with { Children = [.. toc.Children.SelectMany(c => Without(c, repositories))] }];

	private static bool IsFrom(string source, IReadOnlySet<string> repositories)
	{
		var separator = source.IndexOf(':');
		return separator > 0 && repositories.Contains(source[..separator]);
	}

	/// <summary>Every table of contents the navigation mounts, at any depth.</summary>
	public static ImmutableHashSet<Uri> GetAllDeclaredSources(SiteNavigationDefinition siteNavigation)
	{
		var set = new HashSet<Uri>();
		foreach (var mount in SiteNavigationConstraints.AllMounts(siteNavigation))
			_ = set.Add(mount.Source);
		return set.ToImmutableHashSet();
	}

	/// <summary>Every <c>path_prefix</c> the navigation mounts at, as a <c>scheme://prefix/</c> URI.</summary>
	public static ImmutableHashSet<Uri> GetAllPathPrefixes(SiteNavigationDefinition siteNavigation)
	{
		var set = new HashSet<Uri>();
		foreach (var mount in SiteNavigationConstraints.AllMounts(siteNavigation))
		{
			if (!string.IsNullOrEmpty(mount.PathPrefix))
				_ = set.Add(new Uri($"{mount.Source.Scheme}://{mount.PathPrefix.TrimEnd('/')}/"));
		}
		return set.ToImmutableHashSet();
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
/// <remarks>
/// Constructed through <c>SiteNavigationDefinition</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
public sealed record PhantomRegistration
{
	public string Source { get; internal init; } = null!;
}

/// <summary>Where one table of contents mounts in the assembled site: its source and the path prefix it is served under.</summary>
/// <remarks>
/// Constructed with <c>SiteNavigationLoader.GetTocMappings</c>.
/// </remarks>
public record NavigationTocMapping
{
	public required Uri Source { get; init; }
	public required string SourcePathPrefix { get; init; }
}

/// <summary>A link entry within a <c>dropdown:</c> section.</summary>
/// <remarks>
/// Constructed through <c>SiteNavigationDefinition</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
public record SiteDropdownLinkRef
{
	internal SiteDropdownLinkRef(string title, string url)
	{
		Title = title;
		Url = url;
	}

	public string Title { get; internal init; }
	public string Url { get; internal init; }

	public void Deconstruct(out string title, out string url)
	{
		title = Title;
		url = Url;
	}
}

/// <summary>A top-level section of <c>navigation.yml</c>: a title with its children.</summary>
/// <remarks>
/// Constructed through <c>SiteNavigationDefinition</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
[Equatable]
public partial record SiteSectionRef
{
	internal SiteSectionRef(
		string title,
		string? externalUrl,
		IReadOnlyList<SiteTableOfContentsRef> children,
		IReadOnlyList<SiteDropdownLinkRef> dropdownLinks,
		string? floatingKind = null
	)
	{
		Title = title;
		ExternalUrl = externalUrl;
		Children = children;
		DropdownLinks = dropdownLinks;
		FloatingKind = floatingKind;
	}

	public string Title { get; internal init; }
	public string? ExternalUrl { get; internal init; }

	[OrderedEquality]
	public IReadOnlyList<SiteTableOfContentsRef> Children { get; internal init; }

	[OrderedEquality]
	public IReadOnlyList<SiteDropdownLinkRef> DropdownLinks { get; internal init; }

	/// <summary>
	/// <c>floating: &lt;kind&gt;</c> mounts the floating sections of that kind (for example every API) as the section's children
	/// when the build has any. Without them the section keeps its other behaviour, such as an external link.
	/// </summary>
	public string? FloatingKind { get; internal init; }

	public bool IsExternal => ExternalUrl is not null;

	/// <summary>True when the section carries a dropdown list instead of tree children.</summary>
	public bool IsDropdown => DropdownLinks.Count > 0;
}

/// <summary>A table of contents mounted by <c>navigation.yml</c>.</summary>
/// <remarks>
/// Constructed through <c>SiteNavigationDefinition</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
/// <param name="Island">
/// When <c>true</c>, the resolved navigation node is marked as an island from the assembler side.
/// OR-ed with any <c>island: true</c> the content set already declares — can only enable, never disable.
/// </param>
/// <param name="NavigationTitle">
/// Optional assembler-side label for this TOC root. When set, replaces the index page title
/// in the assembled navigation (dropdowns, back-links, sidebar root row). Does not change the page H1.
/// </param>
[Equatable]
public partial record SiteTableOfContentsRef : ITableOfContentsItem
{
	internal SiteTableOfContentsRef(
		Uri source,
		string pathPrefix,
		IReadOnlyList<SiteTableOfContentsRef> children,
		bool island = false,
		string? navigationTitle = null,
		string? shorthandPath = null
	)
	{
		Source = source;
		PathPrefix = pathPrefix;
		Children = children;
		Island = island;
		NavigationTitle = navigationTitle;
		ShorthandPath = shorthandPath;
		DeclaredPathPrefix = !string.IsNullOrEmpty(pathPrefix) ? pathPrefix : shorthandPath;
	}

	public Uri Source { get; internal init; }
	public string PathPrefix { get; internal init; }

	[OrderedEquality]
	public IReadOnlyList<SiteTableOfContentsRef> Children { get; internal init; }

	public bool Island { get; internal init; }
	public string? NavigationTitle { get; internal init; }

	// For site-level TOC refs, the Path is the path prefix (where it will be mounted in the site)
	public string PathRelativeToDocumentationSet => PathPrefix;

	// For site-level TOC refs, PathRelativeToContainer is the same as PathRelativeToDocumentationSet
	// since they're all defined in the same navigation.yml file
	public string PathRelativeToContainer => PathPrefix;

	// For site-level TOC refs, the Context is the navigation.yml file path
	// This will be set during site navigation loading
	public string Context { get; internal init; } = "";

	/// <summary>
	/// The <c>toc:</c> value when written without a scheme (<c>toc: get-started</c>), which mounts a narrative
	/// table of contents and doubles as its path prefix when <c>path_prefix</c> is omitted.
	/// </summary>
	public string? ShorthandPath { get; internal init; }

	/// <summary>
	/// The path prefix <see cref="NavigationTocMapping"/> publishes the mount under, as the toc mapping reader has always read it: the key written last of
	/// <c>path_prefix</c> and a bare <c>toc:</c> name wins (so <c>path_prefix</c> written before a bare <c>toc:</c> is overridden by it), and null when there is neither.
	/// An empty <c>path_prefix:</c> stays empty. A mount built by hand declares its <c>path_prefix</c>, or else its bare <c>toc:</c> name.
	/// </summary>
	public string? DeclaredPathPrefix { get; internal init; }

	/// <summary>Where the entry's mapping starts in the file; diagnostics about it point here.</summary>
	[IgnoreEquality]
	public Diagnostics.SourcePosition? Position { get; internal init; }
}
