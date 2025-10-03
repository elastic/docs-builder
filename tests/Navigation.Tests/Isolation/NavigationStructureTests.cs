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

		var navigation = new DocumentationSetNavigation(docSet, context, TestDocumentationFileFactory.Instance);

		navigation.NavigationItems.ElementAt(0).NavigationIndex.Should().Be(0);
		navigation.NavigationItems.ElementAt(1).NavigationIndex.Should().Be(1);
		navigation.NavigationItems.ElementAt(2).NavigationIndex.Should().Be(2);
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
		                   children:
		                     - toc: performance
		             - title: "External"
		               crosslink: other://link.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs/setup/advanced");
		fileSystem.AddDirectory("/docs/setup/advanced/performance");
		fileSystem.AddFile("/docs/setup/advanced/toc.yml", new MockFileData("toc: []"));
		fileSystem.AddFile("/docs/setup/advanced/performance/toc.yml", new MockFileData("toc: []"));
		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext(fileSystem);
		_ = context.Collector.StartAsync(TestContext.Current.CancellationToken);

		var navigation = new DocumentationSetNavigation(docSet, context, TestDocumentationFileFactory.Instance);

		await context.Collector.StopAsync(TestContext.Current.CancellationToken);


		navigation.NavigationItems.Should().HaveCount(3);
		navigation.IsUsingNavigationDropdown.Should().BeTrue();

		// First item: simple file
		var indexFile = navigation.NavigationItems.ElementAt(0).Should().BeOfType<FileNavigationLeaf<IDocumentationFile>>().Subject;
		indexFile.Url.Should().Be("/"); // index.md becomes /

		// Second item: complex nested structure
		var setupFolder = navigation.NavigationItems.ElementAt(1).Should().BeOfType<FolderNavigation>().Subject;
		setupFolder.NavigationItems.Should().HaveCount(2);
		setupFolder.Url.Should().Be("/setup");

		var setupIndex = setupFolder.NavigationItems.ElementAt(0).Should().BeOfType<FileNavigationLeaf<IDocumentationFile>>().Subject;
		setupIndex.Url.Should().Be("/setup"); // index.md becomes /setup

		var advancedToc = setupFolder.NavigationItems.ElementAt(1).Should().BeOfType<TableOfContentsNavigation>().Subject;
		advancedToc.Url.Should().Be("/setup/advanced");
		advancedToc.NavigationItems.Should().HaveCount(1);

		var performanceToc = advancedToc.NavigationItems.First().Should().BeOfType<TableOfContentsNavigation>().Subject;
		performanceToc.Url.Should().Be("/setup/advanced/performance");
		// Nested TOC has a placeholder since it has no explicit children
		performanceToc.NavigationItems.Should().HaveCount(1);

		// Third item: crosslink
		var crosslink = navigation.NavigationItems.ElementAt(2).Should().BeOfType<CrossLinkNavigationLeaf>().Subject;
		crosslink.IsCrossLink.Should().BeTrue();

		// Verify no errors were emitted
		context.Diagnostics.Should().BeEmpty();
	}
}
