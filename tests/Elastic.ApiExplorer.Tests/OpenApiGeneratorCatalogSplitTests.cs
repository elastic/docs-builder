// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using AwesomeAssertions;
using Elastic.ApiExplorer.Export;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.ApiExplorer.Model;
using Elastic.Documentation;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Search.Contract;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;
using Nullean.ScopedFileSystem;
using Nullean.Snapshots.Html;

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
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "elasticsearch", "index.html")).Should().BeTrue();
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "index.html")).Should().BeFalse();
	}

	[Test]
	public async Task ResolveIndexedTrees_ConfiguredApi_ReturnsSpecUnderItsExplorerKey()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-catalog-split-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(outputRoot);
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var generator = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			context,
			NoopMarkdownStringRenderer.Instance,
			versionIndexClient,
			CreateSequentialReader(SpecDocument("Elasticsearch main"))
		);

		var trees = await generator.ResolveIndexedTrees(TestContext.Current!.Execution.CancellationToken);

		trees.Should().ContainSingle();
		trees[0].ApiKey.Should().Be("elasticsearch");
		trees[0].Moniker.Should().Be("main");
		trees[0].Document.Info!.Title.Should().Be("Elasticsearch main");
	}

	[Test]
	public async Task ResolveIndexedTrees_MainAndMajorDeclared_LeavesOutNext()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-catalog-split-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(outputRoot);
		var handler = new StubHandler(
			request => request.RequestUri!.AbsolutePath.EndsWith("index.json", StringComparison.Ordinal)
				? IndexResponse(MainAnd8Index)
				: SpecResponse()
		);
		using var versionIndexClient = new VersionIndexClient(BaseUri, handler, sleep: (_, _) => Task.CompletedTask);
		var generator = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			context,
			NoopMarkdownStringRenderer.Instance,
			versionIndexClient,
			CreateSequentialReader(SpecDocument("Elasticsearch main"), SpecDocument("Elasticsearch 8"))
		);

		var trees = await generator.ResolveIndexedTrees(TestContext.Current!.Execution.CancellationToken);

		trees.Select(t => $"{t.Moniker} at {t.Suffix}").Should().Equal("8 at elasticsearch");
	}

	[Test]
	public async Task ResolveIndexedTrees_DeclaredTreeFailsToLoad_FailsTheExport()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-catalog-split-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(outputRoot);
		var handler = new StubHandler(
			request =>
				request.RequestUri!.AbsolutePath switch
				{
					var path when path.EndsWith("index.json", StringComparison.Ordinal) => IndexResponse(MainAnd8Index),
					var path when path.Contains("/8.19/", StringComparison.Ordinal) => new HttpResponseMessage(HttpStatusCode.NotFound),
					_ => SpecResponse()
				}
		);
		using var versionIndexClient = new VersionIndexClient(BaseUri, handler, sleep: (_, _) => Task.CompletedTask);
		var reader = A.Fake<IOpenApiSpecificationReader>();
		A.CallTo(() => reader.ReadAsync(A<Stream>._, A<string>._, A<IDiagnosticsCollector?>._)).ReturnsLazily(
			_ => Task.FromResult<OpenApiDocument?>(SpecDocument("Elasticsearch main"))
		);
		var generator = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			context,
			NoopMarkdownStringRenderer.Instance,
			versionIndexClient,
			reader
		);

		var entries = await generator.GenerateProducts(ctx: TestContext.Current!.Execution.CancellationToken);
		var errorsAfterRendering = context.Collector.Errors;
		var export = () => generator.ResolveIndexedTrees(TestContext.Current!.Execution.CancellationToken);

		entries.Should().ContainSingle("pages still render the trees that loaded");
		errorsAfterRendering.Should().Be(0, "rendering skips the missing tree with a warning");
		(await export.Should().ThrowAsync<InvalidOperationException>()).WithMessage(
			"The API search export stops: API 'elasticsearch': version '8' is declared in the version index but could not be loaded."
		);
		context.Collector.Errors.Should().Be(1);
	}

	[Test]
	public async Task ResolveIndexedTrees_MainFailsToLoadWhileNineLoads_FailsTheExport()
	{
		var context = CreateGenerateContext(Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-catalog-split-{Guid.NewGuid():N}"));
		var handler = new StubHandler(
			request =>
				request.RequestUri!.AbsolutePath switch
				{
					var path when path.EndsWith("index.json", StringComparison.Ordinal) => IndexResponse(MainNineAnd8Index),
					var path when path.Contains("/main/", StringComparison.Ordinal) => new HttpResponseMessage(HttpStatusCode.NotFound),
					_ => SpecResponse()
				}
		);
		using var versionIndexClient = new VersionIndexClient(BaseUri, handler, sleep: (_, _) => Task.CompletedTask);
		var generator = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			context,
			NoopMarkdownStringRenderer.Instance,
			versionIndexClient,
			CreateSequentialReader(SpecDocument("Elasticsearch 9"), SpecDocument("Elasticsearch 8"))
		);

		var export = () => generator.ResolveIndexedTrees(TestContext.Current!.Execution.CancellationToken);

		(await export.Should().ThrowAsync<InvalidOperationException>()).WithMessage(
			"The API search export stops: API 'elasticsearch': version 'main' is declared in the version index but could not be loaded."
		);
	}

	[Test]
	public async Task ResolveIndexedTrees_VersionIndexUnavailable_FailsTheExport()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-catalog-split-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(outputRoot);
		var handler = new StubHandler(
			request => request.RequestUri!.AbsolutePath.EndsWith("index.json", StringComparison.Ordinal)
				? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
				: SpecResponse()
		);
		using var versionIndexClient = new VersionIndexClient(BaseUri, handler, sleep: (_, _) => Task.CompletedTask);
		var generator = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			context,
			NoopMarkdownStringRenderer.Instance,
			versionIndexClient,
			A.Fake<IOpenApiSpecificationReader>()
		);

		var export = () => generator.ResolveIndexedTrees(TestContext.Current!.Execution.CancellationToken);

		(await export.Should().ThrowAsync<InvalidOperationException>()).WithMessage(
			"The API search export stops: The API version index could not be fetched (*), so the declared version trees are unknown."
		);
		versionIndexClient.RootIndexFetchError.Should().NotBeNull();
	}

	[Test]
	public async Task ResolveIndexedTrees_VersionNamedNext_FailsTheExport()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-catalog-split-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(outputRoot);
		var handler = new StubHandler(
			request => request.RequestUri!.AbsolutePath.EndsWith("index.json", StringComparison.Ordinal)
				? IndexResponse(MainAndNextIndex)
				: SpecResponse()
		);
		using var versionIndexClient = new VersionIndexClient(BaseUri, handler, sleep: (_, _) => Task.CompletedTask);
		var reader = A.Fake<IOpenApiSpecificationReader>();
		A.CallTo(() => reader.ReadAsync(A<Stream>._, A<string>._, A<IDiagnosticsCollector?>._)).ReturnsLazily(
			_ => Task.FromResult<OpenApiDocument?>(SpecDocument("Elasticsearch main"))
		);
		var generator = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			context,
			NoopMarkdownStringRenderer.Instance,
			versionIndexClient,
			reader
		);

		var export = () => generator.ResolveIndexedTrees(TestContext.Current!.Execution.CancellationToken);

		(await export.Should().ThrowAsync<InvalidOperationException>()).WithMessage(
			"The API search export stops: API 'elasticsearch': version 'next' uses a reserved name, so it is not rendered."
		);
	}

	/*lang=json,strict*/
	private const string MainAndNextIndex =
		"""{"elastic/elasticsearch":{"elasticsearch-openapi.json":{"main":{"version":"main"},"next":{"version":"next"}}}}""";

	/*lang=json,strict*/
	private const string MainAnd8Index =
		"""{"elastic/elasticsearch":{"elasticsearch-openapi.json":{"main":{"version":"main"},"8":{"version":"8.19"}}}}""";

	/*lang=json,strict*/
	private const string MainNineAnd8Index =
		"""{"elastic/elasticsearch":{"elasticsearch-openapi.json":{"main":{"version":"main"},"9":{"version":"9.2"},"8":{"version":"8.19"}}}}""";

	[Test]
	public async Task ExportDocuments_MainNineAndEight_IndexesNineVersionlessAndEightAtV8()
	{
		using var versionIndexClient = MainNineAnd8Client();
		var generator = MainNineAnd8Generator(versionIndexClient);
		var exporter = new OpenApiDocumentExporter(TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9));

		var trees = new List<string>();
		var paths = new List<string>();
		await foreach (var doc in exporter.ExportDocuments([generator], TestContext.Current!.Execution.CancellationToken))
		{
			paths.Add(doc.Path);
			trees.AddRange(doc.Api!.Trees.Select(t => $"{t.Tree} {t.Root}"));
		}

		paths.Should().OnlyContain(p => !p.Contains("/next") && !p.Contains("/v9"));
		trees.Distinct().Should().BeEquivalentTo("9 /docs/api/elasticsearch", "8 /docs/api/elasticsearch/v8");
	}

	[Test]
	public async Task ResolveCatalogEntries_MainAndMajorDeclared_LinksTheVersionlessUrl()
	{
		using var versionIndexClient = MainNineAnd8Client();
		var generator = MainNineAnd8Generator(versionIndexClient);

		var entries = await generator.ResolveCatalogEntries(TestContext.Current!.Execution.CancellationToken);

		entries.Select(e => e.Url).Should().Equal("/docs/api/elasticsearch");
	}

	[Test]
	public async Task ExportDocuments_SameSpecsTwice_GiveEveryDocumentTheSameContentHash()
	{
		var first = await ContentHashes(TwoOperationSpecs("Ping the cluster."));
		var second = await ContentHashes(TwoOperationSpecs("Ping the cluster."));

		second.Should().Equal(first);
	}

	[Test]
	public async Task ExportDocuments_SamePageInTwoTrees_IsOneDocumentWithOneHash()
	{
		var hashes = await ContentHashes(TwoOperationSpecs("Ping the cluster."));

		hashes
			.Keys
			.Should()
			.Contain(
				"/docs/api/elasticsearch/group/endpoint-core [9, 8]",
				"the group page links to its operations in its own tree, and otherwise reads the same"
			);
	}

	[Test]
	public async Task ExportDocuments_OneEditedDescription_ChangesTheHashOfThatPageOnly()
	{
		var before = await ContentHashes(TwoOperationSpecs("Ping the cluster."));
		var after = await ContentHashes(TwoOperationSpecs("Check that the cluster answers."));

		after.Keys.Should().Equal(before.Keys);
		before
			.Where(kv => after[kv.Key] != kv.Value)
			.Select(kv => kv.Key)
			.Should()
			.Equal("/docs/api/elasticsearch/operation/operation-ping [9, 8]");
	}

	[Test]
	public async Task ExportDocuments_PagesWithoutASearchDocument_AreHiddenSitemapOnlyDocuments()
	{
		using var versionIndexClient = MainNineAnd8Client();
		var generator = MainNineAnd8Generator(versionIndexClient, TwoOperationSpecs("Ping the cluster."));
		var exporter = new OpenApiDocumentExporter(TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9));

		var sitemapOnly = new List<DocumentationDocument>();
		await foreach (var doc in exporter.ExportDocuments([generator], TestContext.Current!.Execution.CancellationToken))
		{
			if (doc.Api!.SitemapOnly)
				sitemapOnly.Add(doc);
		}

		sitemapOnly
			.Select(d => d.Path)
			.Should()
			.BeEquivalentTo("/docs/api/elasticsearch/group/endpoint-admin", "/docs/api/elasticsearch/group/endpoint-core");
		sitemapOnly.Should().OnlyContain(d => d.Hidden && d.Body == "" && d.ContentBodyHash != "", "the default tree does not unhide them");
	}

	/// <summary>Each exported document's content hash, keyed by its URL and the trees it stands for.</summary>
	private static async Task<Dictionary<string, string>> ContentHashes(OpenApiDocument[] specs)
	{
		using var versionIndexClient = MainNineAnd8Client();
		var generator = MainNineAnd8Generator(versionIndexClient, specs);
		var exporter = new OpenApiDocumentExporter(TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9));
		var hashes = new Dictionary<string, string>(StringComparer.Ordinal);
		await foreach (var doc in exporter.ExportDocuments([generator], TestContext.Current!.Execution.CancellationToken))
			hashes[$"{doc.Path} [{string.Join(", ", doc.Api!.Trees.Select(t => t.Tree))}]"] = doc.ContentBodyHash;
		return hashes;
	}

	/// <summary>The main, 9 and 8 specs of an API with two operations in two groups, all three the same.</summary>
	private static OpenApiDocument[] TwoOperationSpecs(string pingDescription) =>
		[TwoOperationSpec(pingDescription), TwoOperationSpec(pingDescription), TwoOperationSpec(pingDescription)];

	private static OpenApiDocument TwoOperationSpec(string pingDescription) =>
		new()
		{
			Info = new OpenApiInfo { Title = "Elasticsearch", Version = "1.0", Description = "A search engine." },
			Paths = new OpenApiPaths
			{
				["/ping"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new()
						{
							OperationId = "ping",
							Summary = "Ping",
							Description = pingDescription,
							Tags = new HashSet<OpenApiTagReference> { new("core") },
							Responses = new OpenApiResponses { ["200"] = new OpenApiResponse { Description = "ok" } }
						}
					}
				},
				["/_status"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new()
						{
							OperationId = "status",
							Summary = "Get the status",
							Description = "Returns the status.",
							Tags = new HashSet<OpenApiTagReference> { new("admin") },
							Responses = new OpenApiResponses { ["200"] = new OpenApiResponse { Description = "ok" } }
						}
					}
				}
			},
			Tags = new HashSet<OpenApiTag> { new() { Name = "core" }, new() { Name = "admin" } }
		};

	private static VersionIndexClient MainNineAnd8Client() =>
		new(
			BaseUri,
			new StubHandler(
				request => request.RequestUri!.AbsolutePath.EndsWith("index.json", StringComparison.Ordinal)
					? IndexResponse(MainNineAnd8Index)
					: SpecResponse()
			),
			sleep: (_, _) => Task.CompletedTask
		);

	private static OpenApiGenerator MainNineAnd8Generator(VersionIndexClient versionIndexClient, OpenApiDocument[]? specs = null) =>
		new(
			NullLoggerFactory.Instance,
			CreateGenerateContext(Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-catalog-split-{Guid.NewGuid():N}")),
			NoopMarkdownStringRenderer.Instance,
			versionIndexClient,
			CreateSequentialReader(
				specs ?? [SpecDocument("Elasticsearch main"), SpecDocument("Elasticsearch 9"), SpecDocument("Elasticsearch 8")]
			)
		);

	[Test]
	public async Task ExportDocuments_ApisBeyondElasticsearchAndKibana_AreIndexedOncePerKey()
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
			"""
		);
		using var versionIndexClient = new VersionIndexClient(BaseUri, SharedProductHandler(), sleep: (_, _) => Task.CompletedTask);
		var reader = A.Fake<IOpenApiSpecificationReader>();
		A.CallTo(() => reader.ReadAsync(A<Stream>._, A<string>._, A<IDiagnosticsCollector?>._)).ReturnsLazily(
			_ => Task.FromResult<OpenApiDocument?>(SpecDocument("Spec"))
		);
		var generators = Enumerable
			.Range(0, 2)
			.Select(
				_ => new OpenApiGenerator(
					NullLoggerFactory.Instance,
					context,
					NoopMarkdownStringRenderer.Instance,
					versionIndexClient,
					reader
				)
			)
			.ToList();
		var exporter = new OpenApiDocumentExporter(TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9));

		var paths = new List<string>();
		await foreach (var doc in exporter.ExportDocuments(generators, TestContext.Current!.Execution.CancellationToken))
			paths.Add($"{doc.Path}{(doc.Api!.SitemapOnly ? " (sitemap only)" : "")}");

		paths.Should().BeEquivalentTo(
			"/docs/api/cloud-billing",
			"/docs/api/cloud-billing/operation/operation-ping",
			"/docs/api/cloud-billing/group/endpoint-core (sitemap only)",
			"/docs/api/cloud-connect",
			"/docs/api/cloud-connect/operation/operation-ping",
			"/docs/api/cloud-connect/group/endpoint-core (sitemap only)"
		);
	}

	[Test]
	public async Task ResolveIndexedTrees_KeyDiffersFromProduct_ReturnsBoth()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-catalog-split-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(
			outputRoot,
			"""
			api:
			  cloud-billing:
			    - spec: cloud-billing.json
			      product: elasticsearch
			"""
		);
		using var versionIndexClient = new VersionIndexClient(BaseUri, SharedProductHandler(), sleep: (_, _) => Task.CompletedTask);
		var generator = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			context,
			NoopMarkdownStringRenderer.Instance,
			versionIndexClient,
			CreateSequentialReader(SpecDocument("Cloud Billing API"))
		);

		var trees = await generator.ResolveIndexedTrees(TestContext.Current!.Execution.CancellationToken);

		trees.Should().ContainSingle();
		trees[0].ApiKey.Should().Be("cloud-billing", "the key builds the URL");
		trees[0].ProductId.Should().Be("elasticsearch", "the product id drives product metadata");
	}

	[Test]
	public async Task ResolveIndexedTrees_NoMainVersion_IndexesTheMajorAsDefault()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-catalog-split-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(outputRoot);
		var handler = new StubHandler(
			request => request.RequestUri!.AbsolutePath.EndsWith("index.json", StringComparison.Ordinal)
				? IndexResponse(
					/*lang=json,strict*/
					"""{"elastic/elasticsearch":{"elasticsearch-openapi.json":{"9":{"version":"9.4"}}}}"""
				)
				: SpecResponse()
		);
		using var versionIndexClient = new VersionIndexClient(BaseUri, handler, sleep: (_, _) => Task.CompletedTask);
		var generator = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			context,
			NoopMarkdownStringRenderer.Instance,
			versionIndexClient,
			CreateSequentialReader(SpecDocument("Elasticsearch 9"))
		);

		var trees = await generator.ResolveIndexedTrees(TestContext.Current!.Execution.CancellationToken);

		trees.Select(t => $"{t.Moniker} at {t.Suffix}").Should().Equal("9 at elasticsearch");
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
		entries[0].Url.Should().Be("/docs/api/elasticsearch");
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "elasticsearch", "index.html")).Should().BeTrue();
		context.WriteFileSystem.Directory.Exists(Path.Join(outputRoot, "api", "docs-builder-elasticsearch")).Should().BeFalse();
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

		var html = await context.WriteFileSystem.File.ReadAllTextAsync(Path.Join(outputRoot, "api", "elasticsearch", "index.html"), ctx);
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
			new("elasticsearch", "Elasticsearch", "/docs/api/elasticsearch", "elasticsearch", "A distributed search engine."),
			new("kibana", "Kibana", "/docs/api/kibana", "kibana")
		};

		await generator.GenerateCatalog(entries, TestContext.Current!.Execution.CancellationToken);

		var catalogPath = Path.Join(outputRoot, "api", "index.html");
		context.WriteFileSystem.File.Exists(catalogPath).Should().BeTrue();
		var html = await context.WriteFileSystem.File.ReadAllTextAsync(catalogPath, TestContext.Current!.Execution.CancellationToken);
		// The card is the link itself and the heading sits inside it, so there is no stretched overlay to trap.
		html.Should().MatchHtmlSnapshot(new HtmlSnapshotOptions().Select("#main-container").Remove("svg"));
		html.Should().Contain(
			"<div id=\"main-container\" class=\"flex min-h-screen flex-col items-center\">",
			"a short page must still fill the screen"
		);
		html.Should().NotContain("download");
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
			.ReadAllTextAsync(Path.Join(outputRoot, "api", "elasticsearch", "index.html"), TestContext.Current!.Execution.CancellationToken);
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

		var productHtml = context.WriteFileSystem.File.ReadAllText(Path.Join(outputRoot, "api", "elasticsearch", "index.html"));
		var catalogHtml = context.WriteFileSystem.File.ReadAllText(Path.Join(outputRoot, "api", "index.html"));
		productHtml.Should().Contain("id=\"api-hub-switcher\"");
		productHtml.Should().Contain("<h1>Elasticsearch main</h1>");
		productHtml.Should().Contain("href=\"/docs/api\"").And.Contain(">All APIs<");
		productHtml.Should().Contain(">Elasticsearch main</span>");
		productHtml.Should().Contain(">APIs<");
		productHtml.Should().Contain("href=\"/docs/api/elasticsearch/operation/operation-ping\"");
		productHtml.Should().NotContain("href=\"/docs/api/elasticsearch/operation/operation-ping/\"");
		productHtml.Should().NotContain("href=\"/docs/api/\"");
		catalogHtml.Should().Contain("<a href=\"/docs/api/elasticsearch\" aria-labelledby");
		catalogHtml.Should().NotContain("href=\"/docs/api/elasticsearch/\"");
		catalogHtml.Should().NotContain("id=\"api-hub-switcher\"");
		catalogHtml.Should().Contain("<h1>Elastic APIs</h1>");
		catalogHtml.Should().Contain("api-card api-catalog-card");
		catalogHtml.Should().NotContain("listing-group-chips");
		context.WriteFileSystem.File.Exists(Path.Join(outputRoot, "api", "elasticsearch.md")).Should().BeTrue();
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
			new("elasticsearch", "Elasticsearch", "/docs/api/elasticsearch"),
			new("kibana", "Kibana", "/docs/api/kibana")
		};

		_ = await generator.GenerateProducts(hubEntries, TestContext.Current!.Execution.CancellationToken);

		var productHtml = context.WriteFileSystem.File.ReadAllText(Path.Join(outputRoot, "api", "elasticsearch", "index.html"));
		productHtml.Should().Contain("id=\"api-hub-switcher\"");
		productHtml.Should().Contain("href=\"/docs/api\"").And.Contain(">All APIs<");
		productHtml.Should().Contain("secondary-nav-dropdown-link--selected");
		productHtml.Should().Contain("href=\"/docs/api/elasticsearch\"");
		productHtml.Should().Contain("href=\"/docs/api/kibana\"").And.Contain(">Kibana</span>");
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

		var productHtml = context.WriteFileSystem.File.ReadAllText(Path.Join(outputRoot, "api", "cloud-connect", "index.html"));
		productHtml.Should().Contain("<h1>Elastic Cloud Connected API</h1>");
		productHtml.Should().Contain("href=\"/docs/api/cloud-billing\"").And.Contain(">Cloud Billing API</span>");
		productHtml.Should().Contain("href=\"/docs/api/cloud-connect\"").And.Contain(">Elastic Cloud Connected API</span>");
	}

	[Test]
	public async Task GenerateCatalog_GroupedApis_AttachTheVariantAndLabelTheGroups()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-catalog-split-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(outputRoot);
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance, versionIndexClient);
		var entries = new List<ApiCatalogEntry>
		{
			new("elasticsearch", "Elasticsearch", "/docs/api/elasticsearch", "elasticsearch"),
			new("kibana", "Kibana", "/docs/api/kibana", "kibana"),
			new("elasticsearch-serverless", "Elasticsearch Serverless", "/docs/api/elasticsearch-serverless", "elasticsearch"),
			new("cloud", "Elastic Cloud", "/docs/api/cloud", "cloud-hosted"),
			new("extra", "Extra API", "/docs/api/extra", "extra")
		};

		await generator.GenerateCatalog(entries, TestContext.Current!.Execution.CancellationToken);

		var html = await context
			.WriteFileSystem
			.File
			.ReadAllTextAsync(Path.Join(outputRoot, "api", "index.html"), TestContext.Current!.Execution.CancellationToken);
		// The Serverless card is attached under Elasticsearch, the groups are labelled and the cards sit one level
		// below them; the Serverless tag-on and the Cloud API are compact.
		html.Should().MatchHtmlSnapshot(new HtmlSnapshotOptions().Select("#main-container").Remove("svg"));
	}

	[Test]
	public async Task GenerateCatalog_RendersTheTitleBandOutsideTheContainer()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-catalog-split-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(outputRoot);
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance, versionIndexClient);

		await generator.GenerateCatalog(
			[new("elasticsearch", "Elasticsearch", "/docs/api/elasticsearch", "elasticsearch")],
			TestContext.Current!.Execution.CancellationToken
		);

		var html = await context
			.WriteFileSystem
			.File
			.ReadAllTextAsync(Path.Join(outputRoot, "api", "index.html"), TestContext.Current!.Execution.CancellationToken);
		// One line of text sits under the title, and the band is rendered outside the width-limited container.
		html.Should().MatchHtmlSnapshot(new HtmlSnapshotOptions().Select("#main-container").Remove("#content-container, svg"));
	}

	[Test]
	public async Task GenerateProducts_BetaBanner_SitsOutsideTheSwappedContainer()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-catalog-split-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(outputRoot);
		var ctx = TestContext.Current!.Execution.CancellationToken;
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var generator = new OpenApiGenerator(
			NullLoggerFactory.Instance,
			context,
			NoopMarkdownStringRenderer.Instance,
			versionIndexClient,
			CreateSequentialReader(SpecDocument("Elasticsearch"))
		);

		_ = await generator.GenerateProducts(ctx: ctx);

		var html = await context.WriteFileSystem.File.ReadAllTextAsync(Path.Join(outputRoot, "api", "elasticsearch", "index.html"), ctx);
		// No boosted swap carries the banner; api-beta-banner.ts matches it to each new page.
		html
			.IndexOf("id=\"api-beta-banner\"", StringComparison.Ordinal)
			.Should()
			.BeInRange(0, html.IndexOf("id=\"main-container\"", StringComparison.Ordinal));
	}

	[Test]
	public async Task GenerateCatalog_BetaBanner_SitsOutsideTheSwappedContainer()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-catalog-split-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(outputRoot);
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance, versionIndexClient);

		await generator.GenerateCatalog(
			[new("elasticsearch", "Elasticsearch", "/docs/api/elasticsearch", "elasticsearch")],
			TestContext.Current!.Execution.CancellationToken
		);

		var html = await context
			.WriteFileSystem
			.File
			.ReadAllTextAsync(Path.Join(outputRoot, "api", "index.html"), TestContext.Current!.Execution.CancellationToken);
		html
			.IndexOf("id=\"api-beta-banner\"", StringComparison.Ordinal)
			.Should()
			.BeInRange(0, html.IndexOf("id=\"main-container\"", StringComparison.Ordinal));
	}

	[Test]
	[Arguments(BuildType.Assembler, true)]
	[Arguments(BuildType.Isolated, false)]
	public async Task GenerateCatalog_Finder_AppearsOnlyWhereTheSidebarHasOne(BuildType buildType, bool expected)
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-catalog-split-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(outputRoot, buildType: buildType);
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance, versionIndexClient);

		await generator.GenerateCatalog(
			[new("elasticsearch", "Elasticsearch", "/docs/api/elasticsearch", "elasticsearch")],
			TestContext.Current!.Execution.CancellationToken
		);

		var html = await context
			.WriteFileSystem
			.File
			.ReadAllTextAsync(Path.Join(outputRoot, "api", "index.html"), TestContext.Current!.Execution.CancellationToken);
		if (expected)
		{
			html.Should().Contain("<navigation-search type=\"api\" variant=\"hero\"></navigation-search>");
			Count(html, "<navigation-search").Should().Be(1);
		}
		else
			html.Should().NotContain("navigation-search");
	}

	private static int Count(string html, string value) => html.Split(value).Length - 1;

	[Test]
	public async Task GenerateCatalog_CloudHostedAndEnterprise_ShowCloudMarksNotTheGenericGlyph()
	{
		var outputRoot = Path.Join(Paths.WorkingDirectoryRoot.FullName, $"api-catalog-split-{Guid.NewGuid():N}");
		var context = CreateGenerateContext(outputRoot);
		using var versionIndexClient = new VersionIndexClient(BaseUri, MultiVersionHandler(), sleep: (_, _) => Task.CompletedTask);
		var generator = new OpenApiGenerator(NullLoggerFactory.Instance, context, NoopMarkdownStringRenderer.Instance, versionIndexClient);
		var entries = new List<ApiCatalogEntry>
		{
			new("cloud", "Elastic Cloud API", "/docs/api/cloud", "cloud-hosted"),
			new("cloud-enterprise", "Elastic Cloud Enterprise API", "/docs/api/cloud-enterprise", "cloud-enterprise")
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

	private static BuildContext CreateGenerateContext(
		string outputRoot,
		string? docsetYaml = null,
		string? displayName = null,
		BuildType buildType = BuildType.Isolated
	)
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
			""";
		var fs = new MockFileSystem(new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName });
		fs.AddDirectory(Path.Join(repoRoot, ".git"));
		fs.AddFile(configPath, new MockFileData(docsetYaml));
		var products = new ProductCatalog { Products = new[] { product }.ToFrozenDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase) };
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
		{ UrlPathPrefix = "docs", BuildType = buildType };
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
}
