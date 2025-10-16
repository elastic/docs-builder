// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Navigation.Isolated;
using FluentAssertions;

namespace Elastic.Documentation.Navigation.Tests.Isolation;

/// <summary>
/// Tests that validate all FileInfo properties on FileNavigationItems resolve to files that actually exist.
/// These tests ensure that the navigation system correctly creates file references for all scenarios.
/// </summary>
public class FileInfoValidationTests(ITestOutputHelper output) : DocumentationSetNavigationTestBase(output)
{
	[Fact]
	public void AllFileNavigationItemsHaveValidFileInfoForSimpleFiles()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: getting-started.md
		             - file: installation.md
		             - file: configuration.md
		           """;

		var fileSystem = CreateMockFileSystemWithFiles([
			"/docs/getting-started.md",
			"/docs/installation.md",
			"/docs/configuration.md"
		]);
		var docSet = DocumentationSetFile.LoadAndResolve(yaml, fileSystem.DirectoryInfo.New("/docs"), fileSystem);
		var context = CreateContext(fileSystem);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		// Validate all file navigation items have valid FileInfo
		var fileNavigationItems = GetAllFileNavigationItems(navigation.NavigationItems);
		fileNavigationItems.Should().HaveCount(3);

		foreach (var fileNav in fileNavigationItems)
		{
			fileNav.FileInfo.Should().NotBeNull($"FileInfo for {fileNav.Url} should not be null");
			fileNav.FileInfo.Exists.Should().BeTrue($"File at {fileNav.FileInfo.FullName} should exist");
		}

		// Validate no errors or warnings were emitted
		context.Diagnostics.Should().BeEmpty("navigation construction should not emit any diagnostics");
	}

	[Fact]
	public void AllFileNavigationItemsHaveValidFileInfoForVirtualFiles()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: guide.md
		               children:
		                 - file: section1.md
		                 - file: section2.md
		             - file: advanced.md
		               children:
		                 - file: topics.md
		                 - file: patterns.md
		           """;

		var fileSystem = CreateMockFileSystemWithFiles([
			"/docs/guide.md",
			"/docs/guide/section1.md",
			"/docs/guide/section2.md",
			"/docs/advanced.md",
			"/docs/advanced/topics.md",
			"/docs/advanced/patterns.md"
		]);
		var docSet = DocumentationSetFile.LoadAndResolve(yaml, fileSystem.DirectoryInfo.New("/docs"), fileSystem);
		var context = CreateContext(fileSystem);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		// Validate all file navigation items (including virtual files and their children)
		var fileNavigationItems = GetAllFileNavigationItems(navigation.NavigationItems);
		fileNavigationItems.Should().HaveCount(6, "should include 2 virtual files and 4 child files");

		foreach (var fileNav in fileNavigationItems)
		{
			fileNav.FileInfo.Should().NotBeNull($"FileInfo for {fileNav.Url} should not be null");
			fileNav.FileInfo.Exists.Should().BeTrue($"File at {fileNav.FileInfo.FullName} should exist");
		}

		// Validate no errors or warnings were emitted
		context.Diagnostics.Should().BeEmpty("navigation construction should not emit any diagnostics");
	}

	[Fact]
	public void AllFileNavigationItemsHaveValidFileInfoForFoldersWithFiles()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: setup
		               children:
		                 - file: index.md
		                 - file: installation.md
		                 - file: configuration.md
		             - folder: advanced
		               children:
		                 - file: index.md
		                 - file: topics.md
		           """;

		var fileSystem = CreateMockFileSystemWithFiles([
			"/docs/setup/index.md",
			"/docs/setup/installation.md",
			"/docs/setup/configuration.md",
			"/docs/advanced/index.md",
			"/docs/advanced/topics.md"
		]);
		var docSet = DocumentationSetFile.LoadAndResolve(yaml, fileSystem.DirectoryInfo.New("/docs"), fileSystem);
		var context = CreateContext(fileSystem);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		// Validate all file navigation items within folders
		var fileNavigationItems = GetAllFileNavigationItems(navigation.NavigationItems);
		fileNavigationItems.Should().HaveCount(5, "should include all files within folders");

		foreach (var fileNav in fileNavigationItems)
		{
			fileNav.FileInfo.Should().NotBeNull($"FileInfo for {fileNav.Url} should not be null");
			fileNav.FileInfo.Exists.Should().BeTrue($"File at {fileNav.FileInfo.FullName} should exist");
		}

		// Validate no errors or warnings were emitted
		context.Diagnostics.Should().BeEmpty("navigation construction should not emit any diagnostics");
	}

	/// <summary>
	/// Tests that files within folders inside nested TOCs have their FileInfo paths resolved correctly.
	/// This validates that the full path (including folder components) is used for file resolution.
	/// </summary>
	[Fact]
	public void AllFileNavigationItemsHaveValidFileInfoForDeeplyNestedTocFiles()
	{
		// language=yaml
		var docsetYaml = """
		                 project: 'test-project'
		                 toc:
		                   - file: index.md
		                   - toc: development
		                   - folder: guides
		                     children:
		                       - file: getting-started.md
		                       - toc: advanced
		                 """;

		// Create a TOC file in a subdirectory
		// language=yaml
		var developmentTocYaml = """
		                         toc:
		                           - file: index.md
		                           - file: contributing.md
		                           - folder: internals
		                             children:
		                               - file: architecture.md
		                         """;

		// Create a deeply nested TOC file
		// language=yaml
		var advancedTocYaml = """
		                      toc:
		                        - file: index.md
		                        - file: patterns.md
		                        - toc: performance
		                      """;

		// Create a third-level nested TOC
		// language=yaml
		var performanceTocYaml = """
		                         toc:
		                           - file: index.md
		                           - file: optimization.md
		                         """;

		var fileSystem = CreateMockFileSystemWithFiles([
			"/docs/index.md",
			"/docs/development/toc.yml",
			"/docs/development/index.md",
			"/docs/development/contributing.md",
			"/docs/development/internals/index.md",
			"/docs/development/internals/architecture.md",
			"/docs/guides/getting-started.md",
			"/docs/guides/advanced/toc.yml",
			"/docs/guides/advanced/index.md",
			"/docs/guides/advanced/patterns.md",
			"/docs/guides/advanced/performance/toc.yml",
			"/docs/guides/advanced/performance/index.md",
			"/docs/guides/advanced/performance/optimization.md"
		]);

		// Add TOC file contents
		fileSystem.File.WriteAllText("/docs/development/toc.yml", developmentTocYaml);
		fileSystem.File.WriteAllText("/docs/guides/advanced/toc.yml", advancedTocYaml);
		fileSystem.File.WriteAllText("/docs/guides/advanced/performance/toc.yml", performanceTocYaml);

		var docSet = DocumentationSetFile.LoadAndResolve(docsetYaml, fileSystem.DirectoryInfo.New("/docs"), fileSystem);
		var context = CreateContext(fileSystem);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		// Validate all file navigation items across all nested TOCs
		var fileNavigationItems = GetAllFileNavigationItems(navigation.NavigationItems);
		fileNavigationItems.Count.Should().Be(9, "should include all files from main TOC and nested TOCs");

		foreach (var fileNav in fileNavigationItems)
		{
			fileNav.FileInfo.Should().NotBeNull($"FileInfo for {fileNav.Url} should not be null");
			fileNav.FileInfo.Exists.Should().BeTrue($"File at {fileNav.FileInfo.FullName} should exist for {fileNav.Url}");
		}

		// Note: LoadAndResolve loads the TOC children, which triggers validation warnings
		// This is expected behavior - the warnings indicate TOCs loaded from toc.yml files
		context.Diagnostics.Should().NotBeEmpty();
	}

	/// <summary>
	/// Tests that child files of virtual files in nested TOCs have their FileInfo paths resolved correctly.
	/// This validates that parent virtual file directory components are included in file resolution.
	/// </summary>
	[Fact]
	public void AllFileNavigationItemsHaveValidFileInfoForComplexMixedStructure()
	{
		// language=yaml
		var docsetYaml = """
		                 project: 'test-project'
		                 toc:
		                   - file: index.md
		                   - file: quick-start.md
		                     children:
		                       - file: step1.md
		                       - file: step2.md
		                   - folder: setup
		                     children:
		                       - file: installation.md
		                       - file: configuration.md
		                       - toc: advanced
		                   - toc: reference
		                   - file: faq.md
		                     children:
		                       - file: general.md
		                       - folder: troubleshooting
		                         children:
		                           - file: common-issues.md
		                 """;

		// Setup/advanced TOC
		// language=yaml
		var setupAdvancedTocYaml = """
		                           toc:
		                             - file: index.md
		                             - file: custom-config.md
		                           """;

		// Reference TOC
		// language=yaml
		var referenceTocYaml = """
		                       toc:
		                         - file: index.md
		                         - file: api.md
		                           children:
		                             - file: endpoints.md
		                         - folder: cli
		                           children:
		                             - file: commands.md
		                       """;

		var fileSystem = CreateMockFileSystemWithFiles([
			"/docs/index.md",
			"/docs/quick-start.md",
			"/docs/quick-start/step1.md",
			"/docs/quick-start/step2.md",
			"/docs/setup/installation.md",
			"/docs/setup/configuration.md",
			"/docs/setup/advanced/toc.yml",
			"/docs/setup/advanced/index.md",
			"/docs/setup/advanced/custom-config.md",
			"/docs/reference/toc.yml",
			"/docs/reference/index.md",
			"/docs/reference/api.md",
			"/docs/reference/api/endpoints.md",
			"/docs/reference/cli/index.md",
			"/docs/reference/cli/commands.md",
			"/docs/faq.md",
			"/docs/faq/general.md",
			"/docs/faq/troubleshooting/index.md",
			"/docs/faq/troubleshooting/common-issues.md"
		]);

		// Add TOC file contents
		fileSystem.File.WriteAllText("/docs/setup/advanced/toc.yml", setupAdvancedTocYaml);
		fileSystem.File.WriteAllText("/docs/reference/toc.yml", referenceTocYaml);

		var docSet = DocumentationSetFile.LoadAndResolve(docsetYaml, fileSystem.DirectoryInfo.New("/docs"), fileSystem);
		var context = CreateContext(fileSystem);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		// Validate all file navigation items in this complex structure
		var fileNavigationItems = GetAllFileNavigationItems(navigation.NavigationItems);
		fileNavigationItems.Count.Should().Be(15, "should include all files from all structures");

		foreach (var fileNav in fileNavigationItems)
		{
			fileNav.FileInfo.Should().NotBeNull($"FileInfo for {fileNav.Url} should not be null");
			fileNav.FileInfo.Exists.Should().BeTrue($"File at {fileNav.FileInfo.FullName} should exist for {fileNav.Url}");
		}

		// Note: LoadAndResolve loads the TOC children, which triggers validation warnings
		// This is expected behavior - the warnings indicate TOCs loaded from toc.yml files
		context.Diagnostics.Should().NotBeEmpty();
	}

	[Fact]
	public void AllFileNavigationItemsHaveValidFileInfoForNestedFolders()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: docs
		               children:
		                 - file: index.md
		                 - folder: guides
		                   children:
		                     - file: basics.md
		                     - folder: advanced
		                       children:
		                         - file: expert.md
		           """;

		var fileSystem = CreateMockFileSystemWithFiles([
			"/docs/docs/index.md",
			"/docs/docs/guides/basics.md",
			"/docs/docs/guides/advanced/expert.md"
		]);
		var docSet = DocumentationSetFile.LoadAndResolve(yaml, fileSystem.DirectoryInfo.New("/docs"), fileSystem);
		var context = CreateContext(fileSystem);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		// Validate all file navigation items in nested folders
		var fileNavigationItems = GetAllFileNavigationItems(navigation.NavigationItems);
		fileNavigationItems.Should().HaveCount(3);

		foreach (var fileNav in fileNavigationItems)
		{
			fileNav.FileInfo.Should().NotBeNull($"FileInfo for {fileNav.Url} should not be null");
			fileNav.FileInfo.Exists.Should().BeTrue($"File at {fileNav.FileInfo.FullName} should exist");
		}

		// Validate no errors or warnings were emitted
		context.Diagnostics.Should().BeEmpty("navigation construction should not emit any diagnostics");
	}

	[Fact]
	public void AllFileNavigationItemsHaveValidFileInfoForVirtualFilesWithNestedChildren()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: guide.md
		               children:
		                 - file: chapter1.md
		                   children:
		                     - file: section1.md
		                       children:
		                         - file: subsection1.md
		           """;

		var fileSystem = CreateMockFileSystemWithFiles([
			"/docs/guide.md",
			"/docs/guide/chapter1.md",
			"/docs/guide/chapter1/section1.md",
			"/docs/guide/chapter1/section1/subsection1.md"
		]);
		var docSet = DocumentationSetFile.LoadAndResolve(yaml, fileSystem.DirectoryInfo.New("/docs"), fileSystem);
		var context = CreateContext(fileSystem);

		var navigation = new DocumentationSetNavigation<IDocumentationFile>(docSet, context, GenericDocumentationFileFactory.Instance);

		// Validate all file navigation items in deeply nested virtual file structure
		var fileNavigationItems = GetAllFileNavigationItems(navigation.NavigationItems);
		fileNavigationItems.Should().HaveCount(4);

		foreach (var fileNav in fileNavigationItems)
		{
			fileNav.FileInfo.Should().NotBeNull($"FileInfo for {fileNav.Url} should not be null");
			fileNav.FileInfo.Exists.Should().BeTrue($"File at {fileNav.FileInfo.FullName} should exist for URL {fileNav.Url}");
		}

		// Validate no errors or warnings were emitted
		context.Diagnostics.Should().BeEmpty("navigation construction should not emit any diagnostics");
	}

	/// <summary>
	/// Helper method to create a MockFileSystem with the specified files.
	/// Creates all parent directories automatically.
	/// </summary>
	private static MockFileSystem CreateMockFileSystemWithFiles(string[] filePaths)
	{
		var fileSystem = new MockFileSystem();

		foreach (var filePath in filePaths)
		{
			// Ensure directory exists
			var directory = Path.GetDirectoryName(filePath);
			if (!string.IsNullOrEmpty(directory))
				fileSystem.Directory.CreateDirectory(directory);

			// Create file with simple content (can be overridden later)
			var fileName = Path.GetFileName(filePath);
			var title = fileName.Replace(".md", "").Replace(".yml", "");
			fileSystem.File.WriteAllText(filePath, $"# {title}\n\nContent for {fileName}");
		}

		return fileSystem;
	}

	/// <summary>
	/// Recursively collects all FileNavigationLeaf instances from the navigation tree.
	/// For VirtualFileNavigation items, extracts the Index (which is a FileNavigationLeaf).
	/// </summary>
	private static List<FileNavigationLeaf<IDocumentationFile>> GetAllFileNavigationItems(IReadOnlyCollection<INavigationItem> items)
	{
		var result = new List<FileNavigationLeaf<IDocumentationFile>>();

		foreach (var item in items)
		{
			// Collect direct file navigation leafs
			if (item is FileNavigationLeaf<IDocumentationFile> fileLeaf)
				result.Add(fileLeaf);
			// For virtual file navigation, get the index file
			else if (item is VirtualFileNavigation<IDocumentationFile> virtualFile)
			{
				if (virtualFile.Index is FileNavigationLeaf<IDocumentationFile> indexLeaf)
				{
					result.Add(indexLeaf);
				}
			}

			// Recursively process children
			if (item is INodeNavigationItem<INavigationModel, INavigationItem> node)
			{
				result.AddRange(GetAllFileNavigationItems(node.NavigationItems));
			}
		}

		return result;
	}
}
