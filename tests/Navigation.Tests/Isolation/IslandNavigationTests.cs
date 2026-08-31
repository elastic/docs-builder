// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation.Isolated.Leaf;
using Elastic.Documentation.Navigation.Isolated.Node;
using Elastic.Documentation.Navigation.Tests.Assembler;

namespace Elastic.Documentation.Navigation.Tests.Isolation;

public class IslandNavigationTests(ITestOutputHelper output) : DocumentationSetNavigationTestBase(output)
{
	// ──────────────────────────────────────────────────────────────
	// Goal 4: in an isolated (single-repo) build the docset root
	// with island: true still renders as the main nav, not as an island.
	// ──────────────────────────────────────────────────────────────
	[Fact]
	public void DocsetRootIsland_IsNotAnIsland_InIsolatedBuild()
	{
		// language=yaml
		var yaml =
			"""
		           island: true
		           project: 'docs-builder'
		           toc:
		             - file: index.md
		             - file: page.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddFile("/docs/index.md", new MockFileData("# Root"));
		fileSystem.AddFile("/docs/page.md", new MockFileData("# Page"));

		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(
			docSet,
			context,
			TestDocumentationFileFactory.Instance,
			crossLinkResolver: TestCrossLinkResolver.Instance
		);

		// IsIsland is stored but RendersAsIsland() returns false because Parent is null
		navigation.IsIsland.Should().BeTrue("docset declared island: true");
		navigation.RendersAsIsland().Should().BeFalse("isolated docset root has no parent so the island is suppressed");

		context.Diagnostics.Should().BeEmpty();
	}

	// ──────────────────────────────────────────────────────────────
	// island: true on a child toc.yml root
	// ──────────────────────────────────────────────────────────────
	[Fact]
	public async Task NestedTocIsland_RendersAsIsland()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'docs-builder'
		           toc:
		             - file: index.md
		             - toc: reference
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddFile("/docs/index.md", new MockFileData("# Root"));
		fileSystem.AddFile("/docs/reference/index.md", new MockFileData("# Reference"));
		fileSystem.AddFile("/docs/reference/page.md", new MockFileData("# Page"));
		fileSystem.AddFile(
			"/docs/reference/toc.yml",
			new MockFileData(
				// language=yaml
				"""
			island: true
			toc:
			  - file: index.md
			  - file: page.md
			"""
			)
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

		var reference = navigation
			.NavigationItems
			.ElementAt(0)
			.Should()
			.BeOfType<TableOfContentsNavigation<TestDocumentationFile>>()
			.Subject;

		reference.IsIsland.Should().BeTrue();
		reference.Parent.Should().NotBeNull("reference is a child of the docset");
		reference.RendersAsIsland().Should().BeTrue();

		// Pages inside the island can find the island root via FindIslandRoot
		var page = reference.NavigationItems.ElementAt(0).Should().BeOfType<FileNavigationLeaf<TestDocumentationFile>>().Subject;
		page.FindIslandRoot().Should().BeSameAs(reference);

		context.Diagnostics.Should().BeEmpty();
	}

	// ──────────────────────────────────────────────────────────────
	// island: true inline beside - toc: in the parent YAML
	// ──────────────────────────────────────────────────────────────
	[Fact]
	public async Task InlineTocEntryIsland_RendersAsIsland()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'docs-builder'
		           toc:
		             - file: index.md
		             - toc: advanced
		               island: true
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddFile("/docs/index.md", new MockFileData("# Root"));
		fileSystem.AddFile("/docs/advanced/index.md", new MockFileData("# Advanced"));
		fileSystem.AddFile("/docs/advanced/deep.md", new MockFileData("# Deep"));
		fileSystem.AddFile(
			"/docs/advanced/toc.yml",
			new MockFileData(
				// language=yaml
				"""
			toc:
			  - file: index.md
			  - file: deep.md
			"""
			)
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

		var advanced = navigation
			.NavigationItems
			.ElementAt(0)
			.Should()
			.BeOfType<TableOfContentsNavigation<TestDocumentationFile>>()
			.Subject;

		advanced.IsIsland.Should().BeTrue("inline island: true propagates to the resolved toc ref");
		advanced.RendersAsIsland().Should().BeTrue();

		context.Diagnostics.Should().BeEmpty();
	}

	// ──────────────────────────────────────────────────────────────
	// OR semantics: inline island: true + toc.yml island: true both work
	// ──────────────────────────────────────────────────────────────
	[Fact]
	public async Task Island_OrSemantics_BothSidesWork()
	{
		// language=yaml
		var yamlInlineOnly =
			"""
		                     project: 'docs-builder'
		                     toc:
		                       - file: index.md
		                       - toc: section
		                         island: true
		                     """;
		// language=yaml
		var yamlTocYmlOnly =
			"""
		                     project: 'docs-builder'
		                     toc:
		                       - file: index.md
		                       - toc: section
		                     """;

		// language=yaml
		var tocYmlIsland =
			"""
		                   island: true
		                   toc:
		                     - file: index.md
		                   """;
		// language=yaml
		var tocYmlPlain = """
		                  toc:
		                    - file: index.md
		                  """;

		async Task<TableOfContentsNavigation<TestDocumentationFile>> Build(string docsetYaml, string sectionTocYaml)
		{
			var fs = new MockFileSystem();
			fs.AddFile("/docs/index.md", new MockFileData("# Root"));
			fs.AddFile("/docs/section/index.md", new MockFileData("# Section"));
			fs.AddFile("/docs/section/toc.yml", new MockFileData(sectionTocYaml));
			var ctx = CreateContext(fs);
			var docSet = DocumentationSetFile.LoadAndResolve(ctx.Collector, docsetYaml, fs.NewDirInfo("docs"));
			_ = ctx.Collector.StartAsync(TestContext.Current.CancellationToken);
			var nav = new DocumentationSetNavigation<TestDocumentationFile>(
				docSet,
				ctx,
				TestDocumentationFileFactory.Instance,
				crossLinkResolver: TestCrossLinkResolver.Instance
			);
			await ctx.Collector.StopAsync(TestContext.Current.CancellationToken);
			return nav.NavigationItems.ElementAt(0).Should().BeOfType<TableOfContentsNavigation<TestDocumentationFile>>().Subject;
		}

		// inline only → island
		(await Build(yamlInlineOnly, tocYmlPlain)).RendersAsIsland().Should().BeTrue("inline island: true");
		// toc.yml only → island
		(await Build(yamlTocYmlOnly, tocYmlIsland)).RendersAsIsland().Should().BeTrue("toc.yml island: true");
		// both → island
		(await Build(yamlInlineOnly, tocYmlIsland)).RendersAsIsland().Should().BeTrue("both set");
		// neither → not island
		(await Build(yamlTocYmlOnly, tocYmlPlain)).RendersAsIsland().Should().BeFalse("neither set");
	}

	// ──────────────────────────────────────────────────────────────
	// Nested islands: FindIslandRoot returns the nearest enclosing island
	// ──────────────────────────────────────────────────────────────
	[Fact]
	public async Task FindIslandRoot_ReturnsNearestIsland_WhenIslandsNest()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'docs-builder'
		           toc:
		             - file: index.md
		             - toc: security
		               island: true
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddFile("/docs/index.md", new MockFileData("# Root"));
		fileSystem.AddFile("/docs/security/index.md", new MockFileData("# Security"));
		fileSystem.AddFile("/docs/security/rules/index.md", new MockFileData("# Rules"));
		fileSystem.AddFile("/docs/security/rules/page.md", new MockFileData("# Page"));
		fileSystem.AddFile(
			"/docs/security/toc.yml",
			new MockFileData(
				// language=yaml
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
			new MockFileData(
				// language=yaml
				"""
			toc:
			  - file: index.md
			  - file: page.md
			"""
			)
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

		var security = navigation
			.NavigationItems
			.ElementAt(0)
			.Should()
			.BeOfType<TableOfContentsNavigation<TestDocumentationFile>>()
			.Subject;
		security.RendersAsIsland().Should().BeTrue("security is an island");

		var rules = security.NavigationItems.ElementAt(0).Should().BeOfType<TableOfContentsNavigation<TestDocumentationFile>>().Subject;
		rules.RendersAsIsland().Should().BeTrue("rules is a nested island");

		// A page inside the rules island → nearest island is rules (not security)
		var page = rules.NavigationItems.ElementAt(0).Should().BeOfType<FileNavigationLeaf<TestDocumentationFile>>().Subject;
		page.FindIslandRoot().Should().BeSameAs(rules, "FindIslandRoot returns the nearest enclosing island");

		// The rules index page → nearest island is also rules
		rules.Index.FindIslandRoot().Should().BeSameAs(rules);

		// The security index page → nearest island is security
		security.Index.FindIslandRoot().Should().BeSameAs(security);

		context.Diagnostics.Should().BeEmpty();
	}

	// ──────────────────────────────────────────────────────────────
	// Listing island: folderNavigation.IsIsland = true
	// ──────────────────────────────────────────────────────────────
	[Fact]
	public async Task ListingIsland_MarksListingRootAsIsland()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'docs-builder'
		           toc:
		             - file: index.md
		             - listing: rules
		               glob: '**/*.md'
		               visual: groups
		               island: true
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddFile("/docs/index.md", new MockFileData("# Root"));
		fileSystem.AddFile("/docs/rules/index.md", new MockFileData("# Rules"));
		fileSystem.AddFile("/docs/rules/aws/index.md", new MockFileData("# AWS\nlisting:\n  group: aws"));
		fileSystem.AddFile("/docs/rules/aws/rule1.md", new MockFileData("# Rule 1\nlisting:\n  group: aws"));

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

		// Listing island root
		var listingRoot = navigation.NavigationItems.ElementAt(0).Should().BeOfType<FolderNavigation<TestDocumentationFile>>().Subject;

		listingRoot.IsIsland.Should().BeTrue();
		listingRoot.RendersAsIsland().Should().BeTrue("listing root has a parent");

		// All pages inside should find the listing root as their island
		foreach (var item in listingRoot.NavigationItems)
		{
			item.FindIslandRoot().Should().BeSameAs(listingRoot);
		}

		context.Diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task ListingIsland_WithVisualNone_EmitsError()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'docs-builder'
		           toc:
		             - file: index.md
		             - listing: rules
		               glob: '**/*.md'
		               island: true
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddFile("/docs/index.md", new MockFileData("# Root"));
		fileSystem.AddFile("/docs/rules/index.md", new MockFileData("# Rules"));
		fileSystem.AddFile("/docs/rules/rule1.md", new MockFileData("# Rule 1"));

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

		// The listing with island: true and no visual: should emit an error
		context
			.Diagnostics
			.Should()
			.ContainSingle(d => d.Severity == Severity.Error && d.Message.Contains("island: true") && d.Message.Contains("visual: none"));
		// And the listing is not added to navigation
		navigation.NavigationItems.Should().BeEmpty();
	}
}
