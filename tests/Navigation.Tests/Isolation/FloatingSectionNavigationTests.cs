// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation.Floating;
using Elastic.Documentation.Navigation.Isolated.Node;
using Elastic.Documentation.Navigation.Tests.Assembler;

namespace Elastic.Documentation.Navigation.Tests.Isolation;

public class FloatingSectionNavigationTests() : DocumentationSetNavigationTestBase()
{
	private const string Yaml =
		// language=yaml
		"""
		project: 'docs-builder'
		toc:
		  - file: index.md
		  - api: demo
		  - file: page.md
		""";

	private static (MockFileSystem FileSystem, TestDocumentationSetContext Context, DocumentationSetFile DocSet) Arrange(
		Func<MockFileSystem, TestDocumentationSetContext> createContext
	)
	{
		var fileSystem = new MockFileSystem();
		fileSystem.AddFile("/docs/index.md", new MockFileData("# Root"));
		fileSystem.AddFile("/docs/page.md", new MockFileData("# Page"));
		var context = createContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, Yaml, fileSystem.NewDirInfo("docs"));
		return (fileSystem, context, docSet);
	}

	[Test]
	public async Task FloatingSectionRef_MountedWithProvider_RendersAsIslandUnderTheDocset()
	{
		var (_, context, docSet) = Arrange(CreateContext);
		var registry = new FloatingSectionRegistry([new DemoProvider()]);

		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(
			docSet,
			context,
			TestDocumentationFileFactory.Instance,
			crossLinkResolver: TestCrossLinkResolver.Instance,
			floatingSections: registry
		);
		await Task.CompletedTask;

		var section = navigation.NavigationItems.OfType<DemoRoot>().Should().ContainSingle().Subject;
		section.IsIsland.Should().BeTrue();
		section.RendersAsIsland().Should().BeTrue();
		section.Parent.Should().BeSameAs(navigation);
		section.Url.Should().Be("/api/doc/demo");
		section.NavigationIndex.Should().BeGreaterThan(0, "the section takes part in the navigation order");
		section.Index.NavigationIndex.Should().Be(section.NavigationIndex);
		navigation.NavigationItems.Select(i => i.NavigationTitle).Should().ContainInOrder("Demo API", "page");
		context.Diagnostics.Should().BeEmpty();
	}

	[Test]
	public async Task FloatingSectionRef_WithoutProvider_EmitsAnErrorAndDropsTheEntry()
	{
		var (_, context, docSet) = Arrange(CreateContext);
		_ = context.Collector.StartAsync(TestContext.Current!.Execution.CancellationToken);

		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(
			docSet,
			context,
			TestDocumentationFileFactory.Instance,
			crossLinkResolver: TestCrossLinkResolver.Instance
		);
		await context.Collector.StopAsync(TestContext.Current!.Execution.CancellationToken);

		navigation.NavigationItems.OfType<DemoRoot>().Should().BeEmpty();
		context.Diagnostics.Should().ContainSingle(d => d.Message.Contains("'api: demo'"));
	}

	private sealed class DemoProvider : IFloatingSectionProvider
	{
		public string Kind => "api";

		public bool TryGetSection(string key, [NotNullWhen(true)] out IFloatingSection? section)
		{
			section = key == "demo" ? new DemoSection() : null;
			return section is not null;
		}
	}

	private sealed class DemoSection : IFloatingSection
	{
		public string Kind => "api";
		public string Key => "demo";
		public string UrlRoot => "api/doc/demo";

		public IRootNavigationItem<INavigationModel, INavigationItem> CreateRoot(
			INodeNavigationItem<INavigationModel, INavigationItem>? parent
		) => new DemoRoot(parent);
	}

	private sealed record DemoModel : INavigationModel;

	private sealed class DemoLandingLeaf(DemoRoot root) : ILeafNavigationItem<DemoModel>
	{
		public DemoModel Model { get; } = new();
		public string Url => "/api/doc/demo";
		public string NavigationTitle => "Demo API";
		public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => root;
		public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = root;
		public bool Hidden => false;
		public int NavigationIndex { get; set; }
	}

	private sealed class DemoRoot : IRootNavigationItem<INavigationModel, INavigationItem>, IAssignableIslandNavigation
	{
		public DemoRoot(INodeNavigationItem<INavigationModel, INavigationItem>? parent)
		{
			Parent = parent;
			Index = new DemoLandingLeaf(this);
		}

		public string Url => Index.Url;
		public string NavigationTitle => Index.NavigationTitle;
		public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => this;
		public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
		public bool Hidden => false;
		public int NavigationIndex { get; set; }
		public bool IsIsland { get; set; }
		public string Id => "demo";
		public ILeafNavigationItem<INavigationModel> Index { get; }
		public IReadOnlyCollection<INavigationItem> NavigationItems => [];
		public bool IsUsingNavigationDropdown => false;
		public Uri Identifier => new("api://demo");

		public void SetNavigationItems(IReadOnlyCollection<INavigationItem> navigationItems) { }
	}
}
