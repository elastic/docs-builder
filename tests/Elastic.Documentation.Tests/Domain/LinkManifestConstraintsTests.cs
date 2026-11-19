// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.Links;
using Nullean.Entities;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

public class LinkManifestConstraintsTests
{
	private static readonly string[] PagePaths = ["index.md", "setup.md", "guide/index.md", "guide/intro.md", "reference/api.md"];
	private static readonly string[] CrossLinkPool = ["kibana://reference/page.md#top", "beats://a.md", "other://b/index.md"];
	private static readonly string[] Anchors = ["intro", "usage", "limits"];

	private static readonly Gen<LinkManifest> Manifests = Gen.Select(
		Gen.Int[1, (1 << PagePaths.Length) - 1],
		Gen.Int[0, (1 << Anchors.Length) - 1],
		Gen.OneOf("", "/docs", "/elastic/example-repo"),
		Gen.Int[0, 3],
		Gen.Int[0, 3],
		Build
	);

	private static LinkManifest Build(int pageMask, int anchorMask, string prefix, int redirectCount, int crossLinkCount) =>
		new()
		{
			Origin = Origin(),
			UrlPathPrefix = prefix,
			Links = PagePaths.Where((_, i) => (pageMask & (1 << i)) != 0).ToDictionary(
				p => p,
				p => new LinkMetadata { Anchors = Anchors.Where((_, i) => (anchorMask & (1 << i)) != 0).ToArray() }
			),
			CrossLinks = CrossLinkPool[..crossLinkCount],
			// every redirect points at a page of the manifest and maps an anchor that page has, so none is a chain or a shadow
			Redirects = redirectCount == 0
				? null
				: Enumerable.Range(0, redirectCount).ToDictionary(
					i => $"old{i}.md",
					i => new LinkRedirect { To = PagePaths.First(p => (pageMask & (1 << Array.IndexOf(PagePaths, p))) != 0) }
				)
		};

	private static GitCheckoutInformation Origin() =>
		new() { Branch = "main", Remote = "https://example.test/repo.git", Ref = "abc", RepositoryName = "docs-content" };

	public static IEnumerable<Func<AxisCase>> Axes() =>
		AutoTest.For(LinkManifestConstraints.Instance, Manifests).Seed(RunSeed.Value).CounterExamples(CounterExamples).AsDataSource();

	/// <summary>
	/// Law: every rule of a published <c>links.json</c> holds for every manifest the generator builds, and every rule can be broken by some manifest.
	/// Consequence if broken: a rule that cannot fire, or data that never reaches it, reports green while checking nothing, and other repositories'
	/// builds read a manifest the rules were supposed to flag.
	/// </summary>
	[Test]
	[MethodDataSource(nameof(Axes))]
	public Task EveryDeclaredAxisHolds(AxisCase axis) => axis.RunAsync();

	[Test]
	public void ValidManifest_BreaksNoRule() => LinkManifest.Constraints.Report(Valid()).Violations.Should().BeEmpty();

	[Test]
	public void PageUrl_RemovesEveryMdAndTrailingIndex()
	{
		LinkManifestConstraints.PageUrl("foo.md").Should().Be("foo");
		LinkManifestConstraints.PageUrl("foo/index.md").Should().Be("foo");
		LinkManifestConstraints.PageUrl("index.md").Should().BeEmpty();
		LinkManifestConstraints.PageUrl("a.md.md").Should().Be("a");
	}

	[Test]
	public void CrossLinkWithAWebScheme_NamesTheValidatorsReason() =>
		LinkManifest
			.Constraints
			.Report(Valid() with { CrossLinks = ["https://example.test/x"] })
			.Violations
			.Should()
			.ContainSingle()
			.Which
			.Message
			.Should()
			.Contain("cannot use standard web/protocol schemes");

	[Test]
	public void ExactRuleAfterCatchAll_NamesTheRedirectAndAnchor() =>
		LinkManifest
			.Constraints
			.Report(CounterExamples["redirect-catch-all-after-exact"](Valid()))
			.Violations
			.Should()
			.ContainSingle()
			.Which
			.Message
			.Should()
			.Be("Redirect 'many.md' has exact anchor rule(s) (x) after a catch-all ('!') rule, which wins first, so they are never used.");

	[Test]
	public void ARedirectThatShadowsAPage_IsNamed() =>
		LinkManifest
			.Constraints
			.Report(CounterExamples["redirect-source-is-not-a-page"](Valid()))
			.Violations
			.Should()
			.ContainSingle()
			.Which
			.Message
			.Should()
			.Be("Redirect 'shadow.md' shadows an existing page of the same path; the redirect wins.");

	[Test]
	public void From_BuildsTheManifestABuildPublishes()
	{
		var manifest = LinkManifest.From(
			Origin(),
			"/docs",
			[
				new LinkPage("setup.md", ["intro", "usage"], Hidden: false),
				new LinkPage("index.md", [], Hidden: true),
				new LinkPage("setup.md", ["ignored"], Hidden: true)
			],
			["kibana://b.md", "beats://a.md", "kibana://b.md"],
			redirects: null
		);

		manifest.Links.Keys.Should().Equal("index.md", "setup.md");
		manifest.Links["setup.md"].Should().Be(new LinkMetadata { Anchors = ["intro", "usage"] });
		manifest.Links["index.md"].Should().Be(new LinkMetadata { Anchors = null, Hidden = true });
		manifest.CrossLinks.Should().Equal("beats://a.md", "kibana://b.md");
		LinkManifest.Constraints.Report(manifest).Violations.Should().BeEmpty();
	}

	[Test]
	public void Empty_IsAManifestOfARepositoryWithoutPages()
	{
		var manifest = LinkManifest.Empty("docs-content");

		manifest.Links.Should().BeEmpty();
		manifest.CrossLinks.Should().BeEmpty();
		manifest.UrlPathPrefix.Should().BeEmpty();
		manifest
			.Origin
			.Should()
			.Be(new GitCheckoutInformation { Branch = "main", RepositoryName = "docs-content", Remote = "origin", Ref = "refs/heads/main" });
		LinkManifest.Constraints.Report(manifest).Violations.Should().BeEmpty();
	}

	[Test]
	public void ValueEquality_ComparesPagesRedirectsAndOrderedLists()
	{
		var a = Valid();
		var sameContent = Valid() with { Links = Valid().Links.Reverse().ToDictionary(p => p.Key, p => p.Value) };

		a.Should().Be(sameContent, "the pages are a dictionary, so their order is not part of the manifest");
		a.Should().NotBe(a with { CrossLinks = [.. a.CrossLinks.Reverse(), "extra://x.md"] });
		a.Should().NotBe(a with { Redirects = null });
		a.Should().NotBe(a with
		{
			Links = new Dictionary<string, LinkMetadata>(a.Links) { ["setup.md"] = new LinkMetadata { Anchors = ["other"] } }
		});
	}

	// each call allocates its own section, dictionaries and arrays, so equality cannot pass by reference
	private static FloatingLinkSection Section(bool reverseKeys = false)
	{
		var versions = new[]
		{
			new FloatingLinkVersion { Name = "8", Root = "api/example/8", Current = true },
			new FloatingLinkVersion { Name = "7", Root = "api/example/7", Hidden = true }
		};
		KeyValuePair<string, ulong>[] pages = [new("", 3UL), new("op/a", 1UL), new("op/b", 2UL)];
		KeyValuePair<string, string[]>[] eight = [new("op/a", ["old-a", "older-a"]), new("op/b", ["old-b"])];
		return new FloatingLinkSection
		{
			Versions = versions,
			Pages = new Dictionary<string, ulong>(reverseKeys ? pages.Reverse() : pages),
			Aliases = new Dictionary<string, Dictionary<string, string[]>>
			{
				["8"] = new(reverseKeys ? eight.Reverse() : eight),
				["7"] = new() { ["op/a"] = ["old-a"] }
			}
		};
	}

	[Test]
	public void FloatingValueEquality_IndependentlyAllocatedSections_AreEqualWithEqualHashes()
	{
		var a = Section();
		var b = Section(reverseKeys: true);
		// populate the lookup caches of one side; they are not part of the value
		a.TryResolvePage("8", "op/a", out _).Should().BeTrue();
		a.TryResolvePage(null, "", out _).Should().BeTrue();

		a.Should().Be(b, "the pages and the alias dictionaries are unordered");
		a.GetHashCode().Should().Be(b.GetHashCode());
		WithFloating(Valid(), a).Should().Be(WithFloating(Valid(), b));
		WithFloating(Valid(), a).GetHashCode().Should().Be(WithFloating(Valid(), b).GetHashCode());
	}

	[Test]
	public void FloatingValueEquality_AnyDifference_IsNotEqual()
	{
		var section = Section();
		var differing = new Dictionary<string, FloatingLinkSection>
		{
			["a version moved"] = section with { Versions = [.. section.Versions.Reverse()] },
			["a version renamed"] = section with { Versions = [section.Versions[0] with { Name = "9" }, section.Versions[1]] },
			["a page mask"] = section with { Pages = new Dictionary<string, ulong>(section.Pages) { ["op/a"] = 2UL } },
			["a page added"] = section with { Pages = new Dictionary<string, ulong>(section.Pages) { ["op/c"] = 1UL } },
			["no aliases"] = section with { Aliases = null },
			["an alias version dropped"] = section with
			{
				Aliases = new Dictionary<string, Dictionary<string, string[]>> { ["8"] = section.Aliases!["8"] }
			},
			["an alias list reordered"] = section with
			{
				Aliases = new Dictionary<string, Dictionary<string, string[]>>(section.Aliases!)
				{
					["8"] = new() { ["op/a"] = ["older-a", "old-a"], ["op/b"] = ["old-b"] }
				}
			},
			["an alias changed"] = section with
			{
				Aliases = new Dictionary<string, Dictionary<string, string[]>>(section.Aliases!)
				{
					["8"] = new() { ["op/a"] = ["old-a", "older-a"], ["op/b"] = ["other"] }
				}
			}
		};

		foreach (var (what, other) in differing)
		{
			section.Should().NotBe(other, what);
			WithFloating(Valid(), section).Should().NotBe(WithFloating(Valid(), other), what);
		}
	}

	private static LinkManifest Valid() =>
		new()
		{
			Origin = Origin(),
			UrlPathPrefix = "/docs",
			Links = new Dictionary<string, LinkMetadata>
			{
				["index.md"] = new() { Anchors = [] },
				["setup.md"] = new() { Anchors = ["intro"] },
				["guide/index.md"] = new() { Anchors = ["y"] }
			},
			CrossLinks = ["kibana://reference/page.md#top"],
			Redirects = new Dictionary<string, LinkRedirect>
			{
				["old.md"] = new() { To = "setup.md", Anchors = new Dictionary<string, string?> { ["a"] = "intro" } },
				["moved.md"] = new() { To = "kibana://reference/page.md" }
			}
		};

	private static LinkManifest WithRedirect(LinkManifest m, string source, LinkRedirect redirect) =>
		m with { Redirects = new Dictionary<string, LinkRedirect>(m.Redirects ?? []) { [source] = redirect } };

	private static LinkManifest WithFloating(LinkManifest m, FloatingLinkSection section) =>
		m with { Floating = new Dictionary<string, FloatingLinkSection> { ["api/example"] = section } };

	private static LinkManifest WithPage(LinkManifest m, string path, LinkMetadata page) =>
		m with { Links = new Dictionary<string, LinkMetadata>(m.Links) { [path] = page } };

	// the pages the cases land on, under names the generator never gives a page
	private static LinkManifest WithTargets(LinkManifest m) =>
		WithPage(WithPage(m, "target.md", new LinkMetadata { Anchors = ["intro"] }), "landing.md", new LinkMetadata { Anchors = [] });

	private static IReadOnlyDictionary<string, Func<LinkManifest, LinkManifest>> CounterExamples
	{
		get;
	} = new Dictionary<string, Func<LinkManifest, LinkManifest>>
	{
		["link-manifest-page-paths-are-markdown"] = m => WithPage(m, "notes.txt", new LinkMetadata()),
		["link-manifest-origin-named"] = m => m with { Origin = GitCheckoutInformation.Unavailable },
		["url-path-prefix-normalized"] = m => m with { UrlPathPrefix = "docs/" },
		["link-manifest-page-urls-unique"] =
			m => WithPage(WithPage(m, "dupurl.md", new LinkMetadata()), "dupurl/index.md", new LinkMetadata()),
		["page-path-single-md-extension"] = m => WithPage(m, "a.md.md", new LinkMetadata()),
		["cross-link-scheme-allowed"] = m => m with { CrossLinks = [.. m.CrossLinks, "https://example.test/x"] },
		["redirect-source-is-not-a-page"] =
			m => WithRedirect(WithPage(WithTargets(m), "shadow.md", new LinkMetadata()), "shadow.md", new LinkRedirect { To = "target.md" }),
		["redirect-catch-all-after-exact"] =
			m => WithRedirect(
				WithTargets(m),
				"many.md",
				new LinkRedirect
				{
					Many =
					[
						new LinkSingleRedirect { To = "landing.md", Anchors = LinkRedirect.CatchAllAnchors },
						new LinkSingleRedirect { To = "target.md", Anchors = new Dictionary<string, string?> { ["x"] = "intro" } }
					]
				}
			),
		["redirect-empty-anchors-keeps-anchor"] =
			m => WithRedirect(
				WithTargets(m),
				"empty.md",
				new LinkRedirect { To = "target.md", Anchors = new Dictionary<string, string?>() }
			),
		["redirect-same-page-needs-anchors"] = m => WithRedirect(m, "same.md", new LinkRedirect()),
		["redirect-no-chains"] =
			m => WithRedirect(
				WithRedirect(WithTargets(m), "chain-a.md", new LinkRedirect { To = "chain-b.md" }),
				"chain-b.md",
				new LinkRedirect { To = "target.md" }
			),
		["redirect-target-anchors-exist"] =
			m => WithRedirect(
				WithTargets(m),
				"anch.md",
				new LinkRedirect { To = "target.md", Anchors = new Dictionary<string, string?> { ["x"] = "missing" } }
			),
		// the required members of a floating section are rules too (generated from `required`)
		["floating-versions-required"] = m => WithFloating(m, new FloatingLinkSection { Versions = null!, Pages = [] }),
		["floating-pages-required"] = m => WithFloating(m, new FloatingLinkSection { Versions = [], Pages = null! }),
		["redirect-target-in-navigation"] = m => WithRedirect(m, "nowhere.md", new LinkRedirect { To = "not-a-page.md" })
	};
}
