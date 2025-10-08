// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Navigation.Isolated;
using FluentAssertions;

namespace Elastic.Documentation.Navigation.Tests.Isolation;

public class NavigationStructureTests(ITestOutputHelper output) : DocumentationSetNavigationTestBase(output)
{
	[Fact]
	public void NavigationIndexIsSetCorrectly()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: first.md
		             - file: second.md
		             - file: third.md
		           """;

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext();

		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(docSet, context, TestDocumentationFileFactory.Instance);

		navigation.NavigationItems.ElementAt(0).NavigationIndex.Should().Be(0);
		navigation.NavigationItems.ElementAt(1).NavigationIndex.Should().Be(1);
		navigation.NavigationItems.ElementAt(2).NavigationIndex.Should().Be(2);
	}

	[Fact]
	public void CanQueryNavigationForBothInterfaceAndConcreteTypes()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: first.md
		             - folder: guides
		               children:
		                 - file: second.md
		                 - file: third.md
		             - file: fourth.md
		           """;

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext();

		// Create navigation using the covariant factory interface
		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(docSet, context, TestDocumentationFileFactory.Instance);

		// Query for all leaf items using the base interface type
		var allLeafItems = navigation.NavigationItems
			.SelectMany(item => item is INodeNavigationItem<IDocumentationFile, INavigationItem> node
				? node.NavigationItems.OfType<ILeafNavigationItem<IDocumentationFile>>()
				: item is ILeafNavigationItem<IDocumentationFile> leaf
					? [leaf]
					: [])
			.ToList();

		// All items are queryable as ILeafNavigationItem<IDocumentationFile> due to covariance
		allLeafItems.Should().HaveCount(4);
		allLeafItems.Should().AllBeAssignableTo<ILeafNavigationItem<IDocumentationFile>>();
		allLeafItems.Should().AllBeAssignableTo<ILeafNavigationItem<TestDocumentationFile>>();
		allLeafItems.Select(l => l.NavigationTitle).Should().BeEquivalentTo(["first", "second", "third", "fourth"]);

		// The navigation items themselves are FileNavigationLeaf<TestDocumentationFile> at runtime
		allLeafItems.Should().AllBeOfType<FileNavigationLeaf<TestDocumentationFile>>();

		// And the Model property on each leaf contains TestDocumentationFile instances
		var allModels = allLeafItems.Select(l => l.Model).ToList();
		allModels.Should().AllBeOfType<TestDocumentationFile>();

		// Access the underlying model through the interface
		foreach (var leaf in allLeafItems)
		{
			// The Model property returns IDocumentationFile due to covariance
			leaf.Model.Should().BeAssignableTo<IDocumentationFile>();
			leaf.Model.NavigationTitle.Should().NotBeNullOrEmpty();

			// But at runtime, it's still TestDocumentationFile
			leaf.Model.Should().BeOfType<TestDocumentationFile>();

			// Can access concrete type through pattern matching without explicit cast
			if (leaf.Model is TestDocumentationFile concreteFile)
				concreteFile.NavigationTitle.Should().Be(leaf.NavigationTitle);
		}

		// Demonstrate type-safe LINQ queries work with the interface type
		var firstItem = allLeafItems.FirstOrDefault(l => l.Model.NavigationTitle == "first");
		firstItem.Should().NotBeNull();
		firstItem.Url.Should().Be("/first");
	}

	[Fact]
	public async Task ComplexNestedStructureBuildsCorrectly()
	{
		// language=yaml
		var yaml = """
		           project: 'docs-builder'
		           features:
		             primary-nav: true
		           toc:
		             - file: index.md
		             - folder: setup
		               children:
		                 - file: index.md
		                 - toc: advanced
		             - title: "External"
		               crosslink: other://link.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs/setup/advanced");
		fileSystem.AddDirectory("/docs/setup/advanced/performance");
		fileSystem.AddFile("/docs/setup/advanced/toc.yml", new MockFileData(
			// language=yaml
			"""
			toc:
			  - file: index.md
			  - toc: performance
			"""));

		// language=yaml
		var performanceTocYaml = """
		                         toc:
		                           - file: index.md
		                           - file: tuning.md
		                           - file: benchmarks.md
		                         """;
		fileSystem.AddFile("/docs/setup/advanced/performance/toc.yml", new MockFileData(performanceTocYaml));
		// Add index.md files that should be automatically discovered as placeholders
		fileSystem.AddFile("/docs/setup/advanced/index.md", new MockFileData("# Advanced"));
		fileSystem.AddFile("/docs/setup/advanced/performance/index.md", new MockFileData("# Performance"));
		fileSystem.AddFile("/docs/setup/advanced/performance/tuning.md", new MockFileData("# Tuning"));
		fileSystem.AddFile("/docs/setup/advanced/performance/benchmarks.md", new MockFileData("# Benchmarks"));
		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext(fileSystem);
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(docSet, context, TestDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);

		navigation.NavigationItems.Should().HaveCount(3);
		navigation.IsUsingNavigationDropdown.Should().BeTrue();

		// First item: simple file
		var indexFile = navigation.NavigationItems.ElementAt(0).Should().BeOfType<FileNavigationLeaf<TestDocumentationFile>>().Subject;
		indexFile.Url.Should().Be("/"); // index.md becomes /

		// Second item: complex nested structure
		var setupFolder = navigation.NavigationItems.ElementAt(1).Should().BeOfType<FolderNavigation>().Subject;
		setupFolder.NavigationItems.Should().HaveCount(2);
		setupFolder.Url.Should().Be("/setup");

		var setupIndex = setupFolder.NavigationItems.ElementAt(0).Should().BeOfType<FileNavigationLeaf<TestDocumentationFile>>().Subject;
		setupIndex.Url.Should().Be("/setup"); // index.md becomes /setup

		var advancedToc = setupFolder.NavigationItems.ElementAt(1).Should().BeOfType<TableOfContentsNavigation>().Subject;
		advancedToc.Url.Should().Be("/setup/advanced");
		// Advanced TOC has index.md and the nested performance TOC as children
		advancedToc.NavigationItems.Should().HaveCount(2);

		var advancedIndex = advancedToc.NavigationItems.ElementAt(0).Should().BeOfType<FileNavigationLeaf<TestDocumentationFile>>().Subject;
		advancedIndex.Url.Should().Be("/setup/advanced");

		var performanceToc = advancedToc.NavigationItems.ElementAt(1).Should().BeOfType<TableOfContentsNavigation>().Subject;
		performanceToc.Url.Should().Be("/setup/advanced/performance");
		performanceToc.NavigationItems.Should().HaveCount(3);

		var performanceIndex = performanceToc.NavigationItems.ElementAt(0).Should().BeOfType<FileNavigationLeaf<TestDocumentationFile>>().Subject;
		performanceIndex.Url.Should().Be("/setup/advanced/performance");

		var tuning = performanceToc.NavigationItems.ElementAt(1).Should().BeOfType<FileNavigationLeaf<TestDocumentationFile>>().Subject;
		tuning.Url.Should().Be("/setup/advanced/performance/tuning");

		var benchmarks = performanceToc.NavigationItems.ElementAt(2).Should().BeOfType<FileNavigationLeaf<TestDocumentationFile>>().Subject;
		benchmarks.Url.Should().Be("/setup/advanced/performance/benchmarks");

		// Third item: crosslink
		var crosslink = navigation.NavigationItems.ElementAt(2).Should().BeOfType<CrossLinkNavigationLeaf>().Subject;
		crosslink.IsCrossLink.Should().BeTrue();

		// Verify no errors were emitted
		context.Diagnostics.Should().BeEmpty();
	}

	[Fact]
	public void NestedTocUrlsDoNotDuplicatePath()
	{
		// This test verifies that nested TOC URLs are constructed correctly
		// without duplicating path segments (e.g., /setup/advanced not /setup/setup/advanced)

		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: setup
		               children:
		                 - file: index.md
		                 - toc: advanced
		           """;

		// language=yaml
		var advancedTocYaml = """
		                      toc:
		                        - file: index.md
		                        - toc: performance
		                      """;

		// language=yaml
		var performanceTocYaml = """
		                         toc:
		                           - file: index.md
		                         """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddFile("/docs/setup/index.md", new MockFileData("# Setup"));
		fileSystem.AddFile("/docs/setup/advanced/index.md", new MockFileData("# Advanced Setup"));
		fileSystem.AddFile("/docs/setup/advanced/toc.yml", new MockFileData(advancedTocYaml));
		fileSystem.AddFile("/docs/setup/advanced/performance/index.md", new MockFileData("# Performance"));
		fileSystem.AddFile("/docs/setup/advanced/performance/toc.yml", new MockFileData(performanceTocYaml));

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext(fileSystem);

		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(docSet, context, TestDocumentationFileFactory.Instance);

		var setupFolder = navigation.NavigationItems.First().Should().BeOfType<FolderNavigation>().Subject;
		setupFolder.Url.Should().Be("/setup");

		// Setup folder has index.md and advanced TOC
		setupFolder.NavigationItems.Should().HaveCount(2);

		var advancedToc = setupFolder.NavigationItems.ElementAt(1).Should().BeOfType<TableOfContentsNavigation>().Subject;
		// Verify the URL is /setup/advanced and not /setup/setup/advanced
		advancedToc.Url.Should().Be("/setup/advanced");

		// Advanced TOC has index.md and performance TOC
		advancedToc.NavigationItems.Should().HaveCount(2);

		var performanceToc = advancedToc.NavigationItems.ElementAt(1).Should().BeOfType<TableOfContentsNavigation>().Subject;
		// Verify the URL is /setup/advanced/performance and not /setup/advanced/setup/advanced/performance
		performanceToc.Url.Should().Be("/setup/advanced/performance");

		context.Diagnostics.Should().BeEmpty();
	}
}
