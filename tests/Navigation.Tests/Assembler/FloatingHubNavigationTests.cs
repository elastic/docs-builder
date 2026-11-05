// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Assembler.Building;
using Elastic.Documentation.Assembler.Navigation;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Documentation.Navigation.Floating;
using Elastic.Documentation.Navigation.Isolated.Node;

namespace Elastic.Documentation.Navigation.Tests.Assembler;

public class FloatingHubNavigationTests
{
	private const string SiteNavYaml =
		// language=yaml
		"""
		toc:
		  - toc: observability://
		    path_prefix: observability
		  - section: APIs
		    external: /api/
		    floating: api
		""";

	private const string SiteNavYamlWithoutExternal =
		// language=yaml
		"""
		toc:
		  - toc: observability://
		    path_prefix: observability
		  - section: APIs
		    floating: api
		""";

	private static (SiteNavigation Navigation, TestDocumentationSetContext Context) CreateNavigation(
		FloatingSectionRegistry registry,
		bool navigationPreview = false,
		string navYaml = SiteNavYaml
	)
	{
		var siteNavFile = SiteNavigationFile.Deserialize(navYaml);
		var fileSystem = SiteNavigationTestFixture.CreateMultiRepositoryFileSystem();
		const string docsetPath = "/checkouts/current/observability/docs/docset.yml";
		var obsContext = SiteNavigationTestFixture.CreateAssemblerContext(fileSystem, "/checkouts/current/observability");
		var obsDocset = DocumentationSetFile.LoadAndResolve(
			obsContext.Collector,
			fileSystem.FileInfo.New(docsetPath),
			new CheckoutsFileSystem(fileSystem.DirectoryInfo.New("/checkouts"), inner: fileSystem)
		);
		var obsNav = new DocumentationSetNavigation<IDocumentationFile>(obsDocset, obsContext, GenericDocumentationFileFactory.Instance);
		var siteContext = SiteNavigationTestFixture.CreateContext(fileSystem, "/checkouts/current/observability");

		var navigation = new SiteNavigation(siteNavFile, siteContext, [obsNav], sitePrefix: "/docs", navigationPreview, registry);
		return (navigation, siteContext);
	}

	[Test]
	public async Task FloatingSection_WithSections_MountsTheirRootsUnderAHub()
	{
		var provider = new DemoProvider();
		var (navigation, context) = CreateNavigation(new FloatingSectionRegistry([provider]));

		var hub = navigation.TopLevelItems.OfType<SectionNavigation>().Should().ContainSingle(s => s.Title == "APIs").Subject;
		hub.Url.Should().Be("/docs/api");
		var root = hub.NavigationItems.Should().ContainSingle().Subject.Should().BeOfType<DemoRoot>().Subject;
		root.IsIsland.Should().BeTrue();
		root.Parent.Should().BeSameAs(hub);
		root.NavigationItems.Should().BeEmpty("the root exists before the section is filled");
		navigation.FloatingSections.Should().ContainSingle();

		await navigation.ResolveFloatingSectionsAsync(TestContext.Current!.Execution.CancellationToken);

		root.NavigationItems.Should().ContainSingle();
		provider.Section!.ResolveCount.Should().Be(1);
		navigation
			.NavigationIndexedByOrder
			.Values
			.OfType<DemoPage>()
			.Should()
			.BeEmpty("the children of a floating section keep their own numbering");
		context.Diagnostics.Should().BeEmpty();
	}

	[Test]
	public void FloatingSection_WithoutSections_KeepsTheExternalLink()
	{
		var (navigation, _) = CreateNavigation(FloatingSectionRegistry.Empty, navigationPreview: true);

		navigation.TopLevelItems.OfType<SectionNavigation>().Should().NotContain(s => s.Title == "APIs");
		navigation.FloatingSections.Should().BeEmpty();
		var link = navigation.TopNav!.Items.OfType<TopNavLinkItem>().Should().ContainSingle(i => i.Title == "APIs").Subject;
		link.Url.Should().Be("/docs/api/");
	}

	[Test]
	public void FloatingSection_WithSections_StillLinksTheApisTabToTheHub()
	{
		var (navigation, _) = CreateNavigation(new FloatingSectionRegistry([new DemoProvider()]), navigationPreview: true);

		var link = navigation.TopNav!.Items.OfType<TopNavLinkItem>().Should().ContainSingle(i => i.Title == "APIs").Subject;
		link.Url.Should().Be("/docs/api/");
	}

	[Test]
	public void FloatingSection_WithoutExternal_LinksTheApisTabToTheHub()
	{
		var (navigation, _) = CreateNavigation(
			new FloatingSectionRegistry([new DemoProvider()]),
			navigationPreview: true,
			navYaml: SiteNavYamlWithoutExternal
		);

		var link = navigation.TopNav!.Items.OfType<TopNavLinkItem>().Should().ContainSingle(i => i.Title == "APIs").Subject;
		link.Url.Should().Be("/docs/api");
	}

	[Test]
	public async Task Sitemap_WalksTheFilledSectionAndSkipsHiddenOnesAndAnchors()
	{
		var provider = new DemoProvider();
		var (navigation, _) = CreateNavigation(new FloatingSectionRegistry([provider]));
		await navigation.ResolveFloatingSectionsAsync(TestContext.Current!.Execution.CancellationToken);

		var urls = navigation.NavigationItems.SelectMany(SitemapNavigationHelper.Flatten).Select(i => i.Url).ToArray();

		urls.Should().Contain("/api/doc/demo/operation/operation-search").And.Contain("/docs/api");

		provider.Section!.Root!.Hidden = true;
		navigation
			.NavigationItems
			.SelectMany(SitemapNavigationHelper.Flatten)
			.Select(i => i.Url)
			.Should()
			.NotContain("/api/doc/demo/operation/operation-search", "a hidden root takes its pages out of the sitemap");
	}

	[Test]
	public void Sitemap_SkipsALinkToAHeadingInsideAPage() => SitemapNavigationHelper.Flatten(new AnchorLeaf()).Should().BeEmpty();

	[Test]
	public async Task LlmsWalk_ListsEachApiByItsMarkdownUrlAndSkipsAHiddenOne()
	{
		var provider = new DemoProvider();
		var (navigation, _) = CreateNavigation(new FloatingSectionRegistry([provider]));
		await navigation.ResolveFloatingSectionsAsync(TestContext.Current!.Execution.CancellationToken);
		var enhancer = new LlmsNavigationEnhancer();
		var baseUrl = new Uri("https://www.elastic.co");

		var listed = enhancer.GenerateNavigationSections(navigation, baseUrl);

		listed.Should().Contain("## APIs").And.Contain("* [Demo API](https://www.elastic.co/api/doc/demo.md)");

		provider.Section!.Root!.Hidden = true;
		enhancer.GenerateNavigationSections(navigation, baseUrl).Should().NotContain("Demo API");
	}

	private sealed class AnchorLeaf : ILeafNavigationItem<DemoModel>
	{
		public DemoModel Model { get; } = new();
		public string Url => "/api/doc/demo/group/endpoint-demo#intro";
		public string NavigationTitle => "Intro";
		public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => throw new NotSupportedException();
		public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }
		public bool Hidden => false;
		public int NavigationIndex { get; set; }
	}
}
