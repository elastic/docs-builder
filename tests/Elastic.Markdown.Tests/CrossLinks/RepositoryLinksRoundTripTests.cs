// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Elastic.Documentation.Links;
using Elastic.Documentation.Serialization;
using Elastic.Documentation.Testing;
using Nullean.Snapshots;
using Nullean.Snapshots.Json;

namespace Elastic.Markdown.Tests.CrossLinks;

/// <summary>
/// Locks in what <c>links.json</c> reads and writes today: each manifest is deserialized and serialized again, and the
/// verified file is what a later build would publish. All data is synthetic.
/// </summary>
public class RepositoryLinksRoundTripTests
{
	private const string Origin =
		// language=json
		"""
		"origin": { "branch": "main", "remote": "https://github.com/example-org/example-repo", "ref": "0123456789abcdef0123456789abcdef01234567" },
		"url_path_prefix": "/elastic/example-repo/tree/main"
		""";

	private static string Manifest(string rest) => "{" + Origin + "," + rest + "}";

	private static string RoundTrip(string json) => LinkJson.Serialize(LinkManifestJson.Read(json));

	/// <summary>Whether a manifest reads, or the first line of the exception it throws.</summary>
	private static string Outcome(string json)
	{
		try
		{
			_ = RoundTrip(json);
			return "reads";
		}
		catch (Exception e)
		{
			return $"throws {e.GetType().Name}: {e.Message.Split('\n')[0].TrimEnd('\r')}";
		}
	}

	[Test]
	public void RoundTrip_EmptyManifest_KeepsRequiredProperties() =>
		RoundTrip(Manifest("\"cross_links\": [], \"links\": {}")).Should().MatchJsonSnapshot();

	[Test]
	public void RoundTrip_PagesWithAndWithoutAnchors_WritesAnchorsEmptyOrNull() =>
		RoundTrip(
			Manifest(
				// language=json
				"""
				"cross_links": [],
				"links": {
				  "index.md": {},
				  "empty-anchors.md": { "anchors": [] },
				  "null-anchors.md": { "anchors": null },
				  "some-anchors.md": { "anchors": [ "intro", "Mixed-Case", "intro" ] },
				  "hidden.md": { "hidden": true },
				  "hidden-false.md": { "hidden": false, "anchors": [ "a" ] }
				}
				"""
			)
		).Should().MatchJsonSnapshot();

	[Test]
	public void RoundTrip_CrossLinks_KeepsOrderAndDuplicates() =>
		RoundTrip(
			Manifest(
				// language=json
				"""
				"cross_links": [ "other-repo://b.md", "other-repo://a.md", "other-repo://a.md", "third-repo://x/index.md#anchor" ],
				"links": { "index.md": {} }
				"""
			)
		).Should().MatchJsonSnapshot();

	[Test]
	public void RoundTrip_Redirects_WritesEachShape() =>
		RoundTrip(
			Manifest(
				// language=json
				"""
				"cross_links": [],
				"links": { "index.md": {}, "new.md": {} },
				"redirects": {
				  "to-only.md": { "to": "new.md" },
				  "to-with-anchors.md": { "to": "new.md", "anchors": { "old": "new", "dropped": null } },
				  "catch-all.md": { "to": "new.md", "anchors": { "!": "!" } },
				  "empty-anchors.md": { "to": "new.md", "anchors": {} },
				  "many.md": {
				    "to": "new.md",
				    "many": [
				      { "to": "a.md", "anchors": { "a": "a" } },
				      { "to": "b.md" },
				      { "anchors": { "c": null } }
				    ]
				  },
				  "many-only.md": { "many": [ { "to": "a.md", "anchors": { "!": "!" } } ] },
				  "empty-many.md": { "to": "new.md", "many": [] },
				  "empty-to.md": { "to": "" },
				  "null-to.md": { "to": null },
				  "cross-repo.md": { "to": "other-repo://page.md" }
				}
				"""
			)
		).Should().MatchJsonSnapshot();

	[Test]
	public void RoundTrip_EmptyRedirects_KeepsAnEmptyObject() =>
		RoundTrip(Manifest("\"cross_links\": [], \"links\": {}, \"redirects\": {}")).Should().MatchJsonSnapshot();

	[Test]
	public void RoundTrip_NullRedirects_DropsNothingAndWritesNull() =>
		RoundTrip(Manifest("\"cross_links\": [], \"links\": {}, \"redirects\": null")).Should().MatchJsonSnapshot();

	[Test]
	public void RoundTrip_FloatingSections_KeepsVersionsPagesAndAliases() =>
		RoundTrip(
			Manifest(
				// language=json
				"""
				"cross_links": [],
				"links": { "index.md": {} },
				"floating": {
				  "api/example-api": {
				    "versions": [
				      { "name": "8", "root": "api/doc/example-api/v8", "hidden": true },
				      { "name": "main", "root": "api/doc/example-api", "current": true },
				      { "name": "plain", "root": "api/doc/example-api/plain", "hidden": false, "current": false }
				    ],
				    "pages": { "": 7, "operation/operation-search": 3, "operation/operation-new": 2, "tag/other": 4 },
				    "aliases": { "8": { "operation/operation-search": [ "operation/operation-search-1", "operation/operation-search-2" ] }, "main": {} }
				  },
				  "api/no-aliases": {
				    "versions": [ { "name": "main", "root": "api/doc/no-aliases", "current": true } ],
				    "pages": { "": 1 }
				  }
				}
				"""
			)
		).Should().MatchJsonSnapshot();

	[Test]
	public void RoundTrip_EmptyFloating_WritesAnEmptyObject() =>
		RoundTrip(Manifest("\"cross_links\": [], \"links\": {}, \"floating\": {}")).Should().MatchJsonSnapshot();

	[Test]
	public void RoundTrip_UnknownFields_AreDropped() =>
		RoundTrip(
			Manifest(
				// language=json
				"""
				"content_source": "current",
				"cross_links": [],
				"links": { "index.md": { "anchors": [ "a" ], "title": "ignored", "weight": 3 } },
				"future_object": { "x": [ 1, 2, { "y": null } ] },
				"future_array": [ 1, 2 ],
				"future_string": "text"
				"""
			)
		).Should().MatchJsonSnapshot();

	[Test]
	public void RoundTrip_OriginInformation_KeepsEveryGitField() =>
		RoundTrip(
			"""
			{
			  "origin": {
			    "branch": "feature/x",
			    "remote": "git@github.com:example-org/example-repo.git",
			    "ref": "fedcba9876543210fedcba9876543210fedcba98",
			    "name": "example-repo",
			    "github_ref": "refs/pull/12/merge"
			  },
			  "url_path_prefix": null,
			  "cross_links": [],
			  "links": {}
			}
			"""
		).Should().MatchJsonSnapshot();

	[Test]
	public void RoundTrip_OriginWithoutName_WritesTheUnavailableName() =>
		RoundTrip(
			"""
			{
			  "origin": { "branch": "main", "remote": "https://github.com/example-org/example-repo", "ref": "abc" },
			  "url_path_prefix": "",
			  "cross_links": [],
			  "links": {}
			}
			"""
		).Should().MatchJsonSnapshot();

	[Test]
	public void RoundTrip_UrlPathPrefixShapes_AreKeptVerbatim() =>
		new[] { "\"url_path_prefix\": \"\"", "\"url_path_prefix\": \"/\"", "\"url_path_prefix\": \"/a/b/\"", "\"url_path_prefix\": null" }
			.Select(
				prefix => $$"""{ "origin": { "branch": "main", "remote": "r", "ref": "x" }, {{prefix}}, "cross_links": [], "links": {} }"""
			)
			.Select(json => JsonDocument.Parse(RoundTrip(json)).RootElement.GetProperty("url_path_prefix").GetRawText())
			.Aggregate(new StringBuilder(), (sb, v) => sb.AppendLine(v), sb => sb.ToString())
			.Should()
			.MatchInlineSnapshot("""
				""
				"/"
				"/a/b/"
				null

				""");

	[Test]
	public void SerializeRedirects_EachShape_WritesTheRedirectsOnly()
	{
		var links = LinkManifestJson.Read(
			Manifest(
				// language=json
				"""
				"cross_links": [],
				"links": {},
				"redirects": {
				  "a.md": { "to": "b.md", "anchors": { "x": "y" } },
				  "c.md": { "many": [ { "to": "d.md", "anchors": { "!": "!" } } ] }
				}
				"""
			)
		);

		LinkJson.SerializeRedirects(links.Redirects).Should().MatchJsonSnapshot();
		LinkJson.SerializeRedirects(null).Should().MatchInlineSnapshot("null");
	}

	[Test]
	public void Deserialize_Malformed_Manifests_ThrowToday()
	{
		var cases = new (string Name, string Json)[]
		{
			("empty-object", "{}"),
			("no-origin", """{ "url_path_prefix": "", "cross_links": [], "links": {} }"""),
			("no-url-path-prefix", """{ "origin": { "branch": "main", "remote": "r", "ref": "x" }, "cross_links": [], "links": {} }"""),
			("no-links", Manifest("\"cross_links\": []")),
			("no-cross-links", Manifest("\"links\": {}")),
			("origin-without-ref", """{ "origin": { "branch": "main", "remote": "r" }, "url_path_prefix": "", "cross_links": [], "links": {} }"""),
			("origin-without-branch", """{ "origin": { "remote": "r", "ref": "x" }, "url_path_prefix": "", "cross_links": [], "links": {} }"""),
			("origin-without-remote", """{ "origin": { "branch": "main", "ref": "x" }, "url_path_prefix": "", "cross_links": [], "links": {} }"""),
			("links-is-array", Manifest("\"cross_links\": [], \"links\": []")),
			("anchors-is-string", Manifest("\"cross_links\": [], \"links\": { \"a.md\": { \"anchors\": \"a\" } }")),
			("hidden-is-string", Manifest("\"cross_links\": [], \"links\": { \"a.md\": { \"hidden\": \"yes\" } }")),
			("floating-version-without-root", Manifest(
				"\"cross_links\": [], \"links\": {}, \"floating\": { \"api/x\": { \"versions\": [ { \"name\": \"1\" } ], \"pages\": {} } }"
			)),
			("floating-pages-not-numbers", Manifest(
				"\"cross_links\": [], \"links\": {}, \"floating\": { \"api/x\": { \"versions\": [], \"pages\": { \"a\": \"b\" } } }"
			)),
			("floating-without-pages", Manifest("\"cross_links\": [], \"links\": {}, \"floating\": { \"api/x\": { \"versions\": [] } }")),
			("trailing-comma", Manifest("\"cross_links\": [], \"links\": {},")),
			("not-json", "links"),
			("empty-string", ""),
			("null-literal", "null")
		};

		var sb = new StringBuilder();
		foreach (var (name, json) in cases)
			_ = sb.Append(name).Append(": ").AppendLine(Outcome(json));

		sb.ToString().Should().MatchSnapshot();
	}

	[Test]
	public void RoundTrip_IsStable_SerializingTwiceGivesTheSameText()
	{
		var first = RoundTrip(
			Manifest(
				"""
				"cross_links": [ "other-repo://a.md" ],
				"links": { "index.md": { "anchors": [ "a" ] } },
				"redirects": { "a.md": { "to": "index.md", "anchors": { "x": null } } },
				"floating": { "api/x": { "versions": [ { "name": "main", "root": "api/doc/x", "current": true } ], "pages": { "": 1 } } }
				"""
			)
		);

		RoundTrip(first).Should().Be(first);
	}
}

/// <summary>The registry (<c>link-index.json</c>) that points at every repository's <c>links.json</c>.</summary>
public class LinkRegistryRoundTripTests
{
	[Test]
	public void RoundTrip_Registry_KeepsBranchesAndWritesTheEntryDefaults() =>
		LinkJson
			.Serialize(
				LinkManifestJson.ReadRegistry(
					// language=json
					"""
				{
				  "repositories": {
				    "example-repo": {
				      "main": { "repository": "example-repo", "path": "elastic/example-repo/main/links.json", "branch": "main", "etag": "etag-1", "ref": "abc", "updated_at": "2025-01-02T03:04:05Z" },
				      "9.0": { "repository": "example-repo", "path": "elastic/example-repo/9.0/links.json", "branch": "9.0", "etag": "etag-2" }
				    },
				    "other-repo": {},
				    "future-repo": { "main": { "repository": "future-repo", "path": "p", "branch": "main", "etag": "e", "unheard_of": 1 } }
				  },
				  "unheard_of": [ 1 ]
				}
				"""
				)
			)
			.Should()
			.MatchJsonSnapshot();

	[Test]
	public void WithLinkRegistryEntry_NewerOlderAndNewRepository_UpdatesOnlyWhatIsNewer()
	{
		LinkRegistryEntry Entry(string repo, string branch, string etag, int day) =>
			new()
			{
				Repository = repo,
				Branch = branch,
				Path = $"elastic/{repo}/{branch}/links.json",
				ETag = etag,
				UpdatedAt = new DateTime(2025, 1, day, 0, 0, 0, DateTimeKind.Utc)
			};

		var registry = new LinkRegistry
		{
			Repositories = new Dictionary<string, Dictionary<string, LinkRegistryEntry>>
			{
				["example-repo"] = new() { ["main"] = Entry("example-repo", "main", "original", 5) }
			}
		};

		var result = registry
			.WithLinkRegistryEntry(Entry("example-repo", "main", "older-is-ignored", 1))
			.WithLinkRegistryEntry(Entry("example-repo", "main", "same-day-is-ignored", 5))
			.WithLinkRegistryEntry(Entry("example-repo", "9.0", "new-branch", 1))
			.WithLinkRegistryEntry(Entry("new-repo", "main", "new-repo", 1));
		var updated = result.WithLinkRegistryEntry(Entry("example-repo", "main", "newer-wins", 9));

		// the copy is shallow: the branch dictionaries are shared, so the update shows through the earlier result and the original
		result.Repositories["example-repo"]["main"].ETag.Should().Be("newer-wins");
		registry.Repositories["example-repo"]["main"].ETag.Should().Be("newer-wins");
		LinkJson.Serialize(updated).Should().MatchJsonSnapshot();
	}
}
