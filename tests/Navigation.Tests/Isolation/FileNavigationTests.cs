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

namespace Elastic.Documentation.Navigation.Tests.Isolation;

public class FileNavigationTests() : DocumentationSetNavigationTestBase()
{
	[Test]
	public void FileWithNoChildrenCreatesFileNavigationLeaf()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - file: getting-started.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		NavigationTreeDump
			.ToText(navigation)
			.Should()
			.MatchInlineSnapshot(
				"""
			DocumentationSetNavigation  /getting-started  "getting-started"
			  index: FileNavigationLeaf  /getting-started  "getting-started"
			"""
			);
	}

	[Test]
	public void FileWithChildrenCreatesFileNavigation()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - file: guide.md
		               children:
		                 - file: section1.md
		                 - file: section2.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		NavigationTreeDump
			.ToText(navigation)
			.Should()
			.MatchInlineSnapshot(
				"""
			DocumentationSetNavigation  /guide  "guide"
			  index: FileNavigationLeaf  /guide  "guide"
			  VirtualFileNavigation  /guide  "guide"
			    index: FileNavigationLeaf  /guide  "guide"
			    FileNavigationLeaf  /section1  "section1"
			    FileNavigationLeaf  /section2  "section2"
			"""
			);
	}

	[Test]
	public void FileWithChildrenDeeplinksPreservesPaths()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - file: nest/guide.md
		               children:
		                 - file: section1.md
		                 - file: section2.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		NavigationTreeDump
			.ToText(navigation)
			.Should()
			.MatchInlineSnapshot(
				"""
			DocumentationSetNavigation  /nest/guide  "guide"
			  index: FileNavigationLeaf  /nest/guide  "guide"
			  VirtualFileNavigation  /nest/guide  "guide"
			    index: FileNavigationLeaf  /nest/guide  "guide"
			    FileNavigationLeaf  /nest/section1  "section1"
			    FileNavigationLeaf  /nest/section2  "section2"
			"""
			);
	}

	[Test]
	public void FileWithNestedChildrenBuildsCorrectly()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - file: guide.md
		               children:
		                 - file: chapter1.md
		                   children:
		                     - file: subsection.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		NavigationTreeDump
			.ToText(navigation)
			.Should()
			.MatchInlineSnapshot(
				"""
			DocumentationSetNavigation  /guide  "guide"
			  index: FileNavigationLeaf  /guide  "guide"
			  VirtualFileNavigation  /guide  "guide"
			    index: FileNavigationLeaf  /guide  "guide"
			    VirtualFileNavigation  /chapter1  "chapter1"
			      index: FileNavigationLeaf  /chapter1  "chapter1"
			      FileNavigationLeaf  /subsection  "subsection"
			"""
			);
	}

	[Test]
	public void FileNavigationUrlUpdatesWhenRootChanges()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - file: guide.md
		               children:
		                 - file: section1.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);
		var fileNav = navigation.NavigationItems.First() as INodeNavigationItem<IDocumentationFile, INavigationItem>;
		var child = fileNav!.NavigationItems.First();

		// Initial URLs
		fileNav.Url.Should().Be("/guide");
		child.Url.Should().Be("/section1");

		// Change root URL
		navigation.HomeProvider = new NavigationHomeProvider("/v2", navigation.NavigationRoot);

		// URLs should update dynamically
		fileNav.Url.Should().Be("/v2/guide");
		child.Url.Should().Be("/v2/section1");
	}

	[Test]
	public void FileNavigationMixedWithFolderChildren()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - file: guide.md
		               children:
		                 - file: intro.md
		                 - folder: advanced
		                   children:
		                     - file: topics.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		NavigationTreeDump
			.ToText(navigation)
			.Should()
			.MatchInlineSnapshot(
				"""
			DocumentationSetNavigation  /guide  "guide"
			  index: FileNavigationLeaf  /guide  "guide"
			  VirtualFileNavigation  /guide  "guide"
			    index: FileNavigationLeaf  /guide  "guide"
			    FileNavigationLeaf  /intro  "intro"
			    FolderNavigation  /advanced/topics  "topics"
			      index: FileNavigationLeaf  /advanced/topics  "topics"
			"""
			);
	}
}
