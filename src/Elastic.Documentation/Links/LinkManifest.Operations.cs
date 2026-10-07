// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;
using Generator.Equals;

namespace Elastic.Documentation.Links;

/// <summary>A page a build publishes to <c>links.json</c>, as the build knows it.</summary>
/// <param name="Path">The page's path relative to the documentation set, with <c>/</c> separators.</param>
/// <param name="Anchors">The anchors the page declares; an empty list is written as no anchors list.</param>
/// <param name="Hidden">Whether the page is excluded from indexing.</param>
/// <param name="DuplicateAnchors">Anchors the page declared more than once.</param>
[Equatable]
public sealed partial record LinkPage(
	string Path,
	[property: OrderedEquality] IReadOnlyList<string> Anchors,
	bool Hidden,
	[property: OrderedEquality] IReadOnlyList<string> DuplicateAnchors
);

public partial record LinkManifest
{
	/// <summary>
	/// The manifest a build publishes: one entry per page path (the first of a repeated path wins, in path order), the cross-links it makes
	/// (distinct, in order) and its redirects (keyed ignoring case).
	/// </summary>
	/// <remarks>
	/// Built from facts that were never in a file, so the result carries every finding of <see cref="LinkManifestConstraints.All"/>. The value
	/// rides along when an error is broken. A page without anchors is written without an anchors list, which the rule that every page has one
	/// reports; the wire format other builds read is pinned, so that stays a finding and not a change of the output.
	/// </remarks>
	public static Result<LinkManifest> From(
		GitCheckoutInformation origin,
		string? urlPathPrefix,
		IEnumerable<LinkPage> pages,
		IEnumerable<string> crossLinks,
		IReadOnlyDictionary<string, LinkRedirect>? redirects
	)
	{
		var links = pages
			.DistinctBy(p => p.Path)
			.OrderBy(p => p.Path)
			.ToDictionary(
				p => p.Path,
				p => new LinkMetadata { Anchors = [.. p.Anchors], Hidden = p.Hidden, DuplicateAnchors = p.DuplicateAnchors }
			);
		var manifest = new LinkManifest
		{
			Redirects = redirects is null ? null : new Dictionary<string, LinkRedirect>(redirects, StringComparer.OrdinalIgnoreCase),
			UrlPathPrefix = urlPathPrefix,
			Origin = origin,
			Links = links,
			CrossLinks = [.. crossLinks.ToHashSet().OrderBy(l => l)]
		};
		return DomainOperation.Built(LinkManifestConstraints.All, manifest);
	}

	/// <summary>
	/// The manifest of a repository that has none: no pages, no cross-links, no prefix, originating from <c>refs/heads/main</c> of
	/// <paramref name="repository"/>. A stand-in for a repository whose links could not be fetched, so a resolver can answer "no such page" for
	/// it instead of "no such repository".
	/// </summary>
	public static Result<LinkManifest> Empty(string repository) =>
		DomainOperation.Built(
			LinkManifestConstraints.All,
			new LinkManifest
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
			}
		);
}
