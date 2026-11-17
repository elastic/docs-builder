// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Generator.Equals;

namespace Elastic.Documentation.Links;

/// <summary>A page a build publishes to <c>links.json</c>, as the build knows it.</summary>
/// <param name="Path">The page's path relative to the documentation set, with <c>/</c> separators.</param>
/// <param name="Anchors">The anchors the page declares; an empty list is written as no anchors list.</param>
/// <param name="Hidden">Whether the page is excluded from indexing.</param>
[Equatable]
public sealed partial record LinkPage(string Path, [property: OrderedEquality] IReadOnlyList<string> Anchors, bool Hidden);

public sealed partial record LinkManifest
{
	/// <summary>
	/// The manifest a build publishes: one entry per page path (the first of a repeated path wins, in path order), the cross-links it makes
	/// (distinct, in order) and its redirects as given. A page without anchors is written without an anchors list.
	/// </summary>
	public static LinkManifest From(
		GitCheckoutInformation origin,
		string? urlPathPrefix,
		IEnumerable<LinkPage> pages,
		IEnumerable<string> crossLinks,
		Dictionary<string, LinkRedirect>? redirects,
		Dictionary<string, FloatingLinkSection>? floating = null
	)
	{
		var links = pages
			.DistinctBy(p => p.Path)
			.OrderBy(p => p.Path)
			.ToDictionary(p => p.Path, p => new LinkMetadata { Anchors = p.Anchors.Count == 0 ? null : [.. p.Anchors], Hidden = p.Hidden });
		return new LinkManifest
		{
			Redirects = redirects,
			UrlPathPrefix = urlPathPrefix,
			Origin = origin,
			Links = links,
			CrossLinks = [.. crossLinks.ToHashSet().OrderBy(l => l)],
			Floating = floating
		};
	}

	/// <summary>
	/// The manifest of a repository that has none: no pages, no cross-links, no prefix, originating from <c>refs/heads/main</c> of
	/// <paramref name="repository"/>. A stand-in for a repository whose links could not be fetched, so a resolver can answer "no such page" for
	/// it instead of "no such repository".
	/// </summary>
	public static LinkManifest Empty(string repository) =>
		new()
		{
			Links = [],
			Origin = new GitCheckoutInformation
			{
				Branch = "main",
				RepositoryName = repository,
				Remote = "origin",
				Ref = "refs/heads/main"
			},
			UrlPathPrefix = "",
			CrossLinks = []
		};
}
