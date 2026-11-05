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
	public async Task FloatingSection_FilledAfterConstruction_StaysOutOfTheGlobalNavigationOrder()
	{
		var (_, context, docSet) = Arrange(CreateContext);
		var provider = new DemoProvider();
		var navigation = new DocumentationSetNavigation<TestDocumentationFile>(
			docSet,
			context,
			TestDocumentationFileFactory.Instance,
			crossLinkResolver: TestCrossLinkResolver.Instance,
			floatingSections: new FloatingSectionRegistry([provider])
		);
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

	[Test]
	public async Task FloatingSectionRef_WithOtherKeys_EmitsAnError()
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
			    children:
			      - file: index.md
			""";

		_ = DocumentationSetFile.LoadAndResolve(context.Collector, yaml, fileSystem.NewDirInfo("docs"));
		await context.Collector.StopAsync(TestContext.Current!.Execution.CancellationToken);

		context.Diagnostics.Should().Contain(d => d.Message.Contains("'api: demo'") && d.Message.Contains("'children'"));
	}
}
