// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Extensions;
using FluentAssertions;

namespace Elastic.Documentation.Navigation.Tests.Isolation;

public class FolderExcludeTests(ITestOutputHelper output) : DocumentationSetNavigationTestBase(output)
{
	[Fact]
	public void FolderWithoutExcludeIncludesAllFiles()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: docs
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		fileSystem.AddDirectory("/docs/docs");
		fileSystem.AddFile("/docs/docs/alpha.md", new MockFileData("# Alpha"));
		fileSystem.AddFile("/docs/docs/beta.md", new MockFileData("# Beta"));
		fileSystem.AddFile("/docs/docs/gamma.md", new MockFileData("# Gamma"));

		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var folderItem = docSet.TableOfContents.First().Should().BeOfType<FolderRef>().Subject;

		var fileNames = folderItem.Children.Select(c => c.PathRelativeToDocumentationSet).ToList();
		fileNames.Should().BeEquivalentTo(
			["docs/alpha.md", "docs/beta.md", "docs/gamma.md"],
			options => options.WithStrictOrdering()
		);
	}

	[Fact]
	public void FolderWithExcludeFiltersOutSpecifiedFiles()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: docs
		               exclude:
		                 - beta.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		fileSystem.AddDirectory("/docs/docs");
		fileSystem.AddFile("/docs/docs/alpha.md", new MockFileData("# Alpha"));
		fileSystem.AddFile("/docs/docs/beta.md", new MockFileData("# Beta"));
		fileSystem.AddFile("/docs/docs/gamma.md", new MockFileData("# Gamma"));

		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var folderItem = docSet.TableOfContents.First().Should().BeOfType<FolderRef>().Subject;

		var fileNames = folderItem.Children.Select(c => c.PathRelativeToDocumentationSet).ToList();
		fileNames.Should().BeEquivalentTo(
			["docs/alpha.md", "docs/gamma.md"],
			options => options.WithStrictOrdering()
		);
	}

	[Fact]
	public void FolderWithExcludeMultipleFiles()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: docs
		               exclude:
		                 - alpha.md
		                 - gamma.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		fileSystem.AddDirectory("/docs/docs");
		fileSystem.AddFile("/docs/docs/alpha.md", new MockFileData("# Alpha"));
		fileSystem.AddFile("/docs/docs/beta.md", new MockFileData("# Beta"));
		fileSystem.AddFile("/docs/docs/gamma.md", new MockFileData("# Gamma"));

		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var folderItem = docSet.TableOfContents.First().Should().BeOfType<FolderRef>().Subject;

		var fileNames = folderItem.Children.Select(c => c.PathRelativeToDocumentationSet).ToList();
		fileNames.Should().BeEquivalentTo(["docs/beta.md"], options => options.WithStrictOrdering());
	}

	[Fact]
	public void FolderWithExcludeIsCaseInsensitive()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: docs
		               exclude:
		                 - BETA.MD
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		fileSystem.AddDirectory("/docs/docs");
		fileSystem.AddFile("/docs/docs/alpha.md", new MockFileData("# Alpha"));
		fileSystem.AddFile("/docs/docs/beta.md", new MockFileData("# Beta"));

		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var folderItem = docSet.TableOfContents.First().Should().BeOfType<FolderRef>().Subject;

		var fileNames = folderItem.Children.Select(c => c.PathRelativeToDocumentationSet).ToList();
		fileNames.Should().BeEquivalentTo(["docs/alpha.md"], options => options.WithStrictOrdering());
	}

	[Fact]
	public void FolderWithExcludePreservesIndexMd()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: docs
		               exclude:
		                 - index.md
		                 - beta.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		fileSystem.AddDirectory("/docs/docs");
		fileSystem.AddFile("/docs/docs/index.md", new MockFileData("# Index"));
		fileSystem.AddFile("/docs/docs/alpha.md", new MockFileData("# Alpha"));
		fileSystem.AddFile("/docs/docs/beta.md", new MockFileData("# Beta"));

		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var folderItem = docSet.TableOfContents.First().Should().BeOfType<FolderRef>().Subject;

		var fileNames = folderItem.Children.Select(c => c.PathRelativeToDocumentationSet).ToList();
		// index.md should be excluded too — the user explicitly asked for it
		fileNames.Should().BeEquivalentTo(["docs/alpha.md"], options => options.WithStrictOrdering());
	}
}
