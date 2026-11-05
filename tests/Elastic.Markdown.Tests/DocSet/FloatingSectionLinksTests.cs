// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.Floating;
using Elastic.Markdown.IO;

namespace Elastic.Markdown.Tests.DocSet;

public class FloatingSectionLinksTests
{
	[Test]
	public void CreateLinkReference_WithMountedFloatingSection_ListsOnlyMarkdownPages()
	{
		var fileSystem = new MockFileSystem(
			new Dictionary<string, MockFileData>
			{
				{
					"docs/docset.yml",
					new MockFileData(
						"""
						project: test
						toc:
						- file: index.md
						- api: demo
						- file: page.md
						"""
					)
				},
				{ "docs/index.md", new MockFileData("# Home") },
				{ "docs/page.md", new MockFileData("# Page") }
			},
			new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName }
		);
		var collector = new TestDiagnosticsCollector();
		_ = collector.StartAsync(TestContext.Current!.Execution.CancellationToken);
		var configurationContext = TestHelpers.CreateConfigurationContext(fileSystem);
		var context = new BuildContext(collector, TestHelpers.CreateDocumentationFileSystem(fileSystem), configurationContext);

		var set = new DocumentationSet(
			context,
			new TestLoggerFactory(),
			new TestCrossLinkResolver(),
			floatingSections: new FloatingSectionRegistry([new DemoProvider()])
		);
		var reference = set.CreateLinkReference();

		reference.Links.Keys.Should().BeEquivalentTo("index.md", "page.md");
		set.NavigationIndexedByOrder.Values.Should().Contain(i => i.Url == "/api/doc/demo");
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
