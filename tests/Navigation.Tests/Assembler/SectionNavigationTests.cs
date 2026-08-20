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

	private static (SiteNavigation, DocumentationSetNavigation<IDocumentationFile>, DocumentationSetNavigation<IDocumentationFile>)
		BuildTwoChildSection(ITestOutputHelper output, string siteNavYaml)
	{
		var siteNavFile = SiteNavigationFile.Deserialize(siteNavYaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		var obsCtx = SiteNavigationTestFixture.CreateAssemblerContext(fileSystem, "/checkouts/current/observability", output);
		var obsDocset = DocumentationSetFile.LoadAndResolve(
			obsCtx.Collector,
			fileSystem.FileInfo.New("/checkouts/current/observability/docs/docset.yml"),
			new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem));
		var obsNav = new DocumentationSetNavigation<IDocumentationFile>(obsDocset, obsCtx, GenericDocumentationFileFactory.Instance);

		var searchCtx = SiteNavigationTestFixture.CreateAssemblerContext(fileSystem, "/checkouts/current/serverless-search", output);
		var searchDocset = DocumentationSetFile.LoadAndResolve(
			searchCtx.Collector,
			fileSystem.FileInfo.New("/checkouts/current/serverless-search/docs/docset.yml"),
			new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem));
		var searchNav = new DocumentationSetNavigation<IDocumentationFile>(searchDocset, searchCtx, GenericDocumentationFileFactory.Instance);

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
		var yaml = """
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
		var section = nav.NavigationItems.First()
			.Should().BeOfType<SectionNavigation>().Subject;

		section.Title.Should().Be("Guides");
		section.NavigationItems.Should().HaveCount(2);
	}

	[Fact]
	public void SectionNavigationNode_IsIsland_AndParentIsSiteNavigation()
	{
		// language=yaml
		var yaml = """
		           toc:
		             - section: Guides
		               children:
		                 - toc: observability://
		                   path_prefix: /observability
		                 - toc: serverless-search://
		                   path_prefix: /search
		           """;

		var (nav, _, _) = BuildTwoChildSection(output, yaml);

		var section = nav.NavigationItems.First()
			.Should().BeOfType<SectionNavigation>().Subject;

		section.IsIsland.Should().BeTrue();
		section.Parent.Should().BeSameAs(nav, "SectionNavigation parent must be SiteNavigation");
		section.RendersAsIsland().Should().BeTrue("island + non-null parent");
	}

	[Fact]
	public void SectionChildren_AreNotIslands_SectionIsTheIsland()
	{
		// language=yaml
		var yaml = """
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
		var yaml = """
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
		var deepLeaf = nav.NavigationIndexedByOrder.Values
			.OfType<ILeafNavigationItem<IDocumentationFile>>()
			.FirstOrDefault(l => l.Url.Contains("monitoring"));
		deepLeaf.Should().NotBeNull("fixture has monitoring/ pages");

		var islandRoot = deepLeaf.FindIslandRoot();
		islandRoot.Should().BeSameAs(section,
			"FindIslandRoot walks past child docsets (not islands) and stops at the section island");
	}

	// ──────────────────────────────────────────────────────────────
	// Back-link: immediate parent of obsNav is SectionNavigation
	// ──────────────────────────────────────────────────────────────

	[Fact]
	public void BackLink_FromSectionIsland_OmitsDocs()
	{
		// language=yaml
		var yaml = """
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

		// The section's only ancestor is SiteNavigation ("Docs");
		// assembler omits it because the top-bar Docs tab already links home.
		var renderModel = NavigationRenderModel.Create(
			tree: section,
			topLevelItems: nav.TopLevelItems,
			isUsingNavigationDropdown: false,
			isPrimaryNavEnabled: true,
			isGlobalAssemblyBuild: true);

		// Only ancestor above the section is SiteNavigation ("Docs")
		renderModel.BackLinks.Should().NotContain(link => link.Title == "Docs",
			"assembler Docs tab in the top bar already links to the site root");
		renderModel.BackLinks.Should().NotContain(link => link.Title == "Guides",
			"the section itself is not its own back-link");
	}

	// ──────────────────────────────────────────────────────────────
	// URL invariance: section as root doesn't change child page URLs
	// ──────────────────────────────────────────────────────────────

	[Fact]
	public void ChildPageUrls_AreUnchanged_BySectionParent()
	{
		// language=yaml
		var flat = """
		           toc:
		             - toc: observability://
		               path_prefix: /observability
		             - toc: serverless-search://
		               path_prefix: /search
		           """;

		// language=yaml
		var sectioned = """
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
				new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem));
			var obsNav = new DocumentationSetNavigation<IDocumentationFile>(obsDocset, obsCtx, GenericDocumentationFileFactory.Instance);

			var searchCtx = SiteNavigationTestFixture.CreateAssemblerContext(fileSystem, "/checkouts/current/serverless-search", output);
			var searchDocset = DocumentationSetFile.LoadAndResolve(
				searchCtx.Collector,
				fileSystem.FileInfo.New("/checkouts/current/serverless-search/docs/docset.yml"),
				new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem));
			var searchNav = new DocumentationSetNavigation<IDocumentationFile>(searchDocset, searchCtx, GenericDocumentationFileFactory.Instance);

			var siteCtx = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability", output);
			var siteNav = new SiteNavigation(navFile, siteCtx, [obsNav, searchNav], sitePrefix: "/docs");
			return [..siteNav.NavigationIndexedByOrder.Values
				.OfType<ILeafNavigationItem<IDocumentationFile>>()
				.Select(l => l.Url)
				.Order()];
		}

		var flatUrls = GetLeafUrls(flat);
		var sectionedUrls = GetLeafUrls(sectioned);

		// The set of leaf URLs must be identical regardless of whether entries
		// are nested under a section or flat at the top level.
		sectionedUrls.Should().BeEquivalentTo(flatUrls,
			"grouping toc entries under a section must not change any page URL");
	}

	// ──────────────────────────────────────────────────────────────
	// SectionTopNavBuilder: tab built from section node children
	// ──────────────────────────────────────────────────────────────

	[Fact]
	public void SectionTopNavBuilder_BuildsDropdownTab_WhenDropdownLinksPresent()
	{
		// YAML that has both a children section (to give SiteNavigation a valid index)
		// and a dropdown section to exercise the new dropdown path.
		// language=yaml
		var yaml = """
		           toc:
		             - section: Guides
		               children:
		                 - toc: observability://
		                   path_prefix: /observability
		             - section: Products
		               dropdown:
		                 - title: Elasticsearch
		                   url: solutions/search/elasticsearch
		                 - title: Observability
		                   url: solutions/observability
		           """;

		var (navigation, _, _) = BuildTwoChildSection(output, yaml);
		var navFile = SiteNavigationFile.Deserialize(yaml);

		var renderModel = SectionTopNavBuilder.Build(navigation, navFile);

		renderModel.Should().NotBeNull();
		renderModel.Items.Should().HaveCount(2, "one Guides tab + one Products dropdown");

		var dropdown = renderModel.Items[1].Should().BeOfType<TopNavDropdownItem>().Subject;
		dropdown.Title.Should().Be("Products");
		dropdown.IsActive(currentSectionId: null).Should().BeFalse("dropdown tabs are never active");
		dropdown.Groups.Should().HaveCount(1, "flat dropdown: items become one ungrouped group");

		var group = dropdown.Groups[0];
		group.Label.Should().BeNull("flat dropdown has no group label");
		group.Links.Should().HaveCount(2);
		group.Links[0].Title.Should().Be("Elasticsearch");
		group.Links[0].Url.Should().Be("/docs/solutions/search/elasticsearch",
			"site prefix is prepended to the configured url");
		group.Links[1].Title.Should().Be("Observability");
		group.Links[1].Url.Should().Be("/docs/solutions/observability");
	}

	[Fact]
	public void SectionTopNavBuilder_BuildsTab_WithSectionId()
	{
		// language=yaml
		var yaml = """
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
		tab.SectionId.Should().Be(section.Id,
			"active-tab detection matches NavigationRoot.Id == section.Id");
		tab.SectionIds.Should().BeNull("multi-root SectionIds are not needed when the section is the island");
	}

	[Fact]
	public void SectionTopNavBuilder_SkipsStrayTopLevelTocEntries()
	{
		// language=yaml
		var yaml = """
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

		var renderModel = SectionTopNavBuilder.Build(nav, navFile);

		renderModel.Should().NotBeNull();
		renderModel.Items.Should().ContainSingle()
			.Which.Title.Should().Be("Guides");
	}

	[Fact]
	public void TocNavigationTitle_OverridesTheDocsetIndexTitle()
	{
		// language=yaml
		var yaml = """
		           toc:
		             - section: Guides
		               children:
		                 - toc: observability://
		                   path_prefix: /observability
		                   navigation_title: Manage your Cloud account
		                 - toc: serverless-search://
		                   path_prefix: /search
		           """;

		var (_, obsNav, searchNav) = BuildTwoChildSection(output, yaml);

		obsNav.NavigationTitle.Should().Be("Manage your Cloud account");
		obsNav.NavigationTitleOverride.Should().Be("Manage your Cloud account");
		searchNav.NavigationTitleOverride.Should().BeNull();
	}

	[Fact]
	public void TocNavigationTitle_OverridesNestedTableOfContentsTitle()
	{
		// language=yaml
		var yaml = """
		           toc:
		             - toc: platform://cloud-guide
		               path_prefix: /cloud
		               navigation_title: Manage your Cloud account
		           """;

		var siteNavFile = SiteNavigationFile.Deserialize(yaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();
		var platformCtx = SiteNavigationTestFixture.CreateAssemblerContext(
			fileSystem, "/checkouts/current/platform", output);
		var platformDocset = DocumentationSetFile.LoadAndResolve(
			platformCtx.Collector,
			fileSystem.FileInfo.New("/checkouts/current/platform/docs/docset.yml"),
			new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem));
		var platformNav = new DocumentationSetNavigation<IDocumentationFile>(
			platformDocset, platformCtx, GenericDocumentationFileFactory.Instance);
		var siteCtx = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/platform", output);
		var nav = new SiteNavigation(siteNavFile, siteCtx, [platformNav], sitePrefix: "/docs");

		var cloudGuide = nav.NavigationItems.Should().ContainSingle()
			.Which.Should().BeOfType<TableOfContentsNavigation<IDocumentationFile>>().Subject;
		cloudGuide.NavigationTitleOverride.Should().Be("Manage your Cloud account");
		cloudGuide.NavigationTitle.Should().Be("Manage your Cloud account");
		cloudGuide.Index.NavigationTitle.Should().Be("Cloud Guide");
	}
}
