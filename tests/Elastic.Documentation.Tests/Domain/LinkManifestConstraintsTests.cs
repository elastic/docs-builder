// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Links;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

public class LinkManifestConstraintsTests
{
	[Test]
	public void Laws() =>
		LinkManifestConstraints.All.Laws(DomainGen.Of<LinkManifest>(o => o.Normalize<LinkManifest>(LinkExamples.Manifest)), CounterExamples);

	[Test]
	public void ValidManifest_BreaksNoRule() => LinkManifestConstraints.All.Validate(Valid()).Should().BeEmpty();

	[Test]
	public void PageUrl_RemovesEveryMdAndTrailingIndex()
	{
		LinkManifestConstraints.PageUrl("foo.md").Should().Be("foo");
		LinkManifestConstraints.PageUrl("foo/index.md").Should().Be("foo");
		LinkManifestConstraints.PageUrl("index.md").Should().BeEmpty();
		LinkManifestConstraints.PageUrl("a.md.md").Should().Be("a");
	}

	[Test]
	public void CrossLinkWithAWebScheme_NamesTheValidatorsReason()
	{
		var manifest = Valid() with { CrossLinks = ["https://example.test/x"] };
		LinkManifestConstraints
			.All
			.Validate(manifest)
			.Should()
			.ContainSingle()
			.Which
			.Message
			.Should()
			.Contain("cannot use standard web/protocol schemes");
	}

	[Test]
	public void ExactRuleAfterCatchAll_NamesTheRedirectAndAnchor() =>
		LinkManifestConstraints
			.All
			.Validate(CounterExamples["redirect-catch-all-after-exact"](Valid()))
			.Should()
			.ContainSingle()
			.Which
			.Message
			.Should()
			.Contain("many.md")
			.And
			.Contain("x");

	private static LinkManifest Valid() =>
		new()
		{
			Origin = new GitCheckoutInformation
			{
				Branch = "main",
				Remote = "https://example.test/repo.git",
				Ref = "abc",
				RepositoryName = "docs-content"
			},
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

	private static LinkManifest WithPage(LinkManifest m, string path, LinkMetadata page) =>
		m with { Links = new Dictionary<string, LinkMetadata>(m.Links) { [path] = page } };

	// the pages the redirect cases land on, under names the generator never gives a page (it names them page0.md, page1.md...)
	private static LinkManifest WithSetup(LinkManifest m) =>
		WithPage(WithPage(m, "setup.md", new LinkMetadata { Anchors = ["intro"] }), "index.md", new LinkMetadata { Anchors = [] });

	private static IReadOnlyDictionary<string, Func<LinkManifest, LinkManifest>> CounterExamples
	{
		get;
	} = new Dictionary<string, Func<LinkManifest, LinkManifest>>
	{
		["origin-required"] = m => m with { Origin = null! },
		["origin-branch-required"] = m => m with { Origin = GitCheckoutInformation.Unavailable with { Branch = null! } },
		["origin-remote-required"] = m => m with { Origin = GitCheckoutInformation.Unavailable with { Remote = null! } },
		["origin-ref-required"] = m => m with { Origin = GitCheckoutInformation.Unavailable with { Ref = null! } },
		["links-required"] = m => m with { Links = null! },
		["cross-links-required"] = m => m with { CrossLinks = null! },
		["link-manifest-page-paths-are-markdown"] = m => WithPage(m, "notes.txt", new LinkMetadata()),
		["link-manifest-origin-named"] = m => m with { Origin = GitCheckoutInformation.Unavailable },
		["url-path-prefix-normalized"] = m => m with { UrlPathPrefix = "docs/" },
		["link-manifest-page-urls-unique"] =
			m => WithPage(WithPage(m, "dupurl.md", new LinkMetadata()), "dupurl/index.md", new LinkMetadata()),
		["page-path-single-md-extension"] = m => WithPage(m, "a.md.md", new LinkMetadata()),
		["cross-link-scheme-allowed"] = m => m with { CrossLinks = [.. m.CrossLinks, "https://example.test/x"] },
		["page-anchors-unique"] = m => WithPage(m, "dup.md", new LinkMetadata { Anchors = ["a"], DuplicateAnchors = ["A"] }),
		["redirect-source-is-not-a-page"] =
			m => WithRedirect(WithPage(WithSetup(m), "shadow.md", new LinkMetadata()), "shadow.md", new LinkRedirect { To = "setup.md" }),
		["redirect-catch-all-after-exact"] =
			m => WithRedirect(
				WithSetup(m),
				"many.md",
				new LinkRedirect
				{
					Many =
					[
						new LinkSingleRedirect { To = "index.md", Anchors = LinkRedirect.CatchAllAnchors },
						new LinkSingleRedirect { To = "setup.md", Anchors = new Dictionary<string, string?> { ["x"] = "intro" } }
					]
				}
			),
		["redirect-empty-anchors-keeps-anchor"] =
			m => WithRedirect(WithSetup(m), "empty.md", new LinkRedirect { To = "setup.md", Anchors = new Dictionary<string, string?>() }),
		["redirect-same-page-needs-anchors"] = m => WithRedirect(m, "same.md", new LinkRedirect()),
		["redirect-no-chains"] =
			m => WithRedirect(
				WithRedirect(WithSetup(m), "a.md", new LinkRedirect { To = "b.md" }),
				"b.md",
				new LinkRedirect { To = "setup.md" }
			),
		["redirect-target-anchors-exist"] =
			m => WithRedirect(
				WithSetup(m),
				"anch.md",
				new LinkRedirect { To = "setup.md", Anchors = new Dictionary<string, string?> { ["x"] = "missing" } }
			),
		["redirect-target-in-navigation"] = m => WithRedirect(m, "gone.md", new LinkRedirect { To = "nowhere.md" })
	};
}
