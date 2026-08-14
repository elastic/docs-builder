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
	public void SectionChildren_AreIslands_WithParent_SectionNavigation()
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

		obsNav.IsIsland.Should().BeTrue("child docset must be island");
		obsNav.Parent.Should().BeSameAs(section, "child docset parent must be SectionNavigation");
		obsNav.RendersAsIsland().Should().BeTrue();

		searchNav.IsIsland.Should().BeTrue();
		searchNav.Parent.Should().BeSameAs(section);
	}

	// ──────────────────────────────────────────────────────────────
	// FindIslandRoot: returns child docset, not the section
	// ──────────────────────────────────────────────────────────────

	[Fact]
	public void FindIslandRoot_FromDeepPage_ReturnsChildDocset_NotSection()
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

		var (nav, obsNav, _) = BuildTwoChildSection(output, yaml);

		// Pick a deep leaf inside the observability docset via NavigationIndexedByOrder
		var deepLeaf = nav.NavigationIndexedByOrder.Values
			.OfType<ILeafNavigationItem<IDocumentationFile>>()
			.FirstOrDefault(l => l.Url.Contains("monitoring"));
		deepLeaf.Should().NotBeNull("fixture has monitoring/ pages");

		var islandRoot = deepLeaf!.FindIslandRoot();
		islandRoot.Should().BeSameAs(obsNav,
			"FindIslandRoot must stop at the nearest island (child docset), not at the section");
	}

	// ──────────────────────────────────────────────────────────────
	// Back-link: immediate parent of obsNav is SectionNavigation
	// ──────────────────────────────────────────────────────────────

	[Fact]
	public void BackLink_FromChildIsland_IncludesSection()
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

		var (nav, obsNav, _) = BuildTwoChildSection(output, yaml);

		// Render model for the observability island sidebar
		var renderModel = NavigationRenderModel.Create(
			tree: obsNav,
			topLevelItems: nav.TopLevelItems,
			isUsingNavigationDropdown: false,
			isPrimaryNavEnabled: true,
			isGlobalAssemblyBuild: true);

		// Back-links should include the "Guides" section
		renderModel.BackLinks.Should().Contain(link => link.Title == "Guides",
			"the section is the immediate parent and must appear as a back-link");
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
	public void SectionTopNavBuilder_BuildsTab_WithChildSectionIds()
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
		var navFile = SiteNavigationFile.Deserialize(yaml);

		var renderModel = Elastic.Documentation.Assembler.Navigation.SectionTopNavBuilder.Build(nav, navFile);

		renderModel.Should().NotBeNull();
		renderModel!.Items.Should().HaveCount(1);

		var tab = renderModel.Items.First().Should().BeOfType<TopNavLinkItem>().Subject;
		tab.Title.Should().Be("Guides");
		tab.SectionIds.Should().NotBeNull("section with multiple children uses SectionIds");
		tab.SectionIds!.Should().Contain(obsNav.Id,
			"observability child ID must be in the tab's SectionIds");
		tab.SectionIds.Should().Contain(searchNav.Id,
			"search child ID must be in the tab's SectionIds");
	}
}
