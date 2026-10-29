// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Structural;
using Elastic.ApiExplorer.Topics;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Site.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Elastic.ApiExplorer.Tests;

public class TopicPageTests
{
	private static readonly string KibanaOverview = Path.Combine(
		Paths.WorkingDirectoryRoot.FullName,
		"tests",
		"Elastic.ApiExplorer.Tests",
		"TestData",
		"kibana-api-overview.md"
	);

	[Test]
	public async Task ParseXTopics_TopicsInSpec_ReturnsTitledTopicsInSpecOrder()
	{
		var document = await Load(Spec([
			"""{ "title": "Rate limiting", "content": "Calls are limited." }""",
			"""{ "content": "no title" }""",
			"""{ "title": "Principles", "content": "REST." }"""
		]));

		var topics = OpenApiExtensionReader.ParseXTopics(document);

		topics.Should().Equal(new ApiTopicPage("Rate limiting", "Calls are limited."), new ApiTopicPage("Principles", "REST."));
	}

	[Test]
	public async Task ParseXTopics_NoExtension_ReturnsEmpty()
	{
		var document = await Load(Spec(topics: null));

		OpenApiExtensionReader.ParseXTopics(document).Should().BeEmpty();
	}

	[Test]
	[Arguments("Kibana spaces", "topic-kibana-spaces")]
	[Arguments("Rate limiting", "topic-rate-limiting")]
	[Arguments("Principles", "topic-principles")]
	public void TopicMoniker_Title_MatchesBumpShSlug(string title, string expected) =>
		ApiUrlBuilder.TopicMoniker(title).Should().Be(expected);

	[Test]
	public void ValidateSlugForCollisions_TopicSlug_Throws()
	{
		var act = () => SimpleMarkdownNavigationItem.ValidateSlugForCollisions("topic", "kibana", "/docs/topic.md");

		act.Should().Throw<InvalidOperationException>();
	}

	[Test]
	public async Task CreateNavigation_XTopics_AddsTopicsHeadingAboveEndpoints()
	{
		var navigation = await Navigation(Spec([
			"""{ "title": "Principles", "content": "REST." }""",
			"""{ "title": "Rate limiting", "content": "Calls are limited." }"""
		]));

		var items = navigation.NavigationItems.ToList();
		var topics = items[0].Should().BeOfType<TopicsNavigationItem>().Subject;
		topics.Should().BeAssignableTo<ISidebarHeadingNavigationItem>();
		topics
			.NavigationItems
			.Cast<TopicNavigationItem>()
			.Select(t => (t.NavigationTitle, t.Url))
			.Should()
			.Equal(("Rate limiting", "/api/doc/cloud/topic/topic-rate-limiting"), ("Principles", "/api/doc/cloud/topic/topic-principles"));
		items.Should().ContainSingle(item => item is SidebarSeparatorNavigationItem);
		items
			.IndexOf(items.OfType<SidebarSeparatorNavigationItem>().Single())
			.Should()
			.BeLessThan(items.FindIndex(item => item is TagNavigationItem));
	}

	[Test]
	public async Task CreateNavigation_XTopics_PinsAuthenticationAndRateLimitingThenSortsByTitle()
	{
		var navigation = await Navigation(Spec([
			"""{ "title": "Principles", "content": "REST." }""",
			"""{ "title": "Errors", "content": "Codes." }""",
			"""{ "title": "Rate limiting", "content": "Calls are limited." }""",
			"""{ "title": "authentication", "content": "Use an API key." }"""
		]));

		var titles = navigation
			.NavigationItems
			.OfType<TopicsNavigationItem>()
			.Single()
			.NavigationItems
			.Select(item => item.NavigationTitle);

		titles.Should().Equal("Authentication", "Rate limiting", "Errors", "Principles");
	}

	[Test]
	public async Task CreateNavigation_Children_KeepDeclaredOrder()
	{
		var folder = Path.Combine(Paths.WorkingDirectoryRoot.FullName, "api", "cloud");
		var navigation = await Navigation(Spec(topics: null), [Path.Combine(folder, "z-first.md"), Path.Combine(folder, "a-second.md")]);

		var topics = navigation.NavigationItems.OfType<TopicsNavigationItem>().Single();

		topics.NavigationItems.Cast<SimpleMarkdownNavigationItem>().Select(child => child.Slug).Should().Equal("z-first", "a-second");
	}

	[Test]
	public async Task CreateNavigation_ChildrenAndSchemes_ListsTheGeneratedAuthenticationPageFirst()
	{
		var navigation = await Navigation(Spec(topics: null, withSchemes: true), [KibanaOverview]);

		var topics = navigation.NavigationItems.OfType<TopicsNavigationItem>().Single();

		topics
			.NavigationItems
			.Select(item => item.GetType().Name)
			.Should()
			.Equal(nameof(StructuralNavigationItem), nameof(SimpleMarkdownNavigationItem));
	}

	[Test]
	public async Task CreateNavigation_SchemesWithoutTopics_ListsTheGeneratedAuthenticationPageUnderTopics()
	{
		var navigation = await Navigation(Spec(topics: null, withSchemes: true));

		var topics = navigation.NavigationItems.OfType<TopicsNavigationItem>().Single();
		var authentication = topics.NavigationItems.Should().ContainSingle().Which.Should().BeOfType<StructuralNavigationItem>().Subject;
		authentication.Hidden.Should().BeFalse();
		navigation.AuthenticationUrl.Should().Be("/api/doc/cloud/authentication");
	}

	[Test]
	public async Task CreateNavigation_ChildTitledAuthentication_ShowsTheTabsAndTakesTheBadgeLink()
	{
		var navigation = await NavigationWithChild(
			Spec(topics: null, withSchemes: true),
			"auth-intro.md",
			"# Authentication\n\nUse an API key.\n"
		);

		var all = ApiExplorerFixture.Walk(navigation).ToList();
		var child = all.OfType<SimpleMarkdownNavigationItem>().Single();
		child.ShowsAuthentication.Should().BeTrue();
		navigation.AuthenticationUrl.Should().Be(child.Url);
		all.OfType<StructuralNavigationItem>().Single().Hidden.Should().BeTrue("the generated page stays only for old links");
	}

	[Test]
	public async Task CreateNavigation_ChildrenAndAuthenticationTopic_ChildReplacesTopic()
	{
		var navigation = await NavigationWithChild(
			Spec(["""{ "title": "Authentication", "content": "Use an API key." }"""]),
			"auth-intro.md",
			"# Authentication\n\nUse an API key.\n"
		);

		var all = ApiExplorerFixture.Walk(navigation).ToList();
		all.OfType<StructuralNavigationItem>().Should().BeEmpty("the child page replaces the Authentication topic");
		all.OfType<SimpleMarkdownNavigationItem>().Single().Url.Should().Be("/api/doc/cloud/auth-intro/");
	}

	[Test]
	public async Task Generate_XTopics_WritesTopicPagesWithoutRedirects()
	{
		var ct = TestContext.Current!.Execution.CancellationToken;
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-topics-{Guid.NewGuid():N}");
		var context = OpenApiGeneratorSpecDownloadTests.CreateGenerateContext(outputRoot);
		var document = await Load(
			Spec(
				[
					"""{ "title": "Rate limiting", "content": "Calls are limited." }""",
					"""{ "title": "Authentication", "content": "Use an API key." }"""
				],
				withSchemes: true
			)
		);
		using var versionIndexClient = new VersionIndexClient(
			OpenApiGeneratorSpecDownloadTests.BaseUri,
			OpenApiGeneratorSpecDownloadTests.MainOnlyHandler(),
			sleep: (_, _) => Task.CompletedTask
		);
		var generator = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			context,
			PassthroughMarkdownRenderer.Instance,
			versionIndexClient,
			OpenApiGeneratorSpecDownloadTests.CreateSequentialReader(document)
		);

		await generator.Generate(ct);

		var files = context.WriteFileSystem.File;
		var root = Path.Join(outputRoot, "api", "doc", "elasticsearch");
		var topic = await files.ReadAllTextAsync(Path.Join(root, "topic", "topic-rate-limiting", "index.html"), ct);
		topic.Should().Contain("<h1>Rate limiting</h1>").And.Contain("Calls are limited.");
		// The title sits in the same header as on an operation page, which sets its size and the space above it.
		topic.Should().Contain("<header class=\"api-page-intro\">");
		var authentication = await files.ReadAllTextAsync(Path.Join(root, "authentication", "index.html"), ct);
		authentication
			.Should()
			.Contain("Use an API key.")
			.And
			.Contain("api-authorization-panel")
			.And
			.Contain("<label class=\"tabs-label\"");
		// The page shows the credentials open, under its own title, with no collapsible Authentication heading.
		authentication.Should().NotContain("data-section=\"authentication\"").And.NotContain("api-param-section-toggle");
		// The Topics heading is expanded on pages outside it too, such as the product landing page.
		var landing = await files.ReadAllTextAsync(Path.Join(root, "index.html"), ct);
		// Only the Topics toggle is checked there: every other group stays collapsed.
		System
			.Text
			.RegularExpressions
			.Regex
			.Matches(landing, "<input[^>]*\\bchecked\\b[^>]*>")
			.Select(m => m.Value)
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Contain("checked data-nav-default-open");
		// elastic/docs-infra redirects the bump.sh topic URL; the build writes no page there.
		files.Exists(Path.Join(root, "topic", "topic-authentication", "index.html")).Should().BeFalse();
	}

	[Test]
	public async Task CreateNavigation_TopicsWithTheSameUrl_KeepsTheFirst()
	{
		var navigation = await Navigation(Spec([
			"""{ "title": "Rate limiting", "content": "First." }""",
			"""{ "title": "Rate Limiting", "content": "Second." }""",
			"""{ "title": "Authentication", "content": "Use an API key." }""",
			"""{ "title": "authentication", "content": "Duplicate." }"""
		]));

		var all = ApiExplorerFixture.Walk(navigation).ToList();
		all.OfType<TopicNavigationItem>().Should().ContainSingle().Which.Model.Content.Should().Be("First.");
		all.OfType<StructuralNavigationItem>().Should().ContainSingle().Which.Model.Intro.Should().Be("Use an API key.");
	}

	[Test]
	public async Task CreateNavigation_NoTopicsOrChildren_OmitsTopicsHeading()
	{
		var navigation = await Navigation(Spec(topics: null));

		navigation.NavigationItems.OfType<TopicsNavigationItem>().Should().BeEmpty();
	}

	[Test]
	public async Task CreateNavigation_ChildrenAndXTopics_ChildWithSameTitleReplacesTopic()
	{
		var navigation = await Navigation(Spec(["""{ "title": "Kibana spaces", "content": "Spaces." }"""]), [KibanaOverview]);

		var topics = navigation.NavigationItems.OfType<TopicsNavigationItem>().Single();
		var child = topics.NavigationItems.Should().ContainSingle().Which.Should().BeOfType<SimpleMarkdownNavigationItem>().Subject;
		child.Url.Should().Be("/api/doc/cloud/kibana-api-overview/");
		child.ReplacedTopicUrls.Should().Equal("/api/doc/cloud/topic/topic-kibana-spaces");
		ApiExplorerFixture.Walk(navigation).OfType<TopicNavigationItem>().Should().BeEmpty();
	}

	[Test]
	public async Task CreateNavigation_ChildrenAndUnmatchedTopics_ListsAuthenticationThenChildrenThenTopics()
	{
		var navigation = await Navigation(
			Spec(["""{ "title": "Principles", "content": "REST." }""", """{ "title": "Authentication", "content": "Use an API key." }"""]),
			[KibanaOverview]
		);

		var topics = navigation.NavigationItems.OfType<TopicsNavigationItem>().Single();

		topics
			.NavigationItems
			.Select(item => (item.GetType().Name, item.Url))
			.Should()
			.Equal(
				(nameof(StructuralNavigationItem), "/api/doc/cloud/authentication"),
				(nameof(SimpleMarkdownNavigationItem), "/api/doc/cloud/kibana-api-overview/"),
				(nameof(TopicNavigationItem), "/api/doc/cloud/topic/topic-principles")
			);
	}

	[Test]
	public async Task CreateNavigation_ChildWithSameContentAsTopic_ReplacesTopic()
	{
		var navigation = await NavigationWithChild(
			Spec(["""{ "title": "Kibana spaces", "content": "Spaces organize your saved objects." }"""]),
			"spaces.md",
			"# Spaces\n\nSpaces organize\nyour saved objects.\n"
		);

		var all = ApiExplorerFixture.Walk(navigation).ToList();
		all.OfType<TopicNavigationItem>().Should().BeEmpty();
		all.OfType<SimpleMarkdownNavigationItem>().Should().ContainSingle();
	}

	[Test]
	public async Task CreateNavigation_AuthenticationTopicWithoutSchemes_ListsAuthenticationPage()
	{
		var navigation = await Navigation(Spec(["""{ "title": "Authentication", "content": "Use an API key." }"""]));

		var all = ApiExplorerFixture.Walk(navigation).ToList();
		all.OfType<TopicNavigationItem>().Should().BeEmpty();
		var authentication = all.OfType<StructuralNavigationItem>().Should().ContainSingle().Subject;
		authentication.Url.Should().Be("/api/doc/cloud/authentication");
		authentication.Hidden.Should().BeFalse();
		authentication.Parent.Should().BeOfType<TopicsNavigationItem>();
		authentication.Model.Intro.Should().Be("Use an API key.");
	}

	[Test]
	public async Task CreateNavigation_AuthenticationTopicWithSchemes_ReplacesGeneratedAuthenticationPage()
	{
		var navigation = await Navigation(Spec(["""{ "title": "Authentication", "content": "Use an API key." }"""], withSchemes: true));

		var pages = ApiExplorerFixture
			.Walk(navigation)
			.OfType<StructuralNavigationItem>()
			.Where(item => item.Model.Kind == ApiStructuralKind.Authentication)
			.ToList();
		pages.Should().ContainSingle().Which.Hidden.Should().BeFalse();
	}

	[Test]
	public async Task Overview_XTopics_ListsTopicsOutsideEndpointGroups()
	{
		var context = CreateContext();
		var navigation = await Navigation(
			Spec(["""{ "title": "Principles", "content": "REST." }""", """{ "title": "Authentication", "content": "Use an API key." }"""]),
			context: context
		);

		var index = ApiOverviewBuilder.SplitTopics(
			ApiOverviewBuilder.Sections(ApiOverviewBuilder.Build(navigation, context.VersionsConfiguration))
		);

		index.Topics.Select(row => row.Url).Should().Equal("/api/doc/cloud/authentication", "/api/doc/cloud/topic/topic-principles");
		index.Groups.SelectMany(group => group.Items).Select(row => row.Url).Should().NotContain(url => url!.Contains("/topic/"));
	}

	[Test]
	public async Task TopicPage_WritesCommonMarkWithTitleAndContent()
	{
		var context = CreateContext();
		var document = await Load(Spec(["""{ "title": "Rate limiting", "content": "Calls are limited." }"""]));
		var navigation = CreateGenerator(context).CreateNavigation("cloud", document);
		var topic = ApiExplorerFixture.Walk(navigation).OfType<TopicNavigationItem>().Single();

		var markdown = await topic.Model.RenderCommonMarkAsync(
			RenderContext(context, document, topic),
			null,
			TestContext.Current!.Execution.CancellationToken
		);

		markdown.Should().Be("# Rate limiting\n\nCalls are limited.\n\n".ReplaceLineEndings());
	}

	[Test]
	public async Task AuthenticationTopicPage_WritesIntroAboveSchemes()
	{
		var context = CreateContext();
		var document = await Load(Spec(["""{ "title": "Authentication", "content": "Use an API key." }"""], withSchemes: true));
		var navigation = CreateGenerator(context).CreateNavigation("cloud", document);
		var authentication = ApiExplorerFixture.Walk(navigation).OfType<StructuralNavigationItem>().Single();

		var markdown = await authentication.Model.RenderCommonMarkAsync(
			RenderContext(context, document, authentication),
			null,
			TestContext.Current!.Execution.CancellationToken
		);

		markdown.Should().Contain("Use an API key.");
		markdown.Should().Contain("- Api key: `Authorization: <value>`");
		markdown!
			.IndexOf("Use an API key.", StringComparison.Ordinal)
			.Should()
			.BeLessThan(markdown.IndexOf("- Api key", StringComparison.Ordinal));
		markdown.Should().NotContain("does not declare");
	}

	[Test]
	public async Task AuthenticationTopicPage_NoSchemes_OmitsEmptyMessage()
	{
		var context = CreateContext();
		var document = await Load(Spec(["""{ "title": "Authentication", "content": "Use an API key." }"""]));
		var navigation = CreateGenerator(context).CreateNavigation("cloud", document);
		var authentication = ApiExplorerFixture.Walk(navigation).OfType<StructuralNavigationItem>().Single();

		var markdown = await authentication.Model.RenderCommonMarkAsync(
			RenderContext(context, document, authentication),
			null,
			TestContext.Current!.Execution.CancellationToken
		);

		markdown.Should().Be("# Authentication\n\nUse an API key.\n\n".ReplaceLineEndings());
	}

	private static string Spec(string[]? topics, bool withSchemes = false)
	{
		var xTopics = topics is null ? "" : $"""  "x-topics": [{string.Join(",", topics)}],""";
		var components = withSchemes
			? """  "components": { "securitySchemes": { "apiKey": { "type": "apiKey", "name": "Authorization", "in": "header" } } },"""
			: "";
		return $$"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "Cloud", "version": "1.0" },
			{{xTopics}}
			{{components}}
			  "paths": {
			    "/a": { "get": { "operationId": "a1", "tags": ["deployments"], "responses": { "200": { "description": "ok" } } } }
			  },
			  "tags": [ { "name": "deployments" } ]
			}
			""";
	}

	private static async Task<LandingNavigationItem> Navigation(string spec, string[]? childFiles = null, BuildContext? context = null)
	{
		context ??= CreateContext();
		var document = await Load(spec);
		var apiConfig = childFiles is null
			? null
			: new ResolvedApiConfiguration
			{
				ProductKey = "cloud",
				Product = new Product { Id = "cloud", DisplayName = "Cloud" },
				SpecFileName = "cloud.json",
				Children = [.. childFiles.Select(file => new FileSystem().FileInfo.New(file))]
			};
		return CreateGenerator(context).CreateNavigation("cloud", document, apiConfig);
	}

	/// <summary>Builds the navigation with one child page written to disk; the child is only read while the navigation is built.</summary>
	private static async Task<LandingNavigationItem> NavigationWithChild(string spec, string fileName, string markdown)
	{
		// The build only reads files under the working directory root, so the child lives in the ignored .artifacts folder.
		var folder = Directory.CreateDirectory(
			Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", $"api-topics-{Guid.NewGuid():N}")
		);
		try
		{
			var child = Path.Combine(folder.FullName, fileName);
			await File.WriteAllTextAsync(child, markdown, TestContext.Current!.Execution.CancellationToken);
			return await Navigation(spec, [child]);
		}
		finally
		{
			folder.Delete(recursive: true);
		}
	}

	private static OpenApiGenerator CreateGenerator(BuildContext context) =>
		new(NullLoggerFactory.Instance, context, PassthroughMarkdownRenderer.Instance);

	private static BuildContext CreateContext() =>
		new(
			new DiagnosticsCollector([]),
			DocumentationFileSystem.Resolve(Paths.WorkingDirectoryRoot.FullName),
			TestHelpers.CreateConfigurationContext(new FileSystem())
		);

	private static ApiRenderContext RenderContext(BuildContext context, OpenApiDocument document, INavigationItem current) =>
		new(context, document, new StaticFileContentHashProvider(new EmbeddedOrPhysicalFileProvider(context)))
		{
			NavigationHtml = string.Empty,
			CurrentNavigation = current,
			MarkdownRenderer = PassthroughMarkdownRenderer.Instance
		};

	private static async Task<OpenApiDocument> Load(string json)
	{
		using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
		var result = await OpenApiDocument.LoadAsync(stream, settings: new OpenApiReaderSettings { LeaveStreamOpen = false });
		if (result.Diagnostic?.Errors is { Count: > 0 } errors)
			throw new InvalidOperationException(string.Join(", ", errors.Select(e => e.Message)));
		return result.Document!;
	}
}
