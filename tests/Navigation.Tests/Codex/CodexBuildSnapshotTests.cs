// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using AwesomeAssertions;
using Elastic.Codex;
using Elastic.Codex.Navigation;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Navigation.Isolated.Node;
using Elastic.Documentation.Testing;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Navigation.Tests.Codex;

/// <summary>
/// A codex of five documentation sets in two groups and one ungrouped, built in memory: the navigation tree, the group
/// navigations, the generated output tree and the content section of the landing and group pages. Locks today's behaviour ahead
/// of the domain model refactor. The group identifiers are hashes of the group names, so they are stable across runs.
/// </summary>
public class CodexBuildSnapshotTests : CodexNavigationTestBase
{
	private static readonly CodexDocumentationSetReference[] References =
	[
		new() { Name = "apm-agent", Branch = "main", Group = "observability" },
		new() { Name = "uptime", Branch = "main", Group = "observability" },
		new() { Name = "endpoint", Branch = "main", Group = "security" },
		new() { Name = "standalone", Branch = "main" },
		new() { Name = "tools", Branch = "main", Group = "security" }
	];

	private CodexNavigation CreateNavigation(string sitePrefix = "/docs")
	{
		var fileSystem = new MockFileSystem();
		var navigations = new Dictionary<string, IDocumentationSetNavigation>();
		foreach (var reference in References)
		{
			var docs = $"/{reference.Name}/docs";
			fileSystem.AddFile($"{docs}/index.md", new MockFileData($"# {reference.Name}"));
			fileSystem.AddFile($"{docs}/guide/index.md", new MockFileData($"# {reference.Name} guide"));
			fileSystem.AddFile($"{docs}/guide/setup.md", new MockFileData($"# {reference.Name} setup"));
			fileSystem.AddFile($"{docs}/draft.md", new MockFileData("# Draft"));
			var yaml =
				$"project: '{reference.Name}'\ntoc:\n  - file: index.md\n  - folder: guide\n    children:\n      - file: index.md\n      - file: setup.md\n  - hidden: draft.md\n";
			var docSet = DocumentationSetFile.LoadAndResolve(new DiagnosticsCollector([]), yaml, fileSystem.DirectoryInfo.New(docs));
			var context = new TestDocumentationSetContext(
				fileSystem,
				fileSystem.DirectoryInfo.New(docs),
				fileSystem.DirectoryInfo.New($"/{reference.Name}/output"),
				fileSystem.FileInfo.New($"{docs}/docset.yml"),
				reference.Name
			);
			navigations[reference.Name] = new DocumentationSetNavigation<TestDocumentationFile>(
				docSet,
				context,
				CodexTestDocumentationFileFactory.Instance
			);
		}

		return new CodexNavigation(CreateCodexConfiguration(sitePrefix), References, CreateContext(), navigations);
	}

	[Test]
	public void Navigation_Tree_MatchesSnapshot() => NavigationDump.ToText(CreateNavigation()).Should().MatchSnapshot();

	[Test]
	public void Navigation_WithoutASitePrefix_MatchesSnapshot() =>
		NavigationDump
			.ToText(CreateNavigation(string.Empty))
			.Should()
			.MatchInlineSnapshot(
				"""
			CodexNavigation    "Test Codex"
			  index: CodexIndexLeaf    "Test Codex"
			  GroupLinkLeaf  /g/observability  "Observability" [wrong parent]
			  GroupLinkLeaf  /g/security  "Security" [wrong parent]
			  DocumentationSetNavigation  /r/standalone  "standalone" [wrong parent]
			    index: FileNavigationLeaf  /r/standalone  "standalone"
			    FolderNavigation  /r/standalone/guide  "standalone guide"
			      index: FileNavigationLeaf  /r/standalone/guide  "standalone guide"
			      FileNavigationLeaf  /r/standalone/guide/setup  "standalone setup"
			    FileNavigationLeaf  /r/standalone/draft  "Draft" [hidden]
			"""
			);

	[Test]
	public void Groups_ListTheirDocumentationSets_MatchesSnapshot()
	{
		var navigation = CreateNavigation();
		var sb = new StringBuilder();
		foreach (var group in navigation.GroupNavigations)
		{
			_ = sb.Append(group.GroupSlug).Append(" \"").Append(group.DisplayTitle).Append("\" ").Append(group.Url).Append('\n');
			foreach (var set in group.DocumentationSetInfos)
				_ = sb.Append("  ").Append(set.Name).Append(' ').Append(set.Url).Append('\n');
		}
		// DocumentationSetInfos comes back in hash order, which changes with every process, so the list is sorted here
		_ = sb
			.Append("all: ")
			.Append(string.Join(", ", navigation.DocumentationSetInfos.Select(d => $"{d.Name} {d.Url}").Order(StringComparer.Ordinal)))
			.Append('\n');
		sb
			.ToString()
			.Should()
			.MatchInlineSnapshot(
				"""
			observability "Observability" /docs/g/observability
			  apm-agent /docs/r/apm-agent
			  uptime /docs/r/uptime
			security "Security" /docs/g/security
			  endpoint /docs/r/endpoint
			  tools /docs/r/tools
			all: apm-agent /docs/r/apm-agent, endpoint /docs/r/endpoint, standalone /docs/r/standalone, tools /docs/r/tools, uptime /docs/r/uptime
			"""
			);
	}

	[Test]
	public async Task Generate_OutputTree_MatchesSnapshot()
	{
		var output = await GenerateAsync();

		// the embedded static files are listed by the codex static resources test
		var files = output
			.AllFiles
			.Select(f => output.Path.GetRelativePath("/codex-output", f).Replace('\\', '/'))
			.Where(f => !f.Contains("/_static/", StringComparison.Ordinal))
			.Order(StringComparer.Ordinal);
		string
			.Join('\n', files)
			.Should()
			.MatchInlineSnapshot(
				"""
			docs/g/observability/index.html
			docs/g/security/index.html
			docs/index.html
			"""
			);
	}

	[Test]
	public async Task Generate_LandingPage_Grid_MatchesSnapshot()
	{
		var output = await GenerateAsync();

		output
			.File
			.ReadAllText("/codex-output/docs/index.html")
			.Should()
			.MatchHtmlSnapshot(DocsHtml.Body().Select("section#elastic-docs-v3"));
	}

	[Test]
	public async Task Generate_GroupLandingPage_ContentSection_MatchesSnapshot()
	{
		var output = await GenerateAsync();

		output
			.File
			.ReadAllText("/codex-output/docs/g/observability/index.html")
			.Should()
			.MatchHtmlSnapshot(DocsHtml.Body().Select("section#elastic-docs-v3"));
	}

	private async Task<MockFileSystem> GenerateAsync()
	{
		var output = new MockFileSystem();
		var context = new BuildContext(
			new DiagnosticsCollector([]),
			DocumentationFileSystem.Resolve(Paths.WorkingDirectoryRoot.FullName),
			CreateConfigurationContext()
		)
		{ UrlPathPrefix = "/docs" };
		await new CodexGenerator(NullLoggerFactory.Instance, context, output.DirectoryInfo.New("/codex-output")).Generate(
			CreateNavigation(),
			TestContext.Current!.Execution.CancellationToken
		);
		return output;
	}

	private static ConfigurationContext CreateConfigurationContext() =>
		new()
		{
			Endpoints = new DocumentationEndpoints { Elasticsearch = ElasticsearchEndpoint.Default },
			ConfigurationFileProvider = new ConfigurationFileProvider(
				NullLoggerFactory.Instance,
				new ConfigurationFileSystem(new FileSystem())
			),
			Catalog = new GlobalCatalog(
				new VersioningSystemCatalog { VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>() },
				new ProductCatalog { Products = FrozenDictionary<string, Product>.Empty },
				new LegacyUrlMappingCatalog { Mappings = [] }
			),
			SearchConfiguration = new SearchConfiguration { Synonyms = [], Rules = [], DiminishTerms = [] }
		};
}
