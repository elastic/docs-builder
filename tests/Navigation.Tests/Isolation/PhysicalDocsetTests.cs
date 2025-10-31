// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Diagnostics;
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

		var fileSystem = new FileSystem();
		var docsDir = fileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs"));
		var outputDir = fileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "test-output"));
		var configPath = fileSystem.FileInfo.New(docsetPath);

		var context = new TestDocumentationSetContext(fileSystem, docsDir, outputDir, configPath, output, "docs-builder");
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, configPath, fileSystem);
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(docSet, context, TestDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		// Assert navigation was built successfully
		navigation.NavigationItems.Should().NotBeEmpty();

		// Assert index.md is first
		var firstItem = navigation.Index;
		firstItem.Should().BeOfType<FileNavigationLeaf<TestDocumentationFile>>();
		firstItem.Url.Should().Be("/"); // index.md becomes /

		// Assert folders exist
		var folders = navigation.NavigationItems.OfType<FolderNavigation<TestDocumentationFile>>().ToList();
		folders.Should().NotBeEmpty();

		// Check by URL since folder names derive from index file titles
		var folderUrls = folders.Select(f => f.Url).ToList();
		folderUrls.Should().Contain("/contribute/");

		// No errors or warnings should be emitted during navigation construction
		// Hints are acceptable for best practice guidance
		context.Collector.Errors.Should().Be(0, "no errors should be emitted");
		context.Collector.Warnings.Should().Be(0, "no warnings should be emitted");

		// Verify that the hint about deep-linking virtual file was emitted
		var hints = context.Diagnostics.Where(d => d.Severity == Severity.Hint).ToList();
		hints.Should().Contain(d =>
			d.Message.Contains("nest-under-index/index.md") &&
			d.Message.Contains("deep-linking"),
			"should emit hint for deep-linking virtual file");
	}

	[Fact]
	public async Task PhysicalDocsetNavigationHasCorrectUrls()
	{
		var docsetPath = Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs", "_docset.yml");
		var fileSystem = new FileSystem();
		var docsDir = fileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs"));
		var outputDir = fileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "test-output"));
		var configPath = fileSystem.FileInfo.New(docsetPath);

		var context = new TestDocumentationSetContext(fileSystem, docsDir, outputDir, configPath, output, "docs-builder");
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, configPath, fileSystem);
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(docSet, context, TestDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		// Find the contribute folder by URL
		var contributeFolder = navigation.NavigationItems.OfType<FolderNavigation<TestDocumentationFile>>()
			.FirstOrDefault(f => f.Url == "/contribute/");
		contributeFolder.Should().NotBeNull();

		// Verify nested structure
		contributeFolder.NavigationItems.Should().NotBeEmpty();
	}

	[Fact]
	public async Task PhysicalDocsetNavigationIncludesNestedTocs()
	{
		var docsetPath = Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs", "_docset.yml");
		var fileSystem = new FileSystem();
		var docsDir = fileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs"));
		var outputDir = fileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "test-output"));
		var configPath = fileSystem.FileInfo.New(docsetPath);

		var context = new TestDocumentationSetContext(fileSystem, docsDir, outputDir, configPath, output, "docs-builder");
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, configPath, fileSystem);
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(docSet, context, TestDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		var fileRefs = docSet.TableOfContents.SelectMany(DocumentationSetFile.GetFileRefs).ToList();
		foreach (var fileRef in fileRefs)
		{
			var path = fileSystem.FileInfo.New(Path.Combine(configPath.Directory!.FullName, fileRef.PathRelativeToDocumentationSet));
			path.Exists.Should().BeTrue($"Expected file {path.FullName} to exist");
		}
		fileRefs.Count.Should().Be(fileRefs.Distinct().Count(), "should not have duplicate file references");

		// Find TOC references in the navigation
		var tocNavs = navigation.NavigationItems.OfType<TableOfContentsNavigation<TestDocumentationFile>>().ToList();
		tocNavs.Should().NotBeEmpty();

		// development TOC should exist (check by URL)
		var developmentToc = tocNavs.FirstOrDefault(t => t.Url == "/development/");
		developmentToc.Should().NotBeNull();

		developmentToc.NavigationItems.Should().HaveCount(2);
		developmentToc.Index.Should().NotBeNull();
		developmentToc.NavigationItems.OfType<FileNavigationLeaf<TestDocumentationFile>>().Should().HaveCount(0);
		developmentToc.NavigationItems.OfType<FolderNavigation<TestDocumentationFile>>().Should().HaveCount(1);
		developmentToc.NavigationItems.OfType<TableOfContentsNavigation<TestDocumentationFile>>().Should().HaveCount(1);

		var developmentIndex = developmentToc.Index as FileNavigationLeaf<TestDocumentationFile>;
		developmentIndex.Should().NotBeNull();
		developmentIndex.FileInfo.FullName.Should().Be(Path.Combine(docsDir.FullName, "development", "index.md"));


	}

	[Fact]
	public async Task PhysicalDocsetNavigationHandlesHiddenFiles()
	{
		var docsetPath = Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs", "_docset.yml");
		var fileSystem = new FileSystem();
		var docsDir = fileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs"));
		var outputDir = fileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "test-output"));
		var configPath = fileSystem.FileInfo.New(docsetPath);

		var context = new TestDocumentationSetContext(fileSystem, docsDir, outputDir, configPath, output, "docs-builder");
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, configPath, fileSystem);
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
		var fileSystem = new FileSystem();
		var docsDir = fileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs"));
		var outputDir = fileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "test-output"));
		var configPath = fileSystem.FileInfo.New(docsetPath);

		var context = new TestDocumentationSetContext(fileSystem, docsDir, outputDir, configPath, output, "docs-builder");
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, configPath);
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(docSet, context, TestDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		// Find cross-link items
		var allItems = GetAllNavigationItems(navigation.NavigationItems);
		var crossLinks = allItems.OfType<CrossLinkNavigationLeaf>().ToList();
		crossLinks.Should().NotBeEmpty();
	}

	[Fact]
	public void CovarianceOfNavigationItemsIsRespected()
	{
		var docsetPath = Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs", "_docset.yml");
		var fileSystem = new FileSystem();
		var docsDir = fileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs"));
		var outputDir = fileSystem.DirectoryInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "test-output"));
		var configPath = fileSystem.FileInfo.New(docsetPath);

		var context = new TestDocumentationSetContext(fileSystem, docsDir, outputDir, configPath, output, "docs-builder");
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, configPath);

		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(docSet, context, TestDocumentationFileFactory.Instance);

		// Find cross-link items
		var baseInterfaces = QueryAllAdheringTo<INavigationModel>(navigation);
		var interfaces = QueryAllAdheringTo<IDocumentationFile>(navigation);
		// ReSharper disable once RedundantTypeArgumentsOfMethod
		var concrete = QueryAllAdheringTo<TestDocumentationFile>(navigation);

		baseInterfaces.Count.Should().Be(interfaces.Count);
		interfaces.Count.Should().Be(concrete.Count);
	}

	private static List<INavigationItem> QueryAllAdheringTo<TModel>(INodeNavigationItem<TModel, INavigationItem> navigation)
		where TModel : class, INavigationModel
	{
		var result = new List<INavigationItem> { navigation, navigation.Index };
		foreach (var item in navigation.NavigationItems)
		{
			result.Add(item);
			if (item is INodeNavigationItem<TModel, INavigationItem> node)
				result.AddRange(QueryAllAdheringTo(node));
		}
		return result;
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
