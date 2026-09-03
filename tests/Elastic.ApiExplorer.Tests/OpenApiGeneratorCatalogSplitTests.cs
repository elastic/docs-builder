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
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;
using Nullean.ScopedFileSystem;

namespace Elastic.ApiExplorer.Tests;

public class OpenApiGeneratorCatalogSplitTests
{
	private static readonly Uri BaseUri = new("https://cdn.example/");

	[Fact]
	public async Task GenerateProducts_DoesNotWriteCatalogPage()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-catalog-split-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(outputRoot);
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var reader = CreateSequentialReader(SpecDocument("Elasticsearch main"));
		var generator = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			context,
			NoopMarkdownStringRenderer.Instance,
			versionIndexClient,
			reader
		);

		var entries = await generator.GenerateProducts(TestContext.Current.CancellationToken);

		entries.Should().ContainSingle();
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch", "index.html")).Should().BeTrue();
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "index.html")).Should().BeFalse();
	}

	[Fact]
	public async Task GenerateCatalog_WritesCombinedCatalogFromMultipleEntries()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-catalog-split-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(outputRoot);
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance, versionIndexClient);
		var entries = new List<ApiCatalogEntry>
		{
			new("elasticsearch", "Elasticsearch", "/docs/api/doc/elasticsearch/"),
			new("kibana", "Kibana", "/docs/api/doc/kibana/")
		};

		await generator.GenerateCatalog(entries, TestContext.Current.CancellationToken);

		var catalogPath = Path.Join(outputRoot, "api", "index.html");
		context.WriteFileSystem.File.Exists(catalogPath).Should().BeTrue();
		var html = await context.WriteFileSystem.File.ReadAllTextAsync(catalogPath, TestContext.Current.CancellationToken);
		html.Should().Contain("<h1>API catalog</h1>");
		html.Should().Contain("""<a href="/docs/api/doc/elasticsearch/">Elasticsearch <code>elasticsearch</code></a>""");
		html.Should().Contain("""<a href="/docs/api/doc/kibana/">Kibana <code>kibana</code></a>""");
	}

	[Fact]
	public async Task Generate_StillWritesProductsAndCatalog()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-catalog-split-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(outputRoot);
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var reader = CreateSequentialReader(SpecDocument("Elasticsearch main"));
		var generator = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			context,
			NoopMarkdownStringRenderer.Instance,
			versionIndexClient,
			reader
		);

		await generator.Generate(TestContext.Current.CancellationToken);

		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch", "index.html")).Should().BeTrue();
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "index.html")).Should().BeTrue();
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch.md")).Should().BeTrue();
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api.md")).Should().BeTrue();
	}

	private static BuildContext CreateGenerateContext(string outputRoot)
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var repoRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-catalog-split-repo-{Guid.NewGuid():N}");
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
		)
		{ UrlPathPrefix = "docs" };
	}

	private static OpenApiDocument SpecDocument(string title) =>
		new()
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

	private static IOpenApiSpecificationReader CreateSequentialReader(params OpenApiDocument[] documents)
	{
		var queue = new Queue<OpenApiDocument>(documents);
		var reader = A.Fake<IOpenApiSpecificationReader>();
		A.CallTo(() => reader.ReadAsync(A<Stream>._, A<string>._)).ReturnsLazily(_ => Task.FromResult<OpenApiDocument?>(queue.Dequeue()));
		return reader;
	}

	private static HttpMessageHandler MultiVersionHandler(string repository = "elastic/elasticsearch") =>
		new StubHandler(request =>
		{
			if (request.RequestUri!.AbsolutePath.EndsWith("index.json", StringComparison.Ordinal))
			{
				return IndexResponse(/*lang=json,strict*/
					$$"""
					{
						"{{repository}}": {
							"elasticsearch-openapi.json": {
								"main": { "version": "main" }
							}
						}
					}
					"""
				);
			}

			return SpecResponse();
		});

	private static HttpResponseMessage IndexResponse(string body) =>
		new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

	private static HttpResponseMessage SpecResponse() =>
		new(HttpStatusCode.OK)
		{
			Content = new StringContent(
				/*lang=json,strict*/
				"""{"openapi":"3.1.0","info":{"title":"Spec","version":"1.0"},"paths":{}}""",
				System.Text.Encoding.UTF8,
				"application/json"
			)
		};

	private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(responder(request));
	}
}
