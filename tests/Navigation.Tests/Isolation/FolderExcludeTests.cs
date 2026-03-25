// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Extensions;
using AwesomeAssertions;

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
	public void FolderWithExcludeCanExcludeIndexMd()
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

	[Fact]
	public void FolderExcludePopulatesFolderExcludedFiles()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: docs
		               exclude:
		                 - draft.md
		                 - internal.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		fileSystem.AddDirectory("/docs/docs");
		fileSystem.AddFile("/docs/docs/alpha.md", new MockFileData("# Alpha"));
		fileSystem.AddFile("/docs/docs/draft.md", new MockFileData("# Draft"));
		fileSystem.AddFile("/docs/docs/internal.md", new MockFileData("# Internal"));

		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		docSet.FolderExcludedFiles.Should().BeEquivalentTo(["docs/draft.md", "docs/internal.md"]);
	}

	[Fact]
	public void FolderExcludeCollectsFromNestedFolders()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: reference
		               children:
		                 - file: index.md
		                 - folder: api
		                   exclude:
		                     - main.md
		                 - folder: deps
		                   exclude:
		                     - main.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		fileSystem.AddDirectory("/docs/reference");
		fileSystem.AddFile("/docs/reference/index.md", new MockFileData("# Ref"));
		fileSystem.AddDirectory("/docs/reference/api");
		fileSystem.AddFile("/docs/reference/api/v1.md", new MockFileData("# V1"));
		fileSystem.AddFile("/docs/reference/api/main.md", new MockFileData("# Main"));
		fileSystem.AddDirectory("/docs/reference/deps");
		fileSystem.AddFile("/docs/reference/deps/v1.md", new MockFileData("# V1"));
		fileSystem.AddFile("/docs/reference/deps/main.md", new MockFileData("# Main"));

		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		docSet.FolderExcludedFiles.Should().BeEquivalentTo(
			["reference/api/main.md", "reference/deps/main.md"]
		);
	}
}
