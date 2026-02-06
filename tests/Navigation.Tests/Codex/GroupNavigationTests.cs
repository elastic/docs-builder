// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Codex.Navigation;
using FluentAssertions;

namespace Elastic.Documentation.Navigation.Tests.Codex;

public class GroupNavigationTests
{
	[Fact]
	public void GroupNavigation_SetsPropertiesCorrectly()
	{
		var groupNav = new GroupNavigation("observability", "Observability", "/docs/g/observability");

		groupNav.GroupSlug.Should().Be("observability");
		groupNav.DisplayTitle.Should().Be("Observability");
		groupNav.Url.Should().Be("/docs/g/observability");
		groupNav.NavigationTitle.Should().Be("Observability");
	}

	[Fact]
	public void GroupNavigation_TrimsTrailingSlash()
	{
		var groupNav = new GroupNavigation("tools", "Tools", "/docs/g/tools/");

		groupNav.Url.Should().Be("/docs/g/tools");
	}

	[Fact]
	public void GroupNavigation_HasIndexPage()
	{
		var groupNav = new GroupNavigation("observability", "Observability", "/docs/g/observability");

		groupNav.Index.Should().NotBeNull();
		groupNav.Index.Url.Should().Be("/docs/g/observability");
		groupNav.Index.NavigationTitle.Should().Be("Observability");
	}

	[Fact]
	public void GroupNavigation_IndexNavigationRootPointsToGroup()
	{
		var groupNav = new GroupNavigation("observability", "Observability", "/docs/g/observability");

		groupNav.Index.NavigationRoot.Should().BeSameAs(groupNav);
	}

	[Fact]
	public void GroupNavigation_IsItsOwnNavigationRoot()
	{
		var groupNav = new GroupNavigation("observability", "Observability", "/docs/g/observability");

		groupNav.NavigationRoot.Should().BeSameAs(groupNav);
	}

	[Fact]
	public void GroupNavigation_StartsWithEmptyNavigationItems()
	{
		var groupNav = new GroupNavigation("observability", "Observability", "/docs/g/observability");

		groupNav.NavigationItems.Should().BeEmpty();
	}

	[Fact]
	public void GroupNavigation_CanSetNavigationItems()
	{
		var groupNav = new GroupNavigation("observability", "Observability", "/docs/g/observability");
		var mockItems = new List<INavigationItem>();

		((IAssignableChildrenNavigation)groupNav).SetNavigationItems(mockItems);

		groupNav.NavigationItems.Should().BeSameAs(mockItems);
	}

	[Fact]
	public void GroupNavigation_HasUniqueIdentifier()
	{
		var group1 = new GroupNavigation("observability", "Observability", "/g/observability");
		var group2 = new GroupNavigation("security", "Security", "/g/security");

		group1.Id.Should().NotBe(group2.Id);
		group1.Identifier.Should().NotBe(group2.Identifier);
	}

	[Fact]
	public void GroupNavigation_IdentifierUsesCodexScheme()
	{
		var groupNav = new GroupNavigation("observability", "Observability", "/g/observability");

		groupNav.Identifier.Scheme.Should().Be("codex");
		groupNav.Identifier.Host.Should().Be("group");
		groupNav.Identifier.AbsolutePath.Should().Be("/observability");
	}

	[Fact]
	public void GroupLinkLeaf_PropertiesSetCorrectly()
	{
		var codexNav = CreateMinimalCodexNavigation();
		var linkPage = new GroupLinkPage("Observability", "/docs/g/observability");
		var linkLeaf = new GroupLinkLeaf(linkPage, codexNav);

		linkLeaf.Url.Should().Be("/docs/g/observability");
		linkLeaf.NavigationTitle.Should().Be("Observability");
		linkLeaf.NavigationRoot.Should().BeSameAs(codexNav);
		linkLeaf.Hidden.Should().BeFalse();
	}

	[Fact]
	public void GroupIndexLeaf_PropertiesSetCorrectly()
	{
		var groupNav = new GroupNavigation("observability", "Observability", "/docs/g/observability");

		var indexLeaf = groupNav.Index as GroupIndexLeaf;
		indexLeaf.Should().NotBeNull();
		indexLeaf!.Url.Should().Be("/docs/g/observability");
		indexLeaf.NavigationTitle.Should().Be("Observability");
		indexLeaf.NavigationRoot.Should().BeSameAs(groupNav);
		indexLeaf.Parent.Should().BeSameAs(groupNav);
	}

	private static CodexNavigation CreateMinimalCodexNavigation()
	{
		// Create a minimal codex navigation for testing GroupLinkLeaf
		var config = new Documentation.Configuration.Codex.CodexConfiguration
		{
			Title = "Test Codex",
			SitePrefix = "/docs",
			DocumentationSets = []
		};

		return new CodexNavigation(config, new MinimalCodexContext(), new Dictionary<string, Navigation.Isolated.Node.IDocumentationSetNavigation>());
	}

	private sealed class MinimalCodexContext : ICodexDocumentationContext
	{
		private readonly System.IO.Abstractions.TestingHelpers.MockFileSystem _fs = new();
		public System.IO.Abstractions.IFileInfo ConfigurationPath => _fs.FileInfo.New("/codex.yml");
		public Elastic.Documentation.Diagnostics.IDiagnosticsCollector Collector => new Elastic.Documentation.Diagnostics.DiagnosticsCollector([]);
		public System.IO.Abstractions.IFileSystem ReadFileSystem => _fs;
		public System.IO.Abstractions.IFileSystem WriteFileSystem => _fs;
		public System.IO.Abstractions.IDirectoryInfo OutputDirectory => _fs.DirectoryInfo.New("/output");
		public BuildType BuildType => BuildType.Codex;
		public void EmitError(string message) { }
	}
}
