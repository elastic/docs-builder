// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Links;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.Floating;
using Elastic.Markdown.IO;

namespace Elastic.Markdown.Tests.DocSet;

public class FloatingSectionLinksTests
{
	[Test]
	public async Task CreateLinkReference_WithMountedFloatingSection_ListsOnlyMarkdownPages()
	{
		var provider = new DemoProvider();
		var set = CreateSet(provider);
		await set.ResolveDirectoryTree(TestContext.Current!.Execution.CancellationToken);

		var reference = set.CreateLinkReference();

		reference.Links.Keys.Should().BeEquivalentTo("index.md", "page.md");
		reference
			.Floating
			.Should()
			.ContainKey("api/demo")
			.WhoseValue
			.Versions
			.Should()
			.ContainSingle()
			.Which
			.Root
			.Should()
			.Be("api/doc/demo");
		set.NavigationIndexedByOrder.Values.Should().Contain(i => i.Url == "/api/doc/demo");
		provider.ResolveCalls.Should().Be(1);
	}

	[Test]
	public async Task CreateLinkReference_WhenTheSectionHasNoPages_OmitsTheFloatingProperty()
	{
		var set = CreateSet(new DemoProvider { LinkSection = false });
		await set.ResolveDirectoryTree(TestContext.Current!.Execution.CancellationToken);

		var reference = set.CreateLinkReference();

		reference.Floating.Should().BeNull("a docset with nothing to publish keeps the same links.json as before");
		RepositoryLinks.Serialize(reference).Should().NotContain("floating");
	}

	private static DocumentationSet CreateSet(DemoProvider provider)
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

		return new DocumentationSet(
			context,
			new TestLoggerFactory(),
			new TestCrossLinkResolver(),
			floatingSections: new FloatingSectionRegistry([provider])
		);
	}

	private sealed class DemoProvider : IFloatingSectionProvider
	{
		public int ResolveCalls { get; set; }

		public bool LinkSection { get; init; } = true;

		public string Kind => "api";

		public bool MountsImplicitly => true;

		public IReadOnlyList<IFloatingSection> GetSections() => [new DemoSection(this)];
	}

	private sealed class DemoSection(DemoProvider provider) : IFloatingSection
	{
		public string Kind => "api";
		public string Key => "demo";
		public string UrlRoot => "api/doc/demo";

		public IFloatingSectionRoot CreateRoot(INodeNavigationItem<INavigationModel, INavigationItem>? parent, bool hidden = false) =>
			new DemoRoot(parent) { Hidden = hidden };

		public Task ResolveAsync(Cancel ctx)
		{
			provider.ResolveCalls++;
			return Task.CompletedTask;
		}

		public FloatingLinkSection? ToLinkSection() =>
			provider.LinkSection
				? new FloatingLinkSection
				{
					Versions = [new FloatingLinkVersion { Name = "main", Root = UrlRoot, Current = true }],
					Pages = new Dictionary<string, ulong> { [""] = 1, ["operation/operation-search"] = 1 }
				}
				: null;
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

	private sealed class DemoRoot : IFloatingSectionRoot, IAssignableIslandNavigation
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
		public bool Hidden { get; set; }
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
