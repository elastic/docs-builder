// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Navigation.Isolated;
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

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext();

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		navigation.NavigationItems.Should().HaveCount(1);
		var fileNav = navigation.NavigationItems.First().Should().BeOfType<FileNavigationLeaf<IDocumentationFile>>().Subject;
		fileNav.Url.Should().Be("/getting-started");
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

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext();

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		navigation.NavigationItems.Should().HaveCount(1);
		var fileNav = navigation.NavigationItems.First().Should().BeOfType<VirtualFileNavigation<IDocumentationFile>>().Subject;
		fileNav.Url.Should().Be("/guide");
		fileNav.NavigationItems.Should().HaveCount(2);

		var section1 = fileNav.NavigationItems.ElementAt(0).Should().BeOfType<FileNavigationLeaf<IDocumentationFile>>().Subject;
		section1.Url.Should().Be("/guide/section1");
		section1.Parent.Should().BeSameAs(fileNav);

		var section2 = fileNav.NavigationItems.ElementAt(1).Should().BeOfType<FileNavigationLeaf<IDocumentationFile>>().Subject;
		section2.Url.Should().Be("/guide/section2");
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
		                 - file: nest/section1.md
		                 - file: nest/section2.md
		           """;

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext();

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		navigation.NavigationItems.Should().HaveCount(1);
		var fileNav = navigation.NavigationItems.First().Should().BeOfType<VirtualFileNavigation<IDocumentationFile>>().Subject;
		fileNav.Url.Should().Be("/nest/guide");
		fileNav.NavigationItems.Should().HaveCount(2);

		var section1 = fileNav.NavigationItems.ElementAt(0).Should().BeOfType<FileNavigationLeaf<IDocumentationFile>>().Subject;
		section1.Url.Should().Be("/nest/guide/section1");
		section1.Parent.Should().BeSameAs(fileNav);

		var section2 = fileNav.NavigationItems.ElementAt(1).Should().BeOfType<FileNavigationLeaf<IDocumentationFile>>().Subject;
		section2.Url.Should().Be("/nest/guide/section2");
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

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext();

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		navigation.NavigationItems.Should().HaveCount(1);
		var guideFile = navigation.NavigationItems.First().Should().BeOfType<VirtualFileNavigation<IDocumentationFile>>().Subject;
		guideFile.Url.Should().Be("/guide");
		guideFile.NavigationItems.Should().HaveCount(1);

		var chapter1 = guideFile.NavigationItems.First().Should().BeOfType<VirtualFileNavigation<IDocumentationFile>>().Subject;
		chapter1.Url.Should().Be("/guide/chapter1");
		chapter1.Parent.Should().BeSameAs(guideFile);
		chapter1.NavigationItems.Should().HaveCount(1);

		var subsection = chapter1.NavigationItems.First().Should().BeOfType<FileNavigationLeaf<IDocumentationFile>>().Subject;
		subsection.Url.Should().Be("/guide/chapter1/subsection");
		subsection.Parent.Should().BeSameAs(chapter1);
	}

	[Fact]
	public async Task IndexFileWithChildrenEmitsError()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: index.md
		               children:
		                 - file: section1.md
		           """;

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext();
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		_ = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		var diagnostics = context.Diagnostics;
		diagnostics.Should().ContainSingle(d =>
			d.Message.Contains("is an index file and may not have children"));
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

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext();

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);
		var fileNav = navigation.NavigationItems.First() as INodeNavigationItem<IDocumentationFile, INavigationItem>;
		var child = fileNav!.NavigationItems.First();

		// Initial URLs
		fileNav.Url.Should().Be("/guide");
		child.Url.Should().Be("/guide/section1");

		// Change root URL
		navigation.PathPrefixProvider = new PathPrefixProvider("/v2");

		// URLs should update dynamically
		fileNav.Url.Should().Be("/v2/guide");
		child.Url.Should().Be("/v2/guide/section1");
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

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext();

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		var guideFile = navigation.NavigationItems.First().Should().BeOfType<VirtualFileNavigation<IDocumentationFile>>().Subject;
		guideFile.NavigationItems.Should().HaveCount(2);

		var intro = guideFile.NavigationItems.ElementAt(0).Should().BeOfType<FileNavigationLeaf<IDocumentationFile>>().Subject;
		intro.Url.Should().Be("/guide/intro");

		var advancedFolder = guideFile.NavigationItems.ElementAt(1).Should().BeOfType<FolderNavigation>().Subject;
		advancedFolder.Url.Should().Be("/guide/advanced/topics"); // No index, uses first child
		advancedFolder.NavigationItems.Should().HaveCount(1);

		var topics = advancedFolder.NavigationItems.First().Should().BeOfType<FileNavigationLeaf<IDocumentationFile>>().Subject;
		topics.Url.Should().Be("/guide/advanced/topics");
	}
}
