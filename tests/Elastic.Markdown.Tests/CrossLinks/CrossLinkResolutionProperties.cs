// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Text;
using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Links;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Tests.Domain;

namespace Elastic.Markdown.Tests.CrossLinks;

/// <summary>
/// Service-algorithm laws of cross-link resolution through redirects (<see cref="CrossLinkResolver.TryResolve(Action{string}, FetchedCrossLinks, IUriEnvironmentResolver, Uri, out Uri?)"/>),
/// over generated link manifests whose redirects include chains, cycles, self redirects, anchors, 'many' rules and targets in other repositories. A fixed seed, 200 iterations.
/// </summary>
public class CrossLinkResolutionProperties
{
	private static readonly string[] Pages = ["a.md", "b.md", "c/index.md", "d.md"];
	private static readonly string[] Sources = ["a.md", "b.md", "c/index.md", "old1.md", "old2.md", "old3.md"];
	private static readonly string[] Repositories = ["r1", "r2"];

	/// <summary>One redirect as written: the target (null for an anchors-only redirect), an anchors map, and 'many' rules.</summary>
	private sealed record Redirect(string Source, string? To, string? Anchors, (string To, string Anchors)[] Many);

	private static Gen<string?> Targets { get; } = Gen.OneOfConst<string?>(
		"a.md",
		"b.md",
		"c/index.md",
		"d.md",
		"old1.md",
		"old2.md",
		"old3.md",
		"r2://a.md",
		"r2://missing.md",
		"r9://a.md",
		"https://example.com/x",
		null
	);

	private static Gen<string?> AnchorMaps { get; } = Gen.OneOfConst<string?>(null, "{}", """{ "x": "y" }""", """{ "!": "!" }""");

	private static Gen<Redirect> Redirects { get; } = Gen.Select(
		Gen.OneOfConst(Sources),
		Targets,
		AnchorMaps,
		Gen.Select(Gen.OneOfConst("a.md", "d.md", "r2://a.md"), Gen.OneOfConst("""{ "x": "y" }""", """{ "!": "!" }""")).Array[0, 2],
		(source, to, anchors, many) => new Redirect(source, to, anchors, many)
	);

	private static Gen<Redirect[]> RedirectLists { get; } = Redirects.Array[0, 8].Select(a => a.DistinctBy(r => r.Source).ToArray());

	private static string ManifestJson(IEnumerable<Redirect> redirects)
	{
		var pages = string.Join(",", Pages.Select(p => $$"""  "{{p}}": { "anchors": ["x", "y"] }"""));
		var rules = string.Join(",", redirects.Select(r =>
		{
			var parts = new List<string>();
			if (r.To is not null)
				parts.Add($"\"to\": \"{r.To}\"");
			if (r.Anchors is not null)
				parts.Add($"\"anchors\": {r.Anchors}");
			if (r.Many.Length > 0)
				parts.Add($"\"many\": [{string.Join(",", r.Many.Select(m => $"{{ \"to\": \"{m.To}\", \"anchors\": {m.Anchors} }}"))}]");
			return $"\"{r.Source}\": {{ {string.Join(",", parts)} }}";
		}));
		var sb = new StringBuilder();
		_ = sb.Append(
			"""{ "content_source": "current", "origin": { "branch": "main", "remote": "https://github.com/elastic/r", "ref": "76aac68d066e2af935c38bca8ce04d3ee67a8dd9" }, "url_path_prefix": "/elastic/r/tree/main", "cross_links": [], "links": {"""
		);
		_ = sb.Append(pages).Append("}, \"redirects\": {").Append(rules).Append("} }");
		return sb.ToString();
	}

	private static FetchedCrossLinks Fetched(string json)
	{
		var manifest = LinkLoader.LoadManifest(json, "links.json").Value!;
		var references = Repositories.ToDictionary(r => r, _ => manifest);
		return new FetchedCrossLinks
		{
			DeclaredRepositories = [.. Repositories],
			LinkReferences = references.ToFrozenDictionary(),
			LinkIndexEntries = references.ToDictionary(
				e => e.Key,
				e => new LinkRegistryEntry
				{
					Repository = e.Key,
					Path = $"elastic/{e.Key}/links.json",
					Branch = "main",
					ETag = "etag",
					GitReference = "ref"
				}
			).ToFrozenDictionary()
		};
	}

	private static Gen<(string Scheme, string Path, string? Fragment)> Links { get; } = Gen.Select(
		Gen.OneOfConst("r1", "r2", "zz"),
		Gen.OneOfConst(Sources),
		Gen.OneOfConst<string?>(null, "x", "y", "q"),
		(s, p, f) => (s, p, f)
	);

	private static Uri LinkUri((string Scheme, string Path, string? Fragment) link) =>
		new($"{link.Scheme}://{link.Path}{(link.Fragment is null ? "" : "#" + link.Fragment)}");

	private static (bool Resolved, Uri? Uri, List<string> Errors) Resolve(FetchedCrossLinks fetched, Uri link)
	{
		var errors = new List<string>();
		var resolved = false;
		Uri? uri = null;
		var finished = Task.Run(
			() => resolved = CrossLinkResolver.TryResolve(errors.Add, fetched, new IsolatedBuildEnvironmentUriResolver(), link, out uri)
		).Wait(TimeSpan.FromSeconds(5));
		finished.Should().BeTrue($"resolving {link} terminates");
		return (resolved, uri, errors);
	}

	/// <summary>
	/// Law: resolving a cross-link terminates and never throws for any redirect graph, cycles and self redirects included; it either gives a URL
	/// and no error, or no URL and at least one error; and asking twice gives the same answer.
	/// Consequence if broken: one bad redirect in a repository hangs or crashes every build that links to it, or a failing link is silently accepted.
	/// </summary>
	[Test]
	public void Resolve_Terminates_AndIsAnswerOrError_AndDeterministic() =>
		Gen.Select(RedirectLists, Links).Check(x =>
		{
			var (redirects, link) = x;
			var fetched = Fetched(ManifestJson(redirects));
			var uri = LinkUri(link);

			var first = Resolve(fetched, uri);
			var second = Resolve(fetched, uri);

			if (first.Resolved)
			{
				first.Uri.Should().NotBeNull();
				first.Errors.Should().BeEmpty();
			}
			else
			{
				first.Uri.Should().BeNull();
				first.Errors.Should().NotBeEmpty("a link that does not resolve says why");
			}
			(second.Resolved, second.Uri, second.Errors).Should().BeEquivalentTo((first.Resolved, first.Uri, first.Errors));
		});

	/// <summary>
	/// Law: a redirect is one hop. A link to a source with a local <c>to</c> and no anchor resolves to that target's page, whether the target is a
	/// page, itself a redirect (a chain) or part of a cycle: the resolver never follows a redirect onwards, so it cannot loop.
	/// Consequence if broken: a cycle becomes an infinite loop in the resolver, or a chain resolves differently by where it is entered.
	/// </summary>
	[Test]
	public void Resolve_FollowsExactlyOneHop_ForAnyChainOrCycle() =>
		Gen.Select(RedirectLists, Gen.OneOfConst(Sources)).Check(x =>
		{
			var (redirects, source) = x;
			var redirect = redirects.FirstOrDefault(r => r.Source == source);
			if (redirect?.To is not { Length: > 0 } to || to.Contains("://", StringComparison.Ordinal))
				return;
			var fetched = Fetched(ManifestJson(redirects));
			var uri = LinkUri(("r1", source, null));

			var (resolved, resolvedUri, _) = Resolve(fetched, uri);

			resolved.Should().BeTrue();
			resolvedUri.Should().Be(new IsolatedBuildEnvironmentUriResolver().Resolve(uri, CrossLinkResolver.ToTargetUrlPath(to)));
		});

	private static bool NoChainsReports(Redirect[] redirects) =>
		LinkManifestConstraints
			.All
			.Validate(LinkLoader.LoadManifest(ManifestJson(redirects), "links.json").Value!)
			.Any(v => v.ConstraintId == LinkManifestConstraints.RedirectNoChains.Name);

	private static IEnumerable<(string Source, string Target)> Edges(Redirect[] redirects) =>
		redirects.SelectMany(
			r => new[] { r.To }
				.Concat(r.Many.Select(m => m.To))
				.OfType<string>()
				.Where(t => t.Length > 0)
				.Select(t => (r.Source, Target: t))
		);

	/// <summary>
	/// Law: <c>RedirectNoChains</c> (N30) reports exactly when a local redirect target (a <c>to</c>, or one of its 'many' rules) is itself a
	/// redirect source with a local target other than its own, so a chain or a cycle of two or more local redirects is always reported; when it is
	/// silent, one hop is the end of every local redirect, which is what the resolver does.
	/// Consequence if broken: the resolver sends a reader to an intermediate page that itself redirects, with no rule that warned the author.
	/// </summary>
	[Test]
	public void LocalChainsAndCycles_AreReportedByNoChains() =>
		RedirectLists.Check(redirects =>
		{
			var local = Edges(redirects).Where(e => !e.Target.Contains("://", StringComparison.Ordinal)).ToList();
			var sources = local.Where(e => e.Target != e.Source).Select(e => e.Source).ToHashSet();

			NoChainsReports(redirects).Should().Be(local.Any(e => e.Target != e.Source && sources.Contains(e.Target)));
		});

	/// <summary>
	/// Law: a local redirect whose target redirects onwards is a chain whatever that second redirect points at, so it is reported when the second
	/// one leaves the repository too (<c>c.md</c> to <c>a.md</c>, <c>a.md</c> to <c>r2://a.md</c>).
	/// Consequence if broken: a reader of the first link lands on a page that redirects again, and N30 says nothing.
	/// </summary>
	[Test]
	[Skip("FINDING 5c: RedirectNoChains takes its redirect sources from the local redirects only, so a local redirect onto a page that redirects to another repository is not reported")]
	public void ChainsThroughACrossRepositoryRedirect_AreReportedByNoChains() =>
		RedirectLists.Check(redirects =>
		{
			var edges = Edges(redirects).ToList();
			var sources = edges.Where(e => e.Target != e.Source).Select(e => e.Source).ToHashSet();

			NoChainsReports(redirects)
				.Should()
				.Be(
					edges.Any(
						e => !e.Target.Contains("://", StringComparison.Ordinal) && e.Target != e.Source && sources.Contains(e.Target)
					)
				);
		});
}
