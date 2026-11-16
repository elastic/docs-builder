// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.DocumentationSets;
using Elastic.Documentation.Links;
using Elastic.Documentation.Links.CrossLinks;

namespace Elastic.Markdown.Tests.DocSet;

/// <summary>
/// The build checks the cross-links of its repository once against the fetched registry and targets (<see cref="DocumentationGenerator"/>); the rules the
/// cross-link resolver reports at each link (M30, M31, M34) are not asked again, so a bad link is reported by the resolver only.
/// </summary>
public class LinkGraphCheckTests
{
	private static LinkManifest Manifest(string repository, string pages, params string[] crossLinks) =>
		LinkLoader.LoadManifest(
			$$"""
			{
			  "origin": { "branch": "main", "remote": "https://example.test/{{repository}}.git", "ref": "abc", "name": "{{repository}}" },
			  "url_path_prefix": "/docs",
			  "links": { {{pages}} },
			  "cross_links": [{{string.Join(", ", crossLinks.Select(l => $"\"{l}\""))}}]
			}
			""",
			"links.json"
		).Value!;

	private static LinkRegistry Registry(params string[] repositories) =>
		LinkLoader.LoadRegistry(
			$$"""
			{ "repositories": { {{string.Join(
				", ",
				repositories.Select(r => $"\"{r}\": {{ \"main\": {{ \"repository\": \"{r}\", \"path\": \"elastic/{r}/main/links.json\", \"branch\": \"main\", \"etag\": \"e1\", \"ref\": \"abc\", \"updated_at\": \"2026-01-01T00:00:00Z\" }} }}")
			)}} } }
			""",
			"link-index.json"
		).Value!;

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

	private static FetchedCrossLinks Fetched(LinkRegistry? registry, bool complete = true, params string[] repositories) =>
		new()
		{
			DeclaredRepositories = [.. repositories],
			LinkReferences = repositories.ToDictionary(
				r => r,
				r => Manifest(r, "\"reference/page.md\": { \"anchors\": [\"top\"] }")
			).ToFrozenDictionary(),
			LinkIndexEntries = new Dictionary<string, LinkRegistryEntry>().ToFrozenDictionary(),
			Registry = registry,
			IsComplete = complete
		};

	private static readonly LinkManifest Own = Manifest("mine", "\"index.md\": {}", "kibana://reference/page.md#top");

	[Test]
	public void AnUnusedDeclaration_IsReportedOnce_ByTheRule()
	{
		var report = DocumentationGenerator.CheckLinkGraph(
			Own,
			Fetched(Registry("kibana", "never-linked"), true, "kibana", "never-linked"),
			Docset("kibana", "never-linked")
		);

		var violation = report!.Violations.Should().ContainSingle().Subject;
		violation.ConstraintId.Should().Be(LinkGraphConstraints.CrossLinkDeclarationUsed.Name);
		violation.Message.Should().Be("cross_links entry 'never-linked' is unused: no cross-link of the manifest uses it.");
	}

	[Test]
	public void ALinkTheResolverReports_IsNotReportedAgainByTheGraph()
	{
		// kibana has no such page (M30), and the link names an undeclared scheme (M34): both are the resolver's, at the link
		var own = Manifest("mine", "\"index.md\": {}", "kibana://reference/missing.md", "other://reference/page.md");

		var report = DocumentationGenerator.CheckLinkGraph(own, Fetched(Registry("kibana", "other"), true, "kibana"), Docset("kibana"));

		report!
			.Violations
			.Select(v => v.ConstraintId)
			.Should()
			.NotContain([
				LinkGraphConstraints.CrossLinkResolves.Name,
				LinkGraphConstraints.CrossLinkSchemeDeclared.Name,
				LinkGraphConstraints.CrossLinkRedirectResolves.Name
			]);
	}

	[Test]
	public void ADeclaredRepositoryTheRegistryDoesNotKnow_IsReportedByTheRule()
	{
		var report = DocumentationGenerator.CheckLinkGraph(Own, Fetched(Registry("somewhere-else"), true, "kibana"), Docset("kibana"));

		report!.Violations.Should().Contain(v => v.ConstraintId == LinkGraphConstraints.CrossLinkRepositoryInRegistry.Name);
	}

	[Test]
	public void WithoutACompleteFetchAndARegistry_NothingIsChecked()
	{
		DocumentationGenerator.CheckLinkGraph(Own, Fetched(null, true, "kibana"), Docset("kibana")).Should().BeNull();
		DocumentationGenerator.CheckLinkGraph(Own, Fetched(Registry("kibana"), false, "kibana"), Docset("kibana")).Should().BeNull();
		DocumentationGenerator.CheckLinkGraph(Own, null, Docset("kibana")).Should().BeNull();
	}
}
