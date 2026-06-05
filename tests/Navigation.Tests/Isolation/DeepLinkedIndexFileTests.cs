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

/// <summary>
/// Regression coverage for https://github.com/elastic/docs-builder/issues/764.
/// A childless <c>file: subdir/index.md</c> entry is sugar for <c>folder: subdir, file: index.md</c>,
/// so each entry becomes its own subfolder with a proper index page instead of competing for the
/// parent's index slot (which silently dropped it from the navigation).
/// </summary>
public class DeepLinkedIndexFileTests(ITestOutputHelper output) : DocumentationSetNavigationTestBase(output)
{
	[Fact]
	public void ChildlessDeepLinkedIndexFileBecomesFolderRef()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: reference/1password/index.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var folder = docSet.TableOfContents.First().Should().BeOfType<FolderRef>().Subject;
		folder.PathRelativeToDocumentationSet.Should().Be("reference/1password");

		var index = folder.Children.First().Should().BeOfType<FolderIndexFileRef>().Subject;
		index.PathRelativeToDocumentationSet.Should().Be("reference/1password/index.md");
	}

	[Fact]
	public void BareIndexFileIsNotConverted()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: index.md
		             - file: other.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		// Bare index.md stays on the IndexFileRef path, never wrapped into a folder.
		_ = docSet.TableOfContents.First().Should().BeOfType<IndexFileRef>();
		docSet.TableOfContents.OfType<FolderRef>().Should().BeEmpty();
	}

	[Fact]
	public void DeepLinkedIndexFileWithChildrenStaysVirtualFile()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: reference/aws/index.md
		               children:
		                 - file: cloudfront.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		// Entries that declare explicit children keep the existing virtual-file (deep-linking) semantics.
		var fileRef = docSet.TableOfContents.First().Should().BeOfType<FileRef>().Subject;
		fileRef.Should().NotBeOfType<FolderRef>();
		fileRef.PathRelativeToDocumentationSet.Should().Be("reference/aws/index.md");
		fileRef.Children.Should().ContainSingle();
	}

	[Fact]
	public async Task ChildlessDeepLinkedIndexFileRendersAsSingleLinkFolder()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: index.md
		             - file: reference/1password/index.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		var folder = navigation.NavigationItems.OfType<FolderNavigation<IDocumentationFile>>().Single();
		folder.Url.Should().Be("/reference/1password");
		// No other navigation items, so the frontend renders this as a plain link rather than an expandable folder.
		folder.NavigationItems.Should().BeEmpty();
		context.Collector.Errors.Should().Be(0);
	}

	[Fact]
	public async Task FolderWithChildlessIndexFileChildrenKeepsEveryEntry()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: reference
		               file: index.md
		               children:
		                 - file: 1password/index.md
		                 - file: abnormal_security/index.md
		                 - file: activemq/index.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		var reference = navigation.NavigationItems.OfType<FolderNavigation<IDocumentationFile>>().Single();
		reference.Url.Should().Be("/reference");

		// All three entries survive as their own subfolders; none is consumed as the parent's index.
		var childFolders = reference.NavigationItems.OfType<FolderNavigation<IDocumentationFile>>().ToList();
		childFolders.Select(f => f.Url).Should().BeEquivalentTo(
			["/reference/1password", "/reference/abnormal_security", "/reference/activemq"]);
		childFolders.Should().AllSatisfy(f => f.NavigationItems.Should().BeEmpty());
		context.Collector.Errors.Should().Be(0);
	}

	[Fact]
	public async Task ChildlessIndexFileChildrenUnderVirtualFileResolveWithoutPathDoubling()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: reference/apache-intro.md
		               children:
		                 - file: reference/apache/index.md
		                 - file: reference/apache_spark/index.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		var intro = navigation.NavigationItems.OfType<VirtualFileNavigation<IDocumentationFile>>().Single();
		intro.Url.Should().Be("/reference/apache-intro");

		var childFolders = intro.NavigationItems.OfType<FolderNavigation<IDocumentationFile>>().ToList();
		// Deep-linked index paths resolve relative to the documentation set root, not the parent path.
		childFolders.Select(f => f.Url).Should().BeEquivalentTo(["/reference/apache", "/reference/apache_spark"]);
		context.Collector.Errors.Should().Be(0);
	}
}
