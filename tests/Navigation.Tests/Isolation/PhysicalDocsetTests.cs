// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Navigation.Isolated;
using FluentAssertions;

namespace Elastic.Documentation.Navigation.Tests.Isolation;

public class PhysicalDocsetTests(ITestOutputHelper output)
{
	[Fact]
	public async Task PhysicalDocsetCanBeNavigated()
	{
		var docsetPath = Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs", "_docset.yml");
		File.Exists(docsetPath).Should().BeTrue($"Expected docset file to exist at {docsetPath}");

		var yaml = File.ReadAllText(docsetPath);
		var docSet = DocumentationSetFile.Deserialize(yaml);

		var fileSystem = new FileSystem();
		var docsDir = fileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs"));
		var outputDir = fileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "test-output"));
		var configPath = fileSystem.FileInfo.New(docsetPath);

		var context = new TestDocumentationSetContext(fileSystem, docsDir, outputDir, configPath, output, "docs-builder");
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(docSet, context, TestDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		// Assert navigation was built successfully
		navigation.NavigationItems.Should().NotBeEmpty();

		// Assert index.md is first
		var firstItem = navigation.NavigationItems.ElementAt(0);
		firstItem.Should().BeOfType<FileNavigationLeaf<TestDocumentationFile>>();
		firstItem.Url.Should().Be("/"); // index.md becomes /

		// Assert folders exist
		var folders = navigation.NavigationItems.OfType<FolderNavigation>().ToList();
		folders.Should().NotBeEmpty();

		// Check by URL since folder names derive from index file titles
		var folderUrls = folders.Select(f => f.Url).ToList();
		folderUrls.Should().Contain("/contribute");

		// No errors should be emitted during navigation construction
		context.Diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task PhysicalDocsetNavigationHasCorrectUrls()
	{
		var docsetPath = Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs", "_docset.yml");
		var yaml = File.ReadAllText(docsetPath);
		var docSet = DocumentationSetFile.Deserialize(yaml);

		var fileSystem = new FileSystem();
		var docsDir = fileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs"));
		var outputDir = fileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "test-output"));
		var configPath = fileSystem.FileInfo.New(docsetPath);

		var context = new TestDocumentationSetContext(fileSystem, docsDir, outputDir, configPath, output, "docs-builder");
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(docSet, context, TestDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		// Find the contribute folder by URL
		var contributeFolder = navigation.NavigationItems.OfType<FolderNavigation>()
			.FirstOrDefault(f => f.Url == "/contribute");
		contributeFolder.Should().NotBeNull();

		// Verify nested structure
		contributeFolder.NavigationItems.Should().NotBeEmpty();
	}

	[Fact]
	public async Task PhysicalDocsetNavigationIncludesNestedTocs()
	{
		var docsetPath = Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs", "_docset.yml");
		var yaml = File.ReadAllText(docsetPath);
		var docSet = DocumentationSetFile.Deserialize(yaml);

		var fileSystem = new FileSystem();
		var docsDir = fileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs"));
		var outputDir = fileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "test-output"));
		var configPath = fileSystem.FileInfo.New(docsetPath);

		var context = new TestDocumentationSetContext(fileSystem, docsDir, outputDir, configPath, output, "docs-builder");
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(docSet, context, TestDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		// Find TOC references in the navigation
		var tocNavs = navigation.NavigationItems.OfType<TableOfContentsNavigation>().ToList();
		tocNavs.Should().NotBeEmpty();

		// development TOC should exist (check by URL)
		var developmentToc = tocNavs.FirstOrDefault(t => t.Url == "/development");
		developmentToc.Should().NotBeNull();

		developmentToc.NavigationItems.Should().HaveCount(2);
		developmentToc.NavigationItems.OfType<FileNavigationLeaf<TestDocumentationFile>>().Should().HaveCount(1);
		developmentToc.NavigationItems.OfType<TableOfContentsNavigation>().Should().HaveCount(1);

		var developmentIndex = developmentToc.NavigationItems.OfType<FileNavigationLeaf<TestDocumentationFile>>().First();
		developmentIndex.FileInfo.FullName.Should().Be(Path.Combine(docsDir.FullName, "development", "index.md"));


	}

	[Fact]
	public async Task PhysicalDocsetNavigationHandlesHiddenFiles()
	{
		var docsetPath = Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs", "_docset.yml");
		var yaml = File.ReadAllText(docsetPath);
		var docSet = DocumentationSetFile.Deserialize(yaml);

		var fileSystem = new FileSystem();
		var docsDir = fileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs"));
		var outputDir = fileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "test-output"));
		var configPath = fileSystem.FileInfo.New(docsetPath);

		var context = new TestDocumentationSetContext(fileSystem, docsDir, outputDir, configPath, output, "docs-builder");
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(docSet, context, TestDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		// Find hidden files
		var allItems = GetAllNavigationItems(navigation.NavigationItems);
		var hiddenItems = allItems.Where(i => i.Hidden).ToList();
		hiddenItems.Should().NotBeEmpty();
	}

	[Fact]
	public async Task PhysicalDocsetNavigationHandlesCrossLinks()
	{
		var docsetPath = Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs", "_docset.yml");
		var yaml = File.ReadAllText(docsetPath);
		var docSet = DocumentationSetFile.Deserialize(yaml);

		var fileSystem = new FileSystem();
		var docsDir = fileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs"));
		var outputDir = fileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "test-output"));
		var configPath = fileSystem.FileInfo.New(docsetPath);

		var context = new TestDocumentationSetContext(fileSystem, docsDir, outputDir, configPath, output, "docs-builder");
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(docSet, context, TestDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		// Find cross-link items
		var allItems = GetAllNavigationItems(navigation.NavigationItems);
		var crossLinks = allItems.OfType<CrossLinkNavigationLeaf>().ToList();
		crossLinks.Should().NotBeEmpty();
		crossLinks.Should().AllSatisfy(cl => cl.IsCrossLink.Should().BeTrue());
	}

	private static List<INavigationItem> GetAllNavigationItems(IReadOnlyCollection<INavigationItem> items)
	{
		var result = new List<INavigationItem>();
		foreach (var item in items)
		{
			result.Add(item);
			if (item is INodeNavigationItem<INavigationModel, INavigationItem> node)
				result.AddRange(GetAllNavigationItems(node.NavigationItems));
		}
		return result;
	}
}
