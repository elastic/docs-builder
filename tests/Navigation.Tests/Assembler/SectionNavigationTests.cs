// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Documentation.Navigation.Isolated.Node;
using Elastic.Documentation.Site.Navigation;

namespace Elastic.Documentation.Navigation.Tests.Assembler;

/// <summary>
/// Tests for the <see cref="SectionNavigation"/> tree node created from
/// <c>section:</c> entries with <c>children:</c> in navigation.yml.
/// </summary>
public class SectionNavigationTests(ITestOutputHelper output)
{
	// ──────────────────────────────────────────────────────────────
	// Helpers
	// ──────────────────────────────────────────────────────────────

	private static (SiteNavigation, DocumentationSetNavigation<IDocumentationFile>, DocumentationSetNavigation<IDocumentationFile>) BuildTwoChildSection(
		ITestOutputHelper output,
		string siteNavYaml
	)
	{
		var siteNavFile = SiteNavigationFile.Deserialize(siteNavYaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		var obsCtx = SiteNavigationTestFixture.CreateAssemblerContext(fileSystem, "/checkouts/current/observability", output);
		var obsDocset = DocumentationSetFile.LoadAndResolve(
			obsCtx.Collector,
			fileSystem.FileInfo.New("/checkouts/current/observability/docs/docset.yml"),
			new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
		);
		var obsNav = new DocumentationSetNavigation<IDocumentationFile>(obsDocset, obsCtx, GenericDocumentationFileFactory.Instance);

		var searchCtx = SiteNavigationTestFixture.CreateAssemblerContext(fileSystem, "/checkouts/current/serverless-search", output);
		var searchDocset = DocumentationSetFile.LoadAndResolve(
			searchCtx.Collector,
			fileSystem.FileInfo.New("/checkouts/current/serverless-search/docs/docset.yml"),
			new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
		);
		var searchNav = new DocumentationSetNavigation<IDocumentationFile>(
			searchDocset,
			searchCtx,
			GenericDocumentationFileFactory.Instance
		);

		var siteCtx = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability", output);
		var navigation = new SiteNavigation(siteNavFile, siteCtx, [obsNav, searchNav], sitePrefix: "/docs");
		return (navigation, obsNav, searchNav);
	}

	// ──────────────────────────────────────────────────────────────
	// Tree structure
	// ──────────────────────────────────────────────────────────────

	[Fact]
	public void SectionWithChildren_CreatesSectionNavigationNode()
	{
		// language=yaml
		var yaml =
			"""
		           toc:
		             - section: Guides
		               children:
		                 - toc: observability://
		                   path_prefix: /observability
		                 - toc: serverless-search://
		                   path_prefix: /search
		           """;

		var (nav, _, _) = BuildTwoChildSection(output, yaml);

		// Top-level should have exactly one item: the SectionNavigation
		nav.NavigationItems.Should().HaveCount(1);
		var section = nav.NavigationItems.First().Should().BeOfType<SectionNavigation>().Subject;

		section.Title.Should().Be("Guides");
		section.NavigationItems.Should().HaveCount(2);
	}

	[Fact]
	public void SectionNavigationNode_IsIsland_AndParentIsSiteNavigation()
	{
		// language=yaml
		var yaml =
			"""
		           toc:
		             - section: Guides
		               children:
		                 - toc: observability://
		                   path_prefix: /observability
		                 - toc: serverless-search://
		                   path_prefix: /search
		           """;

		var (nav, _, _) = BuildTwoChildSection(output, yaml);

		var section = nav.NavigationItems.First().Should().BeOfType<SectionNavigation>().Subject;

		section.IsIsland.Should().BeTrue();
		section.Parent.Should().BeSameAs(nav, "SectionNavigation parent must be SiteNavigation");
		section.RendersAsIsland().Should().BeTrue("island + non-null parent");
	}

	[Fact]
	public void SectionChildren_AreNotIslands_SectionIsTheIsland()
	{
		// language=yaml
		var yaml =
			"""
		           toc:
		             - section: Guides
		               children:
		                 - toc: observability://
		                   path_prefix: /observability
		                 - toc: serverless-search://
		                   path_prefix: /search
		           """;

		var (nav, obsNav, searchNav) = BuildTwoChildSection(output, yaml);

		var section = nav.NavigationItems.First().Should().BeOfType<SectionNavigation>().Subject;

		// Children are branches within the section island, not individual islands.
		obsNav.IsIsland.Should().BeFalse("children of a section are not individual islands; the section owns the sidebar");
		obsNav.Parent.Should().BeSameAs(section, "child docset parent must be SectionNavigation");
		obsNav.RendersAsIsland().Should().BeFalse("child is not an island, even with a parent");

		searchNav.IsIsland.Should().BeFalse();
		searchNav.Parent.Should().BeSameAs(section);
	}

	// ──────────────────────────────────────────────────────────────
	// FindIslandRoot: returns child docset, not the section
	// ──────────────────────────────────────────────────────────────

	[Fact]
	public void FindIslandRoot_FromDeepPage_ReturnsSectionNavigation()
	{
		// language=yaml
		var yaml =
			"""
		           toc:
		             - section: Guides
		               children:
		                 - toc: observability://
		                   path_prefix: /observability
		                 - toc: serverless-search://
		                   path_prefix: /search
		           """;

		var (nav, _, _) = BuildTwoChildSection(output, yaml);

		var section = nav.NavigationItems.First().Should().BeOfType<SectionNavigation>().Subject;

		// Pick a deep leaf inside the observability docset via NavigationIndexedByOrder
		var deepLeaf = nav.NavigationIndexedByOrder
			.Values
			.OfType<ILeafNavigationItem<IDocumentationFile>>()
			.FirstOrDefault(l => l.Url.Contains("monitoring"));
		deepLeaf.Should().NotBeNull("fixture has monitoring/ pages");

		var islandRoot = deepLeaf.FindIslandRoot();
		islandRoot.Should().BeSameAs(section, "FindIslandRoot walks past child docsets (not islands) and stops at the section island");
	}

	// ──────────────────────────────────────────────────────────────
	// Back-link: immediate parent of obsNav is SectionNavigation
	// ──────────────────────────────────────────────────────────────

	[Fact]
	public void BackLink_FromSectionIsland_IncludesElasticDocs()
	{
		// language=yaml
		var yaml =
			"""
		           toc:
		             - section: Guides
		               children:
		                 - toc: observability://
		                   path_prefix: /observability
		                 - toc: serverless-search://
		                   path_prefix: /search
		           """;

		var (nav, _, _) = BuildTwoChildSection(output, yaml);

		var section = nav.NavigationItems.First().Should().BeOfType<SectionNavigation>().Subject;

		// The section IS the island; render its sidebar and check back-links
		var renderModel = NavigationRenderModel.Create(
			tree: section,
			topLevelItems: nav.TopLevelItems,
			isUsingNavigationDropdown: false,
			isPrimaryNavEnabled: true,
			isGlobalAssemblyBuild: true
		);

		// Only ancestor above the section is SiteNavigation ("Elastic Docs")
		renderModel.BackLinks.Should().Contain(link => link.Title == "Elastic Docs", "the section's only ancestor is SiteNavigation");
		renderModel.BackLinks.Should().NotContain(link => link.Title == "Guides", "the section itself is not its own back-link");
	}

	// ──────────────────────────────────────────────────────────────
	// URL invariance: section as root doesn't change child page URLs
	// ──────────────────────────────────────────────────────────────

	[Fact]
	public void ChildPageUrls_AreUnchanged_BySectionParent()
	{
		// language=yaml
		var flat =
			"""
		           toc:
		             - toc: observability://
		               path_prefix: /observability
		             - toc: serverless-search://
		               path_prefix: /search
		           """;

		// language=yaml
		var sectioned =
			"""
		                toc:
		                  - section: Guides
		                    children:
		                      - toc: observability://
		                        path_prefix: /observability
		                      - toc: serverless-search://
		                        path_prefix: /search
		                """;

		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		string[] GetLeafUrls(string siteNavYaml)
		{
			var navFile = SiteNavigationFile.Deserialize(siteNavYaml);
			var obsCtx = SiteNavigationTestFixture.CreateAssemblerContext(fileSystem, "/checkouts/current/observability", output);
			var obsDocset = DocumentationSetFile.LoadAndResolve(
				obsCtx.Collector,
				fileSystem.FileInfo.New("/checkouts/current/observability/docs/docset.yml"),
				new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
			);
			var obsNav = new DocumentationSetNavigation<IDocumentationFile>(obsDocset, obsCtx, GenericDocumentationFileFactory.Instance);

			var searchCtx = SiteNavigationTestFixture.CreateAssemblerContext(fileSystem, "/checkouts/current/serverless-search", output);
			var searchDocset = DocumentationSetFile.LoadAndResolve(
				searchCtx.Collector,
				fileSystem.FileInfo.New("/checkouts/current/serverless-search/docs/docset.yml"),
				new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
			);
			var searchNav = new DocumentationSetNavigation<IDocumentationFile>(
				searchDocset,
				searchCtx,
				GenericDocumentationFileFactory.Instance
			);

			var siteCtx = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability", output);
			var siteNav = new SiteNavigation(navFile, siteCtx, [obsNav, searchNav], sitePrefix: "/docs");
			return [
				.. siteNav.NavigationIndexedByOrder.Values.OfType<ILeafNavigationItem<IDocumentationFile>>().Select(l => l.Url).Order()
			];
		}

		var flatUrls = GetLeafUrls(flat);
		var sectionedUrls = GetLeafUrls(sectioned);

		// The set of leaf URLs must be identical regardless of whether entries
		// are nested under a section or flat at the top level.
		sectionedUrls.Should().BeEquivalentTo(flatUrls, "grouping toc entries under a section must not change any page URL");
	}

	// ──────────────────────────────────────────────────────────────
	// SectionTopNavBuilder: tab built from section node children
	// ──────────────────────────────────────────────────────────────

	[Fact]
	public void SectionTopNavBuilder_BuildsTab_WithSectionId()
	{
		// language=yaml
		var yaml =
			"""
		           toc:
		             - section: Guides
		               children:
		                 - toc: observability://
		                   path_prefix: /observability
		                 - toc: serverless-search://
		                   path_prefix: /search
		           """;

		var (nav, _, _) = BuildTwoChildSection(output, yaml);
		var navFile = SiteNavigationFile.Deserialize(yaml);

		var section = nav.NavigationItems.First().Should().BeOfType<SectionNavigation>().Subject;

		var renderModel = SectionTopNavBuilder.Build(nav, navFile);

		renderModel.Should().NotBeNull();
		renderModel.Items.Should().HaveCount(1);

		var tab = renderModel.Items[0].Should().BeOfType<TopNavLinkItem>().Subject;
		tab.Title.Should().Be("Guides");
		// All section pages have NavigationRoot = sectionNav, so a single SectionId suffices
		tab.SectionId.Should().Be(section.Id, "active-tab detection matches NavigationRoot.Id == section.Id");
		tab.SectionIds.Should().BeNull("multi-root SectionIds are not needed when the section is the island");
	}
}
