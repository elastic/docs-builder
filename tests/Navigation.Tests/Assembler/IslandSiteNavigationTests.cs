// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Documentation.Navigation.Isolated.Node;

namespace Elastic.Documentation.Navigation.Tests.Assembler;

public class IslandSiteNavigationTests(ITestOutputHelper output)
{
	// ──────────────────────────────────────────────────────────────
	// navigation.yml island: true marks the resolved node as an island
	// ──────────────────────────────────────────────────────────────
	[Fact]
	public void NavigationYamlIsland_MarksResolvedNode()
	{
		// language=yaml
		var siteNavYaml = """
		                  toc:
		                    - toc: observability://
		                      path_prefix: observability
		                      island: true
		                  """;

		var siteNavFile = SiteNavigationFile.Deserialize(siteNavYaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		var obsContext = SiteNavigationTestFixture.CreateAssemblerContext(fileSystem, "/checkouts/current/observability", output);
		var obsDocset = DocumentationSetFile.LoadAndResolve(obsContext.Collector, fileSystem.FileInfo.New("/checkouts/current/observability/docs/docset.yml"), FileSystemFactory.ScopeSourceDirectory(fileSystem, "/checkouts"));
		var obsNav = new DocumentationSetNavigation<IDocumentationFile>(obsDocset, obsContext, GenericDocumentationFileFactory.Instance);

		var documentationSets = new List<IDocumentationSetNavigation> { obsNav };
		var siteContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability", output);
		var navigation = new SiteNavigation(siteNavFile, siteContext, documentationSets, sitePrefix: null);

		var obsNode = navigation.NavigationItems.ElementAt(0)
			.Should().BeOfType<DocumentationSetNavigation<IDocumentationFile>>().Subject;

		// The node was marked as an island from navigation.yml
		obsNode.IsIsland.Should().BeTrue("navigation.yml declared island: true");
		// After SiteNavigation re-parents it, RendersAsIsland() should be true
		obsNode.RendersAsIsland().Should().BeTrue("node has a parent (SiteNavigation)");
	}

	// ──────────────────────────────────────────────────────────────
	// Goal 4 mirror: docset.yml island: true IS an island in assembler build
	// because the node is re-parented under SiteNavigation
	// ──────────────────────────────────────────────────────────────
	[Fact]
	public void DocsetRootIsland_IsAnIsland_InAssemblerBuild()
	{
		// language=yaml
		var siteNavYaml = """
		                  toc:
		                    - toc: observability://
		                      path_prefix: observability
		                  """;

		var siteNavFile = SiteNavigationFile.Deserialize(siteNavYaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();

		// Inject island: true into the observability docset.yml
		fileSystem.AddFile("/checkouts/current/observability/docs/docset.yml", new MockFileData(
			"""
			project: observability
			island: true
			toc:
			  - file: index.md
			  - folder: getting-started
			    children:
			      - file: quick-start.md
			      - file: installation.md
			  - folder: monitoring
			    children:
			      - file: index.md
			      - file: logs.md
			      - file: metrics.md
			      - file: traces.md
			"""));

		var obsContext = SiteNavigationTestFixture.CreateAssemblerContext(fileSystem, "/checkouts/current/observability", output);
		var obsDocset = DocumentationSetFile.LoadAndResolve(obsContext.Collector, fileSystem.FileInfo.New("/checkouts/current/observability/docs/docset.yml"), FileSystemFactory.ScopeSourceDirectory(fileSystem, "/checkouts"));

		// In isolated build: IsIsland is stored but RendersAsIsland() is false (no parent)
		var isolatedNav = new DocumentationSetNavigation<IDocumentationFile>(obsDocset, obsContext, GenericDocumentationFileFactory.Instance);
		isolatedNav.IsIsland.Should().BeTrue();
		isolatedNav.RendersAsIsland().Should().BeFalse("no parent yet in isolated build");

		// After SiteNavigation re-parents it, RendersAsIsland() should be true
		var documentationSets = new List<IDocumentationSetNavigation> { isolatedNav };
		var siteContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability", output);
		var navigation = new SiteNavigation(siteNavFile, siteContext, documentationSets, sitePrefix: null);

		var obsNode = navigation.NavigationItems.ElementAt(0)
			.Should().BeOfType<DocumentationSetNavigation<IDocumentationFile>>().Subject;
		obsNode.RendersAsIsland().Should().BeTrue("SiteNavigation gave it a parent");
	}

	// ──────────────────────────────────────────────────────────────
	// OR semantics: navigation.yml island: true doesn't override a content-set's island: true
	// and a content-set without island can be made one from navigation.yml
	// ──────────────────────────────────────────────────────────────
	[Fact]
	public void NavigationYamlIsland_DoesNotClearContentSetIsland()
	{
		// Set up: content-set has island: true, navigation.yml does NOT — node should still be island
		// language=yaml
		var siteNavNoIsland = """
		                      toc:
		                        - toc: observability://
		                          path_prefix: observability
		                      """;

		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();
		fileSystem.AddFile("/checkouts/current/observability/docs/docset.yml", new MockFileData(
			"""
			project: observability
			island: true
			toc:
			  - file: index.md
			  - folder: getting-started
			    children:
			      - file: quick-start.md
			      - file: installation.md
			  - folder: monitoring
			    children:
			      - file: index.md
			      - file: logs.md
			      - file: metrics.md
			      - file: traces.md
			"""));

		var obsContext = SiteNavigationTestFixture.CreateAssemblerContext(fileSystem, "/checkouts/current/observability", output);
		var obsDocset = DocumentationSetFile.LoadAndResolve(obsContext.Collector, fileSystem.FileInfo.New("/checkouts/current/observability/docs/docset.yml"), FileSystemFactory.ScopeSourceDirectory(fileSystem, "/checkouts"));
		var obsNav = new DocumentationSetNavigation<IDocumentationFile>(obsDocset, obsContext, GenericDocumentationFileFactory.Instance);
		var documentationSets = new List<IDocumentationSetNavigation> { obsNav };
		var siteContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability", output);
		var navigation = new SiteNavigation(SiteNavigationFile.Deserialize(siteNavNoIsland), siteContext, documentationSets, sitePrefix: null);

		var obsNode = navigation.NavigationItems.ElementAt(0)
			.Should().BeOfType<DocumentationSetNavigation<IDocumentationFile>>().Subject;
		// Content-set declared island: true; navigation.yml didn't; still an island (OR semantics)
		obsNode.IsIsland.Should().BeTrue("content-set set island: true; navigation.yml can't remove it");
		obsNode.RendersAsIsland().Should().BeTrue();
	}
}
