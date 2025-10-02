// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Navigation.Isolated;
using FluentAssertions;

namespace Elastic.Documentation.Navigation.Tests.Isolation;

public class ConstructorTests(ITestOutputHelper output) : DocumentationSetNavigationTestBase(output)
{
	[Fact]
	public void ConstructorInitializesRootProperties()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: index.md
		           """;

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext();

		var navigation = new DocumentationSetNavigation(docSet, context);

		navigation.NavigationRoot.Should().BeSameAs(navigation);
		navigation.Parent.Should().BeNull();
		navigation.Depth.Should().Be(0);
		navigation.Hidden.Should().BeFalse();
		navigation.IsCrossLink.Should().BeFalse();
		navigation.Id.Should().NotBeNullOrEmpty();
		navigation.NavigationTitle.Should().Be("test-project");
		navigation.IsUsingNavigationDropdown.Should().BeFalse();
		navigation.Url.Should().Be("/");
	}

	[Fact]
	public void ConstructorSetsIsUsingNavigationDropdownFromFeatures()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           features:
		             primary-nav: true
		           toc:
		             - file: index.md
		           """;

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext();

		var navigation = new DocumentationSetNavigation(docSet, context);

		navigation.IsUsingNavigationDropdown.Should().BeTrue();
	}

	[Fact]
	public void ConstructorCreatesFileNavigationLeafFromFileRef()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: getting-started.md
		           """;

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext();

		var navigation = new DocumentationSetNavigation(docSet, context);

		navigation.NavigationItems.Should().HaveCount(1);
		var fileNav = navigation.NavigationItems.First().Should().BeOfType<FileNavigationLeaf>().Subject;
		fileNav.NavigationTitle.Should().Be("getting-started");
		fileNav.Url.Should().Be("/getting-started");
		fileNav.Hidden.Should().BeFalse();
		fileNav.NavigationRoot.Should().BeSameAs(navigation);
		fileNav.Parent.Should().BeNull();
	}

	[Fact]
	public void ConstructorCreatesHiddenFileNavigationLeaf()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - hidden: 404.md
		           """;

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext();

		var navigation = new DocumentationSetNavigation(docSet, context);

		navigation.NavigationItems.Should().HaveCount(1);
		var fileNav = navigation.NavigationItems.First().Should().BeOfType<FileNavigationLeaf>().Subject;
		fileNav.Hidden.Should().BeTrue();
		fileNav.Url.Should().Be("/404");
	}

	[Fact]
	public void ConstructorCreatesCrossLinkNavigation()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - title: "External Guide"
		               crosslink: docs-content://guide.md
		           """;

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext();

		var navigation = new DocumentationSetNavigation(docSet, context);

		navigation.NavigationItems.Should().HaveCount(1);
		var crossLink = navigation.NavigationItems.First().Should().BeOfType<CrossLinkNavigationLeaf>().Subject;
		crossLink.NavigationTitle.Should().Be("External Guide");
		crossLink.Url.Should().Be("docs-content://guide.md");
		crossLink.IsCrossLink.Should().BeTrue();
	}

	[Fact]
	public void ConstructorCreatesFolderNavigationWithChildren()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: setup
		               children:
		                 - file: index.md
		                 - file: install.md
		           """;

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext();

		var navigation = new DocumentationSetNavigation(docSet, context);

		navigation.NavigationItems.Should().HaveCount(1);
		var folder = navigation.NavigationItems.First().Should().BeOfType<FolderNavigation>().Subject;
		folder.Depth.Should().Be(1);
		folder.Url.Should().Be("/setup");
		folder.NavigationItems.Should().HaveCount(2);

		var firstFile = folder.NavigationItems.ElementAt(0).Should().BeOfType<FileNavigationLeaf>().Subject;
		firstFile.Url.Should().Be("/setup"); // index.md becomes /setup
		firstFile.Parent.Should().BeSameAs(folder);

		var secondFile = folder.NavigationItems.ElementAt(1).Should().BeOfType<FileNavigationLeaf>().Subject;
		secondFile.Url.Should().Be("/setup/install");
	}

	[Fact]
	public void ConstructorCreatesTableOfContentsNavigationWithChildren()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - toc: api
		               children:
		                 - file: index.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs/api");
		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext(fileSystem);

		var navigation = new DocumentationSetNavigation(docSet, context);

		navigation.NavigationItems.Should().HaveCount(1);
		var toc = navigation.NavigationItems.First().Should().BeOfType<TableOfContentsNavigation>().Subject;
		toc.Depth.Should().Be(1);
		toc.Url.Should().Be("/api");
		toc.NavigationItems.Should().HaveCount(1);

		var file = toc.NavigationItems.First().Should().BeOfType<FileNavigationLeaf>().Subject;
		file.Url.Should().Be("/api"); // index.md becomes /api
		file.Parent.Should().BeSameAs(toc);
		file.NavigationRoot.Should().BeSameAs(navigation);
	}
}
