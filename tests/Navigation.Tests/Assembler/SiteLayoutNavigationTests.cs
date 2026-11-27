// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Elastic.Documentation.Navigation.Floating;

namespace Elastic.Documentation.Navigation.Tests.Assembler;

/// <summary>
/// Small multi-repository layouts, each snapshotted as its tree and the diagnostics the assembly emits. These record what the
/// navigation does today with mounts at path prefixes, nested tocs, hidden pages, duplicate and missing mounts, and islands.
/// </summary>
public class SiteLayoutNavigationTests
{
	private static SiteLayoutResult Build(string navigationYaml, string? sitePrefix = "docs", params string[] repositories) =>
		SiteLayout.Build(
			SiteNavigationTestFixture.CreateExtendedRepositoryFileSystem(),
			navigationYaml,
			sitePrefix,
			declareUnmountedAsPhantoms: true,
			repositories: repositories
		);

	[Test]
	public void Mounts_AtPathPrefixes_AreRootedUnderTheSitePrefix() =>
		Build(
			// language=yaml
			"""
				toc:
				  - toc: kibana://
				    path_prefix: reference/kibana
				  - toc: elasticsearch://reference
				    path_prefix: reference/elasticsearch
				"""
		)
			.ToText()
			.Should()
			.MatchInlineSnapshot(
				"""
			SiteNavigation  /docs  "Docs"
			  index: FileNavigationLeaf  /docs  "docs-content: index"
			  FileNavigationLeaf  /docs/about  "docs-content: about" [wrong parent]
			  FileNavigationLeaf  /docs/legal  "docs-content: legal" [hidden] [wrong parent]
			  DocumentationSetNavigation  /docs/reference/kibana  "kibana: index" [island]
			    index: FileNavigationLeaf  /docs/reference/kibana  "kibana: index"
			    FileNavigationLeaf  /docs/reference/kibana/dashboards  "kibana: dashboards"
			  TableOfContentsNavigation  /docs/reference/elasticsearch  "elasticsearch: reference/index" [island]
			    index: FileNavigationLeaf  /docs/reference/elasticsearch  "elasticsearch: reference/index"
			    FileNavigationLeaf  /docs/reference/elasticsearch/rest-apis  "elasticsearch: reference/rest-apis"
			"""
			);

	[Test]
	public void Mounts_WherePrefixesStartTheSame_StayApart() =>
		Build(
			// language=yaml
			"""
				toc:
				  - toc: apm-server://
				    path_prefix: reference/apm
				  - toc: apm-agents-java://
				    path_prefix: reference/apm-agents/java
				"""
		)
			.ToText()
			.Should()
			.MatchInlineSnapshot(
				"""
			SiteNavigation  /docs  "Docs"
			  index: FileNavigationLeaf  /docs  "docs-content: index"
			  FileNavigationLeaf  /docs/about  "docs-content: about" [wrong parent]
			  FileNavigationLeaf  /docs/legal  "docs-content: legal" [hidden] [wrong parent]
			  DocumentationSetNavigation  /docs/reference/apm  "apm-server: index" [island]
			    index: FileNavigationLeaf  /docs/reference/apm  "apm-server: index"
			    FileNavigationLeaf  /docs/reference/apm/internal  "apm-server: internal" [hidden]
			    FolderNavigation  /docs/reference/apm/setup  "apm-server: setup/index"
			      index: FileNavigationLeaf  /docs/reference/apm/setup  "apm-server: setup/index"
			      FileNavigationLeaf  /docs/reference/apm/setup/secret  "apm-server: setup/secret" [hidden]
			      FileNavigationLeaf  /docs/reference/apm/setup/install  "apm-server: setup/install"
			  DocumentationSetNavigation  /docs/reference/apm-agents/java  "apm-agents-java: index" [island]
			    index: FileNavigationLeaf  /docs/reference/apm-agents/java  "apm-agents-java: index"
			    FileNavigationLeaf  /docs/reference/apm-agents/java/setup  "apm-agents-java: setup"
			"""
			);

	[Test]
	public void NestedTocMounts_ChildrenHangUnderTheirParentMount() =>
		Build(
			// language=yaml
			"""
				toc:
				  - toc: docs-content://get-started
				    path_prefix: get-started
				  - toc: docs-content://reference
				    path_prefix: reference
				    children:
				      - toc: docs-content://reference/apm
				        path_prefix: reference/apm
				        children:
				          - toc: apm-server://
				            path_prefix: reference/apm/server
				      - toc: docs-content://reference/apm-agents
				        path_prefix: reference/apm-agents
				        children:
				          - toc: apm-agents-java://
				            path_prefix: reference/apm-agents/java
				  - toc: docs-content://release-notes
				    path_prefix: release-notes
				    children:
				      - toc: docs-content://release-notes/elasticsearch-clients
				        path_prefix: release-notes/elasticsearch/clients
				      - toc: elasticsearch://release-notes
				        path_prefix: release-notes/elasticsearch
				"""
		)
			.ToText()
			.Should()
			.MatchInlineSnapshot(
				"""
			SiteNavigation  /docs  "Docs"
			  index: FileNavigationLeaf  /docs  "docs-content: index"
			  FileNavigationLeaf  /docs/about  "docs-content: about" [wrong parent]
			  FileNavigationLeaf  /docs/legal  "docs-content: legal" [hidden] [wrong parent]
			  TableOfContentsNavigation  /docs/get-started  "docs-content: get-started/index" [island]
			    index: FileNavigationLeaf  /docs/get-started  "docs-content: get-started/index"
			    FileNavigationLeaf  /docs/get-started/tutorial  "docs-content: get-started/tutorial"
			  TableOfContentsNavigation  /docs/reference  "docs-content: reference/index" [island]
			    index: FileNavigationLeaf  /docs/reference  "docs-content: reference/index"
			    FolderNavigation  /docs/reference/glossary  "docs-content: reference/glossary/index"
			      index: FileNavigationLeaf  /docs/reference/glossary  "docs-content: reference/glossary/index"
			      FileNavigationLeaf  /docs/reference/glossary/draft  "docs-content: reference/glossary/draft" [hidden]
			      FileNavigationLeaf  /docs/reference/glossary/terms  "docs-content: reference/glossary/terms"
			    TableOfContentsNavigation  /docs/reference/apm  "docs-content: reference/apm/index"
			      index: FileNavigationLeaf  /docs/reference/apm  "docs-content: reference/apm/index"
			      FileNavigationLeaf  /docs/reference/apm/overview  "docs-content: reference/apm/overview"
			      DocumentationSetNavigation  /docs/reference/apm/server  "apm-server: index"
			        index: FileNavigationLeaf  /docs/reference/apm/server  "apm-server: index"
			        FileNavigationLeaf  /docs/reference/apm/server/internal  "apm-server: internal" [hidden]
			        FolderNavigation  /docs/reference/apm/server/setup  "apm-server: setup/index"
			          index: FileNavigationLeaf  /docs/reference/apm/server/setup  "apm-server: setup/index"
			          FileNavigationLeaf  /docs/reference/apm/server/setup/secret  "apm-server: setup/secret" [hidden]
			          FileNavigationLeaf  /docs/reference/apm/server/setup/install  "apm-server: setup/install"
			    TableOfContentsNavigation  /docs/reference/apm-agents  "docs-content: reference/apm-agents/index"
			      index: FileNavigationLeaf  /docs/reference/apm-agents  "docs-content: reference/apm-agents/index"
			      FileNavigationLeaf  /docs/reference/apm-agents/overview  "docs-content: reference/apm-agents/overview"
			      DocumentationSetNavigation  /docs/reference/apm-agents/java  "apm-agents-java: index"
			        index: FileNavigationLeaf  /docs/reference/apm-agents/java  "apm-agents-java: index"
			        FileNavigationLeaf  /docs/reference/apm-agents/java/setup  "apm-agents-java: setup"
			  TableOfContentsNavigation  /docs/release-notes  "docs-content: release-notes/index" [island]
			    index: FileNavigationLeaf  /docs/release-notes  "docs-content: release-notes/index"
			    TableOfContentsNavigation  /docs/release-notes/elasticsearch/clients  "docs-content: release-notes/elasticsearch-clients/index"
			      index: FileNavigationLeaf  /docs/release-notes/elasticsearch/clients  "docs-content: release-notes/elasticsearch-clients/index"
			      FileNavigationLeaf  /docs/release-notes/elasticsearch/clients/overview  "docs-content: release-notes/elasticsearch-clients/overview"
			    TableOfContentsNavigation  /docs/release-notes/elasticsearch  "elasticsearch: release-notes/index"
			      index: FileNavigationLeaf  /docs/release-notes/elasticsearch  "elasticsearch: release-notes/index"
			      FileNavigationLeaf  /docs/release-notes/elasticsearch/changelog  "elasticsearch: release-notes/changelog"
			"""
			);

	[Test]
	public void NarrativeRepository_RootPagesBecomeTheSiteRootPages() =>
		Build(
			// language=yaml
			"""
				toc:
				  - toc: get-started
				"""
		)
			.ToText()
			.Should()
			.MatchInlineSnapshot(
				"""
			SiteNavigation  /docs  "Docs"
			  index: FileNavigationLeaf  /docs  "docs-content: index"
			  FileNavigationLeaf  /docs/about  "docs-content: about" [wrong parent]
			  FileNavigationLeaf  /docs/legal  "docs-content: legal" [hidden] [wrong parent]
			  TableOfContentsNavigation  /docs/get-started  "docs-content: get-started/index" [island]
			    index: FileNavigationLeaf  /docs/get-started  "docs-content: get-started/index"
			    FileNavigationLeaf  /docs/get-started/tutorial  "docs-content: get-started/tutorial"
			"""
			);

	[Test]
	public void HiddenPages_AreFlaggedInTheTree() =>
		Build(
			// language=yaml
			"""
				toc:
				  - toc: docs-content://reference
				    path_prefix: reference
				  - toc: apm-server://
				    path_prefix: apm
				"""
		)
			.ToText()
			.Should()
			.MatchInlineSnapshot(
				"""
			SiteNavigation  /docs  "Docs"
			  index: FileNavigationLeaf  /docs  "docs-content: index"
			  FileNavigationLeaf  /docs/about  "docs-content: about" [wrong parent]
			  FileNavigationLeaf  /docs/legal  "docs-content: legal" [hidden] [wrong parent]
			  TableOfContentsNavigation  /docs/reference  "docs-content: reference/index" [island]
			    index: FileNavigationLeaf  /docs/reference  "docs-content: reference/index"
			    FolderNavigation  /docs/reference/glossary  "docs-content: reference/glossary/index"
			      index: FileNavigationLeaf  /docs/reference/glossary  "docs-content: reference/glossary/index"
			      FileNavigationLeaf  /docs/reference/glossary/draft  "docs-content: reference/glossary/draft" [hidden]
			      FileNavigationLeaf  /docs/reference/glossary/terms  "docs-content: reference/glossary/terms"
			  DocumentationSetNavigation  /docs/apm  "apm-server: index" [island]
			    index: FileNavigationLeaf  /docs/apm  "apm-server: index"
			    FileNavigationLeaf  /docs/apm/internal  "apm-server: internal" [hidden]
			    FolderNavigation  /docs/apm/setup  "apm-server: setup/index"
			      index: FileNavigationLeaf  /docs/apm/setup  "apm-server: setup/index"
			      FileNavigationLeaf  /docs/apm/setup/secret  "apm-server: setup/secret" [hidden]
			      FileNavigationLeaf  /docs/apm/setup/install  "apm-server: setup/install"
			"""
			);

	[Test]
	public void DuplicateMount_TheSameTocMountedTwice_RecordsOneMount() =>
		Build(
			// language=yaml
			"""
				toc:
				  - toc: kibana://
				    path_prefix: first
				  - toc: kibana://
				    path_prefix: second
				"""
		)
			.ToText()
			.Should()
			.MatchInlineSnapshot(
				"""
			SiteNavigation  /docs  "Docs"
			  index: FileNavigationLeaf  /docs  "docs-content: index"
			  FileNavigationLeaf  /docs/about  "docs-content: about" [wrong parent]
			  FileNavigationLeaf  /docs/legal  "docs-content: legal" [hidden] [wrong parent]
			  DocumentationSetNavigation  /docs/second  "kibana: index" [island]
			    index: FileNavigationLeaf  /docs/second  "kibana: index"
			    FileNavigationLeaf  /docs/second/dashboards  "kibana: dashboards"
			"""
			);

	[Test]
	public void DuplicateMount_ValidatePathPrefixes_ReportsTheCollision()
	{
		var result = Build(
			// language=yaml
			"""
			toc:
			  - toc: kibana://
			    path_prefix: shared
			  - toc: apm-server://
			    path_prefix: shared
			""",
			repositories: ["kibana", "apm-server"]
		);
		var collector = new TestDiagnosticsCollector();
		var navigationFile = result.SiteContext.ConfigurationPath;

		var valid = Elastic.Documentation.Configuration.Toc.SiteNavigationFile.ValidatePathPrefixes(collector, result.File, navigationFile);

		valid.Should().BeFalse();
		// which of the two a message names first follows the hash order of a set of Uris, which changes with every process
		var messages = collector.Diagnostics.Select(
			d => string.Join(
				" | ",
				Regex
					.Matches(d.Message, "[a-z-]+://shared/", RegexOptions.None, RegexTimeouts.MatchTimeout)
					.Select(m => m.Value)
					.Order(StringComparer.Ordinal)
			)
		);
		string.Join("\n", messages).Should().MatchInlineSnapshot("apm-server://shared/ | kibana://shared/");
	}

	[Test]
	public void MissingMount_NoDocsetForTheRepository_ReportsAnErrorAndSkipsIt() =>
		Build(
			// language=yaml
			"""
				toc:
				  - toc: kibana://
				    path_prefix: kibana
				  - toc: nonexistent-repo://
				    path_prefix: ghost
				""",
			repositories: ["kibana"]
		)
			.ToText()
			.Should()
			.MatchInlineSnapshot(
				"""
			SiteNavigation  /docs  "Docs"
			  index: FileNavigationLeaf  /docs/kibana  "kibana: index"
			  DocumentationSetNavigation  /docs/kibana  "kibana: index" [island]
			    index: FileNavigationLeaf  /docs/kibana  "kibana: index"
			    FileNavigationLeaf  /docs/kibana/dashboards  "kibana: dashboards"
			--- diagnostics
			error /docs/navigation.yml Could not find navigation node for identifier: nonexistent-repo:/// (from source: nonexistent-repo:///)
			"""
			);

	[Test]
	public void MissingMount_NoPathPrefixOutsideTheNarrativeRepository_FallsBackToABadMapping() =>
		Build(
			// language=yaml
			"""
				toc:
				  - toc: kibana://
				""",
			repositories: ["kibana"]
		)
			.ToText()
			.Should()
			.MatchInlineSnapshot(
				"""
			SiteNavigation  /docs  "Docs"
			  index: FileNavigationLeaf  /docs/bad-mapping-kibana  "kibana: index"
			  DocumentationSetNavigation  /docs/bad-mapping-kibana  "kibana: index" [island]
			    index: FileNavigationLeaf  /docs/bad-mapping-kibana  "kibana: index"
			    FileNavigationLeaf  /docs/bad-mapping-kibana/dashboards  "kibana: dashboards"
			--- diagnostics
			error /docs/navigation.yml path_prefix is required for TOC reference: kibana:///
			"""
			);

	[Test]
	public void UnmountedRepositories_EmitAHintUnlessTheyAreDeclaredPhantoms() =>
		SiteLayout
			.Build(
				SiteNavigationTestFixture.CreateExtendedRepositoryFileSystem(),
				// language=yaml
				"""
				toc:
				  - toc: kibana://
				    path_prefix: kibana
				phantoms:
				  - toc: unlisted://
				""",
				repositories: ["kibana", "unlisted", "apm-server"]
			)
			.ToText()
			.Should()
			.MatchInlineSnapshot(
				"""
				SiteNavigation  /docs  "Docs"
				  index: FileNavigationLeaf  /docs/kibana  "kibana: index"
				  DocumentationSetNavigation  /docs/kibana  "kibana: index" [island]
				    index: FileNavigationLeaf  /docs/kibana  "kibana: index"
				    FileNavigationLeaf  /docs/kibana/dashboards  "kibana: dashboards"
				--- diagnostics
				hint /docs/navigation.yml Navigation does not explicitly declare: apm-server:/// as a phantom
				"""
			);

	[Test]
	public void UndeclaredNestedToc_InsideAMount_IsReportedAndStaysOutOfTheTree() =>
		SiteLayout
			.Build(
				SiteNavigationTestFixture.CreateExtendedRepositoryFileSystem(),
				// language=yaml
				"""
				toc:
				  - toc: docs-content://reference
				    path_prefix: reference
				phantoms:
				  - toc: docs-content://
				""",
				repositories: ["docs-content"]
			)
			.ToText()
			.Should()
			.MatchInlineSnapshot(
				"""
				SiteNavigation  /docs  "Docs"
				  index: FileNavigationLeaf  /docs  "docs-content: index"
				  FileNavigationLeaf  /docs/about  "docs-content: about" [wrong parent]
				  FileNavigationLeaf  /docs/legal  "docs-content: legal" [hidden] [wrong parent]
				  TableOfContentsNavigation  /docs/reference  "docs-content: reference/index" [island]
				    index: FileNavigationLeaf  /docs/reference  "docs-content: reference/index"
				    FolderNavigation  /docs/reference/glossary  "docs-content: reference/glossary/index"
				      index: FileNavigationLeaf  /docs/reference/glossary  "docs-content: reference/glossary/index"
				      FileNavigationLeaf  /docs/reference/glossary/draft  "docs-content: reference/glossary/draft" [hidden]
				      FileNavigationLeaf  /docs/reference/glossary/terms  "docs-content: reference/glossary/terms"
				--- diagnostics
				warning /docs/navigation.yml Navigation does not explicitly declare: docs-content://reference/apm
				warning /docs/navigation.yml Navigation does not explicitly declare: docs-content://reference/apm-agents
				hint /docs/navigation.yml Navigation does not explicitly declare: docs-content://get-started/ as a phantom
				hint /docs/navigation.yml Navigation does not explicitly declare: docs-content://reference/apm as a phantom
				hint /docs/navigation.yml Navigation does not explicitly declare: docs-content://reference/apm-agents as a phantom
				hint /docs/navigation.yml Navigation does not explicitly declare: docs-content://release-notes/ as a phantom
				hint /docs/navigation.yml Navigation does not explicitly declare: docs-content://release-notes/elasticsearch-clients as a phantom
				"""
			);

	[Test]
	public void Islands_DeclaredInTheNavigationFile_MarkNestedMounts() =>
		Build(
			// language=yaml
			"""
				toc:
				  - toc: docs-content://reference
				    path_prefix: reference
				    island: true
				    children:
				      - toc: docs-content://reference/apm
				        path_prefix: reference/apm
				        island: true
				      - toc: docs-content://reference/apm-agents
				        path_prefix: reference/apm-agents
				""",
			repositories: ["docs-content"]
		)
			.ToText()
			.Should()
			.MatchInlineSnapshot(
				"""
			SiteNavigation  /docs  "Docs"
			  index: FileNavigationLeaf  /docs  "docs-content: index"
			  FileNavigationLeaf  /docs/about  "docs-content: about" [wrong parent]
			  FileNavigationLeaf  /docs/legal  "docs-content: legal" [hidden] [wrong parent]
			  TableOfContentsNavigation  /docs/reference  "docs-content: reference/index" [island]
			    index: FileNavigationLeaf  /docs/reference  "docs-content: reference/index"
			    FolderNavigation  /docs/reference/glossary  "docs-content: reference/glossary/index"
			      index: FileNavigationLeaf  /docs/reference/glossary  "docs-content: reference/glossary/index"
			      FileNavigationLeaf  /docs/reference/glossary/draft  "docs-content: reference/glossary/draft" [hidden]
			      FileNavigationLeaf  /docs/reference/glossary/terms  "docs-content: reference/glossary/terms"
			    TableOfContentsNavigation  /docs/reference/apm  "docs-content: reference/apm/index" [island]
			      index: FileNavigationLeaf  /docs/reference/apm  "docs-content: reference/apm/index"
			      FileNavigationLeaf  /docs/reference/apm/overview  "docs-content: reference/apm/overview"
			    TableOfContentsNavigation  /docs/reference/apm-agents  "docs-content: reference/apm-agents/index"
			      index: FileNavigationLeaf  /docs/reference/apm-agents  "docs-content: reference/apm-agents/index"
			      FileNavigationLeaf  /docs/reference/apm-agents/overview  "docs-content: reference/apm-agents/overview"
			"""
			);

	[Test]
	public void SitePrefix_Variants_ChangeOnlyTheLeadingPath()
	{
		var sb = new StringBuilder();
		foreach (var prefix in new string?[] { null, "", "/", "docs", "/docs", "/docs/", "a/b", " docs " })
		{
			var result = Build(
				// language=yaml
				"""
				toc:
				  - toc: kibana://
				    path_prefix: /kibana/
				""",
				prefix,
				"kibana"
			);
			_ = sb
				.Append('[')
				.Append(prefix ?? "null")
				.Append("] site=")
				.Append(result.Navigation.Url)
				.Append(" mount=")
				.Append(result.Navigation.NavigationItems.OfType<INavigationItem>().Last().Url)
				.Append('\n');
		}
		sb
			.ToString()
			.Should()
			.MatchInlineSnapshot(
				"""
			[null] site=/ mount=/kibana
			[] site=/ mount=/kibana
			[/] site=/ mount=/kibana
			[docs] site=/docs mount=/docs/kibana
			[/docs] site=/docs mount=/docs/kibana
			[/docs/] site=/docs mount=/docs/kibana
			[a/b] site=/a/b mount=/a/b/kibana
			[ docs ] site=/docs mount=/docs/kibana
			"""
			);
	}

	[Test]
	public void Sections_WithChildren_BecomeIslandsAndPromoteTheirListing() =>
		Build(
			// language=yaml
			"""
				toc:
				  - section: Guides
				    children:
				      - toc: docs-content://get-started
				        path_prefix: get-started
				  - section: Reference
				    children:
				      - toc: docs-content://reference
				        path_prefix: reference
				        children:
				          - toc: docs-content://reference/apm
				            path_prefix: reference/apm
				          - toc: kibana://
				            path_prefix: reference/kibana
				"""
		)
			.ToText()
			.Should()
			.MatchInlineSnapshot(
				"""
			SiteNavigation  /docs  "Docs"
			  index: FileNavigationLeaf  /docs  "docs-content: index"
			  FileNavigationLeaf  /docs/about  "docs-content: about" [wrong parent]
			  FileNavigationLeaf  /docs/legal  "docs-content: legal" [hidden] [wrong parent]
			  SectionNavigation  /docs/get-started  "Guides" [island]
			    index: SectionIndexLeaf  /docs/get-started  "Guides"
			    TableOfContentsNavigation  /docs/get-started  "docs-content: get-started/index"
			      index: FileNavigationLeaf  /docs/get-started  "docs-content: get-started/index"
			      FileNavigationLeaf  /docs/get-started/tutorial  "docs-content: get-started/tutorial"
			  SectionNavigation  /docs/reference  "Reference" [island]
			    index: SectionIndexLeaf  /docs/reference  "Reference"
			    TableOfContentsNavigation  /docs/reference  "docs-content: reference/index"
			      index: FileNavigationLeaf  /docs/reference  "docs-content: reference/index"
			      FolderNavigation  /docs/reference/glossary  "docs-content: reference/glossary/index" [island]
			        index: FileNavigationLeaf  /docs/reference/glossary  "docs-content: reference/glossary/index"
			        FileNavigationLeaf  /docs/reference/glossary/draft  "docs-content: reference/glossary/draft" [hidden]
			        FileNavigationLeaf  /docs/reference/glossary/terms  "docs-content: reference/glossary/terms"
			      TableOfContentsNavigation  /docs/reference/apm  "docs-content: reference/apm/index" [island]
			        index: FileNavigationLeaf  /docs/reference/apm  "docs-content: reference/apm/index"
			        FileNavigationLeaf  /docs/reference/apm/overview  "docs-content: reference/apm/overview"
			      DocumentationSetNavigation  /docs/reference/kibana  "kibana: index" [island]
			        index: FileNavigationLeaf  /docs/reference/kibana  "kibana: index"
			        FileNavigationLeaf  /docs/reference/kibana/dashboards  "kibana: dashboards"
			"""
			);

	[Test]
	public void FloatingHub_WithSections_MountsTheirRootsUnderTheHub() =>
		SiteLayout
			.Build(
				SiteNavigationTestFixture.CreateExtendedRepositoryFileSystem(),
				// language=yaml
				"""
				toc:
				  - toc: kibana://
				    path_prefix: kibana
				  - section: APIs
				    external: /api/
				    floating: api
				""",
				floatingSections: new FloatingSectionRegistry([new DemoProvider()]),
				declareUnmountedAsPhantoms: true
			)
			.ToText()
			.Should()
			.MatchInlineSnapshot(
				"""
				SiteNavigation  /docs  "Docs"
				  index: FileNavigationLeaf  /docs  "docs-content: index"
				  FileNavigationLeaf  /docs/about  "docs-content: about" [wrong parent]
				  FileNavigationLeaf  /docs/legal  "docs-content: legal" [hidden] [wrong parent]
				  DocumentationSetNavigation  /docs/kibana  "kibana: index" [island]
				    index: FileNavigationLeaf  /docs/kibana  "kibana: index"
				    FileNavigationLeaf  /docs/kibana/dashboards  "kibana: dashboards"
				  SectionNavigation  /docs/api  "APIs" [island]
				    index: SectionIndexLeaf  /docs/api  "APIs"
				    DemoRoot  /api/demo  "Demo API" [island]
				      index: DemoLandingLeaf  /api/demo  "Demo API"
				"""
			);
}
