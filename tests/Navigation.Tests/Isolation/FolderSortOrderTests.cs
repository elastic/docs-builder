// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Extensions;
using FluentAssertions;

namespace Elastic.Documentation.Navigation.Tests.Isolation;

public class FolderSortOrderTests(ITestOutputHelper output) : DocumentationSetNavigationTestBase(output)
{
	[Fact]
	public void FolderWithDefaultSortOrderIsAscending()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: api-versions
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		fileSystem.AddDirectory("/docs/api-versions");
		fileSystem.AddFile("/docs/api-versions/v1.md", new MockFileData("# V1"));
		fileSystem.AddFile("/docs/api-versions/v2.md", new MockFileData("# V2"));
		fileSystem.AddFile("/docs/api-versions/v3.md", new MockFileData("# V3"));

		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var folderItem = docSet.TableOfContents.First().Should().BeOfType<FolderRef>().Subject;
		folderItem.SortOrder.Should().Be(SortOrder.Ascending);

		var fileNames = folderItem.Children.Select(c => c.PathRelativeToDocumentationSet).ToList();
		fileNames.Should().BeEquivalentTo(["api-versions/v1.md", "api-versions/v2.md", "api-versions/v3.md"], options => options.WithStrictOrdering());
	}

	[Fact]
	public void FolderWithSortDescendingOrdersFilesZToA()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: api-versions
		               sort: desc
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		fileSystem.AddDirectory("/docs/api-versions");
		fileSystem.AddFile("/docs/api-versions/v1.md", new MockFileData("# V1"));
		fileSystem.AddFile("/docs/api-versions/v2.md", new MockFileData("# V2"));
		fileSystem.AddFile("/docs/api-versions/v3.md", new MockFileData("# V3"));

		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var folderItem = docSet.TableOfContents.First().Should().BeOfType<FolderRef>().Subject;
		folderItem.SortOrder.Should().Be(SortOrder.Descending);

		var fileNames = folderItem.Children.Select(c => c.PathRelativeToDocumentationSet).ToList();
		fileNames.Should().BeEquivalentTo(["api-versions/v3.md", "api-versions/v2.md", "api-versions/v1.md"], options => options.WithStrictOrdering());
	}

	[Fact]
	public void FolderWithSortDescendingLongFormOrdersFilesZToA()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: api-versions
		               sort: descending
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		fileSystem.AddDirectory("/docs/api-versions");
		fileSystem.AddFile("/docs/api-versions/v1.md", new MockFileData("# V1"));
		fileSystem.AddFile("/docs/api-versions/v2.md", new MockFileData("# V2"));
		fileSystem.AddFile("/docs/api-versions/v3.md", new MockFileData("# V3"));

		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var folderItem = docSet.TableOfContents.First().Should().BeOfType<FolderRef>().Subject;
		folderItem.SortOrder.Should().Be(SortOrder.Descending);
	}

	[Fact]
	public void FolderWithSortAscendingExplicitOrdersFilesAToZ()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: api-versions
		               sort: asc
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		fileSystem.AddDirectory("/docs/api-versions");
		fileSystem.AddFile("/docs/api-versions/v3.md", new MockFileData("# V3"));
		fileSystem.AddFile("/docs/api-versions/v1.md", new MockFileData("# V1"));
		fileSystem.AddFile("/docs/api-versions/v2.md", new MockFileData("# V2"));

		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var folderItem = docSet.TableOfContents.First().Should().BeOfType<FolderRef>().Subject;
		folderItem.SortOrder.Should().Be(SortOrder.Ascending);

		var fileNames = folderItem.Children.Select(c => c.PathRelativeToDocumentationSet).ToList();
		fileNames.Should().BeEquivalentTo(["api-versions/v1.md", "api-versions/v2.md", "api-versions/v3.md"], options => options.WithStrictOrdering());
	}

	[Fact]
	public void FolderWithSortDescendingPlacesIndexMdFirst()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: api-versions
		               sort: desc
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		fileSystem.AddDirectory("/docs/api-versions");
		fileSystem.AddFile("/docs/api-versions/index.md", new MockFileData("# Index"));
		fileSystem.AddFile("/docs/api-versions/v1.md", new MockFileData("# V1"));
		fileSystem.AddFile("/docs/api-versions/v2.md", new MockFileData("# V2"));
		fileSystem.AddFile("/docs/api-versions/v3.md", new MockFileData("# V3"));

		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var folderItem = docSet.TableOfContents.First().Should().BeOfType<FolderRef>().Subject;

		var fileNames = folderItem.Children.Select(c => c.PathRelativeToDocumentationSet).ToList();
		fileNames.Should().HaveCount(4);
		fileNames[0].Should().Be("api-versions/index.md");
		fileNames[1].Should().Be("api-versions/v3.md");
		fileNames[2].Should().Be("api-versions/v2.md");
		fileNames[3].Should().Be("api-versions/v1.md");
	}

	[Fact]
	public void FolderWithFileAndSortDescendingPreservesSortOrder()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: api-versions
		               file: api-versions.md
		               sort: desc
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		fileSystem.AddDirectory("/docs/api-versions");

		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var folderItem = docSet.TableOfContents.First().Should().BeOfType<FolderRef>().Subject;
		folderItem.SortOrder.Should().Be(SortOrder.Descending);
	}

	[Fact]
	public void FolderWithExplicitChildrenIgnoresSortOrder()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: api-versions
		               sort: desc
		               children:
		                 - file: v1.md
		                 - file: v2.md
		                 - file: v3.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		fileSystem.AddDirectory("/docs/api-versions");

		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));

		var folderItem = docSet.TableOfContents.First().Should().BeOfType<FolderRef>().Subject;

		var fileNames = folderItem.Children.Select(c => c.PathRelativeToDocumentationSet).ToList();
		fileNames.Should().BeEquivalentTo(["api-versions/v1.md", "api-versions/v2.md", "api-versions/v3.md"], options => options.WithStrictOrdering());
	}
}
