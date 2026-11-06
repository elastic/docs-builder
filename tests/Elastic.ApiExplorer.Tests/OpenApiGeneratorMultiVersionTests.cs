// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Elastic.ApiExplorer.Floating;
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
using Nullean.Snapshots;

namespace Elastic.ApiExplorer.Tests;

public partial class OpenApiGeneratorMultiVersionTests
{
	private static readonly Uri BaseUri = new("https://cdn.example/");

	private static ResolvedApiConfiguration ApiConfig(
		Product product,
		IFileInfo? localSpecFile = null,
		string specFileName = "elasticsearch-openapi.json",
		string? repository = null
	) =>
		new()
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
			ProductDisplayNames = products.ToDictionary(p => p.Id, p => p.DisplayName, StringComparer.OrdinalIgnoreCase).ToFrozenDictionary(
				StringComparer.OrdinalIgnoreCase
			)
		};

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

	[Test]
	public async Task ResolveDocumentsForProduct_MultiMajorIndex_ResolvesMainAndNumericVersions()
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var context = TestHelpers.CreateScopedBuildContext(collector, stack, ProductsFor(product), GitForElasticsearch());
		var handler = MultiVersionHandler();
		using var versionIndexClient = new VersionIndexClient(BaseUri, handler, sleep: (_, _) => Task.CompletedTask);
		var reader = CreateVersionedReader(
			("main", SpecDocument("Elasticsearch main")),
			("9.4", SpecDocument("Elasticsearch 9")),
			("8.19", SpecDocument("Elasticsearch 8"))
		);
		var generator = CreateGenerator(context, versionIndexClient, reader);

		var documents = (await generator.ResolveDocumentsForProduct(
			"elasticsearch",
			ApiConfig(product),
			TestContext.Current!.Execution.CancellationToken
		)).Documents;

		documents.Should().HaveCount(3);
		documents.Select(d => d.Version.Moniker).Should().BeEquivalentTo(["main", "9", "8"]);
		documents.Should().ContainSingle(
			d => ApiUrlBuilder.ProductSuffix("elasticsearch", d.Version.Moniker) == "elasticsearch" && d.Document.Info.Title ==
				"Elasticsearch main"
		);
		documents.Should().ContainSingle(
			d => ApiUrlBuilder.ProductSuffix("elasticsearch", d.Version.Moniker) == "elasticsearch/v9" && d.Document.Info.Title ==
				"Elasticsearch 9"
		);
		documents.Should().ContainSingle(
			d => ApiUrlBuilder.ProductSuffix("elasticsearch", d.Version.Moniker) == "elasticsearch/v8" && d.Document.Info.Title ==
				"Elasticsearch 8"
		);
	}

	[Test]
	public async Task CatalogEntry_MultiMajorIndex_OpensTheHighestNumericMajor()
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var context = TestHelpers.CreateScopedBuildContext(collector, stack, ProductsFor(product), GitForElasticsearch());
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var reader = CreateVersionedReader(
			("main", SpecDocument("Elasticsearch main")),
			("9.4", SpecDocument("Elasticsearch 9")),
			("8.19", SpecDocument("Elasticsearch 8"))
		);
		var generator = CreateGenerator(context, versionIndexClient, reader);
		var apiConfig = ApiConfig(product);
		var resolved = await generator.ResolveDocumentsForProduct(
			"elasticsearch",
			apiConfig,
			TestContext.Current!.Execution.CancellationToken
		);

		var entry = generator.CatalogEntry("elasticsearch", apiConfig, resolved);

		entry.Should().NotBeNull();
		entry!.Url.Should().Be("/api/doc/elasticsearch");
		entry.LatestUrl.Should().Be("/api/doc/elasticsearch/v9");
		entry.OpenUrl.Should().Be("/api/doc/elasticsearch/v9");
	}

	[Test]
	public async Task CatalogEntry_VersionlessProduct_KeepsTheUnversionedUrl()
	{
		var collector = new DiagnosticsCollector([]);
		var versionless = TestHelpers.CreateVersionlessConfiguration();
		var product = TestHelpers.CreateProduct("cloud-serverless", versionless.GetVersioningSystem(VersioningSystemId.Serverless));
		var context = TestHelpers.CreateScopedBuildContext(collector, versionless, ProductsFor(product), GitForElasticsearch());
		using var versionIndexClient = new VersionIndexClient(
			BaseUri,
			MultiVersionHandler(repository: "elastic/serverless-api-specification"),
			sleep: (_, _) => Task.CompletedTask
		);
		var generator = CreateGenerator(context, versionIndexClient, CreateVersionedReader(("main", SpecDocument("Serverless main"))));
		var apiConfig = ApiConfig(
			product,
			specFileName: "elastic-cloud-serverless.yml",
			repository: "elastic/serverless-api-specification"
		);
		var resolved = await generator.ResolveDocumentsForProduct(
			"cloud-serverless",
			apiConfig,
			TestContext.Current!.Execution.CancellationToken
		);

		var entry = generator.CatalogEntry("cloud-serverless", apiConfig, resolved);

		entry.Should().NotBeNull();
		entry!.LatestUrl.Should().BeNull();
		entry.OpenUrl.Should().Be("/api/doc/cloud-serverless");
	}

	[Test]
	public async Task ResolveDocumentsForProduct_VersionlessProduct_RendersMainOnly()
	{
		var collector = new DiagnosticsCollector([]);
		var versionless = TestHelpers.CreateVersionlessConfiguration();
		var product = TestHelpers.CreateProduct("cloud-serverless", versionless.GetVersioningSystem(VersioningSystemId.Serverless));
		var context = TestHelpers.CreateScopedBuildContext(collector, versionless, ProductsFor(product), GitForElasticsearch());
		using var versionIndexClient = new VersionIndexClient(
			BaseUri,
			MultiVersionHandler(repository: "elastic/serverless-api-specification"),
			sleep: (_, _) => Task.CompletedTask
		);
		var reader = CreateVersionedReader(("main", SpecDocument("Serverless main")));
		var generator = CreateGenerator(context, versionIndexClient, reader);
		var apiConfig = ApiConfig(
			product,
			specFileName: "elastic-cloud-serverless.yml",
			repository: "elastic/serverless-api-specification"
		);

		var documents = (await generator.ResolveDocumentsForProduct(
			"cloud-serverless",
			apiConfig,
			TestContext.Current!.Execution.CancellationToken
		)).Documents;

		documents.Should().ContainSingle();
		documents[0].Version.Moniker.Should().Be("main");
		ApiUrlBuilder.ProductSuffix("cloud-serverless", documents[0].Version.Moniker).Should().Be("cloud-serverless");
	}

	[Test]
	public async Task ResolveDocumentsForProduct_LocalMainAndRemoteHistoricalVersions_ResolvesAllTrees()
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var context = TestHelpers.CreateScopedBuildContext(collector, stack, ProductsFor(product), GitForElasticsearch());
		var localFile = new FileSystem().FileInfo.New(Path.Combine(AppContext.BaseDirectory, "TestData", "elasticsearch-openapi.json"));
		var localDocument = SpecDocument("Elasticsearch local main");
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var reader = A.Fake<IOpenApiSpecificationReader>();
		A.CallTo(() => reader.ReadAsync(localFile, A<IDiagnosticsCollector?>._)).Returns(localDocument);
		A.CallTo(() => reader.ReadAsync(A<Stream>._, "elasticsearch-openapi.json", A<IDiagnosticsCollector?>._)).ReturnsLazily(
			(Stream _, string _, IDiagnosticsCollector? _) => SpecDocument("Elasticsearch remote")
		);
		var generator = CreateGenerator(context, versionIndexClient, reader);

		var documents = (await generator.ResolveDocumentsForProduct(
			"elasticsearch",
			ApiConfig(product, localFile),
			TestContext.Current!.Execution.CancellationToken
		)).Documents;

		documents.Should().HaveCount(3);
		documents.Should().ContainSingle(d => d.Version.Moniker == "main" && d.Document == localDocument);
		documents.Should().ContainSingle(d => d.Version.Moniker == "9");
		documents.Should().ContainSingle(d => d.Version.Moniker == "8");
		A.CallTo(() => reader.ReadAsync(localFile, A<IDiagnosticsCollector?>._)).MustHaveHappenedOnceExactly();
		A.CallTo(() => reader.ReadAsync(A<Stream>._, "elasticsearch-openapi.json", A<IDiagnosticsCollector?>._)).MustHaveHappened(
			2,
			Times.Exactly
		);
	}

	[Test]
	public async Task ResolveDocumentsForProduct_MainFetchFails_DoesNotMarkOlderSpecForUnmatchedBaseFiles()
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var context = TestHelpers.CreateScopedBuildContext(collector, stack, ProductsFor(product), GitForElasticsearch());
		var handler = new StubHandler(request =>
		{
			var path = request.RequestUri!.AbsolutePath;
			if (path.EndsWith("index.json", StringComparison.Ordinal))
				return IndexResponse(/*lang=json,strict*/
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
					"""
				);
			if (path.Contains("/main/", StringComparison.Ordinal))
				return new HttpResponseMessage(HttpStatusCode.NotFound);
			return SpecResponse(request);
		});
		using var versionIndexClient = new VersionIndexClient(BaseUri, handler, maxAttempts: 1, sleep: (_, _) => Task.CompletedTask);
		var reader = CreateVersionedReader(("9.4", SpecDocument("Elasticsearch 9")), ("8.19", SpecDocument("Elasticsearch 8")));
		var generator = CreateGenerator(context, versionIndexClient, reader);

		var resolved = await generator.ResolveDocumentsForProduct(
			"elasticsearch",
			ApiConfig(product),
			TestContext.Current!.Execution.CancellationToken
		);

		resolved.Documents.Select(d => d.Version.Moniker).Should().BeEquivalentTo(["9", "8"]);
		resolved.UnmatchedBaseFilesMoniker.Should().BeNull();
	}

	[Test]
	public void CreateNavigation_VersionedSuffix_UsesVersionPrefixedUrls()
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var context = TestHelpers.CreateScopedBuildContext(collector, stack, ProductsFor(product));
		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance);
		var navigation = generator.CreateNavigation("elasticsearch/v8", SpecDocument("Elasticsearch 8"));

		navigation.Url.Should().Be("/api/doc/elasticsearch/v8");
		var operation = navigation
			.NavigationItems
			.OfType<TagNavigationItem>()
			.Single()
			.NavigationItems
			.OfType<OperationNavigationItem>()
			.Single();
		operation.Url.Should().Be("/api/doc/elasticsearch/v8/operation/operation-ping");
	}

	[Test]
	public async Task FloatingSection_AfterResolve_MountsEveryVersionAndHidesTheOlderOnes()
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-explorer-output-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(collector, stack, ProductsFor(product), outputRoot, GitForElasticsearch());
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var reader = CreateVersionedReader(
			("main", SpecDocument("Elasticsearch main")),
			("9.4", SpecDocument("Elasticsearch 9")),
			("8.19", SpecDocument("Elasticsearch 8"))
		);
		var generator = CreateGenerator(context, versionIndexClient, reader);
		var provider = new ApiFloatingSectionProvider(context);
		provider.Attach(generator);
		var section = provider.GetSections().Should().ContainSingle().Subject;

		var root = section.CreateRoot(null);
		root.Url.Should().Be("/api/doc/elasticsearch");
		root.NavigationItems.Should().BeEmpty("the root exists before the specs are fetched");

		await section.ResolveAsync(TestContext.Current!.Execution.CancellationToken);

		var versions = root.NavigationItems.Should().AllBeOfType<LandingNavigationItem>().Subject.ToArray();
		versions
			.Select(v => v.Url)
			.Should()
			.BeEquivalentTo(["/api/doc/elasticsearch", "/api/doc/elasticsearch/v9", "/api/doc/elasticsearch/v8"]);
		versions.Where(v => !v.Hidden).Select(v => v.Url).Should().Equal("/api/doc/elasticsearch");

		// the generator renders from the trees the section mounted, so the build never builds a tree twice
		var trees = await generator.TreesFor(
			"elasticsearch",
			context.Configuration.ApiConfigurations!["elasticsearch"],
			TestContext.Current!.Execution.CancellationToken
		);
		trees.Navigations.Should().Equal(versions);
		await generator.Generate(TestContext.Current!.Execution.CancellationToken);
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch", "v8", "index.html")).Should().BeTrue();
	}

	[Test]
	public async Task FloatingSection_AfterResolve_PrintsTheApiTreeWithItsVersions()
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-explorer-output-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(collector, stack, ProductsFor(product), outputRoot, GitForElasticsearch());
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var reader = CreateVersionedReader(
			("main", SpecDocument("Elasticsearch main")),
			("9.4", SpecDocument("Elasticsearch 9")),
			("8.19", SpecDocument("Elasticsearch 8"))
		);
		var provider = new ApiFloatingSectionProvider(context);
		provider.Attach(CreateGenerator(context, versionIndexClient, reader));
		var section = provider.GetSections().Should().ContainSingle().Subject;
		var root = section.CreateRoot(null);

		await section.ResolveAsync(TestContext.Current!.Execution.CancellationToken);

		NavigationTreePrinter.Print(root, maxDepth: 3).Should().MatchSnapshot();
	}

	[Test]
	public async Task FloatingSection_ToLinkSection_PublishesEachPageOnceWithItsVersions()
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-explorer-output-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(collector, stack, ProductsFor(product), outputRoot, GitForElasticsearch());
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var reader = CreateVersionedReader(
			("main", SpecDocument("Elasticsearch main")),
			("9.4", SpecDocument("Elasticsearch 9")),
			("8.19", SpecDocument("Elasticsearch 8"))
		);
		var provider = new ApiFloatingSectionProvider(context);
		provider.Attach(CreateGenerator(context, versionIndexClient, reader));
		var section = provider.GetSections().Should().ContainSingle().Subject;
		_ = section.CreateRoot(null);
		section.ToLinkSection().Should().BeNull("nothing is resolved yet");

		await section.ResolveAsync(TestContext.Current!.Execution.CancellationToken);
		var links = section.ToLinkSection()!;

		links.Versions.Select(v => v.Name).Should().BeEquivalentTo(["main", "9", "8"]);
		links
			.Versions
			.ToDictionary(v => v.Name, v => v.Root)
			.Should()
			.BeEquivalentTo(
				new Dictionary<string, string>
				{
					["main"] = "api/doc/elasticsearch",
					["9"] = "api/doc/elasticsearch/v9",
					["8"] = "api/doc/elasticsearch/v8"
				},
				"the API Explorer states where each version lives"
			);
		links.Versions.Where(v => !v.Hidden).Select(v => v.Name).Should().Equal("main");
		links.Versions.Where(v => v.Current).Select(v => v.Name).Should().Equal("main");
		var all = (1UL << links.Versions.Length) - 1;
		links.Pages.Should().Contain(new KeyValuePair<string, ulong>("", all), "every version has a landing page");
		links.Pages.Should().Contain(new KeyValuePair<string, ulong>("operation/operation-ping", all), "one entry, not one per version");
		links.TryResolvePage("8", "operation/operation-ping", out var path).Should().BeTrue();
		path.Should().Be("api/doc/elasticsearch/v8/operation/operation-ping");
		links.TryResolvePage(null, "operation/operation-ping", out var currentPath).Should().BeTrue();
		currentPath.Should().Be("api/doc/elasticsearch/operation/operation-ping");
		links.TryResolvePage("main", "operation/operation-missing", out _).Should().BeFalse();
	}

	[Test]
	public async Task FloatingSection_WithoutAGenerator_StaysEmptyAndHidden()
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-explorer-output-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(collector, stack, ProductsFor(product), outputRoot, GitForElasticsearch());
		var provider = new ApiFloatingSectionProvider(context);
		var section = provider.GetSections().Should().ContainSingle().Subject;
		var root = section.CreateRoot(null);

		await section.ResolveAsync(TestContext.Current!.Execution.CancellationToken);

		root.Hidden.Should().BeTrue("a build that skips the API Explorer has no pages to show");
		root.NavigationItems.Should().BeEmpty();
	}

	[Test]
	public async Task FloatingSection_WithoutAMainDocument_IsHidden()
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-explorer-output-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(collector, stack, ProductsFor(product), outputRoot, GitForElasticsearch());
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var reader = CreateVersionedReader(
			missingVersionsAreUnreadable: true,
			("9.4", SpecDocument("Elasticsearch 9")),
			("8.19", SpecDocument("Elasticsearch 8"))
		);
		var generator = CreateGenerator(context, versionIndexClient, reader);
		var provider = new ApiFloatingSectionProvider(context);
		provider.Attach(generator);
		var section = provider.GetSections().Should().ContainSingle().Subject;
		var root = section.CreateRoot(null);

		await section.ResolveAsync(TestContext.Current!.Execution.CancellationToken);

		var trees = await generator.TreesFor(
			"elasticsearch",
			context.Configuration.ApiConfigurations!["elasticsearch"],
			TestContext.Current!.Execution.CancellationToken
		);
		trees.Resolved.Documents.Select(d => d.Version.Moniker).Should().BeEquivalentTo(["9", "8"]);
		root.Hidden.Should().BeTrue("no main document renders no page at the unversioned URL");
	}

	[Test]
	public void FloatingSection_OffersOneSectionPerDeclaredApiAndMountsImplicitly()
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-explorer-output-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(collector, stack, ProductsFor(product), outputRoot, GitForElasticsearch());
		var provider = new ApiFloatingSectionProvider(context);

		provider.GetSections().Select(s => s.Key).Should().Equal("elasticsearch");
		provider.MountsImplicitly.Should().BeTrue("a docset that declares an API has it in its tree without a toc entry");
		provider.HubPath.Should().Be("api");
	}

	[Test]
	public async Task FloatingSection_HiddenRoot_StaysHiddenAfterItIsFilled()
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-explorer-output-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(collector, stack, ProductsFor(product), outputRoot, GitForElasticsearch());
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var reader = CreateVersionedReader(
			("main", SpecDocument("Elasticsearch main")),
			("9.4", SpecDocument("Elasticsearch 9")),
			("8.19", SpecDocument("Elasticsearch 8"))
		);
		var provider = new ApiFloatingSectionProvider(context);
		provider.Attach(CreateGenerator(context, versionIndexClient, reader));
		var section = provider.GetSections().Should().ContainSingle().Subject;
		var hidden = section.CreateRoot(null, hidden: true);
		var visible = provider.GetSections().Single().CreateRoot(null);

		await section.ResolveAsync(TestContext.Current!.Execution.CancellationToken);

		hidden.NavigationItems.Should().NotBeEmpty("a hidden root is still filled, so its pages are in links.json");
		hidden.Hidden.Should().BeTrue("a docset's sidebar does not change because it declares an API");
		visible.Hidden.Should().BeFalse();
	}

	[Test]
	public async Task Generate_WritesDistinctOutputTreesForMainAndReleasedMajors()
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-explorer-output-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(collector, stack, ProductsFor(product), outputRoot, GitForElasticsearch());
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var reader = CreateVersionedReader(
			("main", SpecDocument("Elasticsearch main")),
			("9.4", SpecDocument("Elasticsearch 9")),
			("8.19", SpecDocument("Elasticsearch 8"))
		);
		var generator = CreateGenerator(context, versionIndexClient, reader);

		await generator.Generate(TestContext.Current!.Execution.CancellationToken);

		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch", "index.html")).Should().BeTrue();
		context
			.WriteFileSystem
			.File
			.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch", "authentication", "index.html"))
			.Should()
			.BeFalse();
		context
			.WriteFileSystem
			.File
			.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch", "servers", "index.html"))
			.Should()
			.BeFalse();
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch", "v9", "index.html")).Should().BeTrue();
		context
			.WriteFileSystem
			.File
			.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch", "v9", "authentication", "index.html"))
			.Should()
			.BeFalse();
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch", "v8", "index.html")).Should().BeTrue();
		context
			.WriteFileSystem
			.File
			.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch", "v8", "authentication", "index.html"))
			.Should()
			.BeFalse();
		context
			.WriteFileSystem
			.File
			.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch", "v8", "servers", "index.html"))
			.Should()
			.BeFalse();
		context
			.WriteFileSystem
			.File
			.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch", "v8", "operation", "operation-ping", "index.html"))
			.Should()
			.BeTrue();
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch.md")).Should().BeTrue();
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch", "v9.md")).Should().BeTrue();
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch", "v8.md")).Should().BeTrue();
		context
			.WriteFileSystem
			.File
			.Exists(Path.Join(outputRoot, "api", "doc", "elasticsearch", "v8", "operation", "operation-ping.md"))
			.Should()
			.BeTrue();
	}

	[Test]
	public async Task Generate_VersionPickerKeepsTheSamePageWhenItExists()
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-explorer-output-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(collector, stack, ProductsFor(product), outputRoot, GitForElasticsearch());
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var mainDocument = SpecDocument("Elasticsearch main");
		mainDocument.Paths["/newop"] = new OpenApiPathItem
		{
			Operations = new Dictionary<HttpMethod, OpenApiOperation>
			{
				[HttpMethod.Get] = new()
				{
					OperationId = "newop",
					Tags = new HashSet<OpenApiTagReference> { new("core") },
					Responses = new OpenApiResponses { ["200"] = new OpenApiResponse { Description = "ok" } }
				}
			}
		};
		var reader = CreateVersionedReader(
			("main", mainDocument),
			("9.4", SpecDocument("Elasticsearch 9")),
			("8.19", SpecDocument("Elasticsearch 8"))
		);
		var generator = CreateGenerator(context, versionIndexClient, reader);

		await generator.Generate(TestContext.Current!.Execution.CancellationToken);

		var shared = context
			.WriteFileSystem
			.File
			.ReadAllText(Path.Join(outputRoot, "api", "doc", "elasticsearch", "operation", "operation-ping", "index.html"));
		shared.Should().Contain("href=\"/api/doc/elasticsearch/v8/operation/operation-ping\"");
		shared.Should().Contain("href=\"/api/doc/elasticsearch/v9/operation/operation-ping\"");
		var mainOnly = context
			.WriteFileSystem
			.File
			.ReadAllText(Path.Join(outputRoot, "api", "doc", "elasticsearch", "operation", "operation-newop", "index.html"));
		mainOnly.Should().Contain("href=\"/api/doc/elasticsearch/v8\"");
		mainOnly.Should().NotContain("/api/doc/elasticsearch/v8/operation/operation-newop");
		var older = context
			.WriteFileSystem
			.File
			.ReadAllText(Path.Join(outputRoot, "api", "doc", "elasticsearch", "v8", "operation", "operation-ping", "index.html"));
		older.Should().Contain("href=\"/api/doc/elasticsearch/operation/operation-ping\"");
	}

	[Test]
	public async Task Generate_VersionPickerMatchesAChildPageWithTheTopicItReplaces()
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-explorer-output-{Guid.NewGuid():N}");
		// spaces.v9.md replaces the Kibana spaces topic in main and v9; v8 still builds the topic page.
		var context = CreateGenerateContext(
			collector,
			stack,
			ProductsFor(product),
			outputRoot,
			GitForElasticsearch(),
			("spaces.v9.md", "Kibana spaces")
		);
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var reader = CreateVersionedReader(
			("main", WithTopic(SpecDocument("Elasticsearch main"), "Kibana spaces")),
			("9.4", WithTopic(SpecDocument("Elasticsearch 9"), "Kibana spaces")),
			("8.19", WithTopic(SpecDocument("Elasticsearch 8"), "Kibana spaces"))
		);
		var generator = CreateGenerator(context, versionIndexClient, reader);

		await generator.Generate(TestContext.Current!.Execution.CancellationToken);

		var files = context.WriteFileSystem.File;
		var root = Path.Join(outputRoot, "api", "doc", "elasticsearch");
		files.Exists(Path.Join(root, "v9", "topic", "topic-kibana-spaces", "index.html")).Should().BeFalse();
		var topic = files.ReadAllText(Path.Join(root, "v8", "topic", "topic-kibana-spaces", "index.html"));
		topic.Should().Contain("href=\"/api/doc/elasticsearch/v9/spaces\"");
		var child = files.ReadAllText(Path.Join(root, "v9", "spaces", "index.html"));
		child.Should().Contain("href=\"/api/doc/elasticsearch/v8/topic/topic-kibana-spaces\"");
	}

	[Test]
	public async Task Generate_VersionPickerMatchesAChildPageWithTheAuthenticationPageItReplaces()
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-explorer-output-{Guid.NewGuid():N}");
		// The specs declare no security schemes, so v8 builds /authentication from the topic and v9 has only the child page.
		var context = CreateGenerateContext(
			collector,
			stack,
			ProductsFor(product),
			outputRoot,
			GitForElasticsearch(),
			("auth.v9.md", "Authentication")
		);
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var reader = CreateVersionedReader(
			("main", WithTopic(SpecDocument("Elasticsearch main"), "Authentication")),
			("9.4", WithTopic(SpecDocument("Elasticsearch 9"), "Authentication")),
			("8.19", WithTopic(SpecDocument("Elasticsearch 8"), "Authentication"))
		);
		var generator = CreateGenerator(context, versionIndexClient, reader);

		await generator.Generate(TestContext.Current!.Execution.CancellationToken);

		var files = context.WriteFileSystem.File;
		var root = Path.Join(outputRoot, "api", "doc", "elasticsearch");
		files.Exists(Path.Join(root, "v9", "authentication", "index.html")).Should().BeFalse();
		var authentication = files.ReadAllText(Path.Join(root, "v8", "authentication", "index.html"));
		authentication.Should().Contain("href=\"/api/doc/elasticsearch/v9/auth\"");
		var child = files.ReadAllText(Path.Join(root, "v9", "auth", "index.html"));
		child.Should().Contain("href=\"/api/doc/elasticsearch/v8/authentication\"");
	}

	[Test]
	public async Task Generate_EmptyMetaDescriptions_AreOnlyPagesWithNoSourceSentence()
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-explorer-output-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(collector, stack, ProductsFor(product), outputRoot, GitForElasticsearch());
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var reader = CreateVersionedReader(
			("main", SeoSample("Elasticsearch main", "Search and analyze data.")),
			("9.4", WithTopic(SeoSample("Elasticsearch 9", null), "Authentication")),
			("8.19", SeoSample("Elasticsearch 8", null))
		);

		await CreateGenerator(context, versionIndexClient, reader).Generate(TestContext.Current!.Execution.CancellationToken);

		var pages = ReadMetaDescriptions(context.WriteFileSystem, outputRoot);
		EmptyDescriptionPages(pages)
			.Should()
			.Equal(
				"/api/doc/elasticsearch/authentication",
				"/api/doc/elasticsearch/group/endpoint-bare",
				"/api/doc/elasticsearch/operation/operation-bare",
				"/api/doc/elasticsearch/servers",
				"/api/doc/elasticsearch/v8/authentication",
				"/api/doc/elasticsearch/v8/group/endpoint-bare",
				"/api/doc/elasticsearch/v8/operation/operation-bare",
				"/api/doc/elasticsearch/v8/servers",
				"/api/doc/elasticsearch/v9/group/endpoint-bare",
				"/api/doc/elasticsearch/v9/operation/operation-bare",
				"/api/doc/elasticsearch/v9/servers"
			);
		pages["/api/doc/elasticsearch/v9"]
			.Should()
			.Be("Search and analyze data.", "a landing with no description of its own takes the canonical one");
		pages["/api/doc/elasticsearch/v8"].Should().Be("Search and analyze data.");
		pages["/api/doc/elasticsearch/group/endpoint-described"].Should().Be("The described APIs manage documents.");
		pages["/api/doc/elasticsearch/operation/operation-summarized"].Should().Be("Summarize things");
		pages["/api/doc/elasticsearch/v9/authentication"].Should().Be("Topic text.");

		var markdown = ReadMarkdownDescriptions(context.WriteFileSystem, outputRoot);
		markdown["/api/doc/elasticsearch"].Should().Be("Search and analyze data.");
		markdown["/api/doc/elasticsearch/v8"].Should().Be("Search and analyze data.");
		markdown["/api/doc/elasticsearch/group/endpoint-described"].Should().Be("The described APIs manage documents.");
		markdown["/api/doc/elasticsearch/operation/operation-summarized"].Should().Be("Summarize things");
		markdown["/api/doc/elasticsearch/v9/authentication"].Should().Be("Topic text.");
		markdown["/api/doc/elasticsearch/authentication"].Should().BeEmpty();
		foreach (var (url, description) in markdown.Where(page => pages.ContainsKey(page.Key)))
			description.Should().Be(pages[url], $"the Markdown of {url} carries the same description as its HTML");
	}

	/// <summary>Front matter description per page URL, read from every generated Markdown file. A page without one reads as empty.</summary>
	private static IReadOnlyDictionary<string, string> ReadMarkdownDescriptions(IFileSystem fileSystem, string outputRoot)
	{
		var pages = new SortedDictionary<string, string>(StringComparer.Ordinal);
		foreach (var file in fileSystem.Directory.EnumerateFiles(outputRoot, "*.md", SearchOption.AllDirectories))
		{
			var line = fileSystem.File.ReadLines(file).FirstOrDefault(l => l.StartsWith("description: ", StringComparison.Ordinal));
			var url = "/" + fileSystem.Path.GetRelativePath(outputRoot, file).Replace('\\', '/')[..^".md".Length];
			pages[url] = line is null ? "" : JsonSerializer.Deserialize<string>(line["description: ".Length..]) ?? "";
		}

		return pages;
	}

	[GeneratedRegex("<meta name=\"description\" content=\"([^\"]*)\"")]
	private static partial Regex MetaDescription();

	/// <summary>Meta description per page URL, read from every generated page. The layout drops the tag when a page has none, which reads as empty.</summary>
	private static IReadOnlyDictionary<string, string> ReadMetaDescriptions(IFileSystem fileSystem, string outputRoot)
	{
		var pages = new SortedDictionary<string, string>(StringComparer.Ordinal);
		foreach (var file in fileSystem.Directory.EnumerateFiles(outputRoot, "index.html", SearchOption.AllDirectories))
		{
			var html = fileSystem.File.ReadAllText(file);
			if (!html.Contains("<meta charset", StringComparison.Ordinal))
				continue;

			var match = MetaDescription().Match(html);
			var url = "/" + fileSystem.Path.GetRelativePath(outputRoot, fileSystem.Path.GetDirectoryName(file)!).Replace('\\', '/');
			pages[url] = match.Success ? System.Net.WebUtility.HtmlDecode(match.Groups[1].Value).Trim() : "";
		}

		return pages;
	}

	private static string[] EmptyDescriptionPages(IReadOnlyDictionary<string, string> pages) =>
		[.. pages.Where(page => page.Value.Length == 0).Select(page => page.Key)];

	private static OpenApiDocument SeoSample(string title, string? description)
	{
		var document = SpecDocument(title);
		document.Info.Description = description;
		document.Servers = [new OpenApiServer { Url = "https://example.com" }];
		document.Components = new OpenApiComponents
		{
			SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
			{
				["apiKey"] = new OpenApiSecurityScheme
				{
					Type = SecuritySchemeType.ApiKey,
					Name = "Authorization",
					In = ParameterLocation.Header
				}
			}
		};
		document.Tags = new HashSet<OpenApiTag>
		{
			new() { Name = "described", Description = "## Overview\n\nThe described APIs manage documents.\n\nMore detail." },
			new() { Name = "bare" }
		};
		document.Paths = new OpenApiPaths
		{
			["/summarized"] = SeoPath("summarized", "described", "Summarize things"),
			["/bare"] = SeoPath("bare", "bare", null)
		};
		return document;
	}

	private static OpenApiPathItem SeoPath(string operationId, string tag, string? summary) =>
		new()
		{
			Operations = new Dictionary<HttpMethod, OpenApiOperation>
			{
				[HttpMethod.Get] = new()
				{
					OperationId = operationId,
					Summary = summary,
					Tags = new HashSet<OpenApiTagReference> { new(tag) },
					Responses = new OpenApiResponses { ["200"] = new OpenApiResponse { Description = "ok" } }
				}
			}
		};

	private static OpenApiDocument WithTopic(OpenApiDocument document, string title)
	{
		var topic = new JsonObject { ["title"] = title, ["content"] = "Topic text." };
		document.Extensions = new Dictionary<string, IOpenApiExtension> { ["x-topics"] = new JsonNodeExtension(new JsonArray(topic)) };
		return document;
	}

	private static BuildContext CreateGenerateContext(
		DiagnosticsCollector collector,
		VersionsConfiguration versionsConfiguration,
		ProductsConfiguration productsConfiguration,
		string outputRoot,
		GitCheckoutInformation git,
		(string File, string Heading)? child = null
	)
	{
		var repoRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-multi-version-test-{Guid.NewGuid():N}");
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
		if (child is { } page)
		{
			docsetYaml += $"\n      children:\n        - file: {page.File}\n";
			fs.AddFile(
				Path.Join(repoRoot, "docs", "api", "elasticsearch", page.File),
				new MockFileData($"# {page.Heading}\n\nChild text.\n")
			);
		}
		fs.AddFile(configPath, new MockFileData(docsetYaml));
		var configurationContext = TestHelpers.CreateConfigurationContext(fs, versionsConfiguration, productsConfiguration);

		return new BuildContext(
			collector,
			DocumentationFileSystem.Resolve(
				repoRoot,
				new DocumentationScopeOptions { ConfigurationFile = configPath, Output = outputRoot, Git = git, Inner = fs }
			),
			configurationContext
		);
	}

	private static OpenApiGenerator CreateGenerator(
		BuildContext context,
		VersionIndexClient versionIndexClient,
		IOpenApiSpecificationReader reader
	) => new(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance, versionIndexClient, reader);

	/// <summary>Returns the document whose version segment (<c>main</c>, <c>9.4</c>, <c>8.19</c>) is in the fetched spec path.</summary>
	private static IOpenApiSpecificationReader CreateVersionedReader(params (string Version, OpenApiDocument Document)[] documents) =>
		CreateVersionedReader(false, documents);

	/// <param name="missingVersionsAreUnreadable">Return no document for a version that has none, instead of throwing.</param>
	private static IOpenApiSpecificationReader CreateVersionedReader(
		bool missingVersionsAreUnreadable,
		params (string Version, OpenApiDocument Document)[] documents
	)
	{
		var reader = A.Fake<IOpenApiSpecificationReader>();
		A.CallTo(() => reader.ReadAsync(A<Stream>._, A<string>._, A<IDiagnosticsCollector?>._)).ReturnsLazily(call =>
		{
			using var body = new StreamReader(call.GetArgument<Stream>(0)!);
			var path = body.ReadToEnd();
			var matches = documents.Where(d => path.Contains($"/{d.Version}/", StringComparison.Ordinal)).ToArray();
			return Task.FromResult(missingVersionsAreUnreadable && matches.Length == 0 ? null : matches.Single().Document);
		});
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
					"""
				);
			}

			return SpecResponse(request);
		});

	private static GitCheckoutInformation GitForElasticsearch() =>
		new() { Branch = "main", Remote = "https://github.com/elastic/elasticsearch.git", Ref = "refs/heads/main" };

	private static HttpResponseMessage IndexResponse(string body) =>
		new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

	// The body carries the request path so a versioned reader can tell versions apart.
	private static HttpResponseMessage SpecResponse(HttpRequestMessage request) =>
		new(HttpStatusCode.OK)
		{
			Content = new StringContent(
				/*lang=json,strict*/
				$$$"""{"openapi":"3.1.0","info":{"title":"{{{request.RequestUri!.AbsolutePath}}}","version":"1.0"},"paths":{}}""",
				System.Text.Encoding.UTF8,
				"application/json"
			)
		};
}
