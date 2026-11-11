// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using AwesomeAssertions;
using Elastic.Documentation.Links;
using Elastic.Documentation.Links.CrossLinks;

namespace Elastic.Markdown.Tests.CrossLinks;

/// <summary>
/// <c>links.json</c> is published by one build and read by every other build, including builds of older versions of
/// docs-builder. A newer build can add properties, such as the pages of an API. An older reader must ignore what it
/// does not know and read everything else exactly as before.
/// </summary>
public class LinksJsonForwardCompatibilityTests
{
	private const string Core =
		// language=json
		"""
		"origin": { "branch": "main", "remote": "https://github.com/elastic/docs-content", "ref": "76aac68d066e2af935c38bca8ce04d3ee67a8dd9" },
		"url_path_prefix": "/elastic/docs-content/tree/main",
		"cross_links": [ "kibana://index.md" ],
		"links": {
		  "index.md": {},
		  "get-started/index.md": { "anchors": [ "intro", "use-cases" ] },
		  "hidden-page.md": { "hidden": true }
		},
		"redirects": { "old.md": { "to": "get-started/index.md" } }
		""";

	private static string WithProperty(string property) => "{" + Core + "," + property + "}";

	/// <summary>The section a later build writes for an API: each version states its root, and each page names the versions that contain it.</summary>
	private const string FloatingSection =
		// language=json
		"""
		"floating": {
		  "api/elasticsearch": {
		    "versions": [
		      { "name": "8", "root": "api/doc/elasticsearch/v8", "hidden": true },
		      { "name": "main", "root": "api/doc/elasticsearch", "current": true }
		    ],
		    "pages": { "": 3, "operation/operation-search": 3, "operation/operation-new": 2 },
		    "aliases": { "8": { "operation/operation-search": [ "operation/operation-search-1" ] } }
		  }
		}
		""";

	/// <summary>
	/// The section as a later build writes it, and arbitrary shapes under names no reader knows. A shape under
	/// <c>floating</c> is not arbitrary: a reader that knows that property reads it as what it is.
	/// </summary>
	public static IEnumerable<string> FutureProperties() =>
		[
			FloatingSection,
			// language=json
			"\"future_string\": \"a string\"",
			// language=json
			"\"future_number\": 42",
			// language=json
			"\"future_array\": [ 1, 2, { \"nested\": [ true, null ] } ]",
			// language=json
			"\"future_object\": { \"api/elasticsearch\": { \"versions\": [ { \"name\": \"main\", \"unheard_of\": [ 1 ] } ], \"something_new\": {} } }"
		];

	private static void AssertReadsTheDocumentedProperties(RepositoryLinks links)
	{
		links.Origin.Branch.Should().Be("main");
		links.UrlPathPrefix.Should().Be("/elastic/docs-content/tree/main");
		links.CrossLinks.Should().Equal("kibana://index.md");
		links.Links.Keys.Should().BeEquivalentTo("index.md", "get-started/index.md", "hidden-page.md");
		links.Links["get-started/index.md"].Anchors.Should().Equal("intro", "use-cases");
		links.Links["hidden-page.md"].Hidden.Should().BeTrue();
		links.Redirects.Should().ContainKey("old.md").WhoseValue.To.Should().Be("get-started/index.md");
	}

	[Test]
	[MethodDataSource(nameof(FutureProperties))]
	public void Deserialize_WithAPropertyAnOlderReaderDoesNotKnow_ReadsTheRestUnchanged(string property) =>
		AssertReadsTheDocumentedProperties(RepositoryLinks.Deserialize(WithProperty(property)));

	[Test]
	public void Deserialize_WithAndWithoutAFloatingSection_GivesTheSameLinks()
	{
		var without = RepositoryLinks.Deserialize("{" + Core + "}");
		var with = RepositoryLinks.Deserialize(WithProperty(FloatingSection));

		with.Links.Should().BeEquivalentTo(without.Links);
		with.Redirects.Should().BeEquivalentTo(without.Redirects);
		with.CrossLinks.Should().Equal(without.CrossLinks);
		with.Origin.Should().Be(without.Origin);
	}

	[Test]
	public void CrossLinkFetcher_Deserialize_WithAFloatingSection_ReadsTheRest() =>
		AssertReadsTheDocumentedProperties(CrossLinkFetcher.Deserialize(WithProperty(FloatingSection)));

	[Test]
	[Arguments("docs-content://index.md", true)]
	[Arguments("docs-content://get-started/index.md#intro", true)]
	[Arguments("docs-content://get-started/index.md#missing-anchor", false)]
	[Arguments("docs-content://does-not-exist.md", false)]
	public void CrossLinks_WithAFloatingSectionInTheTargetLinks_ResolveAsBefore(string crossLink, bool expected)
	{
		var resolve = (RepositoryLinks links) =>
		{
			var fetched = new FetchedCrossLinks
			{
				DeclaredRepositories = ["docs-content"],
				LinkReferences = new Dictionary<string, RepositoryLinks> { ["docs-content"] = links }.ToFrozenDictionary(),
				LinkIndexEntries = new Dictionary<string, LinkRegistryEntry>().ToFrozenDictionary()
			};
			var uri = CrossLinkResolver.Resolve(fetched, new IsolatedBuildEnvironmentUriResolver(), new Uri(crossLink)).ResolvedUri();
			return (resolved: uri is not null, uri?.ToString());
		};

		var without = resolve(RepositoryLinks.Deserialize("{" + Core + "}"));
		var with = resolve(RepositoryLinks.Deserialize(WithProperty(FloatingSection)));

		with.resolved.Should().Be(expected);
		with.Should().Be(without, "a floating section changes nothing for pages that are not part of it");
	}
}
