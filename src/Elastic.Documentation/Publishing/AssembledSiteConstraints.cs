// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Links;
using Generator.Equals;
using Nullean.Entities;

namespace Elastic.Documentation.Publishing;

/// <summary>One repository of an assembled site: its name, where it publishes, and its resolved documentation set.</summary>
/// <param name="Name">The repository name, the scheme of every table of contents it builds.</param>
/// <param name="UrlPathPrefix">The path prefix the repository publishes under, as the navigation mounts it (no leading or trailing slash needed).</param>
/// <param name="Docset">The repository's resolved <c>docset.yml</c>.</param>
public sealed record AssembledRepository(string Name, string UrlPathPrefix, DocumentationSetDefinition Docset)
{
	/// <summary>
	/// The identifier of every table of contents this repository builds: <c>name://</c> for the documentation set and
	/// <c>name://path</c> for each nested <c>toc.yml</c>, the form the site navigation keys its nodes by.
	/// </summary>
	public IEnumerable<AssembledToc> BuiltTocs() =>
		[
			new AssembledToc(Name, $"{Name}://", IsRoot: true),
			.. Docset
				.TableOfContents
				.Descendants()
				.OfCase<IsolatedTableOfContentsRef>()
				.Select(t => new AssembledToc(Name, $"{Name}://{t.PathRelativeToDocumentationSet.Value.TrimEnd('/')}", IsRoot: false))
		];
}

/// <summary>A table of contents a repository builds, by the identifier the site navigation knows it as.</summary>
public sealed record AssembledToc(string Repository, string Identifier, bool IsRoot);

/// <summary>
/// The assembled site: every repository the assembler builds, with the navigation that places them. The aggregate a rule that spans
/// repositories (an identifier, an API key or a redirect source that must be unique across all of them) is written against.
/// </summary>
[Equatable]
public sealed partial record AssembledSite(
	[property: OrderedEquality] IReadOnlyList<AssembledRepository> Repositories,
	SiteNavigationDefinition Navigation
);

/// <summary>Every rule that spans the repositories of an <see cref="AssembledSite"/>, in the order they are reported.</summary>
[Constraints]
public static partial class AssembledSiteConstraints
{
	public static Constraint<AssembledSite> TocIdentifiersUnique { get; } = Constraints<AssembledSite>.Unique(
		s => s.Repositories.SelectMany(r => r.BuiltTocs()),
		t => t.Identifier,
		StringComparer.OrdinalIgnoreCase,
		describeGroup: (identifier, tocs) =>
			$"Duplicate navigation identifier: {identifier} is built {tocs.Count} times ({string.Join(", ", tocs.Select(t => t.Repository))})."
	).Describe("G59: A table of contents identifier is built by one repository only; the assembled navigation keys its nodes by it.");

	public static Constraint<AssembledSite> NestedTocDeclared { get; } = Constraints<AssembledSite>
		.Each(
			s => s.Repositories.SelectMany(r => r.BuiltTocs()).Where(t => !t.IsRoot),
			(site, toc) => IsPlaced(site, toc) ? null : $"Navigation does not explicitly declare: {toc.Identifier}"
		)
		.WithSeverity(Severity.Warning)
		.Describe("G63: A nested table of contents of a documentation set is mounted in navigation.yml or registered there as a phantom.");

	public static Constraint<AssembledSite> BuiltTocPlaced { get; } = Constraints<AssembledSite>
		.Each(
			s => s.Repositories.SelectMany(r => r.BuiltTocs()).Where(t => t.IsRoot),
			(site, toc) => IsPlaced(site, toc) ? null : $"Navigation does not explicitly declare: {toc.Identifier} as a phantom"
		)
		.WithSeverity(Severity.Hint)
		.Describe("G64: A documentation set that is built but not mounted in navigation.yml is registered there as a phantom.");

	public static Constraint<AssembledSite> ApiKeyUnique { get; } = Constraints<AssembledSite>.Unique(
		s => s.Repositories.SelectMany(r => r.Docset.Api.Select(api => (Repository: r.Name, Api: api))),
		a => a.Api.ProductKey,
		StringComparer.OrdinalIgnoreCase,
		describeGroup: (key, owners) => $"Duplicate API key '{key}' declared in {string.Join(" and ", owners.Select(o => o.Repository))}"
	).Describe("P12: An api key is declared by one repository only; the duplicate is still rendered and the catalog uses the first owner.");

	public static Constraint<AssembledSite> RedirectSourceUnique { get; } = Constraints<AssembledSite>
		.Unique(
			s => s.Repositories.SelectMany(
				r => (r.Docset.Redirects ?? new Dictionary<string, LinkRedirect>()).Keys.Select(
					key => (Repository: r.Name, Source: PublishedSource(r, key))
				)
			),
			r => r.Source,
			StringComparer.OrdinalIgnoreCase,
			describeGroup: (source, owners) =>
				$"Redirect source '{source}' is declared in {string.Join(" and ", owners.Select(o => o.Repository))}; only the last is kept."
		)
		.WithSeverity(Severity.Warning)
		.Describe("P24: A redirect source, as published, is declared once across repositories; the last one wins by dictionary order.");

	private static string PublishedSource(AssembledRepository repository, string source) =>
		$"{repository.UrlPathPrefix.Trim('/')}/{source.Trim('/')}".Trim('/');

	private static string Normalize(string source) => source.EndsWith("://", StringComparison.Ordinal) ? source : source.TrimEnd('/');

	private static bool IsPlaced(AssembledSite site, AssembledToc toc)
	{
		var identifier = Normalize(toc.Identifier);
		return SiteNavigationConstraints.AllMounts(site.Navigation).Any(
			m => string.Equals(Normalize(SiteNavigationConstraints.NormalizedSource(m)), identifier, StringComparison.OrdinalIgnoreCase)
		)
			|| site
				.Navigation
				.Phantoms
				.Any(
					p => string.Equals(
						Normalize(SiteNavigationConstraints.PhantomSource(p)),
						identifier,
						StringComparison.OrdinalIgnoreCase
					)
				);
	}
}
