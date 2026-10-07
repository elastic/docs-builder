// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;
using Generator.Equals;

namespace Elastic.Documentation.Publishing;

/// <summary>
/// The assembled site's navigation as declared in <c>navigation.yml</c> (or <c>navigation_preview.yml</c>): where each
/// documentation set's table of contents mounts, grouped into top-level sections, plus phantom sources that are
/// known but intentionally not mounted.
/// </summary>
/// <remarks>
/// Constructed with <c>SiteNavigationLoader</c> or <see cref="SiteNavigationDefinition.WithoutRepositories"/>.
/// Members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can build one directly.
/// </remarks>
[Equatable]
public sealed partial record SiteNavigationDefinition
{
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
	/// are kept as they are, even if they belong to one of the repositories. <paramref name="appended"/> entries are mounted
	/// after the rest (a local checkout of a repository the build adds back).
	/// </summary>
	/// <remarks>
	/// Derive, then re-check: the result is checked once by <see cref="SiteNavigationConstraints.All"/> and carries every finding, positioned
	/// in <paramref name="file"/>. Filtering can break a rule the whole navigation kept (a section whose only children were private is now empty,
	/// a promoted child now shares a prefix with a sibling), so the rules run on what is returned and not on what was filtered. The value rides
	/// along when a rule of error severity is broken. With nothing to remove and nothing to append the navigation itself is checked and returned.
	/// </remarks>
	/// <param name="file">The file diagnostics are reported against.</param>
	public Result<SiteNavigationDefinition> WithoutRepositories(
		IReadOnlySet<string> repositories,
		IReadOnlyList<SiteNavigationEntry>? appended = null,
		string file = ""
	)
	{
		var derived = this;
		if (repositories.Count > 0)
			derived = this with
			{
				Phantoms = [.. Phantoms.Where(p => !IsFrom(p.Source, repositories))],
				TableOfContents = [.. TableOfContents.SelectMany(entry => Without(entry, repositories))]
			};
		if (appended is { Count: > 0 })
			derived = derived with { TableOfContents = [.. derived.TableOfContents, .. appended] };

		return DomainOperation.Built(SiteNavigationConstraints.All, derived, file: file);
	}

	private static IEnumerable<SiteNavigationEntry> Without(SiteNavigationEntry entry, IReadOnlySet<string> repositories) => entry switch
	{
		SiteTableOfContentsRef toc => Without(toc, repositories).Select(t => new SiteNavigationEntry(t)),
		SiteSectionRef section => [section with { Children = [.. section.Children.SelectMany(c => Without(c, repositories))] }],
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

	// Uri.TryCreate is not total: on .NET 10 it throws IndexOutOfRangeException for a file:// URI whose host is only a bidirectional control character (U+202E).
	// Such a prefix is not a usable URL path either way, so it is treated as one that cannot be created.
	internal static bool TryCreateUri(string text, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Uri? uri)
	{
		try
		{
			return Uri.TryCreate(text, UriKind.Absolute, out uri);
		}
		catch (IndexOutOfRangeException)
		{
			uri = null;
			return false;
		}
	}

	private static void CollectPathPrefixes(SiteTableOfContentsRef tocRef, HashSet<Uri> set)
	{
		if (!string.IsNullOrEmpty(tocRef.PathPrefix))
		{
			// A path_prefix that is not a legal URL path (a space, a stray colon) cannot be compared as one; it is skipped here rather than throwing out of the constraint that asked.
			if (TryCreateUri($"{tocRef.Source.Scheme}://{tocRef.PathPrefix.TrimEnd('/')}/", out var pathUri))
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
/// <remarks>
/// Constructed through <c>SiteNavigationDefinition</c>.
/// Members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can build one directly.
/// </remarks>
public sealed record PhantomRegistration
{
	public string Source { get; internal init; } = null!;
}

/// <summary>Where one table of contents mounts in the assembled site: its source and the path prefix it is served under.</summary>
/// <remarks>
/// Constructed with <c>SiteNavigationLoader.GetTocMappings</c>.
/// Members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can build one directly.
/// </remarks>
public record NavigationTocMapping
{
	public Uri Source { get; internal init; } = default!;
	public string SourcePathPrefix { get; internal init; } = default!;
}

/// <summary>A link entry within a <c>dropdown:</c> section.</summary>
/// <remarks>
/// Constructed through <c>SiteNavigationDefinition</c>.
/// Members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can build one directly.
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
/// Members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can build one directly.
/// </remarks>
[Equatable]
public partial record SiteSectionRef
{
	internal SiteSectionRef(
		string title,
		string? externalUrl,
		IReadOnlyList<SiteTableOfContentsRef> children,
		IReadOnlyList<SiteDropdownLinkRef> dropdownLinks
	)
	{
		Title = title;
		ExternalUrl = externalUrl;
		Children = children;
		DropdownLinks = dropdownLinks;
	}

	public string Title { get; internal init; }
	public string? ExternalUrl { get; internal init; }

	[OrderedEquality]
	public IReadOnlyList<SiteTableOfContentsRef> Children { get; internal init; }

	[OrderedEquality]
	public IReadOnlyList<SiteDropdownLinkRef> DropdownLinks { get; internal init; }

	public bool IsExternal => ExternalUrl is not null;

	/// <summary>Keys the entry wrote that navigation.yml does not read, in the order written; the loader drops them. See Constraints-Conventions.md.</summary>
	[IgnoreEquality]
	public IReadOnlyList<string> UnknownKeys { get; internal init; } = [];

	/// <summary>Where the entry is declared; diagnostics about it point here.</summary>
	[IgnoreEquality]
	public Diagnostics.SourcePosition? Position { get; internal init; }

	/// <summary>True when the section carries a dropdown list instead of tree children.</summary>
	public bool IsDropdown => DropdownLinks.Count > 0;
}

/// <summary>A table of contents mounted by <c>navigation.yml</c>.</summary>
/// <remarks>
/// Constructed through <c>SiteNavigationDefinition</c>.
/// Members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can build one directly.
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
public partial record SiteTableOfContentsRef
{
	internal SiteTableOfContentsRef(
		Uri source,
		string pathPrefix,
		IReadOnlyList<SiteTableOfContentsRef> children,
		bool island = false,
		string? navigationTitle = null
	)
	{
		Source = source;
		PathPrefix = pathPrefix;
		Children = children;
		Island = island;
		NavigationTitle = navigationTitle;
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

	/// <summary>Where the entry is declared; diagnostics about it point here.</summary>
	[IgnoreEquality]
	public Diagnostics.SourcePosition? Position { get; internal init; }

	/// <summary>Keys the entry wrote that navigation.yml does not read, in the order written (<c>pathprefix</c>, <c>repo</c>); the loader drops them. See Constraints-Conventions.md.</summary>
	[IgnoreEquality]
	public IReadOnlyList<string> UnknownKeys { get; internal init; } = [];

	/// <summary><c>island:</c> as written, or null when absent. <see cref="Island"/> is true only when this parses as the boolean true, so <c>island: yes</c> or <c>island: false</c> cannot be told from nothing.</summary>
	[IgnoreEquality]
	public string? IslandRaw { get; internal init; }
}
