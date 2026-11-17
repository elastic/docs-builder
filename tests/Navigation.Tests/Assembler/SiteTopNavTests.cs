// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Navigation.Floating;

namespace Elastic.Documentation.Navigation.Tests.Assembler;

/// <summary>What <c>SectionTopNavBuilder</c> makes of each shape of <c>section:</c> entry, with the navigation-preview flag on and off.</summary>
public class SiteTopNavTests
{
	private const string AllShapes =
		// language=yaml
		"""
		toc:
		  - section: Guides
		    children:
		      - toc: docs-content://get-started
		        path_prefix: get-started
		  - section: APIs
		    external: /api/
		    floating: api
		  - section: Products
		    dropdown:
		      - title: Kibana
		        url: /kibana/
		      - title: Elasticsearch
		        url: elasticsearch
		  - section: Reference
		    children:
		      - toc: docs-content://reference
		        path_prefix: reference
		  - section: Elastic
		    external: https://www.elastic.co
		  - section: Empty section
		    children:
		      - toc: nonexistent-repo://
		        path_prefix: ghost
		  - toc: kibana://
		    path_prefix: kibana
		""";

	private static SiteLayoutResult Build(
		string yaml,
		bool preview = true,
		string? sitePrefix = "docs",
		FloatingSectionRegistry? floating = null
	) =>
		SiteLayout.Build(
			SiteNavigationTestFixture.CreateExtendedRepositoryFileSystem(),
			yaml,
			sitePrefix,
			preview,
			floating,
			declareUnmountedAsPhantoms: true,
			repositories: ["docs-content", "kibana", "apm-server", "apm-agents-java", "elasticsearch", "unlisted"]
		);

	[Test]
	public void TopNav_EveryTabShape_MatchesSnapshot() =>
		BaselineDumps
			.TopNavToText(Build(AllShapes).Navigation.TopNav)
			.Should()
			.MatchInlineSnapshot(
				"""
			link Guides -> /docs/get-started [section]
			link APIs -> /docs/api/
			dropdown Products
			  group (no label)
			    link Kibana -> /docs/kibana/
			    link Elasticsearch -> /docs/elasticsearch
			link Reference -> /docs/reference [section]
			link Elastic -> https://www.elastic.co [external]
			"""
			);

	[Test]
	public void TopNav_FlagOff_IsAbsent() =>
		BaselineDumps.TopNavToText(Build(AllShapes, preview: false).Navigation.TopNav).Should().MatchInlineSnapshot("(no top nav)");

	[Test]
	public void TopNav_FloatingHubWithSections_LinksTheTabToTheHub() =>
		BaselineDumps
			.TopNavToText(Build(AllShapes, floating: new FloatingSectionRegistry([new DemoProvider()])).Navigation.TopNav)
			.Should()
			.MatchInlineSnapshot(
				"""
				link Guides -> /docs/get-started [section]
				link APIs -> /docs/api/
				dropdown Products
				  group (no label)
				    link Kibana -> /docs/kibana/
				    link Elasticsearch -> /docs/elasticsearch
				link Reference -> /docs/reference [section]
				link Elastic -> https://www.elastic.co [external]
				"""
			);

	[Test]
	public void TopNav_FloatingHubWithoutExternalLink_IsOmittedWhenNothingFloats() =>
		BaselineDumps
			.TopNavToText(
				Build(
					// language=yaml
					"""
					toc:
					  - section: Guides
					    children:
					      - toc: docs-content://get-started
					        path_prefix: get-started
					  - section: APIs
					    floating: api
					"""
				).Navigation.TopNav
			)
			.Should()
			.MatchInlineSnapshot("link Guides -> /docs/get-started [section]");

	[Test]
	public void TopNav_NoSitePrefix_LinksStartAtTheRoot() =>
		BaselineDumps
			.TopNavToText(Build(AllShapes, sitePrefix: null).Navigation.TopNav)
			.Should()
			.MatchInlineSnapshot(
				"""
			link Guides -> /get-started [section]
			link APIs -> /api/
			dropdown Products
			  group (no label)
			    link Kibana -> /kibana/
			    link Elasticsearch -> /elasticsearch
			link Reference -> /reference [section]
			link Elastic -> https://www.elastic.co [external]
			"""
			);

	[Test]
	public void TopNav_OnlyLeftoverTocEntries_IsAbsent() =>
		BaselineDumps
			.TopNavToText(
				Build(
					// language=yaml
					"""
					toc:
					  - toc: kibana://
					    path_prefix: kibana
					"""
				).Navigation.TopNav
			)
			.Should()
			.MatchInlineSnapshot("(no top nav)");

	[Test]
	public void Tree_EveryTabShape_MatchesSnapshot() =>
		Build(AllShapes, floating: new FloatingSectionRegistry([new DemoProvider()]))
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
			  SectionNavigation  /docs/api  "APIs" [island]
			    index: SectionIndexLeaf  /docs/api  "APIs"
			    DemoRoot  /api/doc/demo  "Demo API" [island]
			      index: DemoLandingLeaf  /api/doc/demo  "Demo API"
			  SectionNavigation  /docs/reference  "Reference" [island]
			    index: SectionIndexLeaf  /docs/reference  "Reference"
			    TableOfContentsNavigation  /docs/reference  "docs-content: reference/index"
			      index: FileNavigationLeaf  /docs/reference  "docs-content: reference/index"
			      FolderNavigation  /docs/reference/glossary  "docs-content: reference/glossary/index" [island]
			        index: FileNavigationLeaf  /docs/reference/glossary  "docs-content: reference/glossary/index"
			        FileNavigationLeaf  /docs/reference/glossary/draft  "docs-content: reference/glossary/draft" [hidden]
			        FileNavigationLeaf  /docs/reference/glossary/terms  "docs-content: reference/glossary/terms"
			  SectionNavigation  /  "Empty section" [island]
			    index: SectionIndexLeaf  /  "Empty section"
			  DocumentationSetNavigation  /docs/kibana  "kibana: index" [island]
			    index: FileNavigationLeaf  /docs/kibana  "kibana: index"
			    FileNavigationLeaf  /docs/kibana/dashboards  "kibana: dashboards"
			--- diagnostics
			error /docs/navigation.yml Could not find navigation node for identifier: nonexistent-repo:/// (from source: nonexistent-repo:///)
			"""
			);
}
