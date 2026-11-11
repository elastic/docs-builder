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
		// The roots are stated, not derived: version 8 is not under a "v8" segment, so a reader that assumed one would fail.
		Versions =
		[
			new() { Name = "main", Root = "api/doc/elasticsearch", Current = true },
			new() { Name = "9", Root = "api/doc/elasticsearch/v9", Hidden = true },
			new() { Name = "8", Root = "api/doc/elasticsearch/legacy-8", Hidden = true }
		],
		Pages = new Dictionary<string, ulong>
		{
			[""] = 0b111,
			["operation/operation-search"] = 0b111,
			["operation/operation-new"] = 0b011,
			["types/legacy"] = 0b100,
			["main"] = 0b001
		},
		Aliases = new Dictionary<string, Dictionary<string, string[]>>
		{
			// one canonical page, two former paths: the canonical path is written once
			["9"] = new() { ["operation/operation-search"] = ["operation/operation-search-1", "operation/operation-search-2"] },
			["8"] = new() { ["types/legacy"] = ["operation/operation-old"] }
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
		var crossLinkUri = new Uri(crossLink);
		var resolution = CrossLinkResolver.Resolve(CrossLinks(), new IsolatedBuildEnvironmentUriResolver(), crossLinkUri);
		var uri = resolution.ResolvedUri();
		return (uri is not null, uri?.ToString(), uri is null ? resolution.ToDiagnosticMessage(crossLinkUri) : null);
	}

	[Test]
	public void Resolve_PageNamedLikeVersion_PageWinsOverVersion()
	{
		var (resolved, url, error) = Resolve("elasticsearch://api/elasticsearch/main");

		resolved.Should().BeTrue(error);
		url.Should().EndWith("/api/doc/elasticsearch/main");
	}

	[Test]
	[Arguments("main", "operation/operation-search", true, "api/doc/elasticsearch/operation/operation-search")]
	[Arguments("8", "operation/operation-search", true, "api/doc/elasticsearch/legacy-8/operation/operation-search")]
	[Arguments("", "", true, "api/doc/elasticsearch")]
	[Arguments("", "operation/operation-search", true, "api/doc/elasticsearch/operation/operation-search")]
	[Arguments("8", "", true, "api/doc/elasticsearch/legacy-8")]
	[Arguments("8", "operation/operation-new", false, "")]
	[Arguments("main", "types/legacy", false, "")]
	[Arguments("9", "operation/operation-search-1", true, "api/doc/elasticsearch/v9/operation/operation-search")]
	[Arguments("9", "operation/operation-search-2", true, "api/doc/elasticsearch/v9/operation/operation-search")]
	[Arguments("7", "operation/operation-search", false, "")]
	[Arguments("main", "operation/operation-search-1", false, "")]
	[Arguments("8", "operation/operation-search-1", false, "")]
	[Arguments("8", "operation/operation-old", true, "api/doc/elasticsearch/legacy-8/types/legacy")]
	[Arguments("9", "operation/operation-old", false, "")]
	public void TryResolvePage_Bitmask_ChecksTheVersion(string version, string page, bool expected, string sitePath)
	{
		var resolved = Api.TryResolvePage(version.Length == 0 ? null : version, page, out var path);

		resolved.Should().Be(expected);
		path.Should().Be(sitePath);
	}

	[Test]
	public void With_ReplacingTheAliases_DoesNotKeepAnsweringFromTheOldOnes()
	{
		Api.TryResolvePage("9", "operation/operation-search-1", out _).Should().BeTrue("this warms the alias lookup");

		var replaced = Api with
		{
			Aliases = new Dictionary<string, Dictionary<string, string[]>>
			{
				["9"] = new() { ["operation/operation-search"] = ["operation/operation-search-3"] }
			}
		};

		replaced.TryResolvePage("9", "operation/operation-search-3", out _).Should().BeTrue();
		replaced.TryResolvePage("9", "operation/operation-search-1", out _).Should().BeFalse();
	}

	[Test]
	public void TryResolvePage_WithoutACurrentVersion_DoesNotGuessOne()
	{
		// warm the lookups first: a copy that carried them over would still answer from the versions it replaced
		Api.TryResolvePage(null, "operation/operation-search", out _).Should().BeTrue();
		var noCurrent = Api with { Versions = [.. Api.Versions.Select(v => v with { Current = false })] };

		noCurrent.TryResolvePage(null, "operation/operation-search", out _).Should().BeFalse();
		noCurrent.TryResolvePage("main", "operation/operation-search", out _).Should().BeTrue("naming the version still works");
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
		var (_, url, _) = Resolve("elasticsearch://api/elasticsearch/9/operation/operation-search-1#responses");

		url.Should().Be("https://www.elastic.co/docs/api/doc/elasticsearch/v9/operation/operation-search#responses");
	}

	[Test]
	public void CodexAwareResolver_ForAnApiPage_LinksToThePublishedPage()
	{
		foreach (var relative in new[] { false, true })
		{
			var resolver = new CodexAwareUriResolver(new[] { "other" }.ToFrozenSet(), relative);
			var uri = CrossLinkResolver.Resolve(
				CrossLinks(),
				resolver,
				new Uri("elasticsearch://api/elasticsearch/operation/operation-search")
			).ResolvedUri();

			uri.Should().NotBeNull();
			uri!.ToString().Should().Be("https://www.elastic.co/docs/api/doc/elasticsearch/operation/operation-search");
		}
	}

	[Test]
	public void CrossLink_NamingAVersion_UsesTheRootThatVersionStates()
	{
		var (resolved, url, _) = Resolve("elasticsearch://api/elasticsearch/8/types/legacy");

		resolved.Should().BeTrue();
		url.Should().Be("https://www.elastic.co/docs/api/doc/elasticsearch/legacy-8/types/legacy");
	}

	[Test]
	public void CrossLink_WithAVPrefixedVersion_IsNotAVersionName()
	{
		var (resolved, _, error) = Resolve("elasticsearch://api/elasticsearch/v8/types/legacy");

		resolved.Should().BeFalse("the version is named 8, and no reader guesses a v prefix");
		error.Should().Contain("'v8/types/legacy'").And.Contain("the current version");
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
		var (resolved, _, error) = Resolve("elasticsearch://api/elasticsearch/8/operation/operation-new");

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
		section.Versions.Should().BeEquivalentTo(Api.Versions, options => options.WithStrictOrdering());
		section.Pages.Should().BeEquivalentTo(Api.Pages);
		section.Aliases.Should().BeEquivalentTo(Api.Aliases);
		section.TryResolvePage("9", "operation/operation-search-1", out var path).Should().BeTrue();
		path.Should().Be("api/doc/elasticsearch/v9/operation/operation-search");
	}

	[Test]
	public void RepositoryLinks_WithAFloatingSection_StatesTheRootOfEachVersionInTheJson()
	{
		var json = RepositoryLinks.Serialize(CrossLinks().LinkReferences["elasticsearch"]);

		json.Should().Contain("\"root\": \"api/doc/elasticsearch/legacy-8\"");
		json.Should().NotContain("url_root", "the routing is data, not a section-level pattern");
	}

	[Test]
	public void RepositoryLinks_WithoutFloatingSections_OmitsThePropertyFromTheJson()
	{
		var links = CrossLinks().LinkReferences["elasticsearch"] with { Floating = null };

		RepositoryLinks.Serialize(links).Should().NotContain("floating");
	}
}
