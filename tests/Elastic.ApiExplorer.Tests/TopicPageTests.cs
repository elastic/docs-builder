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

		topics.Should().Equal(new OpenApiTopic("Rate limiting", "Calls are limited."), new OpenApiTopic("Principles", "REST."));
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
			.Equal(("Principles", "/api/doc/cloud/topic/topic-principles"), ("Rate limiting", "/api/doc/cloud/topic/topic-rate-limiting"));
		items.Should().ContainSingle(item => item is SidebarSeparatorNavigationItem);
		items
			.IndexOf(items.OfType<SidebarSeparatorNavigationItem>().Single())
			.Should()
			.BeLessThan(items.FindIndex(item => item is TagNavigationItem));
	}

	[Test]
	public async Task CreateNavigation_NoTopicsOrChildren_OmitsTopicsHeading()
	{
		var navigation = await Navigation(Spec(topics: null));

		navigation.NavigationItems.OfType<TopicsNavigationItem>().Should().BeEmpty();
	}

	[Test]
	public async Task CreateNavigation_XTopicsAndChildren_ForwardsChildToTopicWithSameTitle()
	{
		var navigation = await Navigation(Spec(["""{ "title": "Kibana spaces", "content": "Spaces." }"""]), KibanaOverview);

		var topics = navigation.NavigationItems.OfType<TopicsNavigationItem>().Single();
		var topic = topics.NavigationItems.Should().ContainSingle().Which.Should().BeOfType<TopicNavigationItem>().Subject;
		topic.Url.Should().Be("/api/doc/cloud/topic/topic-kibana-spaces");
		topic.AliasUrls.Should().Equal("/api/doc/cloud/kibana-api-overview/");
	}

	[Test]
	public async Task CreateNavigation_XTopicsAndUnmatchedChild_IgnoresChild()
	{
		var navigation = await Navigation(Spec(["""{ "title": "Principles", "content": "REST." }"""]), KibanaOverview);

		var all = ApiExplorerFixture.Walk(navigation).ToList();
		all.OfType<SimpleMarkdownNavigationItem>().Should().BeEmpty();
		all.OfType<TopicNavigationItem>().Single().AliasUrls.Should().BeEmpty();
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
		authentication.AliasUrls.Should().Equal("/api/doc/cloud/topic/topic-authentication");
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

		index.Topics.Select(row => row.Url).Should().Equal("/api/doc/cloud/topic/topic-principles", "/api/doc/cloud/authentication");
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
		markdown.Should().Contain("## Api key (apiKey)");
		markdown!
			.IndexOf("Use an API key.", StringComparison.Ordinal)
			.Should()
			.BeLessThan(markdown.IndexOf("## Api key", StringComparison.Ordinal));
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

	private static async Task<LandingNavigationItem> Navigation(string spec, string? childFile = null, BuildContext? context = null)
	{
		context ??= CreateContext();
		var document = await Load(spec);
		var apiConfig = childFile is null
			? null
			: new ResolvedApiConfiguration
			{
				ProductKey = "cloud",
				Product = new Product { Id = "cloud", DisplayName = "Cloud" },
				SpecFileName = "cloud.json",
				Children = [new FileSystem().FileInfo.New(childFile)]
			};
		return CreateGenerator(context).CreateNavigation("cloud", document, apiConfig);
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
