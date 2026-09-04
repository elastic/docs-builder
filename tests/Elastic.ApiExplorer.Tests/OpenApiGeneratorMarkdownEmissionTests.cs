// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

public class OpenApiGeneratorMarkdownEmissionTests(ApiExplorerFixture fixture) : IClassFixture<ApiExplorerFixture>
{
	private static readonly Uri BaseUri = new("https://cdn.example/");

	[Fact]
	public async Task Generate_WritesSiblingMarkdownForEveryRenderedPage()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-md-emit-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(outputRoot);
		using var versionIndexClient = new VersionIndexClient(BaseUri, MainOnlyHandler(), sleep: (_, _) => Task.CompletedTask);
		var reader = CreateSequentialReader(fixture.Document);
		var generator = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			context,
			PassthroughMarkdownRenderer.Instance,
			versionIndexClient,
			reader
		);

		await generator.Generate(TestContext.Current.CancellationToken);

		var write = context.WriteFileSystem.File;
		write.Exists(Path.Join(outputRoot, "api.md")).Should().BeTrue();
		write.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch.md")).Should().BeTrue();
		write.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch", "group", "endpoint-search.md")).Should().BeTrue();
		write.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch", "operation", "operation-search.md")).Should().BeTrue();
		write.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch", "types", "_types-query_dsl-querycontainer.md")).Should().BeTrue();
	}

	[Fact]
	public async Task Generate_WritesReadableCommonMarkNotHtmlDocument()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-md-content-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(outputRoot);
		using var versionIndexClient = new VersionIndexClient(BaseUri, MainOnlyHandler(), sleep: (_, _) => Task.CompletedTask);
		var reader = CreateSequentialReader(fixture.Document);
		var generator = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			context,
			PassthroughMarkdownRenderer.Instance,
			versionIndexClient,
			reader
		);

		await generator.Generate(TestContext.Current.CancellationToken);

		var landing = await context
			.WriteFileSystem
			.File
			.ReadAllTextAsync(Path.Join(outputRoot, "api", "doc", "elasticsearch.md"), TestContext.Current.CancellationToken);
		var operation = await context
			.WriteFileSystem
			.File
			.ReadAllTextAsync(
				Path.Join(outputRoot, "api", "doc", "elasticsearch", "operation", "operation-search.md"),
				TestContext.Current.CancellationToken
			);
		var catalog = await context
			.WriteFileSystem
			.File
			.ReadAllTextAsync(Path.Join(outputRoot, "api.md"), TestContext.Current.CancellationToken);

		landing.Should().StartWith("---");
		landing.Should().Contain("type: api");
		landing.Should().Contain("title: Fixture API");
		landing.Should().Contain("url: /api/doc/elasticsearch");
		landing.Should().Contain("resource: /api/doc/elasticsearch");
		landing.Should().Contain("  - elasticsearch");
		landing.Should().NotContain("applies_to:");
		landing.Should().Contain("# Fixture API");
		landing.Should().Contain("Search APIs");
		landing.Should().NotContain("<!DOCTYPE");
		landing.Should().NotContain("<html");

		operation.Should().Contain("type: api");
		operation.Should().Contain("title: Run a search");
		operation.Should().Contain("# Run a search");
		operation.Should().Contain("`POST`");
		operation.Should().Contain("`/{index}/_search`");
		operation.Should().Contain("## Description");
		operation.Should().Contain("Returns hits that match the query");
		operation.Should().Contain(":::{important}");
		operation.Should().NotContain("operation-verb");
		operation.Should().NotContain("All methods and paths");
		operation.Should().NotContain("<!DOCTYPE");
		operation.Should().NotContain("<html");

		catalog.Should().Contain("type: api");
		catalog.Should().Contain("title: API catalog");
		catalog.Should().Contain("description: API products in this documentation set.");
		catalog.Should().Contain("# API catalog");
		catalog.Should().Contain("Fixture API");
		catalog.Should().Contain("`elasticsearch`");
	}

	[Fact]
	public async Task Generate_WritesAlternateLinkOnApiHtml()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-md-alt-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(outputRoot);
		using var versionIndexClient = new VersionIndexClient(BaseUri, MainOnlyHandler(), sleep: (_, _) => Task.CompletedTask);
		var reader = CreateSequentialReader(fixture.Document);
		var generator = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			context,
			PassthroughMarkdownRenderer.Instance,
			versionIndexClient,
			reader
		);

		await generator.Generate(TestContext.Current.CancellationToken);

		var html = await context
			.WriteFileSystem
			.File
			.ReadAllTextAsync(Path.Join(outputRoot, "api", "doc", "elasticsearch", "index.html"), TestContext.Current.CancellationToken);
		var operationHtml = await context
			.WriteFileSystem
			.File
			.ReadAllTextAsync(
				Path.Join(outputRoot, "api", "doc", "elasticsearch", "operation", "operation-search", "index.html"),
				TestContext.Current.CancellationToken
			);

		html.Should().Contain("""<link rel="alternate" type="text/markdown" href="/api/doc/elasticsearch.md" title="Markdown export"/>""");
		html.Should().Contain("View as Markdown");
		operationHtml.Should().Contain(
			"""<link rel="alternate" type="text/markdown" href="/api/doc/elasticsearch/operation/operation-search.md" title="Markdown export"/>"""
		);
		operationHtml.Should().Contain("View as Markdown");
		operationHtml.Should().Contain("class=\"api-operation-description\"");
		operationHtml.Should().NotContain("operation-verb");
		operationHtml.Should().Contain("POST");
		operationHtml.Should().Contain("/{index}/_search");
	}

	[Fact]
	public async Task SimpleMarkdownPage_WritesAuthoredSource()
	{
		var introPath = Path.Combine(
			Paths.WorkingDirectoryRoot.FullName,
			"tests",
			"Elastic.ApiExplorer.Tests",
			"TestData",
			"kibana-api-overview.md"
		);
		var file = new FileSystem().FileInfo.New(introPath);
		var item = new SimpleMarkdownNavigationItem("/api/doc/kibana/kibana-api-overview", "Spaces", file, fixture.Navigation);
		var renderContext = new ApiRenderContext(
			fixture.Context,
			fixture.Document,
			new Elastic.Documentation.Site.FileProviders.StaticFileContentHashProvider(
				new Elastic.Documentation.Site.FileProviders.EmbeddedOrPhysicalFileProvider(fixture.Context)
			)
		)
		{ NavigationHtml = string.Empty, CurrentNavigation = item, MarkdownRenderer = PassthroughMarkdownRenderer.Instance };

		var markdown = await item.RenderCommonMarkAsync(renderContext, TestContext.Current.CancellationToken);

		var wrapped = ApiMarkdownFrontMatter.Wrap(markdown!, item, renderContext, item);

		wrapped.Should().StartWith("---");
		wrapped.Should().Contain("type: api");
		wrapped.Should().Contain("title: Kibana spaces");
		wrapped.Should().NotContain("navigation_title:");
		wrapped.Should().Contain("description: Spaces enable you to organize");
		wrapped.Should().Contain("# Kibana spaces");
		wrapped.Should().NotContain("<!DOCTYPE");
	}

	private static BuildContext CreateGenerateContext(string outputRoot)
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var repoRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-md-repo-{Guid.NewGuid():N}");
		var configPath = Path.Join(repoRoot, "docs", "docset.yml");
		var docsetYaml =
			"""
			api:
			  elasticsearch:
			    - spec: elasticsearch-openapi.json
			      product: elasticsearch
			""";
		var fs = new MockFileSystem(new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName });
		fs.AddDirectory(Path.Join(repoRoot, ".git"));
		fs.AddFile(configPath, new MockFileData(docsetYaml));
		var products = new ProductsConfiguration
		{
			Products = new[] { product }.ToFrozenDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase),
			PublicReferenceProducts = new[] { product }.ToFrozenDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase),
			ProductDisplayNames = new Dictionary<string, string> { [product.Id] = product.DisplayName ?? product.Id }.ToFrozenDictionary(
				StringComparer.OrdinalIgnoreCase
			)
		};
		var configurationContext = TestHelpers.CreateConfigurationContext(fs, stack, products);

		return new BuildContext(
			collector,
			DocumentationFileSystem.Resolve(
				repoRoot,
				new DocumentationScopeOptions
				{
					ConfigurationFile = configPath,
					Output = outputRoot,
					Git = new GitCheckoutInformation
					{
						Branch = "main",
						Remote = "https://github.com/elastic/elasticsearch.git",
						Ref = "refs/heads/main"
					},
					Inner = fs
				}
			),
			configurationContext
		);
	}

	private static IOpenApiSpecificationReader CreateSequentialReader(params OpenApiDocument[] documents)
	{
		var queue = new Queue<OpenApiDocument>(documents);
		var reader = A.Fake<IOpenApiSpecificationReader>();
		A.CallTo(() => reader.ReadAsync(A<Stream>._, A<string>._)).ReturnsLazily(_ => Task.FromResult<OpenApiDocument?>(queue.Dequeue()));
		return reader;
	}

	private static HttpMessageHandler MainOnlyHandler() =>
		new StubHandler(request =>
		{
			if (request.RequestUri!.AbsolutePath.EndsWith("index.json", StringComparison.Ordinal))
			{
				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent(
						/*lang=json,strict*/
						"""
						{
							"elastic/elasticsearch": {
								"elasticsearch-openapi.json": {
									"main": { "version": "main" }
								}
							}
						}
						""",
						System.Text.Encoding.UTF8,
						"application/json"
					)
				};
			}

			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(
					/*lang=json,strict*/
					"""{"openapi":"3.1.0","info":{"title":"Spec","version":"1.0"},"paths":{}}""",
					System.Text.Encoding.UTF8,
					"application/json"
				)
			};
		});

	private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(responder(request));
	}
}
