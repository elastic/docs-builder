// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation.Isolated;
using Elastic.Documentation.Navigation.Isolated.Leaf;
using Elastic.Documentation.Navigation.Isolated.Node;
using Elastic.Documentation.Testing;

namespace Elastic.Documentation.Navigation.Tests.Isolation;

public class DynamicUrlTests() : DocumentationSetNavigationTestBase()
{
	[Test]
	public void DynamicUrlUpdatesWhenRootUrlChanges()
	{
		// language=yaml
		var yaml =
			"""
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

	[Test]
	public void UrlRootPropagatesCorrectlyThroughFolders()
	{
		// language=yaml
		var yaml =
			"""
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

	[Test]
	public void FolderWithoutIndexUsesFirstChildUrl()
	{
		// language=yaml
		var yaml =
			"""
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
		NavigationDump
			.ToText(navigation)
			.Should()
			.MatchInlineSnapshot(
				"""
			DocumentationSetNavigation  /guides/getting-started  "getting-started"
			  index: FileNavigationLeaf  /guides/getting-started  "getting-started"
			  FolderNavigation  /guides/getting-started  "getting-started"
			    index: FileNavigationLeaf  /guides/getting-started  "getting-started"
			    FileNavigationLeaf  /guides/advanced  "advanced"
			"""
			);
	}

	[Test]
	public void FolderWithoutIndexUsesFirstVisibleChildUrlWhenHiddenChildComesFirst()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - folder: guides
		               children:
		                 - hidden: autopilot.md
		                 - file: getting-started.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext();
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);
		NavigationDump
			.ToText(navigation)
			.Should()
			.MatchInlineSnapshot(
				"""
			DocumentationSetNavigation  /guides/getting-started  "getting-started"
			  index: FileNavigationLeaf  /guides/getting-started  "getting-started"
			  FolderNavigation  /guides/getting-started  "getting-started"
			    index: FileNavigationLeaf  /guides/getting-started  "getting-started"
			    FileNavigationLeaf  /guides/autopilot  "autopilot" [hidden]
			"""
			);
	}

	[Test]
	public void FolderWithoutIndexAndOnlyHiddenChildrenIsHidden()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - folder: guides
		               children:
		                 - hidden: autopilot.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext();
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);
		NavigationDump
			.ToText(navigation)
			.Should()
			.MatchInlineSnapshot(
				"""
			DocumentationSetNavigation  /guides/autopilot  "autopilot"
			  index: FileNavigationLeaf  /guides/autopilot  "autopilot" [hidden]
			  FolderNavigation  /guides/autopilot  "autopilot" [hidden]
			    index: FileNavigationLeaf  /guides/autopilot  "autopilot" [hidden]
			"""
			);
	}

	[Test]
	public void FolderWithNestedChildren()
	{
		// language=yaml
		var yaml =
			"""
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
		NavigationDump
			.ToText(navigation)
			.Should()
			.MatchInlineSnapshot(
				"""
			DocumentationSetNavigation  /guides/getting-started  "getting-started"
			  index: FileNavigationLeaf  /guides/getting-started  "getting-started"
			  FolderNavigation  /guides/getting-started  "getting-started"
			    index: FileNavigationLeaf  /guides/getting-started  "getting-started"
			    VirtualFileNavigation  /guides/getting-started  "getting-started"
			      index: FileNavigationLeaf  /guides/getting-started  "getting-started"
			      FileNavigationLeaf  /guides/advanced  "advanced"
			"""
			);
	}

	[Test]
	public void FolderWithNestedDeeplinkedChildren()
	{
		// language=yaml
		var yaml =
			"""
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
		NavigationDump
			.ToText(navigation)
			.Should()
			.MatchInlineSnapshot(
				"""
			DocumentationSetNavigation  /guides/clients/getting-started  "getting-started"
			  index: FileNavigationLeaf  /guides/clients/getting-started  "getting-started"
			  FolderNavigation  /guides/clients/getting-started  "getting-started"
			    index: FileNavigationLeaf  /guides/clients/getting-started  "getting-started"
			    VirtualFileNavigation  /guides/clients/getting-started  "getting-started"
			      index: FileNavigationLeaf  /guides/clients/getting-started  "getting-started"
			      FileNavigationLeaf  /guides/advanced  "advanced"
			"""
			);
	}

	[Test]
	public void FolderWithNestedDeeplinkedOfIndexChildren()
	{
		// language=yaml
		var yaml =
			"""
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
		NavigationDump
			.ToText(navigation)
			.Should()
			.MatchInlineSnapshot(
				"""
			DocumentationSetNavigation  /guides/clients  "index"
			  index: FileNavigationLeaf  /guides/clients  "index"
			  FolderNavigation  /guides/clients  "index"
			    index: FileNavigationLeaf  /guides/clients  "index"
			    VirtualFileNavigation  /guides/clients  "index"
			      index: FileNavigationLeaf  /guides/clients  "index"
			      FileNavigationLeaf  /guides/advanced  "advanced"
			"""
			);
	}

	[Test]
	public void FolderWithIndexUsesOwnUrl()
	{
		// language=yaml
		var yaml =
			"""
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
		NavigationDump
			.ToText(navigation)
			.Should()
			.MatchInlineSnapshot(
				"""
			DocumentationSetNavigation  /guides  "index"
			  index: FileNavigationLeaf  /guides  "index"
			  FolderNavigation  /guides  "index"
			    index: FileNavigationLeaf  /guides  "index"
			    FileNavigationLeaf  /guides/advanced  "advanced"
			"""
			);
	}

	[Test]
	public void UrlRootChangesForTableOfContentsNavigation()
	{
		// language=yaml
		var yaml =
			"""
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
