// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Links;
using Elastic.Documentation.Publishing;
using Nullean.Entities.CsCheck;
using Nullean.Entities.CsCheck.FileSystem;

namespace Elastic.Documentation.Tests.Domain;

public class LinkGraphConstraintsTests
{
	[Test]
	[MethodDataSource(nameof(Axes))]
	public void EveryDeclaredAxisHolds(AxisCase axis) => axis.Run();

	public static IEnumerable<Func<AxisCase>> Axes() =>
		DomainAutoTest.For(LinkGraphConstraints.All, Generated()).Options(o => o.UseFileSystem()).CounterExamples(CounterExamples).Axes();

	/// <summary>
	/// A graph in which every repository the manifest links to is fetched (a target of its own, filed under its own name), in the registry, declared under cross_links and mounted in the
	/// navigation, and every cross-link names a page of that target with an anchor it has, or a redirect of it that maps the anchor. The targets keep their generated pages.
	/// </summary>
	private static LinkGraph Resolved(LinkGraph g)
	{
		var keys = Enumerable.Range(0, g.Targets.Count).Select(i => $"repo{i}").ToList();
		var targets = keys.Zip(g.Targets.Values, (key, t) => (Key: key, Target: t)).ToDictionary(
			x => x.Key,
			x =>
				x.Target with
				{
					Origin = x.Target.Origin with { RepositoryName = x.Key },
					Links = new Dictionary<string, LinkMetadata>(x.Target.Links) { ["reference/page.md"] = new() { Anchors = ["top"] } },
					Redirects = new Dictionary<string, LinkRedirect>(x.Target.Redirects ?? [])
					{
						["reference/old.md"] = new()
						{
							To = "reference/page.md",
							Anchors = new Dictionary<string, string?> { ["a"] = "top" }
						}
					}
				}
		);
		var navigation = SiteNavigationConstraintsTests.Valid(g.Navigation);
		return g with
		{
			Manifest = g.Manifest with
			{
				CrossLinks = [.. keys.SelectMany(k => new[] { $"{k}://reference/page.md#top", $"{k}://reference/old.md#a" })]
			},
			Registry = LinkExamples.Registry(g.Registry, keys),
			Targets = targets,
			Docset = g.Docset with { CrossLinkRepositories = keys },
			Navigation = navigation with
			{
				TableOfContents =
				[
					.. navigation.TableOfContents,
					.. keys.Select(
						k => new SiteNavigationEntry(new SiteTableOfContentsRef(new Uri($"{k}://reference"), $"reference/{k}", []))
					)
				]
			}
		};
	}

	private static Gen<LinkGraph> Generated() =>
		DomainGen.Of<LinkGraph>(
			o => o
				.UseFileSystem()
				.Normalize<LinkManifest>(LinkExamples.Manifest)
				.Normalize<LinkRegistry>(r => LinkExamples.Registry(r))
				.Normalize<LinkGraph>(Resolved)
		);

	[Test]
	public void ValidGraph_BreaksNoRule() => LinkGraphConstraints.All.Validate(Valid()).Should().BeEmpty();

	[Test]
	public void AMissingAnchor_NamesTheLinkAndTheAnchor() =>
		LinkGraphConstraints
			.All
			.Validate(Valid() with { Manifest = Manifest("kibana://reference/page.md#nope") })
			.Should()
			.ContainSingle()
			.Which
			.Message
			.Should()
			.Contain("kibana://reference/page.md#nope")
			.And
			.Contain("#nope");

	[Test]
	public void ARedirectToAnUnfetchedRepository_IsReportedForTheCrossLinkThatHitsIt()
	{
		var target = Target("kibana") with
		{
			Redirects = new Dictionary<string, LinkRedirect> { ["reference/moved.md"] = new() { To = "beats://reference/page.md" } }
		};
		var graph = Valid() with
		{
			Manifest = Manifest("kibana://reference/moved.md"),
			Targets = new Dictionary<string, LinkManifest> { ["kibana"] = target }
		};
		LinkGraphConstraints.All.Validate(graph).Should().ContainSingle().Which.Message.Should().Contain("no links.json was found");
	}

	[Test]
	public void TheIsolatedBuildAsksOnlyTheRulesNoOneElseReports()
	{
		var asked = LinkGraphIsolatedConstraints.All.AsConstraints().Select(r => r.Name).ToList();

		// the resolver reports these at each link, with the rules' ids; the navigation is the assembler's
		asked.Should().NotContain([
			LinkGraphConstraints.CrossLinkSchemeDeclared.Name,
			LinkGraphConstraints.CrossLinkResolves.Name,
			LinkGraphConstraints.CrossLinkRedirectResolves.Name,
			LinkGraphConstraints.CrossLinkRepositoryIsMounted.Name
		]);
		asked.Should().Contain([
			LinkGraphConstraints.CrossLinkRepositoryInRegistry.Name,
			LinkGraphConstraints.CrossLinkDeclarationUsed.Name,
			LinkGraphConstraints.RedirectTargetExists.Name,
			LinkGraphConstraints.CrossLinkTargetNotHidden.Name,
			LinkGraphConstraints.ManifestOriginMatchesRegistryKey.Name
		]);
	}

	[Test]
	public void AnUnusedCrossLinksDeclaration_IsReportedOnceByTheIsolatedBuild()
	{
		var graph = Valid() with { Docset = Docset("kibana", "never-linked") };

		var findings = LinkGraphIsolatedConstraints.All.Validate(graph);

		findings.Should().ContainSingle().Which.ConstraintId.Should().Be(LinkGraphConstraints.CrossLinkDeclarationUsed.Name);
	}

	[Test]
	public void AnUnknownLinkTarget_IsLeftToTheResolverInTheIsolatedBuild()
	{
		// a link to a page the target does not have is M30: reported by the resolver at the link, so not by the graph run of the isolated build
		var graph = WithLink(Valid(), "kibana://reference/missing.md");

		LinkGraphConstraints.All.Validate(graph).Should().Contain(v => v.ConstraintId == LinkGraphConstraints.CrossLinkResolves.Name);
		LinkGraphIsolatedConstraints.All.Validate(graph).Should().BeEmpty();
	}

	private static GitCheckoutInformation Origin(string repository) =>
		new() { Branch = "main", Remote = "https://example.test/repo.git", Ref = "abc", RepositoryName = repository };

	private static LinkManifest Manifest(params string[] crossLinks) =>
		new()
		{
			Origin = Origin("docs-content"),
			UrlPathPrefix = "/docs",
			Links = new Dictionary<string, LinkMetadata> { ["index.md"] = new() },
			CrossLinks = crossLinks
		};

	private static LinkManifest Target(string repository) =>
		new()
		{
			Origin = Origin(repository),
			UrlPathPrefix = "",
			Links = new Dictionary<string, LinkMetadata> { ["reference/page.md"] = new() { Anchors = ["top"] } },
			CrossLinks = [],
			Redirects = new Dictionary<string, LinkRedirect>
			{
				["reference/old.md"] = new() { To = "reference/page.md", Anchors = new Dictionary<string, string?> { ["a"] = "top" } }
			}
		};

	private static LinkRegistry Registry(params string[] repositories) =>
		new()
		{
			Repositories = repositories.ToDictionary(
				r => r,
				r => new Dictionary<string, LinkRegistryEntry>
				{
					["main"] = new() { Repository = r, Branch = "main", Path = $"elastic/{r}/main/links.json", ETag = "etag" }
				}
			)
		};

	private static ResolvedDocumentationSet Docset(params string[] declared)
	{
		var fs = new MockFileSystem(new Dictionary<string, MockFileData> { ["/docs/docset.yml"] = new("project: test") });
		return new ResolvedDocumentationSet
		{
			SourceFile = fs.FileInfo.New("/docs/docset.yml"),
			ScopeDirectory = fs.DirectoryInfo.New("/docs"),
			CrossLinkRepositories = declared
		};
	}

	private static SiteNavigationDefinition Navigation(params string[] sources) =>
		new()
		{
			TableOfContents = [.. sources.Select(s => new SiteNavigationEntry(new SiteTableOfContentsRef(new Uri(s), "reference", [])))]
		};

	private static LinkGraph Valid() =>
		new(
			Manifest("kibana://reference/page.md#top", "kibana://reference/old.md#a"),
			Registry("docs-content", "kibana"),
			new Dictionary<string, LinkManifest> { ["kibana"] = Target("kibana") },
			Docset("kibana"),
			Navigation("kibana://reference", "docs-content://get-started")
		);

	// A repository the graph fetches, declares, registers, mounts and links to correctly; each case then breaks one thing about it (or leaves one thing out).
	private static LinkGraph Fetched(
		LinkGraph g,
		string name,
		bool registered = true,
		bool declared = true,
		bool mounted = true,
		Func<LinkManifest, LinkManifest>? change = null
	) =>
		g with
		{
			Manifest = g.Manifest with
			{
				CrossLinks = [.. g.Manifest.CrossLinks, $"{name}://reference/page.md#top", $"{name}://reference/old.md#a"]
			},
			Registry = registered ? LinkExamples.Registry(g.Registry, [name]) : g.Registry,
			Targets = new Dictionary<string, LinkManifest>(g.Targets) { [name] = (change ?? (m => m))(Target(name)) },
			Docset = declared ? g.Docset with { CrossLinkRepositories = [.. g.Docset.CrossLinkRepositories, name] } : g.Docset,
			Navigation = mounted
				? g.Navigation with
				{
					TableOfContents =
					[
						.. g.Navigation.TableOfContents,
						new SiteNavigationEntry(new SiteTableOfContentsRef(new Uri($"{name}://reference"), $"reference/{name}", []))
					]
				}
				: g.Navigation
		};

	private static LinkGraph WithLink(LinkGraph g, string link) =>
		g with { Manifest = g.Manifest with { CrossLinks = [.. g.Manifest.CrossLinks, link] } };

	private static IReadOnlyDictionary<string, Func<LinkGraph, LinkGraph>> CounterExamples
	{
		get;
	} = new Dictionary<string, Func<LinkGraph, LinkGraph>>
	{
		["cross-link-redirect-resolves"] = g => WithLink(Fetched(g, "beats"), "beats://reference/old.md#zzz"),
		["redirect-target-exists"] =
			g =>
				Fetched(g, "beats") with
				{
					Manifest = g.Manifest with
					{
						CrossLinks = [.. g.Manifest.CrossLinks, "beats://reference/page.md#top", "beats://reference/old.md#a"],
						Redirects = new Dictionary<string, LinkRedirect>(g.Manifest.Redirects ?? [])
						{
							["gone.md"] = new() { To = "beats://reference/missing.md" }
						}
					}
				},
	};
}
