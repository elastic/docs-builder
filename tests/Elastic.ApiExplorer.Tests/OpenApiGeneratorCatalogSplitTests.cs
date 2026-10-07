// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
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

	[Test]
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

		var entries = await generator.GenerateProducts(ctx: TestContext.Current!.Execution.CancellationToken);

		entries.Should().ContainSingle();
		entries[0].Title.Should().Be("Elasticsearch main");
		entries[0].ProductId.Should().Be("elasticsearch");
		entries[0].Description.Should().Be("A **distributed** [search](https://example.com) engine.\n\nMore detail.");
		entries[0].CatalogCategories.Should().Equal("ece", "ess", "self");
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch", "index.html")).Should().BeTrue();
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "index.html")).Should().BeFalse();
	}

	[Test]
	public async Task GenerateProducts_IsolatedFixtureKey_DropsPrefixFromUrl()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-catalog-split-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(
			outputRoot,
			"""
			api:
			  docs-builder-elasticsearch:
			    - spec: elasticsearch-openapi.json
			      product: elasticsearch
			"""
		);
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var reader = CreateSequentialReader(SpecDocument("Elasticsearch main"));
		var generator = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			context,
			NoopMarkdownStringRenderer.Instance,
			versionIndexClient,
			reader
		);

		var entries = await generator.GenerateProducts(ctx: TestContext.Current!.Execution.CancellationToken);

		entries.Should().ContainSingle();
		entries[0].Url.Should().Be("/docs/api/doc/elasticsearch");
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch", "index.html")).Should().BeTrue();
		context.WriteFileSystem.Directory.Exists(Path.Join(outputRoot, "api", "doc", "docs-builder-elasticsearch")).Should().BeFalse();
	}

	[Test]
	public async Task GenerateProducts_RegenerateWithShorterPage_TruncatesPreviousOutput()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-catalog-split-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(outputRoot);
		var ctx = TestContext.Current!.Execution.CancellationToken;
		var longTitle = "Elasticsearch " + new string('x', 4000);
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var first = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			context,
			NoopMarkdownStringRenderer.Instance,
			versionIndexClient,
			CreateSequentialReader(SpecDocument(longTitle))
		);
		var second = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			context,
			NoopMarkdownStringRenderer.Instance,
			versionIndexClient,
			CreateSequentialReader(SpecDocument("Elasticsearch"))
		);

		_ = await first.GenerateProducts(ctx: ctx);
		_ = await second.GenerateProducts(ctx: ctx);

		var html = await context
			.WriteFileSystem
			.File
			.ReadAllTextAsync(Path.Join(outputRoot, "api", "doc", "elasticsearch", "index.html"), ctx);
		html.Should().NotContain(longTitle);
		html.TrimEnd().Should().EndWith("</html>");
	}

	[Test]
	public async Task GenerateCatalog_WritesCombinedCatalogFromMultipleEntries()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-catalog-split-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(outputRoot);
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance, versionIndexClient);
		var entries = new List<ApiCatalogEntry>
		{
			new("elasticsearch", "Elasticsearch", "/docs/api/doc/elasticsearch", "elasticsearch", "A distributed search engine."),
			new("kibana", "Kibana", "/docs/api/doc/kibana", "kibana")
		};

		await generator.GenerateCatalog(entries, TestContext.Current!.Execution.CancellationToken);

		var catalogPath = Path.Join(outputRoot, "api", "index.html");
		context.WriteFileSystem.File.Exists(catalogPath).Should().BeTrue();
		var html = await context.WriteFileSystem.File.ReadAllTextAsync(catalogPath, TestContext.Current!.Execution.CancellationToken);
		html.Should().Contain("<h1>Elastic APIs</h1>");
		html.Should().Contain(
			"<a href=\"/docs/api/doc/elasticsearch\" aria-labelledby=\"api-catalog-title-elasticsearch\" class=\"api-card api-catalog-card "
		);
		html.Should().Contain(
			"<a href=\"/docs/api/doc/kibana\" aria-labelledby=\"api-catalog-title-kibana\" class=\"api-card api-catalog-card "
		);
		html.Should().Contain(
			"<h2 id=\"api-catalog-title-elasticsearch\" class=\"api-card-title api-catalog-card-title ",
			"the heading is inside the link and names it"
		);
		html.Should().NotContain("api-catalog-card-arrow");
		html.Should().NotContain("View docs");
		html.Should().NotContain("rounded-2xl", "the box comes from the shared api-card class");
		html.Split("<a href=").Length.Should().BeGreaterThan(2);
		html.Should().NotContain("after:absolute", "the card is the link itself, so there is no stretched overlay to trap");
		html.Should().Contain("<ul class=\"api-catalog-featured ");
		html.Should().NotContain("lg:grid-cols-4", "no other APIs, so no second grid");
		html.Should().NotContain("hub-card");
		html.Should().Contain("A distributed search engine.");
		html.Should().NotContain("Choose a product");
		html.Should().Contain(
			"<div id=\"main-container\" class=\"flex min-h-screen flex-col items-center\">",
			"a short page must still fill the screen"
		);
		html.Should().NotContain("data-copy-page");
		html.Should().NotContain("api-catalog-badge");
		html.Should().NotContain("api-catalog-card-key");
		html.Should().NotContain("listing-root");
		html.Should().NotContain("listing-group-chips");
		html.Should().NotContain("download");
		html.Should().NotContain("hub-page");
		html.Should().NotContain("markdown-content");
		html.Should().NotContain("id=\"pages-nav\"");
	}

	[Test]
	public async Task GenerateProducts_LandingHeading_ShowsSpecTitleAndProductMark()
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

		_ = await generator.GenerateProducts(ctx: TestContext.Current!.Execution.CancellationToken);

		var html = await context
			.WriteFileSystem
			.File
			.ReadAllTextAsync(
				Path.Join(outputRoot, "api", "doc", "elasticsearch", "index.html"),
				TestContext.Current!.Execution.CancellationToken
			);
		html.Should().Contain("api-landing-heading");
		html.Should().Contain("<h1>Elasticsearch main</h1>");
		html.Should().Contain("<span class=\"api-landing-icon\">");
		html.Should().Contain("viewBox=\"8 4.9995 47.7276 54.001\"");
	}

	[Test]
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

		await generator.Generate(TestContext.Current!.Execution.CancellationToken);

		var productHtml = context.WriteFileSystem.File.ReadAllText(Path.Join(outputRoot, "api", "doc", "elasticsearch", "index.html"));
		var catalogHtml = context.WriteFileSystem.File.ReadAllText(Path.Join(outputRoot, "api", "index.html"));
		productHtml.Should().Contain("id=\"api-hub-switcher\"");
		productHtml.Should().Contain("<h1>Elasticsearch main</h1>");
		productHtml.Should().Contain("<option value=\"/docs/api/doc/elasticsearch\" selected>Elasticsearch main</option>");
		productHtml.Should().Contain("Back to hub");
		catalogHtml.Should().NotContain("id=\"api-hub-switcher\"");
		catalogHtml.Should().Contain("<h1>Elastic APIs</h1>");
		catalogHtml.Should().Contain("api-card api-catalog-card");
		catalogHtml.Should().NotContain("listing-group-chips");
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch.md")).Should().BeTrue();
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api.md")).Should().BeTrue();
	}

	[Test]
	public async Task GenerateProducts_WithHubEntries_WritesSiblingApisOnProductPage()
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
		var hubEntries = new List<ApiCatalogEntry>
		{
			new("elasticsearch", "Elasticsearch", "/docs/api/doc/elasticsearch"),
			new("kibana", "Kibana", "/docs/api/doc/kibana")
		};

		_ = await generator.GenerateProducts(hubEntries, TestContext.Current!.Execution.CancellationToken);

		var productHtml = context.WriteFileSystem.File.ReadAllText(Path.Join(outputRoot, "api", "doc", "elasticsearch", "index.html"));
		productHtml.Should().Contain("id=\"api-hub-switcher\"");
		productHtml.Should().Contain("<option value=\"/docs/api/\">Back to hub</option>");
		productHtml.Should().Contain("<option value=\"/docs/api/doc/elasticsearch\" selected>Elasticsearch</option>");
		productHtml.Should().Contain("<option value=\"/docs/api/doc/kibana\">Kibana</option>");
	}

	[Test]
	public async Task Generate_SharedProductDisplayName_UsesLandingTitleInSwitcher()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-catalog-split-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(
			outputRoot,
			"""
			api:
			  cloud-billing:
			    - spec: cloud-billing.json
			      product: elasticsearch
			  cloud-connect:
			    - spec: cloud-connect.json
			      product: elasticsearch
			""",
			displayName: "Elastic Cloud Hosted"
		);
		using var versionIndexClient = new VersionIndexClient(BaseUri, SharedProductHandler(), sleep: (_, _) => Task.CompletedTask);
		var reader = A.Fake<IOpenApiSpecificationReader>();
		A.CallTo(() => reader.ReadAsync(A<Stream>._, A<string>._, A<IDiagnosticsCollector?>._)).ReturnsLazily(call =>
		{
			var specFileName = call.GetArgument<string>(1);
			var title = specFileName == "cloud-billing.json" ? "Cloud Billing API" : "Elastic Cloud Connected API";
			return Task.FromResult<OpenApiDocument?>(SpecDocument(title));
		});
		var generator = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			context,
			NoopMarkdownStringRenderer.Instance,
			versionIndexClient,
			reader
		);

		await generator.Generate(TestContext.Current!.Execution.CancellationToken);

		var productHtml = context.WriteFileSystem.File.ReadAllText(Path.Join(outputRoot, "api", "doc", "cloud-connect", "index.html"));
		productHtml.Should().Contain("<h1>Elastic Cloud Connected API</h1>");
		productHtml.Should().Contain("<option value=\"/docs/api/doc/cloud-billing\">Cloud Billing API</option>");
		productHtml.Should().Contain("<option value=\"/docs/api/doc/cloud-connect\" selected>Elastic Cloud Connected API</option>");
	}

	[Test]
	public async Task GenerateCatalog_CategoriesDeclared_RendersOneListWithDeploymentTags()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-catalog-split-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(outputRoot);
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance, versionIndexClient);
		var entries = new List<ApiCatalogEntry>
		{
			new("elasticsearch", "Elasticsearch", "/docs/api/doc/elasticsearch", "elasticsearch") { CatalogCategories = ["self", "ess"] },
			new("serverless", "Elasticsearch Serverless", "/docs/api/doc/serverless", "elasticsearch")
			{
				CatalogCategories = ["serverless"]
			},
			new("connect", "Cloud Connect", "/docs/api/doc/connect", "ess")
		};

		await generator.GenerateCatalog(entries, TestContext.Current!.Execution.CancellationToken);

		var html = await context
			.WriteFileSystem
			.File
			.ReadAllTextAsync(Path.Join(outputRoot, "api", "index.html"), TestContext.Current!.Execution.CancellationToken);
		html.Should().NotContain("<ul class=\"api-catalog-featured ", "Kibana is missing, so there is no featured pair");
		html.Should().Contain("lg:grid-cols-4");
		html.Should().NotContain("Other APIs");
		html.Split("href=\"/docs/api/doc/elasticsearch\"").Length.Should().Be(2, "an API shows once, however many deployments it declares");
		html
			.IndexOf("Cloud Connect", StringComparison.Ordinal)
			.Should()
			.BeLessThan(html.IndexOf("Elasticsearch Serverless", StringComparison.Ordinal), "APIs without a priority key sort by title");
		html.Should().Contain("<div class=\"applies mt-auto flex flex-wrap gap-1.5 pt-2\">");
		html.Should().Contain("<span class=\"applicable-info\">");
		html.Should().Contain("Self-managed");
		html.Should().Contain("<abbr title=\"Elastic Cloud Hosted\" class=\"no-underline\">ECH</abbr>");
		html.Should().NotContain("applicable-separator", "no lifecycle or version is known, so the pill is name only");
		html.Should().NotContain("data-group");
		html.Should().NotContain("data-listing-groups");
		html.Should().NotContain("listing-group-chips");
		html.Should().NotContain("No APIs match your filter.");
	}

	[Test]
	public async Task GenerateCatalog_RendersTheTitleBandOutsideTheContainerAndNoSearchField()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-catalog-split-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(outputRoot);
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance, versionIndexClient);

		await generator.GenerateCatalog(
			[new("elasticsearch", "Elasticsearch", "/docs/api/doc/elasticsearch", "elasticsearch")],
			TestContext.Current!.Execution.CancellationToken
		);

		var html = await context
			.WriteFileSystem
			.File
			.ReadAllTextAsync(Path.Join(outputRoot, "api", "index.html"), TestContext.Current!.Execution.CancellationToken);
		html.Should().Contain("hub-hero api-catalog-hero");
		html.Should().Contain("<p class=\"api-catalog-lede\">" + ApiCatalog.PageLede + "</p>", "one line of text sits under the title");
		html
			.IndexOf("api-catalog-band", StringComparison.Ordinal)
			.Should()
			.BeGreaterThan(-1)
			.And
			.BeLessThan(
				html.IndexOf("id=\"content-container\"", StringComparison.Ordinal),
				"the band is rendered outside the width-limited container"
			);
		html.Should().NotContain("navigation-search", "the catalog has no search field");
	}

	[Test]
	public async Task GenerateCatalog_CloudHostedAndEnterprise_ShowCloudMarksNotTheGenericGlyph()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-catalog-split-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(outputRoot);
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance, versionIndexClient);
		var entries = new List<ApiCatalogEntry>
		{
			new("cloud", "Elastic Cloud API", "/docs/api/doc/cloud", "cloud-hosted"),
			new("cloud-enterprise", "Elastic Cloud Enterprise API", "/docs/api/doc/cloud-enterprise", "cloud-enterprise")
		};

		await generator.GenerateCatalog(entries, TestContext.Current!.Execution.CancellationToken);

		var html = await context
			.WriteFileSystem
			.File
			.ReadAllTextAsync(Path.Join(outputRoot, "api", "index.html"), TestContext.Current!.Execution.CancellationToken);
		html.Should().Contain("api-catalog-card-icon");
		html.Should().Contain("#0080D5", "the cloud marks are drawn, not the fallback glyph");
		html.Should().NotContain("M13.5 5 10.5 19", "that is the generic fallback glyph");
	}

	private static BuildContext CreateGenerateContext(string outputRoot, string? docsetYaml = null, string? displayName = null)
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack), displayName);
		var repoRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-catalog-split-repo-{Guid.NewGuid():N}");
		var configPath = Path.Join(repoRoot, "docs", "docset.yml");
		docsetYaml ??=
			"""
			api:
			  elasticsearch:
			    - spec: elasticsearch-openapi.json
			      product: elasticsearch
			      catalog:
			        categories:
			          - self
			          - ece
			          - ess
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
			Info = new OpenApiInfo
			{
				Title = title,
				Version = "1.0",
				Description = "A **distributed** [search](https://example.com) engine.\n\nMore detail."
			},
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
		var queue = new ConcurrentQueue<OpenApiDocument>(documents);
		var reader = A.Fake<IOpenApiSpecificationReader>();
		A.CallTo(() => reader.ReadAsync(A<Stream>._, A<string>._, A<IDiagnosticsCollector?>._)).ReturnsLazily(
			_ => Task.FromResult<OpenApiDocument?>(queue.TryDequeue(out var next) ? next : null)
		);
		return reader;
	}

	private static HttpMessageHandler SharedProductHandler() =>
		new StubHandler(request =>
		{
			if (request.RequestUri!.AbsolutePath.EndsWith("index.json", StringComparison.Ordinal))
			{
				return IndexResponse(
					/*lang=json,strict*/
					"""
					{
						"elastic/elasticsearch": {
							"cloud-billing.json": {
								"main": { "version": "main" }
							},
							"cloud-connect.json": {
								"main": { "version": "main" }
							}
						}
					}
					"""
				);
			}

			return SpecResponse();
		});

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
