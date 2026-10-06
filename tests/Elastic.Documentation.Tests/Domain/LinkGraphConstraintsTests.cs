// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Links;
using Elastic.Documentation.Publishing;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

public class LinkGraphConstraintsTests
{
	[Test]
	public void Laws() => LinkGraphConstraints.All.Laws(Valid(), CounterExamples);

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

	private static LinkGraph WithTarget(LinkGraph g, LinkManifest target) =>
		g with { Targets = new Dictionary<string, LinkManifest> { ["kibana"] = target } };

	private static IReadOnlyDictionary<string, Func<LinkGraph, LinkGraph>> CounterExamples
	{
		get;
	} = new Dictionary<string, Func<LinkGraph, LinkGraph>>
	{
		// the other repository is declared, mounted and used, so only the registry lacks it
		["cross-link-repository-in-registry"] =
			g =>
				g with
				{
					Manifest = Manifest("kibana://reference/page.md#top", "beats://reference/x.md"),
					Docset = Docset("kibana", "beats"),
					Navigation = Navigation("kibana://reference", "beats://reference")
				},
		["cross-link-scheme-declared"] =
			g =>
				g with
				{
					Manifest = Manifest("kibana://reference/page.md#top", "beats://reference/x.md"),
					Registry = Registry("docs-content", "kibana", "beats"),
					Navigation = Navigation("kibana://reference", "beats://reference")
				},
		["cross-link-declaration-used"] = g => g with { Docset = Docset("kibana", "beats") },
		["cross-link-resolves"] = g => g with { Manifest = Manifest("kibana://reference/missing.md") },
		["cross-link-redirect-resolves"] = g => g with { Manifest = Manifest("kibana://reference/old.md#zzz") },
		["redirect-target-exists"] =
			g =>
				g with
				{
					Manifest = g.Manifest with
					{
						Redirects = new Dictionary<string, LinkRedirect> { ["gone.md"] = new() { To = "kibana://reference/missing.md" } }
					}
				},
		["cross-link-target-not-hidden"] =
			g => WithTarget(
				g,
				Target("kibana") with
				{
					Links = new Dictionary<string, LinkMetadata> { ["reference/page.md"] = new() { Anchors = ["top"], Hidden = true } }
				}
			),
		["cross-link-repository-is-mounted"] = g => g with { Navigation = Navigation("docs-content://get-started") },
		["manifest-origin-matches-registry-key"] = g => WithTarget(g, Target("kibana") with { Origin = Origin("elastic") })
	};
}
