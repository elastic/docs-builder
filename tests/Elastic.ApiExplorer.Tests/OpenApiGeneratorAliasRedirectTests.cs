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

public class OpenApiGeneratorAliasRedirectTests(ApiExplorerFixture fixture) : IClassFixture<ApiExplorerFixture>
{
	private static readonly Uri BaseUri = new("https://cdn.example/");

	[Fact]
	public async Task Generate_WithAlias_CollectsAliasRedirects()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-alias-{Guid.NewGuid():N}");
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
		var canonicalLanding = Path.Join(outputRoot, "api", "doc", "cloud-serverless", "index.html");
		var aliasLanding = Path.Join(outputRoot, "api", "doc", "elastic-cloud-serverless", "index.html");

		write.Exists(canonicalLanding).Should().BeTrue("canonical landing page must exist");
		write.Exists(aliasLanding).Should().BeFalse("alias path must not have its own HTML file");

		generator.AliasRedirects.Should().ContainKey("/api/doc/elastic-cloud-serverless");
		generator.AliasRedirects["/api/doc/elastic-cloud-serverless"].Should().Be("/api/doc/cloud-serverless");
	}

	[Fact]
	public async Task Generate_WithAlias_OmitsVersionedAliasRedirects()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-alias-versioned-{Guid.NewGuid():N}");
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

		// cloud-serverless is versionless so only main is rendered; no /v{N}/ alias entries should exist
		generator.AliasRedirects.Keys.Should().NotContain(k => k.Contains("/v", StringComparison.OrdinalIgnoreCase));
	}

	private static BuildContext CreateGenerateContext(string outputRoot)
	{
		var collector = new DiagnosticsCollector([]);
		var serverless = TestHelpers.CreateVersionlessConfiguration();
		var product = TestHelpers.CreateProduct("cloud-serverless", serverless.GetVersioningSystem(VersioningSystemId.Serverless));
		var repoRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-alias-repo-{Guid.NewGuid():N}");
		var configPath = Path.Join(repoRoot, "docs", "docset.yml");
		var docsetYaml =
			"""
			api:
			  cloud-serverless:
			    - spec: cloud-serverless-openapi.yaml
			      product: cloud-serverless
			      aliases: [elastic-cloud-serverless]
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
		var configurationContext = TestHelpers.CreateConfigurationContext(fs, serverless, products);

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
						Remote = "https://github.com/elastic/cloud-serverless.git",
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
							"elastic/cloud-serverless": {
								"cloud-serverless-openapi.yaml": {
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
					"""{"openapi":"3.1.0","info":{"title":"Cloud Serverless API","version":"1.0"},"paths":{}}""",
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
