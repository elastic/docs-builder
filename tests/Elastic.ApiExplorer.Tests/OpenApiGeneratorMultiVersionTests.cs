// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
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
using Elastic.Documentation.Testing;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;
using Nullean.Snapshots;

namespace Elastic.ApiExplorer.Tests;

public class OpenApiGeneratorMultiVersionTests
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

		var resolved = await generator.ResolveDocumentsForProduct(
			"elasticsearch",
			ApiConfig(product),
			TestContext.Current!.Execution.CancellationToken
		);

		resolved
			.Documents
			.Select(d => $"{d.Document.Info.Title} at {string.Join(", ", resolved.Routes.Suffixes(d.Version.Moniker))}")
			.Should()
			.BeEquivalentTo(
				"Elasticsearch main at elasticsearch/next",
				"Elasticsearch 9 at elasticsearch, elasticsearch/v9",
				"Elasticsearch 8 at elasticsearch/v8"
			);
	}

	[Test]
	public async Task ResolveDocumentsForProduct_VersionNamedNext_ReportsAnErrorAndLeavesItOut()
	{
		var collector = new RecordingDiagnosticsCollector();
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var context = TestHelpers.CreateScopedBuildContext(collector, stack, ProductsFor(product), GitForElasticsearch());
		var handler = new StubHandler(
			request => request.RequestUri!.AbsolutePath.EndsWith("index.json", StringComparison.Ordinal)
				? IndexResponse(/*lang=json,strict*/
					"""
					{
						"elastic/elasticsearch": {
							"elasticsearch-openapi.json": {
								"main": { "version": "main" },
								"next": { "version": "next" },
								"9": { "version": "9.4" }
							}
						}
					}
					"""
				)
				: SpecResponse(request)
		);
		using var versionIndexClient = new VersionIndexClient(BaseUri, handler, sleep: (_, _) => Task.CompletedTask);
		var reader = CreateVersionedReader(("main", SpecDocument("Elasticsearch main")), ("9.4", SpecDocument("Elasticsearch 9")));
		var generator = CreateGenerator(context, versionIndexClient, reader);

		var resolved = await generator.ResolveDocumentsForProduct(
			"elasticsearch",
			ApiConfig(product),
			TestContext.Current!.Execution.CancellationToken
		);

		resolved.Documents.Select(d => d.Version.Moniker).Should().BeEquivalentTo(["main", "9"]);
		resolved.ReservedMonikers.Should().Equal("next");
		resolved.UnresolvedMonikers.Should().BeEmpty("the reserved version is left out on purpose, not lost");
		// the scoped docset fixture adds its own product errors, which this test is not about
		collector
			.Diagnostics
			.Where(d => d.Message.StartsWith("API 'elasticsearch'", StringComparison.Ordinal))
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error  API 'elasticsearch': the version index declares a version named 'next', which is the URL segment of the unreleased 'main' spec. Rename the version; it is not rendered."
			);
	}

	[Test]
	public async Task ResolveDocumentsForProduct_VersionlessProductDeclaringNext_ReportsTheError()
	{
		var collector = new RecordingDiagnosticsCollector();
		var versionless = TestHelpers.CreateVersionlessConfiguration();
		var product = TestHelpers.CreateProduct("cloud-serverless", versionless.GetVersioningSystem(VersioningSystemId.Serverless));
		var context = TestHelpers.CreateScopedBuildContext(collector, versionless, ProductsFor(product), GitForElasticsearch());
		var handler = new StubHandler(
			request => request.RequestUri!.AbsolutePath.EndsWith("index.json", StringComparison.Ordinal)
				? IndexResponse(/*lang=json,strict*/
					"""
					{
						"elastic/serverless-api-specification": {
							"elastic-cloud-serverless.yml": {
								"main": { "version": "main" },
								"next": { "version": "next" }
							}
						}
					}
					"""
				)
				: SpecResponse(request)
		);
		using var versionIndexClient = new VersionIndexClient(BaseUri, handler, sleep: (_, _) => Task.CompletedTask);
		var generator = CreateGenerator(context, versionIndexClient, CreateVersionedReader(("main", SpecDocument("Serverless main"))));

		var resolved = await generator.ResolveDocumentsForProduct(
			"cloud-serverless",
			ApiConfig(product, specFileName: "elastic-cloud-serverless.yml", repository: "elastic/serverless-api-specification"),
			TestContext.Current!.Execution.CancellationToken
		);

		resolved.Documents.Select(d => d.Version.Moniker).Should().Equal("main");
		collector
			.Diagnostics
			.Where(d => d.Message.StartsWith("API 'cloud-serverless'", StringComparison.Ordinal))
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error  API 'cloud-serverless': the version index declares a version named 'next', which is the URL segment of the unreleased 'main' spec. Rename the version; it is not rendered."
			);
	}

	[Test]
	public async Task CatalogEntry_MultiMajorIndex_OpensTheVersionlessUrlTitledByTheNewestMajor()
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
		entry!.Url.Should().Be("/api/elasticsearch");
		entry.Title.Should().Be("Elasticsearch 9");
	}

	[Test]
	public async Task CatalogEntry_NewestMajorFailsToLoad_LinksToATreeThatRendered()
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var context = TestHelpers.CreateScopedBuildContext(collector, stack, ProductsFor(product), GitForElasticsearch());
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var reader = CreateVersionedReader(
			missingVersionsAreUnreadable: true,
			("main", SpecDocument("Elasticsearch main")),
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
		entry!
			.Url
			.Should()
			.Be("/api/elasticsearch/next", "nothing renders at /api/elasticsearch, so the card opens the first tree that did");
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
		entry!.Url.Should().Be("/api/cloud-serverless");
		entry.Title.Should().Be("Serverless main");
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

		var resolved = await generator.ResolveDocumentsForProduct(
			"cloud-serverless",
			apiConfig,
			TestContext.Current!.Execution.CancellationToken
		);

		resolved.Documents.Should().ContainSingle().Which.Version.Moniker.Should().Be("main");
		resolved.Routes.Suffixes("main").Should().Equal("cloud-serverless");
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

		navigation.Url.Should().Be("/api/elasticsearch/v8");
		var operation = navigation.Tags().Single().NavigationItems.OfType<OperationNavigationItem>().Single();
		operation.Url.Should().Be("/api/elasticsearch/v8/operation/operation-ping");
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
		root.Url.Should().Be("/api/elasticsearch");
		root.NavigationItems.Should().BeEmpty("the root exists before the specs are fetched");

		await section.ResolveAsync(TestContext.Current!.Execution.CancellationToken);

		var versions = root.NavigationItems.Should().AllBeOfType<LandingNavigationItem>().Subject.ToArray();
		versions
			.Select(v => v.Url)
			.Should()
			.BeEquivalentTo(["/api/elasticsearch/next", "/api/elasticsearch", "/api/elasticsearch/v9", "/api/elasticsearch/v8"]);
		versions.Where(v => !v.Hidden).Select(v => v.Url).Should().Equal("/api/elasticsearch");
		root.NavigationTitle.Should().Be("Elasticsearch 9", "the versionless tree titles the API");

		// the generator renders from the trees the section mounted, so the build never builds a tree twice
		var trees = await generator.TreesFor(
			"elasticsearch",
			context.Configuration.ApiConfigurations!["elasticsearch"],
			TestContext.Current!.Execution.CancellationToken
		);
		trees.Trees.Select(t => t.Navigation).Should().Equal(versions);
		await generator.Generate(TestContext.Current!.Execution.CancellationToken);
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "elasticsearch", "v8", "index.html")).Should().BeTrue();
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

		NavigationTreePrinter.Print(root, maxDepth: 4).Should().MatchSnapshot();
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
					["main"] = "api/elasticsearch/next",
					["9"] = "api/elasticsearch",
					["8"] = "api/elasticsearch/v8"
				},
				"the API Explorer states where each version lives"
			);
		links.Versions.Where(v => !v.Hidden).Select(v => v.Name).Should().Equal("9");
		links.Versions.Where(v => v.Current).Select(v => v.Name).Should().Equal("9");
		links.Versions.Single(v => v.Name == "main").Hidden.Should().BeTrue();
		links.Versions.Select(v => $"{v.Name} {v.MirrorRoot}").Should().Equal("main ", "9 api/elasticsearch/v9", "8 ");
		links.ContainsSitePath("api/elasticsearch/v9/operation/operation-ping").Should().BeTrue("the numbered copy of 9 is a page too");
		links.ContainsSitePath("api/elasticsearch/next/operation/operation-ping").Should().BeTrue();
		var all = (1UL << links.Versions.Length) - 1;
		links.Pages.Should().Contain(new KeyValuePair<string, ulong>("", all), "every version has a landing page");
		links.Pages.Should().Contain(new KeyValuePair<string, ulong>("operation/operation-ping", all), "one entry, not one per version");
		links.TryResolvePage("8", "operation/operation-ping", out var path).Should().BeTrue();
		path.Should().Be("api/elasticsearch/v8/operation/operation-ping");
		links.TryResolvePage(null, "operation/operation-ping", out var currentPath).Should().BeTrue();
		currentPath.Should().Be("api/elasticsearch/operation/operation-ping");
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
	public async Task FloatingSection_WithoutAMainDocument_ShowsTheNewestMajor()
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
		root.Hidden.Should().BeFalse("the newest major owns the unversioned URL");
	}

	[Test]
	public async Task FloatingSection_NewestMajorFailsToLoad_IsHiddenAndNoOlderMajorTakesItsUrl()
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-explorer-output-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(collector, stack, ProductsFor(product), outputRoot, GitForElasticsearch());
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var reader = CreateVersionedReader(
			missingVersionsAreUnreadable: true,
			("main", SpecDocument("Elasticsearch main")),
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
		trees.Trees.Select(t => t.Suffix).Should().BeEquivalentTo(["elasticsearch/next", "elasticsearch/v8"]);
		root.Hidden.Should().BeTrue("the declared newest major did not load, so nothing renders at the unversioned URL");

		var links = section.ToLinkSection()!;
		links
			.Versions
			.Select(v => (v.Name, v.Root, v.Current))
			.Should()
			.BeEquivalentTo(
				[("main", "api/elasticsearch/next", false), ("8", "api/elasticsearch/v8", false)],
				"the versions that rendered stay linkable, and none is current"
			);
		links.TryResolvePage("8", "operation/operation-ping", out var path).Should().BeTrue();
		path.Should().Be("api/elasticsearch/v8/operation/operation-ping");
		links.TryResolvePage(null, "operation/operation-ping", out _).Should().BeFalse("no version owns the unversioned URL");
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

		var files = context.WriteFileSystem.File;
		// the catalog titles every page's API switcher with the newest major, so only the absence of main's title tells the trees apart
		files.ReadAllText(Path.Join(outputRoot, "api", "elasticsearch", "index.html")).Should().NotContain("Elasticsearch main");
		files.ReadAllText(Path.Join(outputRoot, "api", "elasticsearch", "v9", "index.html")).Should().NotContain("Elasticsearch main");
		files.ReadAllText(Path.Join(outputRoot, "api", "elasticsearch", "next", "index.html")).Should().Contain("Elasticsearch main");
		files.Exists(Path.Join(outputRoot, "api", "elasticsearch", "operation", "operation-ping", "index.html")).Should().BeTrue();
		files.Exists(Path.Join(outputRoot, "api", "elasticsearch", "v9", "operation", "operation-ping", "index.html")).Should().BeTrue();
		files.Exists(Path.Join(outputRoot, "api", "elasticsearch", "next", "operation", "operation-ping", "index.html")).Should().BeTrue();
		context
			.WriteFileSystem
			.File
			.Exists(Path.Join(outputRoot, "api", "elasticsearch", "authentication", "index.html"))
			.Should()
			.BeFalse();
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "elasticsearch", "servers", "index.html")).Should().BeFalse();
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "elasticsearch", "v9", "index.html")).Should().BeTrue();
		context
			.WriteFileSystem
			.File
			.Exists(Path.Join(outputRoot, "api", "elasticsearch", "v9", "authentication", "index.html"))
			.Should()
			.BeFalse();
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "elasticsearch", "v8", "index.html")).Should().BeTrue();
		context
			.WriteFileSystem
			.File
			.Exists(Path.Join(outputRoot, "api", "elasticsearch", "v8", "authentication", "index.html"))
			.Should()
			.BeFalse();
		context
			.WriteFileSystem
			.File
			.Exists(Path.Join(outputRoot, "api", "elasticsearch", "v8", "servers", "index.html"))
			.Should()
			.BeFalse();
		context
			.WriteFileSystem
			.File
			.Exists(Path.Join(outputRoot, "api", "elasticsearch", "v8", "operation", "operation-ping", "index.html"))
			.Should()
			.BeTrue();
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "elasticsearch.md")).Should().BeTrue();
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "elasticsearch", "v9.md")).Should().BeTrue();
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "elasticsearch", "next.md")).Should().BeTrue();
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "elasticsearch", "v8.md")).Should().BeTrue();
		context
			.WriteFileSystem
			.File
			.Exists(Path.Join(outputRoot, "api", "elasticsearch", "v8", "operation", "operation-ping.md"))
			.Should()
			.BeTrue();
	}

	[Test]
	[Arguments(true)]
	[Arguments(false)]
	public async Task Generate_EachTree_RendersTheCanonicalAndRobotsOfItsPageType(bool allowIndexing)
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-explorer-output-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(
			collector,
			stack,
			ProductsFor(product),
			outputRoot,
			GitForElasticsearch(),
			allowIndexing: allowIndexing
		);
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var reader = CreateVersionedReader(
			("main", SpecDocument("Elasticsearch main")),
			("9.4", SpecDocument("Elasticsearch 9")),
			("8.19", SpecDocument("Elasticsearch 8"))
		);

		await CreateGenerator(context, versionIndexClient, reader).Generate(TestContext.Current!.Execution.CancellationToken);

		static string Attribute(string html, string prefix) =>
			Regex.Match(html, Regex.Escape(prefix) + "\"([^\"]*)\"", RegexOptions.None, RegexTimeouts.MatchTimeout).Groups[1].Value;

		string Head(params string[] segments)
		{
			var html = context
				.WriteFileSystem
				.File
				.ReadAllText(Path.Join([outputRoot, "api", "elasticsearch", .. segments, "operation", "operation-ping", "index.html"]));
			return $"canonical={Attribute(html, "<link rel=\"canonical\" href=")} robots={Attribute(html, "<meta name=\"robots\" content=")}";
		}

		// the Markdown export names the same canonical as the HTML page, so the numbered copy never names itself
		string MarkdownUrl(params string[] segments) =>
			context.WriteFileSystem.File.ReadAllLines(Path.Join([
				outputRoot,
				"api",
				"elasticsearch",
				.. segments,
				"operation",
				"operation-ping.md"
			])).First(static l => l.StartsWith("url: ", StringComparison.Ordinal))["url: ".Length..];

		var heads =
			$"versionless: {Head()}\nv9: {Head("v9")}\nv8: {Head("v8")}\nnext: {Head("next")}\nmarkdown v9: {MarkdownUrl("v9")}\nmarkdown versionless: {MarkdownUrl()}";
		if (allowIndexing)
		{
			heads.Should().MatchInlineSnapshot(
				"""
				versionless: canonical=https://www.elastic.co/api/elasticsearch/operation/operation-ping robots=index, follow
				v9: canonical=https://www.elastic.co/api/elasticsearch/operation/operation-ping robots=index, follow
				v8: canonical=https://www.elastic.co/api/elasticsearch/v8/operation/operation-ping robots=index, follow
				next: canonical=https://www.elastic.co/api/elasticsearch/next/operation/operation-ping robots=noindex
				markdown v9: https://www.elastic.co/api/elasticsearch/operation/operation-ping
				markdown versionless: https://www.elastic.co/api/elasticsearch/operation/operation-ping
				"""
			);
		}
		else
		{
			// the build's own noindex wins on every page, and the canonical still follows the page type, as on staging and edge
			heads.Should().MatchInlineSnapshot(
				"""
				versionless: canonical=https://www.elastic.co/api/elasticsearch/operation/operation-ping robots=noindex, nofollow
				v9: canonical=https://www.elastic.co/api/elasticsearch/operation/operation-ping robots=noindex, nofollow
				v8: canonical=https://www.elastic.co/api/elasticsearch/v8/operation/operation-ping robots=noindex, nofollow
				next: canonical=https://www.elastic.co/api/elasticsearch/next/operation/operation-ping robots=noindex, nofollow
				markdown v9: https://www.elastic.co/api/elasticsearch/operation/operation-ping
				markdown versionless: https://www.elastic.co/api/elasticsearch/operation/operation-ping
				"""
			);
		}
	}

	[Test]
	public async Task Generate_EachTree_RendersTheVersionNoticeOfItsPageType()
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

		await CreateGenerator(context, versionIndexClient, reader).Generate(TestContext.Current!.Execution.CancellationToken);

		string Notice(params string[] segments) => ReadNotice(context, outputRoot, segments);

		string Badges(params string[] segments) => ReadBadges(context, outputRoot, segments);

		string[] ping = ["operation", "operation-ping"];
		var notices = string.Join(
			"\n",
			$"versionless badges: {Badges(ping)}",
			$"v9 badges: {Badges(["v9", .. ping])}",
			$"v8 badges: {Badges(["v8", .. ping])}",
			$"next badges: {Badges(["next", .. ping])}",
			$"versionless: {Notice(ping)}",
			$"v9: {Notice(["v9", .. ping])}",
			$"v8: {Notice(["v8", .. ping])}",
			$"next: {Notice(["next", .. ping])}",
			$"versionless landing: {Notice()}",
			$"v8 landing: {Notice("v8")}",
			$"next landing: {Notice("next")}"
		);

		notices.Should().MatchInlineSnapshot(
			"""
			versionless badges: none
			v9 badges: none
			v8 badges: <span class="api-title-badges"><span class="applies applies-inline"><span class="applicable-info"><span class="applicable-name">Version 8</span></span></span><span class="applies applies-inline"><a class="applicable-info" href="/api/elasticsearch/operation/operation-ping"><span class="applicable-name">Latest: v9</span></a></span></span></header>
			next badges: none
			versionless: none
			v9: none
			v8: none
			next: <p class="api-version-notice api-version-notice--unreleased" role="note"><span class="applies applies-inline"><span class="applicable-info api-badge--warning"><span class="applicable-name">Unreleased</span></span></span><span>This version is not released yet and can change.</span><a href="/api/elasticsearch/operation/operation-ping"><span>View the latest version (v9)</span><svg class="api-version-notice__arrow" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M17.25 8.25 21 12m0 0-3.75 3.75M21 12H3"/></svg></a></p>
			versionless landing: none
			v8 landing: <p class="api-version-notice api-version-notice--older" role="note"><span class="applies applies-inline"><span class="applicable-info"><span class="applicable-name">Version 8</span></span></span><a href="/api/elasticsearch"><span>View the latest version (v9)</span><svg class="api-version-notice__arrow" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M17.25 8.25 21 12m0 0-3.75 3.75M21 12H3"/></svg></a></p>
			next landing: <p class="api-version-notice api-version-notice--unreleased" role="note"><span class="applies applies-inline"><span class="applicable-info api-badge--warning"><span class="applicable-name">Unreleased</span></span></span><span>This version is not released yet and can change.</span><a href="/api/elasticsearch"><span>View the latest version (v9)</span><svg class="api-version-notice__arrow" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M17.25 8.25 21 12m0 0-3.75 3.75M21 12H3"/></svg></a></p>
			"""
		);
	}

	[Test]
	public async Task Generate_NewestMajorFailsToLoad_RendersTheNoticesWithoutALatestVersionLink()
	{
		var collector = new DiagnosticsCollector([]);
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-explorer-output-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(collector, stack, ProductsFor(product), outputRoot, GitForElasticsearch());
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var reader = CreateVersionedReader(
			missingVersionsAreUnreadable: true,
			("main", SpecDocument("Elasticsearch main")),
			("8.19", SpecDocument("Elasticsearch 8"))
		);

		await CreateGenerator(context, versionIndexClient, reader).Generate(TestContext.Current!.Execution.CancellationToken);

		string[] ping = ["operation", "operation-ping"];
		var notices = string.Join(
			"\n",
			$"v8 notice: {ReadNotice(context, outputRoot, ["v8", .. ping])}",
			$"v8 badges: {ReadBadges(context, outputRoot, ["v8", .. ping])}",
			$"next notice: {ReadNotice(context, outputRoot, ["next", .. ping])}",
			$"v8 landing notice: {ReadNotice(context, outputRoot, ["v8"])}"
		);

		notices.Should().MatchInlineSnapshot(
			"""
			v8 notice: none
			v8 badges: <span class="api-title-badges"><span class="applies applies-inline"><span class="applicable-info"><span class="applicable-name">Version 8</span></span></span></span></header>
			next notice: <p class="api-version-notice api-version-notice--unreleased" role="note"><span class="applies applies-inline"><span class="applicable-info api-badge--warning"><span class="applicable-name">Unreleased</span></span></span><span>This version is not released yet and can change.</span></p>
			v8 landing notice: <p class="api-version-notice api-version-notice--older" role="note"><span class="applies applies-inline"><span class="applicable-info"><span class="applicable-name">Version 8</span></span></span></p>
			"""
		);
	}

	[Test]
	public async Task Generate_SupplementalFileForTheNewestMajor_IsReportedOnceForBothCopies()
	{
		var collector = new RecordingDiagnosticsCollector();
		var stack = TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9);
		var product = TestHelpers.CreateProduct("elasticsearch", stack.GetVersioningSystem(VersioningSystemId.Stack));
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-explorer-output-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(collector, stack, ProductsFor(product), outputRoot, GitForElasticsearch());
		var apiFolder = Path.Join(context.DocumentationSourceDirectory.FullName, "api", "elasticsearch");
		_ = context.ReadFileSystem.Directory.CreateDirectory(apiFolder);
		context.ReadFileSystem.File.WriteAllText(Path.Join(apiFolder, "op-missing.v9.md"), "# Missing\n");
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var reader = CreateVersionedReader(
			("main", SpecDocument("Elasticsearch main")),
			("9.4", SpecDocument("Elasticsearch 9")),
			("8.19", SpecDocument("Elasticsearch 8"))
		);

		await CreateGenerator(context, versionIndexClient, reader).Generate(TestContext.Current!.Execution.CancellationToken);

		collector
			.Diagnostics
			.Where(d => d.Message.Contains("op-missing", StringComparison.Ordinal))
			.Select(d => d.Message)
			.Should()
			.ContainSingle("the /v9 copy reads the same file and does not report it again")
			.Which
			.Should()
			.Be("API supplemental file 'op-missing.v9.md' does not match any operationId in version 9");
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

		var files = context.WriteFileSystem.File;
		var shared = files.ReadAllText(Path.Join(outputRoot, "api", "elasticsearch", "operation", "operation-ping", "index.html"));
		shared.Should().Contain("href=\"/api/elasticsearch/v8/operation/operation-ping\"");
		shared.Should().Contain("href=\"/api/elasticsearch/next/operation/operation-ping\"");
		var mainOnly = files.ReadAllText(
			Path.Join(outputRoot, "api", "elasticsearch", "next", "operation", "operation-newop", "index.html")
		);
		mainOnly.Should().Contain("href=\"/api/elasticsearch/v8\"");
		mainOnly.Should().Contain("href=\"/api/elasticsearch\"");
		mainOnly.Should().NotContain("/api/elasticsearch/v8/operation/operation-newop");
		var older = files.ReadAllText(Path.Join(outputRoot, "api", "elasticsearch", "v8", "operation", "operation-ping", "index.html"));
		older.Should().Contain("href=\"/api/elasticsearch/operation/operation-ping\"");
		// the numbered copy of the newest major keeps its reader on its own URLs
		var numbered = files.ReadAllText(Path.Join(outputRoot, "api", "elasticsearch", "v9", "operation", "operation-ping", "index.html"));
		numbered.Should().Contain("href=\"/api/elasticsearch/v9/operation/operation-ping\"");
		numbered.Should().Contain("href=\"/api/elasticsearch/next/operation/operation-ping\"");
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
		var root = Path.Join(outputRoot, "api", "elasticsearch");
		files.Exists(Path.Join(root, "v9", "topic", "topic-kibana-spaces", "index.html")).Should().BeFalse();
		var topic = files.ReadAllText(Path.Join(root, "v8", "topic", "topic-kibana-spaces", "index.html"));
		topic.Should().Contain("href=\"/api/elasticsearch/spaces\"");
		var child = files.ReadAllText(Path.Join(root, "v9", "spaces", "index.html"));
		child.Should().Contain("href=\"/api/elasticsearch/v8/topic/topic-kibana-spaces\"");
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
		var root = Path.Join(outputRoot, "api", "elasticsearch");
		files.Exists(Path.Join(root, "v9", "authentication", "index.html")).Should().BeFalse();
		var authentication = files.ReadAllText(Path.Join(root, "v8", "authentication", "index.html"));
		authentication.Should().Contain("href=\"/api/elasticsearch/auth\"");
		var child = files.ReadAllText(Path.Join(root, "v9", "auth", "index.html"));
		child.Should().Contain("href=\"/api/elasticsearch/v8/authentication\"");
	}

	private static OpenApiDocument WithTopic(OpenApiDocument document, string title)
	{
		var topic = new JsonObject { ["title"] = title, ["content"] = "Topic text." };
		document.Extensions = new Dictionary<string, IOpenApiExtension> { ["x-topics"] = new JsonNodeExtension(new JsonArray(topic)) };
		return document;
	}

	/// <summary>The version notice under the title of a generated page, on one line, or <c>none</c>.</summary>
	private static string ReadNotice(BuildContext context, string outputRoot, string[] segments) =>
		ReadFragment(context, outputRoot, segments, "<p class=\"api-version-notice.*?</p>");

	/// <summary>The badges row of a generated operation page, on one line, or <c>none</c>.</summary>
	private static string ReadBadges(BuildContext context, string outputRoot, string[] segments) =>
		ReadFragment(context, outputRoot, segments, "<span class=\"api-title-badges\">.*?</span>\\s*</header>");

	private static string ReadFragment(BuildContext context, string outputRoot, string[] segments, string pattern)
	{
		var html = context.WriteFileSystem.File.ReadAllText(Path.Join([outputRoot, "api", "elasticsearch", .. segments, "index.html"]));
		var fragment = Regex.Match(html, pattern, RegexOptions.Singleline, RegexTimeouts.MatchTimeout).Value;
		return fragment.Length == 0
			? "none"
			: Regex.Replace(fragment, @"\s+", " ", RegexOptions.None, RegexTimeouts.MatchTimeout).Replace(
				"> <",
				"><",
				StringComparison.Ordinal
			);
	}

	private static BuildContext CreateGenerateContext(
		DiagnosticsCollector collector,
		VersionsConfiguration versionsConfiguration,
		ProductsConfiguration productsConfiguration,
		string outputRoot,
		GitCheckoutInformation git,
		(string File, string Heading)? child = null,
		bool allowIndexing = false
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
		)
		{ AllowIndexing = allowIndexing, CanonicalBaseUrl = new Uri("https://www.elastic.co") };
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
