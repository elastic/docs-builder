// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Navigation.Isolated;
using FluentAssertions;

namespace Elastic.Documentation.Navigation.Tests;

public class DocumentationSetNavigationTests(ITestOutputHelper output)
{
	private TestDocumentationSetContext CreateContext(MockFileSystem? fileSystem = null)
	{
		fileSystem ??= new MockFileSystem();
		var sourceDir = fileSystem.DirectoryInfo.New("/docs");
		var outputDir = fileSystem.DirectoryInfo.New("/output");
		var configPath = fileSystem.FileInfo.New("/docs/docset.yml");

		return new TestDocumentationSetContext(fileSystem, sourceDir, outputDir, configPath, output);
	}

	[Fact]
	public void ConstructorInitializesRootProperties()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: index.md
		           """;

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext();

		var navigation = new DocumentationSetNavigation(docSet, context);

		navigation.NavigationRoot.Should().BeSameAs(navigation);
		navigation.Parent.Should().BeNull();
		navigation.Depth.Should().Be(0);
		navigation.Hidden.Should().BeFalse();
		navigation.IsCrossLink.Should().BeFalse();
		navigation.Id.Should().NotBeNullOrEmpty();
		navigation.NavigationTitle.Should().Be("test-project");
		navigation.IsUsingNavigationDropdown.Should().BeFalse();
		navigation.Url.Should().Be("/");
	}

	[Fact]
	public void ConstructorSetsIsUsingNavigationDropdownFromFeatures()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           features:
		             primary-nav: true
		           toc:
		             - file: index.md
		           """;

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext();

		var navigation = new DocumentationSetNavigation(docSet, context);

		navigation.IsUsingNavigationDropdown.Should().BeTrue();
	}

	[Fact]
	public void ConstructorCreatesFileNavigationLeafFromFileRef()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: getting-started.md
		           """;

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext();

		var navigation = new DocumentationSetNavigation(docSet, context);

		navigation.NavigationItems.Should().HaveCount(1);
		var fileNav = navigation.NavigationItems.First().Should().BeOfType<FileNavigationLeaf>().Subject;
		fileNav.NavigationTitle.Should().Be("getting-started");
		fileNav.Url.Should().Be("/getting-started");
		fileNav.Hidden.Should().BeFalse();
		fileNav.NavigationRoot.Should().BeSameAs(navigation);
		fileNav.Parent.Should().BeNull();
	}

	[Fact]
	public void ConstructorCreatesHiddenFileNavigationLeaf()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - hidden: 404.md
		           """;

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext();

		var navigation = new DocumentationSetNavigation(docSet, context);

		navigation.NavigationItems.Should().HaveCount(1);
		var fileNav = navigation.NavigationItems.First().Should().BeOfType<FileNavigationLeaf>().Subject;
		fileNav.Hidden.Should().BeTrue();
		fileNav.Url.Should().Be("/404");
	}

	[Fact]
	public void ConstructorCreatesCrossLinkNavigation()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - title: "External Guide"
		               crosslink: docs-content://guide.md
		           """;

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext();

		var navigation = new DocumentationSetNavigation(docSet, context);

		navigation.NavigationItems.Should().HaveCount(1);
		var crossLink = navigation.NavigationItems.First().Should().BeOfType<CrossLinkNavigationLeaf>().Subject;
		crossLink.NavigationTitle.Should().Be("External Guide");
		crossLink.Url.Should().Be("docs-content://guide.md");
		crossLink.IsCrossLink.Should().BeTrue();
	}

	[Fact]
	public void ConstructorCreatesFolderNavigationWithChildren()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: setup
		               children:
		                 - file: index.md
		                 - file: install.md
		           """;

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext();

		var navigation = new DocumentationSetNavigation(docSet, context);

		navigation.NavigationItems.Should().HaveCount(1);
		var folder = navigation.NavigationItems.First().Should().BeOfType<FolderNavigation>().Subject;
		folder.Depth.Should().Be(1);
		folder.Url.Should().Be("/setup");
		folder.NavigationItems.Should().HaveCount(2);

		var firstFile = folder.NavigationItems.ElementAt(0).Should().BeOfType<FileNavigationLeaf>().Subject;
		firstFile.Url.Should().Be("/setup/index");
		firstFile.Parent.Should().BeSameAs(folder);

		var secondFile = folder.NavigationItems.ElementAt(1).Should().BeOfType<FileNavigationLeaf>().Subject;
		secondFile.Url.Should().Be("/setup/install");
	}

	[Fact]
	public void ConstructorCreatesTableOfContentsNavigationWithChildren()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - toc: api
		               children:
		                 - file: index.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs/api");
		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext(fileSystem);

		var navigation = new DocumentationSetNavigation(docSet, context);

		navigation.NavigationItems.Should().HaveCount(1);
		var toc = navigation.NavigationItems.First().Should().BeOfType<TableOfContentsNavigation>().Subject;
		toc.Depth.Should().Be(1);
		toc.Url.Should().Be("/api");
		toc.NavigationItems.Should().HaveCount(1);

		var file = toc.NavigationItems.First().Should().BeOfType<FileNavigationLeaf>().Subject;
		file.Url.Should().Be("/api/index");
		file.Parent.Should().BeSameAs(toc);
		file.NavigationRoot.Should().BeSameAs(navigation);
	}

	[Fact]
	public void DynamicUrlUpdatesWhenRootUrlChanges()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: setup
		               children:
		                 - file: install.md
		           """;

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext();

		var navigation = new DocumentationSetNavigation(docSet, context);
		var folder = navigation.NavigationItems.First() as FolderNavigation;
		var file = folder!.NavigationItems.First();

		// Initial URL
		file.Url.Should().Be("/setup/install");

		// Change root URL
		navigation.Url = "/v8.0";

		// URLs should update dynamically
		folder.Url.Should().Be("/v8.0/setup");
		file.Url.Should().Be("/v8.0/setup/install");
	}

	[Fact]
	public void UrlRootPropagatesCorrectlyThroughFolders()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: outer
		               children:
		                 - folder: inner
		                   children:
		                     - file: deep.md
		           """;

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext();

		var navigation = new DocumentationSetNavigation(docSet, context);
		var outerFolder = navigation.NavigationItems.First() as FolderNavigation;
		var innerFolder = outerFolder!.NavigationItems.First() as FolderNavigation;
		var file = innerFolder!.NavigationItems.First();

		file.Url.Should().Be("/outer/inner/deep");

		// Change root URL
		navigation.Url = "/base";

		file.Url.Should().Be("/base/outer/inner/deep");
	}

	[Fact]
	public void UrlRootChangesForTableOfContentsNavigation()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: guides
		               children:
		                 - toc: api
		                   children:
		                     - file: reference.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs/guides/api");
		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext(fileSystem);

		var navigation = new DocumentationSetNavigation(docSet, context);
		var folder = navigation.NavigationItems.First() as FolderNavigation;
		var toc = folder!.NavigationItems.First() as TableOfContentsNavigation;
		var file = toc!.NavigationItems.First();

		// The TOC becomes the new URL root, so the file URL is based on TOC's URL
		toc.Url.Should().Be("/guides/api");
		file.Url.Should().Be("/guides/api/reference");

		// Change root URL
		navigation.Url = "/v2";

		// Both TOC and file URLs should update
		toc.Url.Should().Be("/v2/guides/api");
		file.Url.Should().Be("/v2/guides/api/reference");
	}

	[Fact]
	public async Task ValidationEmitsErrorWhenTableOfContentsHasNonTocChildrenAndNestedTocNotAllowed()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - toc: api
		               children:
		                 - toc: nested-toc
		                   children:
		                     - file: should-error.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs/api");
		fileSystem.AddDirectory("/docs/api/nested-toc");
		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext(fileSystem);
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		_ = new DocumentationSetNavigation(docSet, context);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		var diagnostics = context.Diagnostics;
		diagnostics.Should().ContainSingle(d =>
			d.Message.Contains("TableOfContents navigation does not allow nested children"));
	}

	[Fact]
	public async Task ValidationEmitsErrorWhenTableOfContentsHasNonTocChildren()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - toc: api
		               children:
		                 - file: should-error.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs/api");
		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext(fileSystem);
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		_ = new DocumentationSetNavigation(docSet, context);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		// Check using Errors count instead of Diagnostics collection
		context.Collector.Errors.Should().BeGreaterThan(0);
		var diagnostics = context.Diagnostics;
		diagnostics.Should().ContainSingle(d =>
			d.Message.Contains("TableOfContents navigation may only contain other TOC references as children"));
	}

	[Fact]
	public void NavigationIndexIsSetCorrectly()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: first.md
		             - file: second.md
		             - file: third.md
		           """;

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext();

		var navigation = new DocumentationSetNavigation(docSet, context);

		navigation.NavigationItems.ElementAt(0).NavigationIndex.Should().Be(0);
		navigation.NavigationItems.ElementAt(1).NavigationIndex.Should().Be(1);
		navigation.NavigationItems.ElementAt(2).NavigationIndex.Should().Be(2);
	}

	[Fact]
	public async Task ComplexNestedStructureBuildsCorrectly()
	{
		// language=yaml
		var yaml = """
		           project: 'docs-builder'
		           features:
		             primary-nav: true
		           toc:
		             - file: index.md
		             - folder: setup
		               children:
		                 - file: index.md
		                 - toc: advanced
		                   children:
		                     - toc: performance
		                       children:
		                         - file: index.md
		             - title: "External"
		               crosslink: other://link.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs/setup/advanced");
		fileSystem.AddDirectory("/docs/setup/advanced/performance");
		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext(fileSystem);
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		var navigation = new DocumentationSetNavigation(docSet, context);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);


		navigation.NavigationItems.Should().HaveCount(3);
		navigation.IsUsingNavigationDropdown.Should().BeTrue();

		// First item: simple file
		var indexFile = navigation.NavigationItems.ElementAt(0).Should().BeOfType<FileNavigationLeaf>().Subject;
		indexFile.Url.Should().Be("/index");

		// Second item: complex nested structure
		var setupFolder = navigation.NavigationItems.ElementAt(1).Should().BeOfType<FolderNavigation>().Subject;
		setupFolder.NavigationItems.Should().HaveCount(2);

		var setupIndex = setupFolder.NavigationItems.ElementAt(0).Should().BeOfType<FileNavigationLeaf>().Subject;
		setupIndex.Url.Should().Be("/setup/index");

		var advancedToc = setupFolder.NavigationItems.ElementAt(1).Should().BeOfType<TableOfContentsNavigation>().Subject;
		advancedToc.Url.Should().Be("/setup/advanced");
		advancedToc.NavigationItems.Should().HaveCount(1);

		var performanceToc = advancedToc.NavigationItems.First().Should().BeOfType<TableOfContentsNavigation>().Subject;
		performanceToc.Url.Should().Be("/setup/advanced/performance");

		var perfIndex = performanceToc.NavigationItems.First().Should().BeOfType<FileNavigationLeaf>().Subject;
		perfIndex.Url.Should().Be("/setup/advanced/performance/index");

		// Third item: crosslink
		var crosslink = navigation.NavigationItems.ElementAt(2).Should().BeOfType<CrossLinkNavigationLeaf>().Subject;
		crosslink.IsCrossLink.Should().BeTrue();

		// Verify no errors were emitted
		context.Diagnostics.Should().BeEmpty();
	}

}
