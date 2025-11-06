// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation.Isolated;
using Elastic.Documentation.Navigation.Isolated.Leaf;
using Elastic.Documentation.Navigation.Isolated.Node;
using FluentAssertions;

namespace Elastic.Documentation.Navigation.Tests.Isolation;

public class DynamicUrlTests(ITestOutputHelper output) : DocumentationSetNavigationTestBase(output)
{
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

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext();
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);
		var folder = navigation.NavigationItems.First() as FolderNavigation<IDocumentationFile>;
		folder.Should().NotBeNull();
		var file = folder.Index;

		// Initial URL
		file.Url.Should().Be("/setup/install");

		// Change root URL
		navigation.HomeProvider = new NavigationHomeProvider("/v8.0", navigation.NavigationRoot);

		// URLs should update dynamically
		// Since folder has no index child, its URL is the first child's URL
		folder.Url.Should().Be("/v8.0/setup/install");
		file.Url.Should().Be("/v8.0/setup/install");

		// Change root URL
		navigation.HomeProvider = new NavigationHomeProvider("/v9.0", navigation.NavigationRoot);

		// URLs should update dynamically
		// Since folder has no index child, its URL is the first child's URL
		folder.Url.Should().Be("/v9.0/setup/install");
		file.Url.Should().Be("/v9.0/setup/install");
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

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext();
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);
		var outerFolder = navigation.NavigationItems.First() as FolderNavigation<IDocumentationFile>;
		var innerFolder = outerFolder!.NavigationItems.First() as FolderNavigation<IDocumentationFile>;
		var file = innerFolder!.Index;

		file.Url.Should().Be("/outer/inner/deep");

		// Change root URL
		navigation.HomeProvider = new NavigationHomeProvider("/base", navigation.NavigationRoot);

		file.Url.Should().Be("/base/outer/inner/deep");
	}

	[Fact]
	public void FolderWithoutIndexUsesFirstChildUrl()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: guides
		               children:
		                 - file: getting-started.md
		                 - file: advanced.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext();
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);
		var folder = navigation.NavigationItems.First() as FolderNavigation<IDocumentationFile>;

		// Folder has no index.md, so URL should be the first child's URL
		folder!.Url.Should().Be("/guides/getting-started");
	}

	[Fact]
	public void FolderWithNestedChildren()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: guides
		               children:
		                 - file: getting-started.md
		                   children:
		                     - file: advanced.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext();
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);
		var folder = navigation.NavigationItems.First() as FolderNavigation<IDocumentationFile>;

		// Folder has no index.md, so URL should be the first child's URL
		folder!.Url.Should().Be("/guides/getting-started");

		var gettingStarted = folder.NavigationItems.First() as VirtualFileNavigation<IDocumentationFile>;
		gettingStarted.Should().NotBeNull();
		gettingStarted.Url.Should().Be("/guides/getting-started");
		var advanced = gettingStarted.NavigationItems.First() as FileNavigationLeaf<IDocumentationFile>;
		advanced.Should().NotBeNull();
		advanced.Url.Should().Be("/guides/advanced");

		advanced.Parent.Should().BeSameAs(gettingStarted);
		gettingStarted.Parent.Should().BeSameAs(folder);
	}

	[Fact]
	public void FolderWithNestedDeeplinkedChildren()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: guides
		               children:
		                 - file: clients/getting-started.md
		                   children:
		                     - file: advanced.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext();
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);
		var folder = navigation.NavigationItems.First() as FolderNavigation<IDocumentationFile>;

		// Folder has no index.md, so URL should be the first child's URL
		folder!.Url.Should().Be("/guides/clients/getting-started");

		var gettingStarted = folder.NavigationItems.First() as VirtualFileNavigation<IDocumentationFile>;
		gettingStarted.Should().NotBeNull();
		gettingStarted.Url.Should().Be("/guides/clients/getting-started");
		var advanced = gettingStarted.NavigationItems.First() as FileNavigationLeaf<IDocumentationFile>;
		advanced.Should().NotBeNull();
		advanced.Url.Should().Be("/guides/advanced");

		advanced.Parent.Should().BeSameAs(gettingStarted);
		gettingStarted.Parent.Should().BeSameAs(folder);
	}

	[Fact]
	public void FolderWithNestedDeeplinkedOfIndexChildren()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: guides
		               children:
		                 - file: clients/index.md
		                   children:
		                     - file: advanced.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext();
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);
		var folder = navigation.NavigationItems.First() as FolderNavigation<IDocumentationFile>;

		// Folder has no index.md, so URL should be the first child's URL
		folder!.Url.Should().Be("/guides/clients");

		var gettingStarted = folder.NavigationItems.First() as VirtualFileNavigation<IDocumentationFile>;
		gettingStarted.Should().NotBeNull();
		gettingStarted.Url.Should().Be("/guides/clients");
		var advanced = gettingStarted.NavigationItems.First() as FileNavigationLeaf<IDocumentationFile>;
		advanced.Should().NotBeNull();
		advanced.Url.Should().Be("/guides/advanced");

		advanced.Parent.Should().BeSameAs(gettingStarted);
		gettingStarted.Parent.Should().BeSameAs(folder);
	}

	[Fact]
	public void FolderWithIndexUsesOwnUrl()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: guides
		               children:
		                 - file: index.md
		                 - file: advanced.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext();
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);
		var folder = navigation.NavigationItems.First() as FolderNavigation<IDocumentationFile>;

		// Folder has index.md, so URL should be the folder path
		folder!.Url.Should().Be("/guides");
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
		           """;

		// language=yaml
		var tocYaml = """
		              toc:
		                - file: reference.md
		              """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs/guides/api");
		fileSystem.AddFile("/docs/guides/api/toc.yml", new MockFileData(tocYaml));
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);
		var folder = navigation.NavigationItems.First() as FolderNavigation<IDocumentationFile>;
		var toc = folder!.NavigationItems.First() as TableOfContentsNavigation<IDocumentationFile>;
		var file = toc!.Index;

		// The TOC becomes the new URL root, so the file URL is based on TOC's URL
		toc.Url.Should().Be("/guides/api/reference");
		file.Url.Should().Be("/guides/api/reference");

		// Change root URL
		navigation.HomeProvider = new NavigationHomeProvider("/v2", navigation.NavigationRoot);

		// Both TOC and file URLs should update
		navigation.Url.Should().Be("/v2/guides/api/reference");
		toc.Url.Should().Be("/v2/guides/api/reference");
		file.Url.Should().Be("/v2/guides/api/reference");
	}
}
