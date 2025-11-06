// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation.Isolated;
using Elastic.Documentation.Navigation.Isolated.Node;
using FluentAssertions;

namespace Elastic.Documentation.Navigation.Tests.Isolation;

public class FolderIndexFileRefTests(ITestOutputHelper output) : DocumentationSetNavigationTestBase(output)
{
	[Fact]
	public async Task FolderWithFileCreatesCorrectStructure()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: getting-started
		               file: getting-started.md
		               children:
		                 - file: install.md
		                 - file: configure.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		// Should create a FolderNavigation with the file as index
		navigation.NavigationItems.Should().HaveCount(1);
		var folder = navigation.NavigationItems.First().Should().BeOfType<FolderNavigation<IDocumentationFile>>().Subject;

		// Children should be scoped to the folder
		folder.Url.Should().Be("/getting-started/getting-started");
		folder.NavigationItems.Should().HaveCount(2); // install.md, configure.md

		// Verify no errors
		context.Collector.Errors.Should().Be(0);
	}

	[Fact]
	public async Task FolderWithFileChildrenPathsAreScopedToFolder()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: getting-started
		               file: getting-started.md
		               children:
		                 - file: install.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		_ = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		// Verify that the FileRef for getting-started.md is a FolderIndexFileRef
		var folderItem = docSet.TableOfContents.First().Should().BeOfType<FolderRef>().Subject;
		folderItem.Children.Should().HaveCount(2); // getting-started.md and install.md

		var indexFile = folderItem.Children.First().Should().BeOfType<FolderIndexFileRef>().Subject;
		indexFile.PathRelativeToDocumentationSet.Should().Be("getting-started/getting-started.md");

		var childFile = folderItem.Children.ElementAt(1).Should().BeOfType<FileRef>().Subject;
		childFile.PathRelativeToDocumentationSet.Should().Be("getting-started/install.md");
	}

	[Fact]
	public async Task FolderWithFileEmitsHintWhenFileNameDoesNotMatchFolder()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: getting-started
		               file: intro.md
		               children:
		                 - file: install.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		_ = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		// Should emit hint about file name not matching folder name
		context.Collector.Hints.Should().BeGreaterThan(0);
		var diagnostics = context.Diagnostics;
		diagnostics.Should().Contain(d =>
			d.Severity == Severity.Hint &&
			d.Message.Contains("intro.md") &&
			d.Message.Contains("getting-started") &&
			d.Message.Contains("Best practice"));
	}

	[Fact]
	public async Task FolderWithFileDoesNotEmitHintWhenFileNameMatchesFolder()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: getting-started
		               file: getting-started.md
		               children:
		                 - file: install.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		_ = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		// Should not emit any hints
		context.Collector.Hints.Should().Be(0);
		context.Diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task FolderWithFileEmitsErrorForDeepLinkingInFile()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: getting-started
		               file: intro/file.md
		               children:
		                 - file: install.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		_ = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		// Should emit error about deep linking in the file attribute
		context.Collector.Errors.Should().BeGreaterThan(0);
		var diagnostics = context.Diagnostics;
		diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("Deep linking on folder 'file' is not supported") &&
			d.Message.Contains("intro/file.md"));
	}

	[Fact]
	public async Task FolderWithIndexMdFileDoesNotNeedToMatchFolderName()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: getting-started
		               file: index.md
		               children:
		                 - file: install.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		_ = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		// index.md is a special case - should not emit hint
		// (Though the hint check doesn't exclude index.md, it's a reasonable best practice to allow it)
		context.Collector.Errors.Should().Be(0);
	}

	[Fact]
	public async Task FolderWithFileCaseInsensitiveMatch()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: GettingStarted
		               file: getting-started.md
		               children:
		                 - file: install.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		_ = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		// Case-insensitive match should not emit hint
		context.Collector.Hints.Should().Be(0);
		context.Diagnostics.Should().BeEmpty();
	}
}
