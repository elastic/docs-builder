// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Publishing;
using Generator.Equals;
using Nullean.Entities;

namespace Elastic.Documentation.Links;

/// <summary>
/// The cross-links of one repository together with everything they resolve against: the link registry (the index of
/// published repositories), the manifests fetched for their targets (keyed by repository, as <c>FetchedCrossLinks</c>
/// holds them), the documentation set that declares which repositories it links to, and the site navigation that
/// mounts them. It holds no data of its own: every member is an existing aggregate.
/// </summary>
[Equatable]
public sealed partial record LinkGraph(
	LinkManifest Manifest,
	LinkRegistry Registry,
	[property: UnorderedEquality] IReadOnlyDictionary<string, LinkManifest> Targets,
	ResolvedDocumentationSet Docset,
	SiteNavigationDefinition Navigation
);

/// <summary>Every rule a <see cref="LinkGraph"/> must satisfy, in the order they are reported.</summary>
[Constraints]
public static partial class LinkGraphConstraints
{
	public static Constraint<LinkGraph> CrossLinkRepositoryInRegistry { get; } = Constraints<LinkGraph>.References(
		g => g.Manifest.CrossLinks,
		l => Scheme(l) is { } scheme ? [scheme] : [],
		g => g.Registry.Repositories.Keys,
		StringComparer.OrdinalIgnoreCase
	).Describe(
		"M29: The scheme of a cross-link is a repository in the link registry; a declared but unfetched repository is given a synthesized URL instead of an error."
	);

	public static Constraint<LinkGraph> CrossLinkSchemeDeclared { get; } = Constraints<LinkGraph>.References(
		g => g.Manifest.CrossLinks,
		l => Scheme(l) is { } scheme ? [scheme] : [],
		g => g.Docset.CrossLinkRepositories,
		StringComparer.OrdinalIgnoreCase
	).Describe("M34: The scheme of a cross-link is declared under cross_links in docset.yml.");

	public static Constraint<LinkGraph> CrossLinkDeclarationUsed { get; } = Constraints<LinkGraph>
		.EachUsed(
			g => g.Docset.CrossLinkRepositories,
			r => r,
			g => ParsedLinks(g.Manifest).Select(l => l.Uri.Scheme),
			StringComparer.OrdinalIgnoreCase,
			describe: r => $"cross_links entry '{r}' is unused: no cross-link of the manifest uses it."
		)
		.WithSeverity(Severity.Warning)
		.Describe("N24: Every repository declared under cross_links in docset.yml is the scheme of at least one cross-link.");

	public static Constraint<LinkGraph> CrossLinkResolves { get; } = Constraints<LinkGraph>.Each(
		g => ParsedLinks(g.Manifest),
		CrossLinkThatDoesNotResolve
	).Describe(
		"M30: A cross-link names a page in its target's links.json and, when it has a fragment, an anchor of that page (also the inbound check, N35)."
	);

	public static Constraint<LinkGraph> CrossLinkRedirectResolves { get; } = Constraints<LinkGraph>.Each(
		g => ParsedLinks(g.Manifest),
		RedirectHopThatDoesNotResolve
	).Describe(
		"M31: A cross-link that lands on a target's redirect has its anchor mapped by the redirect, and the redirect's repository and file exist."
	);

	public static Constraint<LinkGraph> RedirectTargetExists { get; } = Constraints<LinkGraph>.Each(
		g => LinkManifestConstraints.RedirectTargets(g.Manifest),
		RedirectTargetThatDoesNotExist
	).Describe("N29: A cross-repository redirect target names a repository whose manifest is loaded and that contains the file.");

	public static Constraint<LinkGraph> CrossLinkTargetNotHidden { get; } = Constraints<LinkGraph>
		.Each(g => ParsedLinks(g.Manifest), LinkToHiddenPage)
		.WithSeverity(Severity.Warning)
		.Describe(
			"N20: A cross-link does not point at a page its target marks hidden (LinkMetadata.Hidden, which also carries exclusion from indexing)."
		);

	public static Constraint<LinkGraph> CrossLinkRepositoryIsMounted { get; } = Constraints<LinkGraph>
		.Each(LinksOutsideTheNavigation, l => $"'{l.Link}' is not under any mount of the site navigation, so its path is used unchanged.")
		.WithSeverity(Severity.Warning)
		.Describe("N15: The source of a cross-link is mounted in the site navigation; without a mapping its path is used unchanged.");

	public static Constraint<LinkGraph> ManifestOriginMatchesRegistryKey { get; } = Constraints<LinkGraph>
		.Each(
			g => g.Targets,
			t => t.Value.Origin.RepositoryName == t.Key,
			t => $"The manifest filed under '{t.Key}' says its origin repository is '{t.Value.Origin.RepositoryName}'."
		)
		.WithSeverity(Severity.Warning)
		.Describe("N47: The origin repository of a manifest is the repository it is filed under.");

	private static string? Scheme(string link) => Uri.TryCreate(link, UriKind.Absolute, out var uri) ? uri.Scheme : null;

	private static IEnumerable<(string Link, Uri Uri)> ParsedLinks(LinkManifest manifest)
	{
		foreach (var link in manifest.CrossLinks)
		{
			if (Uri.TryCreate(link, UriKind.Absolute, out var uri))
				yield return (link, uri);
		}
	}

	private static string? CrossLinkThatDoesNotResolve(LinkGraph graph, (string Link, Uri Uri) l)
	{
		var (link, uri) = l;
		if (!graph.Targets.TryGetValue(uri.Scheme, out var target))
			return null;
		var path = LinkManifestConstraints.LookupPath(uri);
		// a redirect is checked before the page: M31 follows it
		if (target.Redirects is { } redirects && redirects.ContainsKey(path))
			return null;
		if (!target.Links.TryGetValue(path, out var page))
			return $"'{link}': '{path}' is not a valid link in the '{uri.Scheme}' cross link index.";
		return uri.Fragment.Length > 1 && (!page.Anchors.Contains(uri.Fragment.TrimStart('#'), StringComparer.OrdinalIgnoreCase))
			? $"'{link}': '{path}' has no anchor named: '{uri.Fragment}'."
			: null;
	}

	private static string? RedirectHopThatDoesNotResolve(LinkGraph graph, (string Link, Uri Uri) l)
	{
		var (link, uri) = l;
		if (!graph.Targets.TryGetValue(uri.Scheme, out var target) || target.Redirects is not { } redirects)
			return null;
		var path = LinkManifestConstraints.LookupPath(uri);
		if (!redirects.TryGetValue(path, out var rule))
			return null;

		var fragment = uri.Fragment.TrimStart('#');
		string? destination = null;
		if (fragment.Length > 0)
		{
			foreach (var sub in rule.Many ?? [])
			{
				if (string.IsNullOrEmpty(sub.To) || sub.Anchors is not { Count: > 0 } anchors)
					continue;
				if (anchors.ContainsKey("!") || anchors.ContainsKey(fragment))
				{
					destination = sub.To;
					break;
				}
			}
			if (destination is null && rule.Anchors is { Count: > 0 } top && !top.ContainsKey("!") && !top.ContainsKey(fragment))
				return $"'{link}': redirect rule for '{path}' in '{uri.Scheme}' found, but its top-level rule did not handle anchor '#{fragment}'.";
		}
		destination ??= string.IsNullOrEmpty(rule.To) ? null : rule.To;
		return destination is not null
			&& LinkManifestConstraints.IsCrossRepositoryTarget(destination, out var targetUri)
			&& MissingTarget(graph, destination, targetUri) is { } problem ? $"'{link}': {problem}" : null;
	}

	private static string? RedirectTargetThatDoesNotExist(LinkGraph graph, (string Source, string To) redirect) =>
		LinkManifestConstraints.IsCrossRepositoryTarget(redirect.To, out var targetUri)
			&& MissingTarget(graph, redirect.To, targetUri) is { } problem ? $"Redirect '{redirect.Source}': {problem}" : null;

	private static string? MissingTarget(LinkGraph graph, string to, Uri targetUri)
	{
		if (!graph.Targets.TryGetValue(targetUri.Scheme, out var target))
			return $"redirect target '{to}' points to repository '{targetUri.Scheme}' for which no links.json was found.";
		var file = $"{targetUri.Host}/{targetUri.AbsolutePath.TrimStart('/')}";
		return target.Links.ContainsKey(file)
			? null
			: $"redirect target '{to}' points to file '{file}' which was not found in repository '{targetUri.Scheme}'s links.json.";
	}

	private static string? LinkToHiddenPage(LinkGraph graph, (string Link, Uri Uri) l) =>
		graph.Targets.TryGetValue(l.Uri.Scheme, out var target)
			&& target.Links.TryGetValue(LinkManifestConstraints.LookupPath(l.Uri), out var page)
			&& page.Hidden ? $"'{l.Link}' points at a page that '{l.Uri.Scheme}' marks hidden." : null;

	private static IEnumerable<(string Link, Uri Uri)> LinksOutsideTheNavigation(LinkGraph graph)
	{
		var mounts = SiteNavigationConstraints.AllMounts(graph.Navigation).Select(toc => SourceOf(toc.Source)).ToList();
		return ParsedLinks(graph.Manifest).Where(l => !mounts.Any(m => SourceOf(l.Uri).StartsWith(m, StringComparison.Ordinal)));
	}

	// The way the publish environment resolver builds a source to compare, so a link is matched exactly as it is resolved.
	private static string SourceOf(Uri uri) => $"{uri.Scheme}://{uri.Host}/{uri.AbsolutePath.TrimStart('/')}".TrimEnd('/');
}

/// <summary>
/// The rules of <see cref="LinkGraphConstraints"/> run on the graph of one repository built on its own (a documentation set built in isolation).
/// <c>CrossLinkSchemeDeclared</c> (M34), <c>CrossLinkResolves</c> (M30) and <c>CrossLinkRedirectResolves</c> (M31) are not in it: the cross-link
/// resolver reports them while it resolves each link, at the line and column of the link, with the rules' ids (<c>CrossLinkResolver.TryResolve</c>),
/// which the graph's manifest-level violation cannot say. <c>CrossLinkRepositoryIsMounted</c> (N15) is not in it either: a single repository has no
/// site navigation; the assembler is where it can run.
/// </summary>
public static class LinkGraphIsolatedConstraints
{
	/// <summary>The checks run once, after the documentation set was built.</summary>
	public static ConstraintSet<LinkGraph> All { get; } = LinkGraphConstraints.All.Without(
		LinkGraphConstraints.CrossLinkSchemeDeclared,
		LinkGraphConstraints.CrossLinkResolves,
		LinkGraphConstraints.CrossLinkRedirectResolves,
		LinkGraphConstraints.CrossLinkRepositoryIsMounted
	);
}
