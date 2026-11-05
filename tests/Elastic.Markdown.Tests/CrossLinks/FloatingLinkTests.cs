// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Links;
using Elastic.Documentation.Links.CrossLinks;

namespace Elastic.Markdown.Tests.CrossLinks;

public class FloatingLinkTests
{
	private static readonly FloatingLinkSection Api = new()
	{
		UrlRoot = "api/doc/elasticsearch",
		Versions = ["main", "9", "8"],
		Hidden = 0b110,
		Pages = new Dictionary<string, ulong>
		{
			[""] = 0b111,
			["operation/operation-search"] = 0b111,
			["operation/operation-new"] = 0b011,
			["types/legacy"] = 0b100
		},
		Aliases = new Dictionary<string, Dictionary<string, string>>
		{
			["9"] = new() { ["operation/operation-search-1"] = "operation/operation-search" },
			["8"] = new() { ["operation/operation-old"] = "types/legacy" }
		}
	};

	private static FetchedCrossLinks CrossLinks()
	{
		var links = new RepositoryLinks
		{
			Origin = GitCheckoutInformation.Unavailable,
			UrlPathPrefix = "/elastic/elasticsearch/tree/main",
			Links = new Dictionary<string, LinkMetadata> { ["index.md"] = new() },
			CrossLinks = [],
			Floating = new Dictionary<string, FloatingLinkSection> { ["api/elasticsearch"] = Api }
		};
		return new FetchedCrossLinks
		{
			DeclaredRepositories = ["elasticsearch"],
			LinkReferences = new Dictionary<string, RepositoryLinks> { ["elasticsearch"] = links }.ToFrozenDictionary(),
			LinkIndexEntries = new Dictionary<string, LinkRegistryEntry>().ToFrozenDictionary()
		};
	}

	private static (bool Resolved, string? Url, string? Error) Resolve(string crossLink)
	{
		string? error = null;
		var resolved = CrossLinkResolver.TryResolve(
			e => error = e,
			CrossLinks(),
			new IsolatedBuildEnvironmentUriResolver(),
			new Uri(crossLink),
			out var uri
		);
		return (resolved, uri?.ToString(), error);
	}

	[Test]
	[Arguments("main", "operation/operation-search", true, "api/doc/elasticsearch/operation/operation-search")]
	[Arguments("8", "operation/operation-search", true, "api/doc/elasticsearch/v8/operation/operation-search")]
	[Arguments("", "", true, "api/doc/elasticsearch")]
	[Arguments("8", "", true, "api/doc/elasticsearch/v8")]
	[Arguments("8", "operation/operation-new", false, "")]
	[Arguments("main", "types/legacy", false, "")]
	[Arguments("9", "operation/operation-search-1", true, "api/doc/elasticsearch/v9/operation/operation-search")]
	[Arguments("7", "operation/operation-search", false, "")]
	[Arguments("main", "operation/operation-search-1", false, "")]
	[Arguments("8", "operation/operation-search-1", false, "")]
	[Arguments("8", "operation/operation-old", true, "api/doc/elasticsearch/v8/types/legacy")]
	[Arguments("9", "operation/operation-old", false, "")]
	public void TryResolvePage_Bitmask_ChecksTheVersion(string version, string page, bool expected, string sitePath)
	{
		var resolved = Api.TryResolvePage(version.Length == 0 ? "main" : version, page, out var path);

		resolved.Should().Be(expected);
		path.Should().Be(sitePath);
	}

	[Test]
	public void CrossLink_ToAnApiPage_ResolvesToThePublishedPage()
	{
		var (resolved, url, _) = Resolve("elasticsearch://api/elasticsearch/operation/operation-search");

		resolved.Should().BeTrue();
		url.Should().Be("https://www.elastic.co/docs/api/doc/elasticsearch/operation/operation-search");
	}

	[Test]
	public void CrossLink_ToAnApiPageWithAnchor_KeepsTheAnchor()
	{
		var (resolved, url, _) = Resolve("elasticsearch://api/elasticsearch/operation/operation-search#responses");

		resolved.Should().BeTrue();
		url.Should().Be("https://www.elastic.co/docs/api/doc/elasticsearch/operation/operation-search#responses");
	}

	[Test]
	public void CrossLink_ToAnAliasWithAnchor_KeepsTheAnchor()
	{
		var (_, url, _) = Resolve("elasticsearch://api/elasticsearch/v9/operation/operation-search-1#responses");

		url.Should().Be("https://www.elastic.co/docs/api/doc/elasticsearch/v9/operation/operation-search#responses");
	}

	[Test]
	public void CodexAwareResolver_ForAnApiPage_LinksToThePublishedPage()
	{
		foreach (var relative in new[] { false, true })
		{
			var resolver = new CodexAwareUriResolver(new[] { "other" }.ToFrozenSet(), relative);
			var resolved = CrossLinkResolver.TryResolve(
				_ => { },
				CrossLinks(),
				resolver,
				new Uri("elasticsearch://api/elasticsearch/operation/operation-search"),
				out var uri
			);

			resolved.Should().BeTrue();
			uri!.ToString().Should().Be("https://www.elastic.co/docs/api/doc/elasticsearch/operation/operation-search");
		}
	}

	[Test]
	public void CrossLink_ToAnOlderVersion_KeepsTheVersionSegment()
	{
		var (resolved, url, _) = Resolve("elasticsearch://api/elasticsearch/v8/types/legacy");

		resolved.Should().BeTrue();
		url.Should().Be("https://www.elastic.co/docs/api/doc/elasticsearch/v8/types/legacy");
	}

	[Test]
	public void CrossLink_ToTheApiLanding_ResolvesToTheProductRoot()
	{
		var (resolved, url, _) = Resolve("elasticsearch://api/elasticsearch");

		resolved.Should().BeTrue();
		url.Should().Be("https://www.elastic.co/docs/api/doc/elasticsearch");
	}

	[Test]
	public void CrossLink_ToAPageMissingInThatVersion_Fails()
	{
		var (resolved, _, error) = Resolve("elasticsearch://api/elasticsearch/v8/operation/operation-new");

		resolved.Should().BeFalse();
		error.Should().Contain("api/elasticsearch").And.Contain("version '8'");
	}

	[Test]
	public void CrossLink_ToAMarkdownPage_StillResolvesThroughTheRepositoryLinks()
	{
		var (resolved, url, _) = Resolve("elasticsearch://index.md");

		resolved.Should().BeTrue();
		url.Should().Contain("elastic/elasticsearch/tree/main");
	}

	[Test]
	public void RepositoryLinks_WithAFloatingSection_RoundTripsThroughJson()
	{
		var links = CrossLinks().LinkReferences["elasticsearch"];

		var roundTripped = RepositoryLinks.Deserialize(RepositoryLinks.Serialize(links));

		var section = roundTripped.Floating.Should().ContainKey("api/elasticsearch").WhoseValue;
		section.Versions.Should().Equal("main", "9", "8");
		section.Hidden.Should().Be(0b110UL);
		section.Pages.Should().BeEquivalentTo(Api.Pages);
		section.Aliases.Should().BeEquivalentTo(Api.Aliases);
		section.TryResolvePage("9", "operation/operation-search-1", out var path).Should().BeTrue();
		path.Should().Be("api/doc/elasticsearch/v9/operation/operation-search");
	}

	[Test]
	public void RepositoryLinks_WithoutFloatingSections_OmitsThePropertyFromTheJson()
	{
		var links = CrossLinks().LinkReferences["elasticsearch"] with { Floating = null };

		RepositoryLinks.Serialize(links).Should().NotContain("floating");
	}
}
