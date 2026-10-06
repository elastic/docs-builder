// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Publishing;

/// <summary>Every rule a <see cref="SiteNavigationDefinition"/> must satisfy, in the order they are reported.</summary>
[Constraints]
public static partial class SiteNavigationConstraints
{
	public static Constraint<SiteNavigationDefinition> MountHasPathPrefix { get; } = Constraints<SiteNavigationDefinition>.Each(
		AllMounts,
		MountWithoutPathPrefix,
		toc => toc.Position is { } p ? new SourceLocation(null, p.Line, p.Column, p.Column - p.EndColumn) : null
	).Describe("Every mounted table of contents has a path_prefix, except a bare 'toc: name', which is published under its name.");

	public static Constraint<SiteNavigationDefinition> PathPrefixesUnique { get; } = Constraints<SiteNavigationDefinition>.Unique(
		SiteNavigationDefinition.GetAllPathPrefixes,
		pathPrefix => $"{pathPrefix.Host}/{pathPrefix.AbsolutePath.Trim('/')}/",
		describeGroup: (_, prefixes) => $"Duplicate path prefix: {prefixes[1]} duplicate: {prefixes[0]}"
	).Describe("No two repositories publish under the same path prefix.");

	public static Constraint<SiteNavigationDefinition> PathPrefixIsAUrlPath { get; } = Constraints<SiteNavigationDefinition>.Each(
		AllMounts,
		PathPrefixThatIsNotAUrlPath,
		LocationOf
	).Describe("Every path_prefix is a legal URL path, so it can be compared and published.");

	public static Constraint<SiteNavigationDefinition> RepositoryPathPrefixesUnique { get; } = Constraints<SiteNavigationDefinition>
		.Unique(
			n => AllMounts(n).Where(toc => !string.IsNullOrEmpty(toc.PathPrefix)),
			toc => (Repository: toc.Source.Scheme, Prefix: toc.PathPrefix.Trim('/')),
			at: LocationOf,
			describeGroup: (key, mounts) =>
				$"Path prefix '{key.Prefix}' is mounted {mounts.Count} times by {key.Repository}: {string.Join(", ", mounts.Select(NormalizedSource))}"
		)
		.WithSeverity(Severity.Warning)
		.Describe("No repository mounts two tables of contents under the same path prefix.");

	public static Constraint<SiteNavigationDefinition> SectionExternalExclusive { get; } = Constraints<SiteNavigationDefinition>
		.Each(
			Sections,
			s => !(s.IsExternal && (s.Children.Count > 0 || s.IsDropdown)),
			s => $"Section '{s.Title}' is external ('{s.ExternalUrl}'), so its children and dropdown are ignored."
		)
		.WithSeverity(Severity.Warning)
		.Describe("G57: A section is external, has children, or has a dropdown; 'external' wins over the other two, which are dropped.");

	public static Constraint<SiteNavigationDefinition> SectionNotEmpty { get; } = Constraints<SiteNavigationDefinition>
		.Each(
			Sections,
			s => s.IsExternal || s.Children.Count > 0 || s.IsDropdown,
			s => $"Section '{s.Title}' has no children, dropdown or external url, so it is dropped."
		)
		.WithSeverity(Severity.Warning)
		.Describe("G57: A section has an external url, children or a dropdown; an empty section is dropped.");

	public static Constraint<SiteNavigationDefinition> DropdownLinkComplete { get; } = Constraints<SiteNavigationDefinition>
		.Each(
			n => Sections(n).SelectMany(s => s.DropdownLinks.Select(l => (Section: s, Link: l))),
			d => !string.IsNullOrWhiteSpace(d.Link.Title) && !string.IsNullOrWhiteSpace(d.Link.Url),
			d => $"A dropdown link of section '{d.Section.Title}' has no title or no url ('{d.Link.Title}', '{d.Link.Url}')."
		)
		.WithSeverity(Severity.Warning)
		.Describe("G57: Every dropdown link has a title and a url; the loader drops a link that lacks one.");

	public static Constraint<SiteNavigationDefinition> MountSourceUnique { get; } = Constraints<SiteNavigationDefinition>
		.Unique(AllMounts, NormalizedSource, at: LocationOf)
		.WithSeverity(Severity.Warning)
		.Describe("G65: A table of contents is mounted once; a second declaration of it silently overwrites the first (also G71).");

	public static Constraint<SiteNavigationDefinition> SectionTitlesUnique { get; } = Constraints<SiteNavigationDefinition>.Unique(
		n => Sections(n),
		s => s.Title,
		StringComparer.OrdinalIgnoreCase
	).Describe("N7: Top-level section titles are unique, ignoring case, because section ids derive from the title.");

	public static Constraint<SiteNavigationDefinition> DropdownLinkUrlValid { get; } = Constraints<SiteNavigationDefinition>
		.Each(
			n => Sections(n).SelectMany(s => s.DropdownLinks.Select(l => (Section: s, Link: l))),
			d => string.IsNullOrWhiteSpace(d.Link.Url) || IsSiteRelative(d.Link.Url),
			d =>
				$"Dropdown link '{d.Link.Title}' of section '{d.Section.Title}' has url '{d.Link.Url}', which is not a path relative to the site; the top navigation prefixes it with the site url."
		)
		.WithSeverity(Severity.Warning)
		.Describe("N10: A dropdown link url is a path relative to the site, because the top navigation prefixes it with the site url.");

	public static Constraint<SiteNavigationDefinition> PhantomNotMounted { get; } = Constraints<SiteNavigationDefinition>
		.Disjoint(
			n => n.Phantoms.Select(PhantomSource),
			n => AllMounts(n).Select(NormalizedSource),
			StringComparer.OrdinalIgnoreCase,
			describe: source => $"Phantom {source} is also mounted."
		)
		.WithSeverity(Severity.Warning)
		.Describe("N13: A phantom source is not also mounted; a phantom is known but deliberately absent from the navigation.");

	public static Constraint<SiteNavigationDefinition> MountSourcesSegmentPrefixFree { get; } = Constraints<SiteNavigationDefinition>
		.NoPrefixes(
			n => AllMounts(n).Select(toc => (Toc: toc, Source: NormalizedSource(toc).TrimEnd('/'))),
			mount => mount.Source,
			PrefixMatch.TextNotSegment,
			comparison: StringComparison.OrdinalIgnoreCase,
			at: mount => LocationOf(mount.Toc),
			describe: (outer, inner) =>
				$"{inner.Source}/ starts with {outer.Source}/ without a '/' boundary, so a link to it can resolve to the shorter mount."
		)
		.WithSeverity(Severity.Warning)
		.Describe(
			"N14: No mounted source of a repository is a string prefix of another without a '/' boundary, because cross-link resolution compares with a plain StartsWith."
		);

	public static Constraint<SiteNavigationDefinition> MountIsPublished { get; } = Constraints<SiteNavigationDefinition>
		.Each(
			AllMounts,
			toc => !DroppedSources.Contains(NormalizedSource(toc), StringComparer.OrdinalIgnoreCase),
			toc => $"{NormalizedSource(toc)} is mounted but dropped from the output without notice.",
			LocationOf
		)
		.WithSeverity(Severity.Warning)
		.Describe("N17: A mounted source is published; four sources are dropped from the output by a deny list in the assembler.");

	public static Constraint<SiteNavigationDefinition> EntryUnknownKeys { get; } = Constraints<SiteNavigationDefinition>
		.Each(
			UnknownKeysOfEntries,
			k => $"Navigation entry '{k.Entry}' has the key '{k.Key}', which is not read and is dropped. Check for typos.",
			k => k.Position is { } p ? new SourceLocation(null, p.Line, p.Column) : null
		)
		.WithSeverity(Severity.Warning)
		.Describe(
			"G55: A navigation entry writes only keys the loader reads (toc, path_prefix, children, island, navigation_title; section, external, children, dropdown); any other key, such as a mistyped 'pathprefix', is dropped without notice."
		);

	public static Constraint<SiteNavigationDefinition> MountRepoKey { get; } = Constraints<SiteNavigationDefinition>
		.Each(
			AllMounts,
			toc => !toc.UnknownKeys.Contains("repo"),
			toc =>
				$"Navigation entry '{NormalizedSource(toc)}' has a 'repo:' key. Only the old TOC mapping reader accepted it; the navigation parser drops it.",
			LocationOf
		)
		.WithSeverity(Severity.Warning)
		.Describe(
			"G69: An entry is a 'toc:' entry; 'repo:' is not a key of navigation.yml, so a 'repo:' (alone or beside 'toc:') is ignored (N18)."
		);

	public static Constraint<SiteNavigationDefinition> IslandIsBoolean { get; } = Constraints<SiteNavigationDefinition>
		.Each(
			AllMounts,
			toc => toc.IslandRaw is not { } raw || bool.TryParse(raw, out _),
			toc =>
				$"Navigation entry '{NormalizedSource(toc)}' has island '{toc.IslandRaw}', which is not a boolean, so it is read as false.",
			LocationOf
		)
		.WithSeverity(Severity.Warning)
		.Describe("G56: 'island' is true or false; any other text is read as false without notice.");

	/// <summary>The keys of every entry and mount that the loader drops, except the 'repo' key that has its own rule on mounts.</summary>
	private static IEnumerable<(string Entry, string Key, Diagnostics.SourcePosition? Position)> UnknownKeysOfEntries(
		SiteNavigationDefinition navigation
	) =>
		[
			.. AllMounts(navigation).SelectMany(
				toc => toc.UnknownKeys.Where(k => k != "repo").Select(k => (NormalizedSource(toc), k, toc.Position))
			),
			.. Sections(navigation).SelectMany(s => s.UnknownKeys.Select(k => (s.Title, k, s.Position)))
		];

	// GlobalNavigationPathProvider drops these without a diagnostic.
	private static string[] DroppedSources =>
		["cloud://saas/", "docs-content://serverless/", "eland://sphinx/", "elasticsearch-py://sphinx/"];

	/// <summary>Every top-level section of the navigation.</summary>
	public static IEnumerable<SiteSectionRef> Sections(SiteNavigationDefinition navigation) =>
		navigation.TableOfContents.OfCase<SiteSectionRef>();

	private static SourceLocation? LocationOf(SiteTableOfContentsRef toc) =>
		toc.Position is { } p ? new SourceLocation(null, p.Line, p.Column) : null;

	private static bool IsSiteRelative(string url) =>
		!url.Contains(':', StringComparison.Ordinal)
			&& !url.StartsWith("//", StringComparison.Ordinal)
			&& Uri.TryCreate(url, UriKind.Relative, out _);

	internal static string PhantomSource(PhantomRegistration phantom)
	{
		var source = phantom.Source.Contains("://", StringComparison.Ordinal)
			? phantom.Source
			: ContentSourceMoniker.CreateString(NarrativeRepository.RepositoryName, phantom.Source);
		return source.EndsWith("://", StringComparison.OrdinalIgnoreCase) ? source : source.TrimEnd('/') + "/";
	}

	/// <summary>A mount's source as its published URI: normalized to end in <c>/</c>, with a bare name expanded to the narrative.</summary>
	public static string NormalizedSource(SiteTableOfContentsRef toc)
	{
		var source = toc.ShorthandPath is { } shorthand
			? ContentSourceMoniker.CreateString(NarrativeRepository.RepositoryName, shorthand)
			: toc.Source.OriginalString;
		return source.EndsWith("://", StringComparison.OrdinalIgnoreCase) ? source : source.TrimEnd('/') + "/";
	}

	/// <summary>Where a mount publishes: its path_prefix, a bare name, or null when it has neither.</summary>
	public static string? DeclaredPathPrefix(SiteTableOfContentsRef toc) =>
		!string.IsNullOrEmpty(toc.PathPrefix) ? toc.PathPrefix : toc.ShorthandPath;

	private static string? MountWithoutPathPrefix(SiteTableOfContentsRef toc)
	{
		if (!string.IsNullOrEmpty(DeclaredPathPrefix(toc)))
			return null;
		var source = NormalizedSource(toc);
		if (!Uri.TryCreate(source, UriKind.Absolute, out var sourceUri))
			return null;
		var sourcePrefix = $"{sourceUri.Host}/{sourceUri.AbsolutePath.TrimStart('/')}";
		return $"Path prefix is not defined for: {source}, falling back to {sourcePrefix} which may be incorrect";
	}

	// path-prefixes-unique compares prefixes as URIs and skips one that cannot be built; this is the rule that says so.
	private static string? PathPrefixThatIsNotAUrlPath(SiteTableOfContentsRef toc) =>
		!string.IsNullOrEmpty(toc.PathPrefix)
			&& !SiteNavigationDefinition.TryCreateUri($"{toc.Source.Scheme}://{toc.PathPrefix.TrimEnd('/')}/", out _)
			? $"Path prefix '{toc.PathPrefix}' of {NormalizedSource(toc)} is not a legal URL path."
			: null;

	/// <summary>Every mount, depth-first in declaration order, including those under sections.</summary>
	public static IEnumerable<SiteTableOfContentsRef> AllMounts(SiteNavigationDefinition navigation)
	{
		foreach (var entry in navigation.TableOfContents)
		{
			if (entry.Value is SiteTableOfContentsRef toc)
			{
				foreach (var mount in WithChildren(toc))
					yield return mount;
			}
			else
			{
				foreach (var child in entry.Children)
					foreach (var mount in WithChildren(child))
						yield return mount;
			}
		}

		static IEnumerable<SiteTableOfContentsRef> WithChildren(SiteTableOfContentsRef toc) =>
			[toc, .. toc.Children.SelectMany(WithChildren)];
	}
}
