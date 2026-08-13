// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation.Isolated.Node;
using Elastic.Documentation.Navigation.Tests.Isolation;
using Elastic.Documentation.Site.Navigation;

namespace Elastic.Documentation.Navigation.Tests.Rendering;

public class NavigationRenderModelTests(ITestOutputHelper output) : DocumentationSetNavigationTestBase(output)
{
	[Fact]
	public void EquivalentTrees_ProduceSameContentHash()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: index.md
		             - folder: setup
		               children:
		                 - file: index.md
		                 - file: install.md
		           """;

		var first = CreateRenderModel(yaml);
		var second = CreateRenderModel(yaml);

		first.ContentHash.Should().Be(second.ContentHash);
	}

	[Fact]
	public void DifferentPages_ProduceDifferentContentHashes()
	{
		// language=yaml
		var first = CreateRenderModel("""
		                              project: 'test-project'
		                              toc:
		                                - file: index.md
		                                - file: overview.md
		                              """);
		// language=yaml
		var second = CreateRenderModel("""
		                               project: 'test-project'
		                               toc:
		                                 - file: index.md
		                                 - file: reference.md
		                               """);

		first.ContentHash.Should().NotBe(second.ContentHash);
	}

	[Fact]
	public void ReorderedSiblings_ProduceDifferentContentHashes()
	{
		// language=yaml
		var first = CreateRenderModel("""
		                              project: 'test-project'
		                              toc:
		                                - file: index.md
		                                - file: alpha.md
		                                - file: beta.md
		                              """);
		// language=yaml
		var second = CreateRenderModel("""
		                               project: 'test-project'
		                               toc:
		                                 - file: index.md
		                                 - file: beta.md
		                                 - file: alpha.md
		                               """);

		first.ContentHash.Should().NotBe(second.ContentHash);
	}

	[Fact]
	public void HiddenItems_AreExcludedFromTheTree_AndChangeTheContentHash()
	{
		// language=yaml
		var visible = CreateRenderModel("""
		                                project: 'test-project'
		                                toc:
		                                  - file: index.md
		                                  - file: guide.md
		                                  - file: secret.md
		                                """);
		// language=yaml
		var hidden = CreateRenderModel("""
		                               project: 'test-project'
		                               toc:
		                                 - file: index.md
		                                 - file: guide.md
		                                 - hidden: secret.md
		                               """);

		visible.Tree.Should().Contain(n => n.Url == "/secret");
		hidden.Tree.Should().NotContain(n => n.Url == "/secret");
		hidden.ContentHash.Should().NotBe(visible.ContentHash);
	}

	[Fact]
	public void PrimaryNav_OmitsIndexRow_AndChangesTheContentHash()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: index.md
		             - file: guide.md
		           """;

		var withIndexRow = CreateRenderModel(yaml);
		var withoutIndexRow = CreateRenderModel(yaml, isPrimaryNavEnabled: true);

		withIndexRow.RootIndex.Should().NotBeNull();
		withIndexRow.RootIndex.Url.Should().Be("/");
		withoutIndexRow.RootIndex.Should().BeNull();
		withoutIndexRow.ContentHash.Should().NotBe(withIndexRow.ContentHash);
	}

	[Fact]
	public void Nodes_CarryToggleStateAndNavigationItems()
	{
		// language=yaml
		var model = CreateRenderModel("""
		                              project: 'test-project'
		                              toc:
		                                - file: index.md
		                                - folder: setup
		                                  children:
		                                    - file: index.md
		                                    - file: install.md
		                              """);

		var node = model.Tree.Should().ContainSingle(n => n.Kind == NavigationRenderNodeKind.Node).Subject;
		node.Url.Should().Be("/setup");
		node.Id.Should().NotBeNullOrEmpty();
		node.ShowToggle.Should().BeTrue();
		node.NavigationItems.Should().ContainSingle(n => n.Url == "/setup/install");
	}

	// ──────────────────────────────────────────────────────────────
	// Island rendering
	// ──────────────────────────────────────────────────────────────

	[Fact]
	public async Task IslandNode_ProjectsAsIslandKind_WithNoChildren()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: index.md
		             - toc: reference
		               island: true
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		fileSystem.AddFile("/docs/index.md", new MockFileData("# Root"));
		fileSystem.AddFile("/docs/reference/index.md", new MockFileData("# Reference"));
		fileSystem.AddFile("/docs/reference/page.md", new MockFileData("# Page"));
		fileSystem.AddFile("/docs/reference/toc.yml", new MockFileData(
			"""
			toc:
			  - file: index.md
			  - file: page.md
			"""));

		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);
		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(docSet, context, TestDocumentationFileFactory.Instance, crossLinkResolver: TestCrossLinkResolver.Instance);
		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		var model = NavigationRenderModel.Create(
			tree: navigation,
			topLevelItems: navigation.NavigationItems.OfType<INodeNavigationItem<INavigationModel, INavigationItem>>().ToList(),
			isUsingNavigationDropdown: false,
			isPrimaryNavEnabled: false,
			isGlobalAssemblyBuild: false);

		// The island node appears in the tree
		var islandNode = model.Tree.Should().ContainSingle(n => n.Kind == NavigationRenderNodeKind.Island).Subject;
		islandNode.Url.Should().Be("/reference");
		// Island nodes have no NavigationItems — they are stubs (the subtree lives in the island sidebar)
		islandNode.NavigationItems.Should().BeEmpty("island stubs have no subtree in the parent nav");
	}

	[Fact]
	public async Task CreateIsland_BuildsBackLinkStack_RootFirst()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: index.md
		             - toc: security
		               island: true
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		fileSystem.AddFile("/docs/index.md", new MockFileData("# Root"));
		fileSystem.AddFile("/docs/security/index.md", new MockFileData("# Security"));
		fileSystem.AddFile("/docs/security/rules/index.md", new MockFileData("# Rules"));
		fileSystem.AddFile("/docs/security/rules/page.md", new MockFileData("# Page"));
		fileSystem.AddFile("/docs/security/toc.yml", new MockFileData(
			"""
			toc:
			  - file: index.md
			  - toc: rules
			    island: true
			"""));
		fileSystem.AddFile("/docs/security/rules/toc.yml", new MockFileData(
			"""
			toc:
			  - file: index.md
			  - file: page.md
			"""));

		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);
		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(docSet, context, TestDocumentationFileFactory.Instance, crossLinkResolver: TestCrossLinkResolver.Instance);
		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		var security = (TableOfContentsNavigation<TestDocumentationFile>)navigation.NavigationItems.ElementAt(0);
		var rules = (TableOfContentsNavigation<TestDocumentationFile>)security.NavigationItems.ElementAt(0);

		// Island view model for the nested `rules` island
		var islandModel = NavigationRenderModel.CreateIsland(rules);

		// Back links: root-first → docset root, then security (enclosing island, also the immediate parent).
		// security is the immediate parent AND an enclosing island; the walk visits it once, so no dedup needed.
		// Stack: docset root (/), then security (/security) — total 2 entries.
		islandModel.BackLinks.Should().HaveCount(2);
		islandModel.BackLinks[0].Url.Should().Be("/", "first entry is the top navigation root");
		islandModel.BackLinks[1].Url.Should().Be("/security", "second is the enclosing island (also the immediate parent)");

		// The island URL is the rules section root
		islandModel.Url.Should().Be("/security/rules");
		// NavigationTitle comes from the index filename via TestDocumentationFileFactory (not the H1 header)
		islandModel.NavigationTitle.Should().Be("index", "TestDocumentationFileFactory uses filename without extension");

		// The tree contains only the non-index pages
		islandModel.Tree.Should().ContainSingle(n => n.Url == "/security/rules/page");
	}

	[Fact]
	public async Task CreateIsland_ContentHash_DiffersByTreeStructure()
	{
		// Islands with different page trees must produce different hashes even when back links are identical.
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: index.md
		             - toc: reference
		               island: true
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		fileSystem.AddFile("/docs/index.md", new MockFileData("# Root"));
		fileSystem.AddFile("/docs/reference/index.md", new MockFileData("# Reference"));
		fileSystem.AddFile("/docs/reference/page-a.md", new MockFileData("# Page A"));
		fileSystem.AddFile("/docs/reference/toc.yml", new MockFileData(
			"""
			toc:
			  - file: index.md
			  - file: page-a.md
			"""));

		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);
		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(docSet, context, TestDocumentationFileFactory.Instance, crossLinkResolver: TestCrossLinkResolver.Instance);
		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		var reference = (TableOfContentsNavigation<TestDocumentationFile>)navigation.NavigationItems.ElementAt(0);
		var model1 = NavigationRenderModel.CreateIsland(reference);

		// Second navigation: same structure but a different page name in the island tree
		// language=yaml
		var yaml2 = """
		            project: 'test-project'
		            toc:
		              - file: index.md
		              - toc: reference
		                island: true
		            """;
		var fileSystem2 = new MockFileSystem();
		fileSystem2.AddDirectory("/docs");
		fileSystem2.AddFile("/docs/index.md", new MockFileData("# Root"));
		fileSystem2.AddFile("/docs/reference/index.md", new MockFileData("# Reference"));
		fileSystem2.AddFile("/docs/reference/page-b.md", new MockFileData("# Page B"));  // ← different page
		fileSystem2.AddFile("/docs/reference/toc.yml", new MockFileData(
			"""
			toc:
			  - file: index.md
			  - file: page-b.md
			"""));
		var context2 = CreateContext(fileSystem2);
		var docSet2 = DocumentationSetFile.LoadAndResolve(context2.Collector, yaml2, fileSystem2.NewDirInfo("docs"));
		_ = context2.Collector.StartAsync(TestContext.Current.CancellationToken);
		var navigation2 = new DocumentationSetNavigation<TestDocumentationFile>(docSet2, context2, TestDocumentationFileFactory.Instance, crossLinkResolver: TestCrossLinkResolver.Instance);
		await context2.Collector.StopAsync(TestContext.Current.CancellationToken);

		var reference2 = (TableOfContentsNavigation<TestDocumentationFile>)navigation2.NavigationItems.ElementAt(0);
		var model2 = NavigationRenderModel.CreateIsland(reference2);

		// Different tree content (page-a vs page-b) → different content hash
		model1.ContentHash.Should().NotBe(model2.ContentHash, "different tree pages produce different content hashes");
		// Also verify the trees actually differ (sanity check for the test setup)
		model1.Tree.Should().ContainSingle(n => n.Url.EndsWith("page-a", StringComparison.Ordinal));
		model2.Tree.Should().ContainSingle(n => n.Url.EndsWith("page-b", StringComparison.Ordinal));
	}

	private NavigationRenderModel CreateRenderModel(string yaml, bool isPrimaryNavEnabled = false)
	{
		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(docSet, context, TestDocumentationFileFactory.Instance);
		return NavigationRenderModel.Create(
			tree: navigation,
			topLevelItems: navigation.NavigationItems.OfType<INodeNavigationItem<INavigationModel, INavigationItem>>().ToList(),
			isUsingNavigationDropdown: false,
			isPrimaryNavEnabled: isPrimaryNavEnabled,
			isGlobalAssemblyBuild: false);
	}
}
