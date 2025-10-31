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

public class FileNavigationTests(ITestOutputHelper output) : DocumentationSetNavigationTestBase(output)
{
	[Fact]
	public void FileWithNoChildrenCreatesFileNavigationLeaf()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: getting-started.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		navigation.NavigationItems.Should().HaveCount(0);
		var fileNav = navigation.Index.Should().BeOfType<FileNavigationLeaf<IDocumentationFile>>().Subject;
		fileNav.Url.Should().Be("/getting-started/");
	}

	[Fact]
	public void FileWithChildrenCreatesFileNavigation()
	{
		// language=yaml
		var yaml = """
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

		navigation.NavigationItems.Should().HaveCount(1);
		var fileNav = navigation.NavigationItems.First().Should().BeOfType<VirtualFileNavigation<IDocumentationFile>>().Subject;
		fileNav.Url.Should().Be("/guide/");
		fileNav.NavigationItems.Should().HaveCount(2);

		var section1 = fileNav.NavigationItems.ElementAt(0).Should().BeOfType<FileNavigationLeaf<IDocumentationFile>>().Subject;
		section1.Url.Should().Be("/section1/");
		section1.Parent.Should().BeSameAs(fileNav);

		var section2 = fileNav.NavigationItems.ElementAt(1).Should().BeOfType<FileNavigationLeaf<IDocumentationFile>>().Subject;
		section2.Url.Should().Be("/section2/");
		section2.Parent.Should().BeSameAs(fileNav);
	}

	[Fact]
	public void FileWithChildrenDeeplinksPreservesPaths()
	{
		// language=yaml
		var yaml = """
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

		navigation.NavigationItems.Should().HaveCount(1);
		var fileNav = navigation.NavigationItems.First().Should().BeOfType<VirtualFileNavigation<IDocumentationFile>>().Subject;
		fileNav.Url.Should().Be("/nest/guide/");
		fileNav.NavigationItems.Should().HaveCount(2);

		var section1 = fileNav.NavigationItems.ElementAt(0).Should().BeOfType<FileNavigationLeaf<IDocumentationFile>>().Subject;
		section1.Url.Should().Be("/nest/section1/");
		section1.Parent.Should().BeSameAs(fileNav);

		var section2 = fileNav.NavigationItems.ElementAt(1).Should().BeOfType<FileNavigationLeaf<IDocumentationFile>>().Subject;
		section2.Url.Should().Be("/nest/section2/");
		section2.Parent.Should().BeSameAs(fileNav);
	}

	[Fact]
	public void FileWithNestedChildrenBuildsCorrectly()
	{
		// language=yaml
		var yaml = """
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

		navigation.NavigationItems.Should().HaveCount(1);
		var guideFile = navigation.NavigationItems.First().Should().BeOfType<VirtualFileNavigation<IDocumentationFile>>().Subject;
		guideFile.Url.Should().Be("/guide/");
		guideFile.NavigationItems.Should().HaveCount(1);

		var chapter1 = guideFile.NavigationItems.First().Should().BeOfType<VirtualFileNavigation<IDocumentationFile>>().Subject;
		chapter1.Url.Should().Be("/chapter1/");
		chapter1.Parent.Should().BeSameAs(guideFile);
		chapter1.NavigationItems.Should().HaveCount(1);

		var subsection = chapter1.NavigationItems.First().Should().BeOfType<FileNavigationLeaf<IDocumentationFile>>().Subject;
		subsection.Url.Should().Be("/subsection/");
		subsection.Parent.Should().BeSameAs(chapter1);
	}

	[Fact]
	public void FileNavigationUrlUpdatesWhenRootChanges()
	{
		// language=yaml
		var yaml = """
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
		fileNav.Url.Should().Be("/guide/");
		child.Url.Should().Be("/section1/");

		// Change root URL
		navigation.HomeProvider = new NavigationHomeProvider("/v2", navigation.NavigationRoot);

		// URLs should update dynamically
		fileNav.Url.Should().Be("/v2/guide/");
		child.Url.Should().Be("/v2/section1/");
	}

	[Fact]
	public void FileNavigationMixedWithFolderChildren()
	{
		// language=yaml
		var yaml = """
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

		var guideFile = navigation.NavigationItems.First().Should().BeOfType<VirtualFileNavigation<IDocumentationFile>>().Subject;
		guideFile.NavigationItems.Should().HaveCount(2);

		var intro = guideFile.NavigationItems.ElementAt(0).Should().BeOfType<FileNavigationLeaf<IDocumentationFile>>().Subject;
		intro.Url.Should().Be("/intro/");

		var advancedFolder = guideFile.NavigationItems.ElementAt(1).Should().BeOfType<FolderNavigation<IDocumentationFile>>().Subject;
		advancedFolder.Url.Should().Be("/advanced/topics/"); // No index, uses first child
		advancedFolder.NavigationItems.Should().HaveCount(0);

		var topics = advancedFolder.Index.Should().BeOfType<FileNavigationLeaf<IDocumentationFile>>().Subject;
		topics.Url.Should().Be("/advanced/topics/");
	}
}
