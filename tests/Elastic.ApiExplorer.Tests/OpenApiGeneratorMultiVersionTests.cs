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
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

public class OpenApiGeneratorMultiVersionTests
{
	private static readonly Uri BaseUri = new("https://cdn.example/");

	private static BuildContext CreateContext(
		DiagnosticsCollector collector,
		VersionsConfiguration? versionsConfiguration = null,
		ProductsConfiguration? productsConfiguration = null,
		GitCheckoutInformation? git = null)
	{
		return new BuildContext(collector,
			DocumentationFileSystem.Resolve(new FileSystem().DirectoryInfo.New(Paths.WorkingDirectoryRoot.FullName), new DocumentationScopeOptions { Git = git }),
			TestHelpers.CreateConfigurationContext(new FileSystem(), versionsConfiguration, productsConfiguration));
	}

	private static ResolvedApiConfiguration ApiConfig(
		Product product,
		IFileInfo? localSpecFile = null,
		string specFileName = "elasticsearch-openapi.json",
		string? repository = null) => new()
		{
			ProductKey = product.Id,
			Product = product,
			SpecFileName = specFileName,
			LocalSpecFile = localSpecFile,
			Repository = repository
		};

	private static ProductsConfiguration ProductsFor(params Product[] products) =>
		new()
		{
			Products = products.ToFrozenDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase),
			PublicReferenceProducts = products.ToFrozenDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase),
			ProductDisplayNames = products.ToDictionary(p => p.Id, p => p.DisplayName, StringComparer.OrdinalIgnoreCase).ToFrozenDictionary(StringComparer.OrdinalIgnoreCase)
		};

	private static OpenApiDocument SpecDocument(string title) => new()
	{
		Info = new OpenApiInfo { Title = title, Version = "1.0" },
		Paths = new OpenApiPaths
		{
			["/ping"] = new OpenApiPathItem
			{
				Operations = new Dictionary<HttpMethod, OpenApiOperation>
				{
					[HttpMethod.Get] = new()
					{
						OperationId = "ping",
						Tags = new HashSet<OpenApiTagReference> { new("core") },
						Responses = new OpenApiResponses { ["200"] = new OpenApiResponse { Description = "ok" } }
					}
				}
			}
		},
		Tags = new HashSet<OpenApiTag> { new() { Name = "core" } }
	};

	[Fact]
	public async Task ResolveDocumentsForProduct_MultiMajorIndex_ResolvesMainAndNumericVersions()
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var context = CreateContext(collector, stack, ProductsFor(product), GitForElasticsearch());
		var handler = MultiVersionHandler();
		using var versionIndexClient = new VersionIndexClient(BaseUri, handler, sleep: (_, _) => Task.CompletedTask);
		var reader = CreateSequentialReader(
			SpecDocument("Elasticsearch main"),
			SpecDocument("Elasticsearch 9"),
			SpecDocument("Elasticsearch 8"));
		var generator = CreateGenerator(context, versionIndexClient, reader);

		var documents = await generator.ResolveDocumentsForProduct("elasticsearch", ApiConfig(product), TestContext.Current.CancellationToken);

		documents.Should().HaveCount(3);
		documents.Select(d => d.Version.Moniker).Should().BeEquivalentTo(["main", "9", "8"]);
		documents.Should().ContainSingle(d =>
			ApiUrlBuilder.ProductSuffix("elasticsearch", d.Version.Moniker) == "elasticsearch"
			&& d.Document.Info.Title == "Elasticsearch main");
		documents.Should().ContainSingle(d =>
			ApiUrlBuilder.ProductSuffix("elasticsearch", d.Version.Moniker) == "elasticsearch/v9"
			&& d.Document.Info.Title == "Elasticsearch 9");
		documents.Should().ContainSingle(d =>
			ApiUrlBuilder.ProductSuffix("elasticsearch", d.Version.Moniker) == "elasticsearch/v8"
			&& d.Document.Info.Title == "Elasticsearch 8");
	}

	[Fact]
	public async Task ResolveDocumentsForProduct_VersionlessProduct_RendersMainOnly()
	{
		var collector = new DiagnosticsCollector([]);
		var versionless = TestHelpers.CreateVersionlessConfiguration();
		var product = TestHelpers.CreateProduct("cloud-serverless", versionless.GetVersioningSystem(VersioningSystemId.Serverless));
		var context = CreateContext(collector, versionless, ProductsFor(product), GitForElasticsearch());
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(repository: "elastic/serverless-api-specification"), sleep: (_, _) => Task.CompletedTask);
		var reader = CreateSequentialReader(SpecDocument("Serverless main"));
		var generator = CreateGenerator(context, versionIndexClient, reader);
		var apiConfig = ApiConfig(product, specFileName: "elastic-cloud-serverless.yml", repository: "elastic/serverless-api-specification");

		var documents = await generator.ResolveDocumentsForProduct("cloud-serverless", apiConfig, TestContext.Current.CancellationToken);

		documents.Should().ContainSingle();
		documents[0].Version.Moniker.Should().Be("main");
		ApiUrlBuilder.ProductSuffix("cloud-serverless", documents[0].Version.Moniker).Should().Be("cloud-serverless");
	}

	[Fact]
	public async Task ResolveDocumentsForProduct_LocalMainAndRemoteHistoricalVersions_ResolvesAllTrees()
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var context = CreateContext(collector, stack, ProductsFor(product), GitForElasticsearch());
		var localFile = new FileSystem().FileInfo.New(Path.Combine(Paths.WorkingDirectoryRoot.FullName, "docs", "elasticsearch-openapi-docs.json"));
		var localDocument = SpecDocument("Elasticsearch local main");
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var reader = A.Fake<IOpenApiSpecificationReader>();
		A.CallTo(() => reader.ReadAsync(localFile)).Returns(localDocument);
		A.CallTo(() => reader.ReadAsync(A<Stream>._, "elasticsearch-openapi.json"))
			.ReturnsLazily((Stream _, string _) => SpecDocument("Elasticsearch remote"));
		var generator = CreateGenerator(context, versionIndexClient, reader);

		var documents = await generator.ResolveDocumentsForProduct("elasticsearch", ApiConfig(product, localFile), TestContext.Current.CancellationToken);

		documents.Should().HaveCount(3);
		documents.Should().ContainSingle(d => d.Version.Moniker == "main" && d.Document == localDocument);
		documents.Should().ContainSingle(d => d.Version.Moniker == "9");
		documents.Should().ContainSingle(d => d.Version.Moniker == "8");
		A.CallTo(() => reader.ReadAsync(localFile)).MustHaveHappenedOnceExactly();
		A.CallTo(() => reader.ReadAsync(A<Stream>._, "elasticsearch-openapi.json")).MustHaveHappened(2, Times.Exactly);
	}

	[Fact]
	public void CreateNavigation_VersionedSuffix_UsesVersionPrefixedUrls()
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var context = CreateContext(collector, stack, ProductsFor(product));
		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance);
		var navigation = generator.CreateNavigation("elasticsearch/v8", SpecDocument("Elasticsearch 8"));

		navigation.Url.Should().Be("/api/doc/elasticsearch/v8");
		var operation = navigation.NavigationItems
			.OfType<TagNavigationItem>()
			.Single()
			.NavigationItems
			.OfType<OperationNavigationItem>()
			.Single();
		operation.Url.Should().Be("/api/doc/elasticsearch/v8/operation/operation-ping");
	}

	[Fact]
	public async Task Generate_WritesDistinctOutputTreesForMainAndReleasedMajors()
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-explorer-output-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(collector, stack, ProductsFor(product), outputRoot, GitForElasticsearch());
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var reader = CreateSequentialReader(
			SpecDocument("Elasticsearch main"),
			SpecDocument("Elasticsearch 9"),
			SpecDocument("Elasticsearch 8"));
		var generator = CreateGenerator(context, versionIndexClient, reader);

		await generator.Generate(TestContext.Current.CancellationToken);

		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch", "index.html")).Should().BeTrue();
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch", "v9", "index.html")).Should().BeTrue();
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch", "v8", "index.html")).Should().BeTrue();
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch", "v8", "operation", "operation-ping", "index.html")).Should().BeTrue();
	}

	[Fact]
	public async Task Generate_OperationPage_RendersApiVersionSwitcherAndSuppressesNavDropdown()
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-explorer-output-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(collector, stack, ProductsFor(product), outputRoot, GitForElasticsearch());
		context.Configuration.Features.PrimaryNavEnabled = true;
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var reader = CreateSequentialReader(
			SpecDocument("Elasticsearch main"),
			SpecDocument("Elasticsearch 9"),
			SpecDocument("Elasticsearch 8"));
		var generator = CreateGenerator(context, versionIndexClient, reader);

		await generator.Generate(TestContext.Current.CancellationToken);

		var operationPage = Path.Join(outputRoot, "api", "doc", "elasticsearch", "operation", "operation-ping", "index.html");
		var html = await context.WriteFileSystem.File.ReadAllTextAsync(operationPage, TestContext.Current.CancellationToken);

		html.Should().Contain("id=\"api-version-dropdown\"");
		html.Should().Contain("/api/doc/elasticsearch/v8/operation/operation-ping");
		html.Should().NotContain("id=\"pages-dropdown\"");
		html.Should().NotContain("id=\"nav-dropdown\"");
	}

	private static BuildContext CreateGenerateContext(
		DiagnosticsCollector collector,
		VersionsConfiguration versionsConfiguration,
		ProductsConfiguration productsConfiguration,
		string outputRoot,
		GitCheckoutInformation git)
	{
		var repoRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-multi-version-test-{Guid.NewGuid():N}");
		var configPath = Path.Join(repoRoot, "docs", "docset.yml");
		var docsetYaml = """
			api:
			  elasticsearch:
			    - spec: elasticsearch-openapi.json
			      product: elasticsearch
			""";
		var fs = new MockFileSystem(new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName });
		fs.AddDirectory(Path.Join(repoRoot, ".git"));
		fs.AddFile(configPath, new MockFileData(docsetYaml));
		var configurationContext = TestHelpers.CreateConfigurationContext(fs, versionsConfiguration, productsConfiguration);

		return new BuildContext(collector,
			DocumentationFileSystem.Resolve(repoRoot, new DocumentationScopeOptions
			{
				ConfigurationFile = configPath,
				Output = outputRoot,
				Git = git,
				Inner = fs
			}),
			configurationContext);
	}

	private static OpenApiGenerator CreateGenerator(BuildContext context, VersionIndexClient versionIndexClient, IOpenApiSpecificationReader reader) =>
		new(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance, versionIndexClient, reader);

	private static IOpenApiSpecificationReader CreateSequentialReader(params OpenApiDocument[] documents)
	{
		var queue = new Queue<OpenApiDocument>(documents);
		var reader = A.Fake<IOpenApiSpecificationReader>();
		A.CallTo(() => reader.ReadAsync(A<Stream>._, A<string>._))
			.ReturnsLazily(_ => Task.FromResult<OpenApiDocument?>(queue.Dequeue()));
		return reader;
	}

	private static HttpMessageHandler MultiVersionHandler(string repository = "elastic/elasticsearch") =>
		new StubHandler(request =>
		{
			if (request.RequestUri!.AbsolutePath.EndsWith("index.json", StringComparison.Ordinal))
			{
				return IndexResponse(/*lang=json,strict*/ $$"""
					{
						"{{repository}}": {
							"elasticsearch-openapi.json": {
								"main": { "version": "main" },
								"9": { "version": "9.4" },
								"8": { "version": "8.19" }
							},
							"elastic-cloud-serverless.yml": {
								"main": { "version": "main" },
								"8": { "version": "8.19" }
							}
						}
					}
					""");
			}

			return SpecResponse();
		});

	private static GitCheckoutInformation GitForElasticsearch() => new()
	{
		Branch = "main",
		Remote = "https://github.com/elastic/elasticsearch.git",
		Ref = "refs/heads/main"
	};

	private static HttpResponseMessage IndexResponse(string body) =>
		new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

	private static HttpResponseMessage SpecResponse() => new(HttpStatusCode.OK)
	{
		Content = new StringContent(
			/*lang=json,strict*/ """{"openapi":"3.1.0","info":{"title":"Spec","version":"1.0"},"paths":{}}""",
			System.Text.Encoding.UTF8, "application/json")
	};

	private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(responder(request));
	}
}
