// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation.Isolated.Node;
using Elastic.Documentation.Navigation.Tests.Isolation;
using Elastic.Documentation.Site.Navigation;

namespace Elastic.Documentation.Navigation.Tests.Rendering;

public class NavigationRenderModelTests(ITestOutputHelper output) : DocumentationSetNavigationTestBase(output)
{
	[Fact]
	public void EquivalentTrees_ProduceSameContentHash()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: index.md
		             - folder: setup
		               children:
		                 - file: index.md
		                 - file: install.md
		           """;

		var first = CreateRenderModel(yaml);
		var second = CreateRenderModel(yaml);

		first.ContentHash.Should().Be(second.ContentHash);
	}

	[Fact]
	public void DifferentPages_ProduceDifferentContentHashes()
	{
		// language=yaml
		var first = CreateRenderModel("""
		                              project: 'test-project'
		                              toc:
		                                - file: index.md
		                                - file: overview.md
		                              """);
		// language=yaml
		var second = CreateRenderModel("""
		                               project: 'test-project'
		                               toc:
		                                 - file: index.md
		                                 - file: reference.md
		                               """);

		first.ContentHash.Should().NotBe(second.ContentHash);
	}

	[Fact]
	public void ReorderedSiblings_ProduceDifferentContentHashes()
	{
		// language=yaml
		var first = CreateRenderModel("""
		                              project: 'test-project'
		                              toc:
		                                - file: index.md
		                                - file: alpha.md
		                                - file: beta.md
		                              """);
		// language=yaml
		var second = CreateRenderModel("""
		                               project: 'test-project'
		                               toc:
		                                 - file: index.md
		                                 - file: beta.md
		                                 - file: alpha.md
		                               """);

		first.ContentHash.Should().NotBe(second.ContentHash);
	}

	[Fact]
	public void HiddenItems_AreExcludedFromTheTree_AndChangeTheContentHash()
	{
		// language=yaml
		var visible = CreateRenderModel("""
		                                project: 'test-project'
		                                toc:
		                                  - file: index.md
		                                  - file: guide.md
		                                  - file: secret.md
		                                """);
		// language=yaml
		var hidden = CreateRenderModel("""
		                               project: 'test-project'
		                               toc:
		                                 - file: index.md
		                                 - file: guide.md
		                                 - hidden: secret.md
		                               """);

		visible.Tree.Should().Contain(n => n.Url == "/secret");
		hidden.Tree.Should().NotContain(n => n.Url == "/secret");
		hidden.ContentHash.Should().NotBe(visible.ContentHash);
	}

	[Fact]
	public void PrimaryNav_OmitsIndexRow_AndChangesTheContentHash()
	{
		// language=yaml
		var yaml = """
		           project: 'test-project'
		           toc:
		             - file: index.md
		             - file: guide.md
		           """;

		var withIndexRow = CreateRenderModel(yaml);
		var withoutIndexRow = CreateRenderModel(yaml, isPrimaryNavEnabled: true);

		withIndexRow.RootIndex.Should().NotBeNull();
		withIndexRow.RootIndex.Url.Should().Be("/");
		withoutIndexRow.RootIndex.Should().BeNull();
		withoutIndexRow.ContentHash.Should().NotBe(withIndexRow.ContentHash);
	}

	[Fact]
	public void Nodes_CarryToggleStateAndNavigationItems()
	{
		// language=yaml
		var model = CreateRenderModel("""
		                              project: 'test-project'
		                              toc:
		                                - file: index.md
		                                - folder: setup
		                                  children:
		                                    - file: index.md
		                                    - file: install.md
		                              """);

		var node = model.Tree.Should().ContainSingle(n => n.Kind == NavigationRenderNodeKind.Node).Subject;
		node.Url.Should().Be("/setup");
		node.Id.Should().NotBeNullOrEmpty();
		node.ShowToggle.Should().BeTrue();
		node.NavigationItems.Should().ContainSingle(n => n.Url == "/setup/install");
	}

	private NavigationRenderModel CreateRenderModel(string yaml, bool isPrimaryNavEnabled = false)
	{
		var fileSystem = new MockFileSystem();
		fileSystem.AddDirectory("/docs");
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(docSet, context, TestDocumentationFileFactory.Instance);
		return NavigationRenderModel.Create(
			tree: navigation,
			topLevelItems: navigation.NavigationItems.OfType<INodeNavigationItem<INavigationModel, INavigationItem>>().ToList(),
			isUsingNavigationDropdown: false,
			isPrimaryNavEnabled: isPrimaryNavEnabled,
			isGlobalAssemblyBuild: false);
	}
}
