// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Navigation.Isolated;
using FluentAssertions;

namespace Elastic.Documentation.Navigation.Tests.Isolation;

public class DynamicUrlTests(ITestOutputHelper output) : DocumentationSetNavigationTestBase(output)
{
	[Fact]
	public void DynamicUrlUpdatesWhenRootUrlChanges()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: setup
		               children:
		                 - file: install.md
		           """;

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext();

		var navigation = new DocumentationSetNavigation(docSet, context);
		var folder = navigation.NavigationItems.First() as FolderNavigation;
		var file = folder!.NavigationItems.First();

		// Initial URL
		file.Url.Should().Be("/setup/install");

		// Change root URL
		navigation.PathPrefixProvider = new PathPrefixProvider("/v8.0");

		// URLs should update dynamically
		// Since folder has no index child, its URL is the first child's URL
		folder.Url.Should().Be("/v8.0/setup/install");
		file.Url.Should().Be("/v8.0/setup/install");

		// Change root URL
		navigation.PathPrefixProvider = new PathPrefixProvider("/v9.0");

		// URLs should update dynamically
		// Since folder has no index child, its URL is the first child's URL
		folder.Url.Should().Be("/v9.0/setup/install");
		file.Url.Should().Be("/v9.0/setup/install");
	}

	[Fact]
	public void UrlRootPropagatesCorrectlyThroughFolders()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: outer
		               children:
		                 - folder: inner
		                   children:
		                     - file: deep.md
		           """;

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext();

		var navigation = new DocumentationSetNavigation(docSet, context);
		var outerFolder = navigation.NavigationItems.First() as FolderNavigation;
		var innerFolder = outerFolder!.NavigationItems.First() as FolderNavigation;
		var file = innerFolder!.NavigationItems.First();

		file.Url.Should().Be("/outer/inner/deep");

		// Change root URL
		navigation.PathPrefixProvider = new PathPrefixProvider("/base");

		file.Url.Should().Be("/base/outer/inner/deep");
	}

	[Fact]
	public void FolderWithoutIndexUsesFirstChildUrl()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: guides
		               children:
		                 - file: getting-started.md
		                 - file: advanced.md
		           """;

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext();

		var navigation = new DocumentationSetNavigation(docSet, context);
		var folder = navigation.NavigationItems.First() as FolderNavigation;

		// Folder has no index.md, so URL should be the first child's URL
		folder!.Url.Should().Be("/guides/getting-started");
	}

	[Fact]
	public void FolderWithIndexUsesOwnUrl()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: guides
		               children:
		                 - file: index.md
		                 - file: advanced.md
		           """;

		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext();

		var navigation = new DocumentationSetNavigation(docSet, context);
		var folder = navigation.NavigationItems.First() as FolderNavigation;

		// Folder has index.md, so URL should be the folder path
		folder!.Url.Should().Be("/guides");
	}

	[Fact]
	public void UrlRootChangesForTableOfContentsNavigation()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - folder: guides
		               children:
		                 - toc: api
		                   children:
		                     - file: reference.md
		           """;

		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs/guides/api");
		var docSet = DocumentationSetFile.Deserialize(yaml);
		var context = CreateContext(fileSystem);

		var navigation = new DocumentationSetNavigation(docSet, context);
		var folder = navigation.NavigationItems.First() as FolderNavigation;
		var toc = folder!.NavigationItems.First() as TableOfContentsNavigation;
		var file = toc!.NavigationItems.First();

		// The TOC becomes the new URL root, so the file URL is based on TOC's URL
		toc.Url.Should().Be("/guides/api");
		file.Url.Should().Be("/guides/api/reference");

		// Change root URL
		navigation.PathPrefixProvider = new PathPrefixProvider("/v2");

		// Both TOC and file URLs should update
		toc.Url.Should().Be("/v2/guides/api");
		file.Url.Should().Be("/v2/guides/api/reference");
	}
}
