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
		var yaml =
			"""
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
		var first = CreateRenderModel(
			"""
		                              project: 'test-project'
		                              toc:
		                                - file: index.md
		                                - file: overview.md
		                              """
		);
		// language=yaml
		var second = CreateRenderModel(
			"""
		                               project: 'test-project'
		                               toc:
		                                 - file: index.md
		                                 - file: reference.md
		                               """
		);

		first.ContentHash.Should().NotBe(second.ContentHash);
	}

	[Fact]
	public void ReorderedSiblings_ProduceDifferentContentHashes()
	{
		// language=yaml
		var first = CreateRenderModel(
			"""
		                              project: 'test-project'
		                              toc:
		                                - file: index.md
		                                - file: alpha.md
		                                - file: beta.md
		                              """
		);
		// language=yaml
		var second = CreateRenderModel(
			"""
		                               project: 'test-project'
		                               toc:
		                                 - file: index.md
		                                 - file: beta.md
		                                 - file: alpha.md
		                               """
		);

		first.ContentHash.Should().NotBe(second.ContentHash);
	}

	[Fact]
	public void HiddenItems_AreExcludedFromTheTree_AndChangeTheContentHash()
	{
		// language=yaml
		var visible = CreateRenderModel(
			"""
		                                project: 'test-project'
		                                toc:
		                                  - file: index.md
		                                  - file: guide.md
		                                  - file: secret.md
		                                """
		);
		// language=yaml
		var hidden = CreateRenderModel(
			"""
		                               project: 'test-project'
		                               toc:
		                                 - file: index.md
		                                 - file: guide.md
		                                 - hidden: secret.md
		                               """
		);

		visible.Tree.Should().Contain(n => n.Url == "/secret");
		hidden.Tree.Should().NotContain(n => n.Url == "/secret");
		hidden.ContentHash.Should().NotBe(visible.ContentHash);
	}

	[Fact]
	public void PrimaryNav_OmitsIndexRow_AndChangesTheContentHash()
	{
		// language=yaml
		var yaml =
			"""
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
		var model = CreateRenderModel(
			"""
		                              project: 'test-project'
		                              toc:
		                                - file: index.md
		                                - folder: setup
		                                  children:
		                                    - file: index.md
		                                    - file: install.md
		                              """
		);

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
		var yaml =
			"""
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
		fileSystem.AddFile(
			"/docs/reference/toc.yml",
			new MockFileData("""
			toc:
			  - file: index.md
			  - file: page.md
			""")
		);

		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);
		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(
			docSet,
			context,
			TestDocumentationFileFactory.Instance,
			crossLinkResolver: TestCrossLinkResolver.Instance
		);
		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		var model = NavigationRenderModel.Create(
			tree: navigation,
			topLevelItems: navigation.NavigationItems.OfType<INodeNavigationItem<INavigationModel, INavigationItem>>().ToList(),
			isUsingNavigationDropdown: false,
			isPrimaryNavEnabled: false,
			isGlobalAssemblyBuild: false
		);

		// The island node appears in the tree
		var islandNode = model.Tree.Should().ContainSingle(n => n.Kind == NavigationRenderNodeKind.Island).Subject;
		islandNode.Url.Should().Be("/reference");
		// Island nodes have no NavigationItems — they are stubs (the subtree lives in the island sidebar)
		islandNode.NavigationItems.Should().BeEmpty("island stubs have no subtree in the parent nav");
	}

	[Fact]
	public async Task Create_BuildsBackLinkStack_RootFirst()
	{
		// language=yaml
		var yaml =
			"""
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
		fileSystem.AddFile(
			"/docs/security/toc.yml",
			new MockFileData(
				"""
			toc:
			  - file: index.md
			  - toc: rules
			    island: true
			"""
			)
		);
		fileSystem.AddFile(
			"/docs/security/rules/toc.yml",
			new MockFileData("""
			toc:
			  - file: index.md
			  - file: page.md
			""")
		);

		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);
		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(
			docSet,
			context,
			TestDocumentationFileFactory.Instance,
			crossLinkResolver: TestCrossLinkResolver.Instance
		);
		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		var security = (TableOfContentsNavigation<TestDocumentationFile>)navigation.NavigationItems.ElementAt(0);
		var rules = (TableOfContentsNavigation<TestDocumentationFile>)security.NavigationItems.ElementAt(0);

		// Model for the nested `rules` island, without dropdown (isolated build)
		var model = NavigationRenderModel.Create(
			tree: rules,
			topLevelItems: [navigation],
			isUsingNavigationDropdown: false,
			isPrimaryNavEnabled: false,
			isGlobalAssemblyBuild: false
		);

		// Back links: root-first → docset root, then security (enclosing island, also the immediate parent).
		// Stack: docset root (/), then security (/security) — total 2 entries.
		model.BackLinks.Should().HaveCount(2);
		model.BackLinks[0].Url.Should().Be("/", "first entry is the top navigation root");
		model.BackLinks[1].Url.Should().Be("/security", "second is the enclosing island (also the immediate parent)");

		// The island URL is the rules section root
		rules.Url.Should().Be("/security/rules");
		// RootIndex title comes from the island node title (isolated build, no primary nav, renders as island)
		model.RootIndex!.NavigationTitle.Should().Be("index", "TestDocumentationFileFactory uses filename without extension");

		// The tree contains only the non-index pages
		model.Tree.Should().ContainSingle(n => n.Url == "/security/rules/page");
	}

	[Fact]
	public async Task Create_ContentHash_DiffersByTreeStructure()
	{
		// Islands with different page trees must produce different hashes even when back links are identical.
		// language=yaml
		var yaml =
			"""
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
		fileSystem.AddFile(
			"/docs/reference/toc.yml",
			new MockFileData("""
			toc:
			  - file: index.md
			  - file: page-a.md
			""")
		);

		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);
		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(
			docSet,
			context,
			TestDocumentationFileFactory.Instance,
			crossLinkResolver: TestCrossLinkResolver.Instance
		);
		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		var reference = (TableOfContentsNavigation<TestDocumentationFile>)navigation.NavigationItems.ElementAt(0);
		var model1 = NavigationRenderModel.Create(
			tree: reference,
			topLevelItems: [],
			isUsingNavigationDropdown: false,
			isPrimaryNavEnabled: false,
			isGlobalAssemblyBuild: false
		);

		// Second navigation: same structure but a different page name in the island tree
		// language=yaml
		var yaml2 =
			"""
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
		fileSystem2.AddFile("/docs/reference/page-b.md", new MockFileData("# Page B")); // ← different page
		fileSystem2.AddFile(
			"/docs/reference/toc.yml",
			new MockFileData("""
			toc:
			  - file: index.md
			  - file: page-b.md
			""")
		);
		var context2 = CreateContext(fileSystem2);
		var docSet2 = DocumentationSetFile.LoadAndResolve(context2.Collector, yaml2, fileSystem2.NewDirInfo("docs"));
		_ = context2.Collector.StartAsync(TestContext.Current.CancellationToken);
		var navigation2 = new DocumentationSetNavigation<TestDocumentationFile>(
			docSet2,
			context2,
			TestDocumentationFileFactory.Instance,
			crossLinkResolver: TestCrossLinkResolver.Instance
		);
		await context2.Collector.StopAsync(TestContext.Current.CancellationToken);

		var reference2 = (TableOfContentsNavigation<TestDocumentationFile>)navigation2.NavigationItems.ElementAt(0);
		var model2 = NavigationRenderModel.Create(
			tree: reference2,
			topLevelItems: [],
			isUsingNavigationDropdown: false,
			isPrimaryNavEnabled: false,
			isGlobalAssemblyBuild: false
		);

		// Different tree content (page-a vs page-b) → different content hash
		model1.ContentHash.Should().NotBe(model2.ContentHash, "different tree pages produce different content hashes");
		// Also verify the trees actually differ (sanity check for the test setup)
		model1.Tree.Should().ContainSingle(n => n.Url.EndsWith("page-a", StringComparison.Ordinal));
		model2.Tree.Should().ContainSingle(n => n.Url.EndsWith("page-b", StringComparison.Ordinal));
	}

	[Fact]
	public void Create_TopLevelIsland_HasDropdownAndNoBackLinks()
	{
		// A top-level section (Parent is nav root, grandparent is null) has the dropdown
		// as its only mechanism — no back-link trail is generated.
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - file: index.md
		             - file: page.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		fileSystem.AddFile("/docs/index.md", new MockFileData("# Root"));
		fileSystem.AddFile("/docs/page.md", new MockFileData("# Page"));

		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(docSet, context, TestDocumentationFileFactory.Instance);

		// The navigation root itself has no parent, so it doesn't render as island
		// But treat it as the top-level island to simulate the SiteNavigation implicit mark
		// We verify back-links are empty when only parent is the nav root
		var model = NavigationRenderModel.Create(
			tree: navigation,
			topLevelItems: [navigation],
			isUsingNavigationDropdown: true,
			isPrimaryNavEnabled: true,
			isGlobalAssemblyBuild: true
		);

		// No back-links: the only ancestor would be the nav root, which the dropdown replaces
		model.BackLinks.Should().BeEmpty("top-level sections rely on the dropdown, not back-links");
		model.IsUsingNavigationDropdown.Should().BeTrue();
		model.CurrentTopLevelUrl.Should().Be(navigation.Url, "current top-level resolves to itself");
	}

	[Fact]
	public async Task Create_NestedIsland_KeepsTopLevelBackLink_AlongsideDropdown()
	{
		// In the assembled site: SiteNavigation (nav root) → top-level section (island) → inner island.
		// The dropdown represents the nav root; the top-level section STILL appears as a back-link
		// because re-selecting the active dropdown item is a poor UX substitute for a direct link.
		// We simulate this with a 3-level tree: docset root → elasticsearch (island) → clients (island).
		// docset root plays the nav-root role (Parent=null), elasticsearch plays top-level section.
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - file: index.md
		             - toc: elasticsearch
		               island: true
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		fileSystem.AddFile("/docs/index.md", new MockFileData("# Root"));
		fileSystem.AddFile("/docs/elasticsearch/index.md", new MockFileData("# Elasticsearch"));
		fileSystem.AddFile("/docs/elasticsearch/clients/index.md", new MockFileData("# Clients"));
		fileSystem.AddFile(
			"/docs/elasticsearch/toc.yml",
			new MockFileData(
				"""
			toc:
			  - file: index.md
			  - toc: clients
			    island: true
			"""
			)
		);
		fileSystem.AddFile(
			"/docs/elasticsearch/clients/toc.yml",
			new MockFileData("""
			toc:
			  - file: index.md
			""")
		);

		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);
		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(
			docSet,
			context,
			TestDocumentationFileFactory.Instance,
			crossLinkResolver: TestCrossLinkResolver.Instance
		);
		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		var elasticsearch = navigation.NavigationItems
			.ElementAt(0)
			.Should()
			.BeOfType<TableOfContentsNavigation<TestDocumentationFile>>()
			.Subject;
		var clients = elasticsearch.NavigationItems
			.ElementAt(0)
			.Should()
			.BeOfType<TableOfContentsNavigation<TestDocumentationFile>>()
			.Subject;

		// elasticsearch is the "top-level section" (Parent=navigation/nav-root, island=true)
		elasticsearch.RendersAsIsland().Should().BeTrue();
		// clients is nested island (Parent=elasticsearch)
		clients.RendersAsIsland().Should().BeTrue();

		var model = NavigationRenderModel.Create(
			tree: clients,
			topLevelItems: [elasticsearch], // elasticsearch = the "top-level section" in the dropdown

			isUsingNavigationDropdown: true,
			isPrimaryNavEnabled: true,
			isGlobalAssemblyBuild: false
		);

		// ← elasticsearch appears even though the dropdown also shows elasticsearch as active.
		// The nav root (navigation, Parent=null) is suppressed because the dropdown covers it.
		model.BackLinks
			.Should()
			.ContainSingle(b => b.Url == elasticsearch.Url, "← elasticsearch stays even though the dropdown names it as the active section");
		// Dropdown correctly identifies elasticsearch as the current top-level section
		model.CurrentTopLevelUrl.Should().Be(elasticsearch.Url);
		// Nav root must NOT appear in back-links (dropdown suppresses it)
		model.BackLinks.Should().NotContain(b => b.Url == navigation.Url, "nav root is represented by the dropdown, not a back-link");
	}

	[Fact]
	public async Task Create_WithoutDropdown_KeepsFullBackLinkTrail()
	{
		// Isolated build without primary nav: back-links include the navigation root
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - file: index.md
		             - toc: clients
		               island: true
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		fileSystem.AddFile("/docs/index.md", new MockFileData("# Root"));
		fileSystem.AddFile("/docs/clients/index.md", new MockFileData("# Clients"));
		fileSystem.AddFile("/docs/clients/toc.yml", new MockFileData("toc:\n  - file: index.md\n"));

		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);
		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(
			docSet,
			context,
			TestDocumentationFileFactory.Instance,
			crossLinkResolver: TestCrossLinkResolver.Instance
		);
		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		var clients = navigation.NavigationItems.ElementAt(0).Should().BeOfType<TableOfContentsNavigation<TestDocumentationFile>>().Subject;

		var model = NavigationRenderModel.Create(
			tree: clients,
			topLevelItems: [navigation],
			isUsingNavigationDropdown: false,
			isPrimaryNavEnabled: false,
			isGlobalAssemblyBuild: false
		);

		// Without dropdown the nav root IS included in back-links
		model.BackLinks.Should().ContainSingle(b => b.Url == navigation.Url, "without dropdown the nav root appears as a back-link");
		model.IsUsingNavigationDropdown.Should().BeFalse();
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
			isGlobalAssemblyBuild: false
		);
	}
}
