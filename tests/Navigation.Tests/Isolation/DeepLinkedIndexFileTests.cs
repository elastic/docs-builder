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

/// <summary>Regression coverage for https://github.com/elastic/docs-builder/issues/764: a childless <c>file: subdir/index.md</c> entry becomes its own single-page folder instead of being silently dropped from the navigation.</summary>
public class DeepLinkedIndexFileTests() : DocumentationSetNavigationTestBase()
{
	[Test]
	public void ChildlessDeepLinkedIndexFileBecomesFolderRef()
	{
		// language=yaml
		var yaml =
			"""
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

	[Test]
	public void BareIndexFileIsNotConverted()
	{
		// language=yaml
		var yaml =
			"""
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

	[Test]
	public void DeepLinkedIndexFileWithChildrenStaysVirtualFile()
	{
		// language=yaml
		var yaml =
			"""
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

	[Test]
	public async Task ChildlessDeepLinkedIndexFileRendersAsSingleLinkFolder()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - file: index.md
		             - file: reference/1password/index.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current!.Execution.CancellationToken);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current!.Execution.CancellationToken);

		NavigationTreeDump
			.ToText(navigation)
			.Should()
			.MatchInlineSnapshot(
				"""
			DocumentationSetNavigation  /  "index"
			  index: FileNavigationLeaf  /  "index"
			  FolderNavigation  /reference/1password  "index"
			    index: FileNavigationLeaf  /reference/1password  "index"
			"""
			);

		context.Collector.Errors.Should().Be(0);
	}

	[Test]
	public async Task FolderWithChildlessIndexFileChildrenKeepsEveryEntry()
	{
		// language=yaml
		var yaml =
			"""
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
		_ = context.Collector.StartAsync(TestContext.Current!.Execution.CancellationToken);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current!.Execution.CancellationToken);

		NavigationTreeDump
			.ToText(navigation)
			.Should()
			.MatchInlineSnapshot(
				"""
			DocumentationSetNavigation  /reference  "index"
			  index: FileNavigationLeaf  /reference  "index"
			  FolderNavigation  /reference  "index"
			    index: FileNavigationLeaf  /reference  "index"
			    FolderNavigation  /reference/1password  "index"
			      index: FileNavigationLeaf  /reference/1password  "index"
			    FolderNavigation  /reference/abnormal_security  "index"
			      index: FileNavigationLeaf  /reference/abnormal_security  "index"
			    FolderNavigation  /reference/activemq  "index"
			      index: FileNavigationLeaf  /reference/activemq  "index"
			"""
			);

		context.Collector.Errors.Should().Be(0);
	}

	[Test]
	public async Task ChildlessIndexFileChildrenUnderVirtualFileResolveWithoutPathDoubling()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - file: reference/apache-intro.md
		               children:
		                 - file: apache/index.md
		                 - file: apache_spark/index.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current!.Execution.CancellationToken);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current!.Execution.CancellationToken);

		NavigationTreeDump
			.ToText(navigation)
			.Should()
			.MatchInlineSnapshot(
				"""
			DocumentationSetNavigation  /reference/apache-intro  "apache-intro"
			  index: FileNavigationLeaf  /reference/apache-intro  "apache-intro"
			  VirtualFileNavigation  /reference/apache-intro  "apache-intro"
			    index: FileNavigationLeaf  /reference/apache-intro  "apache-intro"
			    FolderNavigation  /reference/apache  "index"
			      index: FileNavigationLeaf  /reference/apache  "index"
			    FolderNavigation  /reference/apache_spark  "index"
			      index: FileNavigationLeaf  /reference/apache_spark  "index"
			"""
			);

		context.Collector.Errors.Should().Be(0);
	}

	[Test]
	public async Task ChildlessDeepLinkedIndexFileNestedUnderExplicitFolderResolvesParentRelative()
	{
		// Regression for a path-resolution bug flagged in review: a multi-segment sugar-expanded
		// child (e.g. "integrations/aws/index.md") must resolve relative to its parent folder, exactly
		// like a plain FileRef child would -- not relative to the containing toc.yml/docset.yml.
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - folder: reference
		               file: index.md
		               children:
		                 - file: integrations/aws/index.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current!.Execution.CancellationToken);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current!.Execution.CancellationToken);

		NavigationTreeDump
			.ToText(navigation)
			.Should()
			.MatchInlineSnapshot(
				"""
			DocumentationSetNavigation  /reference  "index"
			  index: FileNavigationLeaf  /reference  "index"
			  FolderNavigation  /reference  "index"
			    index: FileNavigationLeaf  /reference  "index"
			    FolderNavigation  /reference/integrations/aws  "index"
			      index: FileNavigationLeaf  /reference/integrations/aws  "index"
			"""
			);

		context.Collector.Errors.Should().Be(0);
	}

	/// <summary>Baseline showing plain (non-sugar) file children under a virtual file already double a
	/// redundantly-repeated path segment; the sugar-folder case is intentionally consistent with this.</summary>
	[Test]
	public async Task ProbePlainFileChildUnderVirtualFileWithFullPath()
	{
		// language=yaml
		var yaml =
			"""
		           project: 'test-project'
		           toc:
		             - file: reference/apache-intro.md
		               children:
		                 - file: reference/apache-other.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current!.Execution.CancellationToken);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current!.Execution.CancellationToken);

		NavigationTreeDump
			.ToText(navigation)
			.Should()
			.MatchInlineSnapshot(
				"""
			DocumentationSetNavigation  /reference/apache-intro  "apache-intro"
			  index: FileNavigationLeaf  /reference/apache-intro  "apache-intro"
			  VirtualFileNavigation  /reference/apache-intro  "apache-intro"
			    index: FileNavigationLeaf  /reference/apache-intro  "apache-intro"
			    FileNavigationLeaf  /reference/reference/apache-other  "apache-other"
			"""
			);
	}
}
