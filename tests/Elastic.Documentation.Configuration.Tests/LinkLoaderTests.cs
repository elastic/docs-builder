// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Links;
using Elastic.Documentation.Serialization;

namespace Elastic.Documentation.Configuration.Tests;

/// <summary>
/// <see cref="LinkLoader"/> reads a <c>links.json</c> or <c>link-index.json</c> exactly as the build always did, and checks the record against its
/// rules without ever refusing it: the file belongs to another repository, so a finding never stops a build. All data is synthetic.
/// </summary>
public class LinkLoaderTests
{
	private const string Origin =
		"""
		"origin": { "branch": "main", "remote": "https://example.test/repo.git", "ref": "abc", "name": "repo" },
		"url_path_prefix": "/docs"
		""";

	private static string Manifest(string rest) => "{" + Origin + "," + rest + "}";

	[Test]
	public void LoadManifest_AValidManifest_BreaksNoRule() =>
		LinkLoader
			.LoadManifest(
				Manifest(
					"""
				"links": { "index.md": { "anchors": [ "a" ] }, "setup.md": {} },
				"cross_links": [ "other-repo://page.md" ],
				"redirects": { "old.md": { "to": "setup.md" } }
				"""
				),
				"links.json"
			)
			.Findings()
			.Should()
			.BeEmpty();

	[Test]
	public void LoadManifest_AManifestThatBreaksRules_IsStillLoadedWithItsFindings()
	{
		var result = LinkLoader.LoadManifest(
			Manifest(
				"""
			"links": { "index.md": {}, "notes.txt": {}, "a.md.md": {} },
			"cross_links": []
			"""
			),
			"elastic/repo/main/links.json"
		);

		var loaded = result.Value.Should().BeOfType<Loaded<LinkManifest>>().Subject;
		loaded.Value.Links.Keys.Should().Equal("index.md", "notes.txt", "a.md.md");
		result.HasErrors().Should().BeTrue();
		loaded.Findings.Should().OnlyContain(d => d.File == "elastic/repo/main/links.json");
		loaded
			.Findings
			.Select(d => d.RuleId)
			.Should()
			.Contain([
				LinkManifestConstraints.LinkManifestPagePathsAreMarkdown.Name,
				LinkManifestConstraints.PagePathSingleMdExtension.Name
			]);
	}

	[Test]
	[Arguments("{ not json")]
	[Arguments("")]
	[Arguments("null")]
	[Arguments("{}")]
	public void LoadManifest_TextThatDoesNotParse_IsOneErrorAndNoManifest(string text)
	{
		var result = LinkLoader.LoadManifest(text, "links.json");

		result
			.Value
			.Should()
			.BeOfType<Unreadable<LinkManifest>>()
			.Which
			.Findings
			.Should()
			.ContainSingle(d => d.Severity == Severity.Error && d.File == "links.json");
	}

	/// <summary>
	/// Law: a manifest that the JSON reader accepts is loaded, whatever it holds, because the reader accepted it before rules existed. A rule that threw
	/// on a null page, redirect or cross-link would turn a readable file into a failed build.
	/// </summary>
	[Test]
	[Arguments("\"links\": { \"a.md\": null }, \"cross_links\": []")]
	[Arguments("\"links\": {}, \"cross_links\": [ null ]")]
	[Arguments("\"links\": { \"a.md\": {} }, \"cross_links\": [], \"redirects\": { \"b.md\": null }")]
	[Arguments("\"links\": { \"a.md\": {} }, \"cross_links\": [], \"redirects\": { \"b.md\": { \"to\": \"a.md\", \"many\": [ null ] } }")]
	[Arguments("\"links\": { \"a.md\": {} }, \"cross_links\": [], \"redirects\": { \"b.md\": { \"to\": \"a.md\", \"anchors\": { \"x\": null } } }")]
	[Arguments("\"links\": { \"a.md\": { \"anchors\": null } }, \"cross_links\": [], \"redirects\": { \"b.md\": { \"to\": \"a.md\", \"anchors\": { \"x\": \"y\" } } }")]
	[Arguments("\"links\": {}, \"cross_links\": [], \"redirects\": null, \"floating\": null")]
	public void LoadManifest_HostileButReadableValues_AreLoaded(string rest) =>
		LinkLoader.LoadManifest(Manifest(rest), "links.json").Value.Should().BeOfType<Loaded<LinkManifest>>();

	[Test]
	[Arguments("""{ "origin": null, "url_path_prefix": null, "links": {}, "cross_links": [] }""")]
	[Arguments("""{ "origin": { "branch": "main", "remote": "r", "ref": "x" }, "url_path_prefix": "", "links": null, "cross_links": null }""")]
	public void LoadManifest_NullRequiredMembers_AreLoadedWithoutThrowing(string json) =>
		LinkLoader.LoadManifest(json, "links.json").Value.Should().BeOfType<Loaded<LinkManifest>>();

	[Test]
	public void LoadRegistry_KeepsTheETag_AndReportsWhatItBreaks()
	{
		var result = LinkLoader.LoadRegistry(
			"""
			{ "repositories": { "kibana": { "8.x": { "repository": "kibana", "path": "elastic/kibana/8.x/links.json", "branch": "8.x", "etag": "e", "ref": "abc", "updated_at": "2026-01-01T00:00:00Z" } } } }
			""",
			"link-index.json",
			"\"etag-1\""
		);

		var loaded = result.Value.Should().BeOfType<Loaded<LinkRegistry>>().Subject;
		loaded.Value.ETag.Should().Be("\"etag-1\"");
		loaded.Findings.Select(d => d.RuleId).Should().Equal(LinkRegistryConstraints.RepositoryHasMainOrMaster.Name);
	}

	[Test]
	[Arguments("{ \"repositories\": { \"r\": null } }")]
	[Arguments("{ \"repositories\": { \"r\": { \"main\": null } } }")]
	public void LoadRegistry_HostileButReadableValues_AreLoaded(string json) =>
		LinkLoader.LoadRegistry(json, "link-index.json").Value.Should().BeOfType<Loaded<LinkRegistry>>();

	[Test]
	[Arguments("{ not json")]
	[Arguments("null")]
	[Arguments("{}")]
	public void LoadRegistry_TextThatDoesNotParse_IsOneErrorAndNoRegistry(string text) =>
		LinkLoader.LoadRegistry(text, "link-index.json").Value.Should().BeOfType<Unreadable<LinkRegistry>>();

	[Test]
	public void ManifestAndRegistry_WriteWhatTheyRead()
	{
		var manifest =
			LinkLoader.LoadManifest(
				Manifest("\"links\": { \"index.md\": { \"anchors\": [ \"a\" ] } }, \"cross_links\": [ \"other-repo://page.md\" ]"),
				"links.json"
			).ValueOrNull()!;
		var registry = LinkLoader.LoadRegistry(LinkJson.Serialize(LinkRegistryOf("kibana")), "link-index.json").ValueOrNull()!;

		LinkLoader.LoadManifest(LinkJson.Serialize(manifest), "links.json").ValueOrNull().Should().Be(manifest);
		registry.Should().Be(LinkRegistryOf("kibana"));
	}

	private static LinkRegistry LinkRegistryOf(string repository) =>
		new()
		{
			Repositories = new Dictionary<string, Dictionary<string, LinkRegistryEntry>>
			{
				[repository] = new()
				{
					["main"] = new LinkRegistryEntry
					{
						Repository = repository,
						Branch = "main",
						Path = $"elastic/{repository}/main/links.json",
						ETag = "e",
						GitReference = "abc",
						UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
					}
				}
			}
		};
}
