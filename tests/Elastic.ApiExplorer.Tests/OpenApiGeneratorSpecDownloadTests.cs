// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using AwesomeAssertions;
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

public class OpenApiGeneratorSpecDownloadTests(ApiExplorerFixture fixture) : IClassFixture<ApiExplorerFixture>
{
	private static readonly Uri BaseUri = new("https://cdn.example/");

	[Fact]
	public async Task Generate_WritesJsonAndYamlSiblingsForProductLanding()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-spec-emit-{Guid.NewGuid():N}");
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
		var jsonPath = Path.Join(outputRoot, "api", "doc", "elasticsearch.json");
		var yamlPath = Path.Join(outputRoot, "api", "doc", "elasticsearch.yaml");
		write.Exists(jsonPath).Should().BeTrue();
		write.Exists(yamlPath).Should().BeTrue();

		var json = await write.ReadAllTextAsync(jsonPath, TestContext.Current.CancellationToken);
		var yaml = await write.ReadAllTextAsync(yamlPath, TestContext.Current.CancellationToken);

		json.Should().Contain("\"openapi\"");
		json.Should().Contain("Fixture API");
		json.Length.Should().BeGreaterThan(100);
		yaml.Should().Contain("openapi:");
		yaml.Should().Contain("Fixture API");
		yaml.Length.Should().BeGreaterThan(100);
	}

	[Fact]
	public async Task Generate_WritesVersionedSpecSiblings()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-spec-v8-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(outputRoot);
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var reader = CreateSequentialReader(fixture.Document, fixture.Document, fixture.Document);
		var generator = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			context,
			PassthroughMarkdownRenderer.Instance,
			versionIndexClient,
			reader
		);

		await generator.Generate(TestContext.Current.CancellationToken);

		var write = context.WriteFileSystem.File;
		write.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch", "v8.json")).Should().BeTrue();
		write.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch", "v8.yaml")).Should().BeTrue();
	}

	[Fact]
	public async Task Generate_LandingHtmlContainsDownloadSourceLinks()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-spec-html-{Guid.NewGuid():N}");
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

		html.Should().Contain("Download source");
		html.Should().Contain("href=\"/api/doc/elasticsearch.json\"");
		html.Should().Contain("href=\"/api/doc/elasticsearch.yaml\"");
		html.Should().Contain("download");
	}

	private static BuildContext CreateGenerateContext(string outputRoot)
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var repoRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-spec-repo-{Guid.NewGuid():N}");
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

	private static HttpMessageHandler MultiVersionHandler() =>
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
									"main": { "version": "main" },
									"9": { "version": "9.4" },
									"8": { "version": "8.19" }
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
