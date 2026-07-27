// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Navigation.Isolated.Leaf;
using Elastic.Documentation.Navigation.Isolated.Node;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.Navigation.Tests.Isolation;

public class PhysicalDocsetTests(ITestOutputHelper output)
{
	[Fact]
	public async Task PhysicalDocsetCanBeNavigated()
	{
		var docsetPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs", "_docset.yml");
		File.Exists(docsetPath).Should().BeTrue($"Expected docset file to exist at {docsetPath}");

		var fileSystem = new FileSystem();
		var docsDir = fileSystem.DirectoryInfo.New(Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs"));
		var outputDir = fileSystem.DirectoryInfo.New(Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "test-output"));
		var configPath = fileSystem.FileInfo.New(docsetPath);

		var context = new TestDocumentationSetContext(fileSystem, docsDir, outputDir, configPath, output, "docs-builder");
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, configPath, context.ReadFileSystem, noSuppress: [HintType.DeepLinkingVirtualFile]);

		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(docSet, context, TestDocumentationFileFactory.Instance, crossLinkResolver: TestCrossLinkResolver.Instance);

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

		// Check key folders by URL
		var folderUrls = folders.Select(f => f.Url).ToList();
		folderUrls.Should().Contain("/getting-started");
		folderUrls.Should().Contain("/documentation");

		// No errors or warnings should be emitted during navigation construction
		context.Collector.Errors.Should().Be(0, "no errors should be emitted");
		context.Collector.Warnings.Should().Be(0, "no warnings should be emitted");
	}

	[Fact]
	public async Task PhysicalDocsetNavigationHasCorrectUrls()
	{
		var docsetPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs", "_docset.yml");
		var fileSystem = new FileSystem();
		var docsDir = fileSystem.DirectoryInfo.New(Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs"));
		var outputDir = fileSystem.DirectoryInfo.New(Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "test-output"));
		var configPath = fileSystem.FileInfo.New(docsetPath);

		var context = new TestDocumentationSetContext(fileSystem, docsDir, outputDir, configPath, output, "docs-builder");
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, configPath, FileSystemFactory.RealGitRootForPath(null));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(docSet, context, TestDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		// Find the documentation folder by URL
		var documentationFolder = navigation.NavigationItems.OfType<FolderNavigation<TestDocumentationFile>>()
			.FirstOrDefault(f => f.Url == "/documentation");
		documentationFolder.Should().NotBeNull();

		// Verify nested structure
		documentationFolder.NavigationItems.Should().NotBeEmpty();
	}

	[Fact]
	public async Task PhysicalDocsetNavigationIncludesNestedTocs()
	{
		var docsetPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs", "_docset.yml");
		var fileSystem = new FileSystem();
		var docsDir = fileSystem.DirectoryInfo.New(Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs"));
		var outputDir = fileSystem.DirectoryInfo.New(Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "test-output"));
		var configPath = fileSystem.FileInfo.New(docsetPath);

		var context = new TestDocumentationSetContext(fileSystem, docsDir, outputDir, configPath, output, "docs-builder");
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, configPath, FileSystemFactory.RealGitRootForPath(null));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(docSet, context, TestDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		var fileRefs = docSet.TableOfContents.SelectMany(DocumentationSetFile.GetFileRefs).ToList();
		foreach (var fileRef in fileRefs)
		{
			var path = fileSystem.FileInfo.New(Path.Join(configPath.Directory!.FullName, fileRef.PathRelativeToDocumentationSet));
			path.Exists.Should().BeTrue($"Expected file {path.FullName} to exist");
		}
		fileRefs.Count.Should().Be(fileRefs.Distinct().Count(), "should not have duplicate file references");

		// Find development folder — it's now a regular folder, not a nested toc
		var developmentFolder = navigation.NavigationItems.OfType<FolderNavigation<TestDocumentationFile>>()
			.FirstOrDefault(t => t.Url == "/development");
		developmentFolder.Should().NotBeNull();
		developmentFolder.NavigationItems.Should().NotBeEmpty();
	}

	[Fact]
	public async Task PhysicalDocsetNavigationHandlesHiddenFiles()
	{
		var docsetPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs", "_docset.yml");
		var fileSystem = new FileSystem();
		var docsDir = fileSystem.DirectoryInfo.New(Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs"));
		var outputDir = fileSystem.DirectoryInfo.New(Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "test-output"));
		var configPath = fileSystem.FileInfo.New(docsetPath);

		var context = new TestDocumentationSetContext(fileSystem, docsDir, outputDir, configPath, output, "docs-builder");
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, configPath, FileSystemFactory.RealGitRootForPath(null));
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(docSet, context, TestDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		// Find hidden files
		var allItems = GetAllNavigationItems(navigation.NavigationItems);
		var hiddenItems = allItems.Where(i => i.Hidden).ToList();
		hiddenItems.Should().NotBeEmpty();
	}

	[Fact]
	public async Task PhysicalTestDocsetNavigationHandlesCrossLinks()
	{
		var docsetPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs-tests", "docset.yml");
		File.Exists(docsetPath).Should().BeTrue($"Expected test docset file to exist at {docsetPath}");

		var fileSystem = new FileSystem();
		var docsDir = fileSystem.DirectoryInfo.New(Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs-tests"));
		var outputDir = fileSystem.DirectoryInfo.New(Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "test-output"));
		var configPath = fileSystem.FileInfo.New(docsetPath);

		var context = new TestDocumentationSetContext(fileSystem, docsDir, outputDir, configPath, output, "doc-builder-tests");
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, configPath);
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(docSet, context, TestDocumentationFileFactory.Instance, crossLinkResolver: TestCrossLinkResolver.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		// Find cross-link items
		var allItems = GetAllNavigationItems(navigation.NavigationItems);
		var crossLinks = allItems.OfType<CrossLinkNavigationLeaf>().ToList();
		crossLinks.Should().NotBeEmpty();
	}

	[Fact]
	public void CovarianceOfNavigationItemsIsRespected()
	{
		var docsetPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs", "_docset.yml");
		var fileSystem = new FileSystem();
		var docsDir = fileSystem.DirectoryInfo.New(Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs"));
		var outputDir = fileSystem.DirectoryInfo.New(Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "test-output"));
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
