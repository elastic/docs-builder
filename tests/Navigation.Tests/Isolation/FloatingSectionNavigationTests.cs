// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
		  - file: page.md
		""";

	private (TestDocumentationSetContext Context, DocumentationSetFile DocSet) Arrange()
	{
		var fileSystem = new MockFileSystem();
		fileSystem.AddFile("/docs/index.md", new MockFileData("# Root"));
		fileSystem.AddFile("/docs/page.md", new MockFileData("# Page"));
		var context = CreateContext(fileSystem);
		var docSet = DocumentationSetFile.LoadAndResolve(context.Collector, Yaml, fileSystem.NewDirInfo("docs"));
		return (context, docSet);
	}

	private static DocumentationSetNavigation<TestDocumentationFile> CreateNavigation(
		TestDocumentationSetContext context,
		DocumentationSetFile docSet,
		FloatingSectionRegistry? registry = null
	) =>
		new(
			docSet,
			context,
			TestDocumentationFileFactory.Instance,
			crossLinkResolver: TestCrossLinkResolver.Instance,
			floatingSections: registry
		);

	[Test]
	public void FloatingSection_FromAProviderThatMountsImplicitly_IsInTheTreeWithoutATocEntry()
	{
		var (context, docSet) = Arrange();

		var navigation = CreateNavigation(context, docSet, new FloatingSectionRegistry([new DemoProvider()]));

		var section = navigation.NavigationItems.OfType<DemoRoot>().Should().ContainSingle().Subject;
		section.IsIsland.Should().BeTrue();
		section.RendersAsIsland().Should().BeTrue();
		section.Parent.Should().BeSameAs(navigation);
		section.Url.Should().Be("/api/doc/demo");
		section.NavigationIndex.Should().BeGreaterThan(0, "the section takes part in the navigation order");
		section.Index.NavigationIndex.Should().Be(section.NavigationIndex);
		navigation.NavigationItems.Last().Should().BeSameAs(section, "implicit sections follow the toc entries");
		navigation.FloatingSections.Should().ContainSingle();
		context.Diagnostics.Should().BeEmpty();
	}

	[Test]
	public void FloatingSection_MountedImplicitly_IsHiddenInTheSidebar()
	{
		var (context, docSet) = Arrange();

		var navigation = CreateNavigation(context, docSet, new FloatingSectionRegistry([new DemoProvider()]));

		navigation
			.NavigationItems
			.OfType<DemoRoot>()
			.Single()
			.Hidden
			.Should()
			.BeTrue("a docset's sidebar does not change because it declares an API");
		navigation.NavigationItems.Where(i => !i.Hidden).Select(i => i.NavigationTitle).Should().Equal("page");
	}

	[Test]
	public void FloatingSection_FromAProviderThatDoesNotMountImplicitly_IsNotInTheTree()
	{
		var (context, docSet) = Arrange();

		var navigation = CreateNavigation(context, docSet, new FloatingSectionRegistry([new DemoProvider { MountsImplicitly = false }]));

		navigation.NavigationItems.OfType<DemoRoot>().Should().BeEmpty();
		navigation.FloatingSections.Should().BeEmpty();
	}

	[Test]
	public void FloatingSection_WithoutAProvider_LeavesTheNavigationUnchanged()
	{
		var (context, docSet) = Arrange();

		var navigation = CreateNavigation(context, docSet);

		navigation.NavigationItems.OfType<DemoRoot>().Should().BeEmpty();
		navigation.NavigationItems.Select(i => i.NavigationTitle).Should().Equal("page");
		navigation.FloatingSections.Should().BeEmpty();
	}

	[Test]
	public async Task FloatingSection_FilledAfterConstruction_StaysOutOfTheGlobalNavigationOrder()
	{
		var (context, docSet) = Arrange();
		var provider = new DemoProvider();
		var navigation = CreateNavigation(context, docSet, new FloatingSectionRegistry([provider]));
		var root = navigation.NavigationItems.OfType<DemoRoot>().Should().ContainSingle().Subject;
		root.NavigationItems.Should().BeEmpty("the root exists before the section is resolved");

		await navigation.FloatingSections.Should().ContainSingle().Subject.ResolveAsync(TestContext.Current!.Execution.CancellationToken);
		var lookup = navigation.BuildNavigationLookups([]);

		root.NavigationItems.Should().ContainSingle("the section filled its children");
		lookup.Values.OfType<DemoRoot>().Should().ContainSingle();
		lookup.Values.OfType<DemoPage>().Should().BeEmpty("the children of a floating section keep their own numbering");
		provider.Section!.ResolveCount.Should().Be(1);
	}

	[Test]
	public async Task TocEntry_ForAnApi_IsNoLongerAKnownKey()
	{
		var fileSystem = new MockFileSystem();
		fileSystem.AddFile("/docs/index.md", new MockFileData("# Root"));
		var context = CreateContext(fileSystem);
		_ = context.Collector.StartAsync(TestContext.Current!.Execution.CancellationToken);
		// language=yaml
		const string yaml =
			"""
			project: 'docs-builder'
			toc:
			  - file: index.md
			  - api: demo
			""";

		_ = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		await context.Collector.StopAsync(TestContext.Current!.Execution.CancellationToken);

		context
			.Diagnostics
			.Should()
			.Contain(
				d => d.Message.StartsWith("Unknown key 'api'", StringComparison.Ordinal),
				"a docset declares its APIs in the api: block, and the toc takes no entry for them"
			);
	}
}
