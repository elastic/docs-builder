// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Elastic.ApiExplorer.Export;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Inference;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Contract;
using Elastic.Documentation.Versions;
using FakeItEasy;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using Nullean.Snapshots.Json;
using static System.StringComparison;

namespace Elastic.ApiExplorer.Tests;

public class OpenApiDocumentExporterTests
{
	private static readonly HttpClient HttpClient = new();
	private const string BaseUrl = "https://www.elastic.co";

	[Test]
	[Skip("This spams elastic.co, run this manually")]
	public async Task ExportedDocumentUrlsShouldReturnSuccessStatusCode()
	{
		// Arrange
		var versionsConfiguration = new VersionsConfiguration
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>
			{
				{
					VersioningSystemId.Stack,
					new VersioningSystem
					{
						Id = VersioningSystemId.Stack,
						Base = new SemVersion(8, 0, 0),
						Current = new SemVersion(9, 2, 0)
					}
				}
			}
		};

		var exporter = new OpenApiDocumentExporter(versionsConfiguration);
		const int limitPerSource = 300;

		// Act - Collect all documents, tracking source
		var documents = new List<(string Url, string Source)>();
		foreach (var doc in await FetchDocuments(exporter, limitPerSource, TestContext.Current!.Execution.CancellationToken))
		{
			if (!string.IsNullOrEmpty(doc.Path))
			{
				// Determine source from URL
				var source = doc.Path.Contains("/elasticsearch/") ? "elasticsearch" : "kibana";
				documents.Add((doc.Path, source));
			}
		}

		// Assert we have documents from both sources
		documents.Should().NotBeEmpty("the exporter should return at least some documents");
		var elasticsearchDocs = documents.Where(d => d.Source == "elasticsearch").ToList();
		var kibanaDocs = documents.Where(d => d.Source == "kibana").ToList();

		elasticsearchDocs.Should().NotBeEmpty("should have Elasticsearch documents");
		kibanaDocs.Should().NotBeEmpty("should have Kibana documents");

		// Take all documents as sample (already limited)
		var sample = documents.Select(d => d.Url).ToList();

		// Test each URL in parallel
		var failures = new ConcurrentBag<(string Url, int StatusCode)>();

		await Parallel.ForEachAsync(sample, new ParallelOptions
		{
			MaxDegreeOfParallelism = 10,
			CancellationToken = TestContext.Current!.Execution.CancellationToken
		}, async (url, ct) =>
		{
			var fullUrl = $"{BaseUrl}{url}";

			try
			{
				using var request = new HttpRequestMessage(HttpMethod.Head, fullUrl);

				// Mimic browser headers
				request.Headers.Add(
					"User-Agent",
					"Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
				);
				request.Headers.Add(
					"Accept",
					"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8"
				);
				request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
				request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
				request.Headers.Add("DNT", "1");
				request.Headers.Add("Connection", "keep-alive");
				request.Headers.Add("Upgrade-Insecure-Requests", "1");
				request.Headers.Add("Sec-Fetch-Dest", "document");
				request.Headers.Add("Sec-Fetch-Mode", "navigate");
				request.Headers.Add("Sec-Fetch-Site", "none");
				request.Headers.Add("Sec-Fetch-User", "?1");
				request.Headers.Add("Cache-Control", "max-age=0");

				var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

				if (!response.IsSuccessStatusCode)
				{
					failures.Add((url, (int)response.StatusCode));
				}
			}
			catch
			{
				failures.Add((url, -1)); // Use -1 to indicate exception
			}
		});

		// Assert all URLs returned 200
		failures.Should().BeEmpty(
			$"all sampled URLs should return 200 OK, but the following failed: {string.Join(", ", failures.Select(f => $"{f.Url} ({f.StatusCode})"))}"
		);
	}

	[Test]
	public async Task DescriptionWithHtmlShouldHaveTagsStrippedForSearchIndex()
	{
		// Arrange
		var versionsConfiguration = new VersionsConfiguration
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>
			{
				{
					VersioningSystemId.Stack,
					new VersioningSystem
					{
						Id = VersioningSystemId.Stack,
						Base = new SemVersion(8, 0, 0),
						Current = new SemVersion(9, 2, 0)
					}
				}
			}
		};

		var exporter = new OpenApiDocumentExporter(versionsConfiguration);

		var documents = await FetchDocuments(exporter, 100, TestContext.Current!.Execution.CancellationToken);

		documents.Should().NotBeEmpty("the exporter should return documents");

		foreach (var doc in documents)
		{
			doc.Description.Should().NotBeNullOrWhiteSpace();
			doc.Description.Should().NotContain("<div>", "HTML tags should be stripped for the search index");
			doc.Description.Should().NotContain("<span", "HTML tags should be stripped for the search index");
			doc.Description.Should().NotContain("methods and paths for this operation", "the fixed method list is not the search hit");
			doc.Description.Should().NotContain("method and path for this operation", "the fixed method list is not the search hit");
		}
	}

	[Test]
	public void ConvertToDocuments_KeyDiffersFromProduct_InfersFromProductAndKeepsKeyInUrl()
	{
		var inferrer = A.Fake<IDocumentInferrerService>();
		A.CallTo(() => inferrer.InferForOpenApi(A<string>._)).Returns(new DocumentInferenceResult());
		var spec = new OpenApiDocument
		{
			Paths = new OpenApiPaths
			{
				["/api/v1/deployments"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new OpenApiOperation { OperationId = "list-deployments", Summary = "List Deployments" }
					}
				}
			}
		};

		var doc = new OpenApiDocumentExporter(StackVersions(), inferrer)
			.ConvertToDocuments(new IndexedTree("cloud", "cloud-hosted", "main", spec))
			.Single();

		doc.Path.Should().StartWith("/docs/api/doc/cloud/operation/");
		A.CallTo(() => inferrer.InferForOpenApi("cloud-hosted")).MustHaveHappenedOnceExactly();
		A.CallTo(() => inferrer.InferForOpenApi("cloud")).MustNotHaveHappened();
	}

	private static async Task<List<DocumentationDocument>> FetchDocuments(OpenApiDocumentExporter exporter, int limitPerSource, Cancel ctx)
	{
		var documents = new List<DocumentationDocument>();
		// The object keys VersionIndexClient builds from index.json; the previous spec host was retired on 2026-11-06.
		foreach (var (key, objectKey) in new[]
		{
			("elasticsearch", "elastic/elasticsearch-specification/main/elasticsearch.json"),
			("kibana", "elastic/kibana/main/kibana.yaml")
		})
		{
			var stream = await HttpClient.GetStreamAsync(new Uri(VersionIndexClient.DefaultBaseUri, objectKey), ctx);
			var spec = (await OpenApiReader.Instance.ReadAsync(stream, Path.GetFileName(objectKey)))!;
			documents.AddRange(exporter.ConvertToDocuments(new IndexedTree(key, key, "main", spec)).Take(limitPerSource));
		}

		return documents;
	}

	private static VersionsConfiguration StackVersions() =>
		new()
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>
			{
				{
					VersioningSystemId.Stack,
					new VersioningSystem
					{
						Id = VersioningSystemId.Stack,
						Base = new SemVersion(8, 0, 0),
						Current = new SemVersion(9, 2, 0)
					}
				}
			}
		};

	private static DocumentationDocument ExportOperation(string? summary, string? description, string operationId = "esql-put-data-source")
	{
		var spec = new OpenApiDocument
		{
			Paths = new OpenApiPaths
			{
				["/_query/data_source/{name}"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Put] = new OpenApiOperation { OperationId = operationId, Summary = summary, Description = description }
					}
				}
			}
		};

		return new OpenApiDocumentExporter(StackVersions())
			.ConvertToDocuments(new IndexedTree("elasticsearch", "elasticsearch", "main", spec))
			.Single();
	}

	[Test]
	public void Operation_DescriptionAfterMethodList_IndexesTheProseAndTheSummaryTitle()
	{
		const string raw =
			"""
			**All methods and paths for this operation:**

			<div>
			<span class="operation-verb get">GET</span>
			<span class="operation-path">/_query/data_source</span>
			</div>

			Returns one or more data sources.
			""";

		var doc = ExportOperation("Get ES|QL data sources\n", raw, "esql-get-data-source");

		doc.Title.Should().Be("Get ES|QL data sources");
		doc.Title.Should().NotBe("Get ES|QL data sources - esql-get-data-source");
		doc.SearchTitle.Should().Contain("esql-get-data-source");
		doc.SearchTitle.Should().Contain("PUT /_query/data_source/{name}");
		doc.Description.Should().Be("Returns one or more data sources.");
		doc.Body.Should().Contain("Returns one or more data sources.");
		doc.Body.Should().NotContain("All methods and paths for this operation");
		doc.Body.Should().NotContain("/_query/data_source</span>");
	}

	[Test]
	public async Task ConvertToDocuments_UnionsThatDifferOnlyByDescription_IndexBothListings()
	{
		var spec = await TestSpecs.LoadSpecAsync(
			"""
		{
		  "openapi":"3.0.3", "info":{"title":"Fields","version":"1"},
		  "paths":{"/thing":{"post":{"operationId":"thing","responses":{"200":{"description":"OK"}},
		    "requestBody":{"content":{"application/json":{"schema":{"type":"object","properties":{
		      "choice":{"oneOf":[
		        {"type":"object","description":"Code it","properties":{"c1":{"type":"string"},"c2":{"type":"string"},"c3":{"type":"string"},"c4":{"type":"string"},"c5":{"type":"string"},"c6":{"type":"string"}}},
		        {"type":"object","description":"Number it","properties":{"n1":{"type":"integer"},"n2":{"type":"integer"},"n3":{"type":"integer"},"n4":{"type":"integer"},"n5":{"type":"integer"},"n6":{"type":"integer"}}}
		      ]},
		      "pick":{"oneOf":[
		        {"type":"object","description":"Code it, but differently","properties":{"c1":{"type":"string"},"c2":{"type":"string"},"c3":{"type":"string"},"c4":{"type":"string"},"c5":{"type":"string"},"c6":{"type":"string"}}},
		        {"type":"object","description":"Number it, but differently","properties":{"n1":{"type":"integer"},"n2":{"type":"integer"},"n3":{"type":"integer"},"n4":{"type":"integer"},"n5":{"type":"integer"},"n6":{"type":"integer"}}}
		      ]}
		    }}}}}}}}
		}
		"""
		);

		var doc = new OpenApiDocumentExporter(StackVersions())
			.ConvertToDocuments(new IndexedTree("fields", "fields", "main", spec))
			.Single();

		// Large enough to be shared, alike in every way but the variant descriptions: the page keeps both listings, so the index must too.
		doc.Api!.Properties.Select(p => p.Path).Should().Contain(["choice.c1", "choice.n6", "pick.c1", "pick.n6"]);
	}

	[Test]
	public void Operation_MethodList_IndexesEveryRouteWithItsMethods()
	{
		const string raw =
			"""
			**All methods and paths for this operation:**

			<div>
			<span class="operation-verb get">GET</span>
			<span class="operation-path">/_query/data_source</span>
			</div>

			Returns one or more data sources.
			""";

		var doc = ExportOperation("Get ES|QL data sources", raw, "esql-get-data-source");

		doc.Api!
			.Routes
			.Select(r => $"{string.Join(",", r.Methods)} {r.Path}")
			.Should()
			.Equal("PUT /_query/data_source/{name}", "GET /_query/data_source");
	}

	[Test]
	public void Operation_OnlyMethodList_IndexesTheSummary()
	{
		const string raw =
			"""
			**Spaces method and path for this operation:**

			<div><span class="operation-verb get">get</span>&nbsp;<span class="operation-path">/s/{space_id}/api/actions/connector_types</span></div>
			""";

		var doc = ExportOperation("Get connector types", raw, "get-actions-connector-types");

		doc.Title.Should().Be("Get connector types");
		doc.Description.Should().Be("Get connector types");
		doc.Description.Should().NotContain("method and path");
		doc.Body.Should().NotContain("Spaces method and path");
	}

	[Test]
	public void Operation_EmptySummary_IndexesTheDescription()
	{
		const string raw =
			"""
			**Spaces method and path for this operation:**

			<div><span class="operation-verb get">get</span> <span class="operation-path">/callback</span></div>

			Returns the OAuth callback script
			""";

		var doc = ExportOperation(summary: null, raw, "get-actions-connector-oauth-callback-script");

		doc.Title.Should().Be("get-actions-connector-oauth-callback-script");
		doc.Description.Should().Be("Returns the OAuth callback script");
		doc.Description.Should().NotBeNullOrWhiteSpace();
	}

	[Test]
	public void Operation_EmptySummaryAndOnlyMethodList_IndexesTheOperationId()
	{
		const string raw =
			"""
			**All methods and paths for this operation:**

			<div><span class="operation-verb put">PUT</span> <span class="operation-path">/_query/data_source/{name}</span></div>
			""";

		var doc = ExportOperation(summary: "  ", raw, "esql-put-data-source");

		doc.Title.Should().Be("esql-put-data-source");
		doc.Description.Should().Be("esql-put-data-source");
	}

	[Test]
	public void Operation_PlainDescription_IsIndexedUnchanged()
	{
		const string raw = "Creates or replaces a named data source.";

		var doc = ExportOperation("Create or update an ES|QL data source", raw);

		doc.Title.Should().Be("Create or update an ES|QL data source");
		doc.Description.Should().Be(raw);
	}

	[Test]
	[Arguments("Generally available; Added in 9.5.0", 0)]
	[Arguments("Generally available; Added in 9.2.0", 1)]
	[Arguments("Deprecated; Deprecated in 9.5.0", 1)]
	[Arguments("Beta; Added in 8.12.0; Generally available; Added in 9.5.0", 1)]
	public void ConvertToDocuments_XState_SkipsOnlyApisIntroducedAfterCurrentStack(string xState, int expectedCount) =>
		ExportWithState(xState).Should().HaveCount(expectedCount);

	[Test]
	public void ConvertToDocuments_XState_IndexesSameApplicabilityAsBadge()
	{
		var applies = ExportWithState("Technical Preview; added in 9.0.0").Single().Applies;

		applies.Should().NotBeNull();
		var entry = applies.Single();
		entry.Type.Should().Be("stack");
		entry.Lifecycle.Should().Be("preview");
		entry.Version.Should().StartWith("9.0");
	}

	private static List<DocumentationDocument> ExportWithState(string xState)
	{
		var spec = new OpenApiDocument
		{
			Paths = new OpenApiPaths
			{
				["/_thing"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new OpenApiOperation
						{
							OperationId = "thing",
							Summary = "Get a thing",
							Extensions = new Dictionary<string, IOpenApiExtension>
							{
								["x-state"] = new JsonNodeExtension(JsonValue.Create(xState))
							}
						}
					}
				}
			}
		};

		return new OpenApiDocumentExporter(StackVersions())
			.ConvertToDocuments(new IndexedTree("elasticsearch", "elasticsearch", "main", spec))
			.ToList();
	}

	[Test]
	public async Task ConvertToDocuments_ElasticsearchFixture_IndexesTagNamesAsHeadings()
	{
		var specPath = Path.Join(AppContext.BaseDirectory, "TestData", "elasticsearch-openapi.json");
		var spec = await OpenApiReader.Instance.ReadAsync(new FileSystem().FileInfo.New(specPath));

		var docs = new OpenApiDocumentExporter(StackVersions())
			.ConvertToDocuments(new IndexedTree("elasticsearch", "elasticsearch", "main", spec!))
			.ToList();

		docs.Should().AllSatisfy(d => d.Headings.Should().NotBeEmpty($"{d.Api!.OperationId} is tagged"));
		var follow = docs.Single(d => d.Api!.OperationId == "ccr-follow");
		follow.Headings.Should().Equal("ccr");
		follow
			.Parents[^1]
			.Should()
			.BeEquivalentTo(new ParentDocument
			{
				Title = "Cross-cluster replication",
				Path = "/docs/api/doc/elasticsearch/group/endpoint-ccr"
			});
	}

	[Test]
	public async Task ConvertToDocuments_TagNotDeclaredGlobally_IndexesTagNameAsHeading()
	{
		var spec = await TestSpecs.LoadSpecAsync(
			"""
			{"openapi":"3.0.3","info":{"title":"t","version":"1"},
			 "paths":{"/{index}/_ccr/follow":{"put":{"operationId":"ccr-follow","tags":["ccr"],"responses":{"200":{"description":"ok"}}}}}}
			"""
		);

		var doc = new OpenApiDocumentExporter(StackVersions())
			.ConvertToDocuments(new IndexedTree("elasticsearch", "elasticsearch", "main", spec))
			.Single();

		doc.Headings.Should().Equal("ccr");
		doc
			.Parents[^1]
			.Should()
			.BeEquivalentTo(new ParentDocument { Title = "Ccr", Path = "/docs/api/doc/elasticsearch/group/endpoint-ccr" });
	}

	private static async Task<List<DocumentationDocument>> ElasticsearchFixtureDocuments()
	{
		var specPath = Path.Join(AppContext.BaseDirectory, "TestData", "elasticsearch-openapi.json");
		var spec = await OpenApiReader.Instance.ReadAsync(new FileSystem().FileInfo.New(specPath));
		return new OpenApiDocumentExporter(StackVersions())
			.ConvertToDocuments(new IndexedTree("elasticsearch", "elasticsearch", "main", spec!))
			.ToList();
	}

	private static async Task<string[]> SearchRequestChildNames(string parent)
	{
		var search = (await ElasticsearchFixtureDocuments()).Single(d => d.Api!.OperationId == "search");
		return search.Api!.Properties.Where(p => p.Location == "request" && p.Parent == parent).Select(p => p.Name).Distinct().ToArray();
	}

	[Test]
	public async Task ConvertToDocuments_ElasticsearchFixture_RetrieverStandardListsItsDirectChildren() =>
		(await SearchRequestChildNames("retriever.standard")).Should().Equal(
			"filter",
			"min_score",
			"_name",
			"query",
			"search_after",
			"terminate_after",
			"sort",
			"collapse"
		);

	[Test]
	public async Task ConvertToDocuments_ElasticsearchFixture_RetrieverListsEveryRetrieverKind() =>
		(await SearchRequestChildNames("retriever")).Should().Equal(
			"standard",
			"knn",
			"rrf",
			"text_similarity_reranker",
			"rule",
			"rescorer",
			"linear",
			"pinned",
			"diversify"
		);

	[Test]
	public async Task ConvertToDocuments_ElasticsearchFixture_TypePageFieldHasNoChildren() =>
		(await SearchRequestChildNames("retriever.standard.query")).Should().BeEmpty();

	[Test]
	public async Task ConvertToDocuments_ElasticsearchFixture_IndexesTheParentOfEveryRow()
	{
		var docs = await ElasticsearchFixtureDocuments();
		var search = docs.Single(d => d.Api!.OperationId == "search").Api!.Properties;
		var request = search.Where(p => p.Location == "request").ToArray();

		request.Single(p => p.Path == "retriever.standard.min_score").Parent.Should().Be("retriever.standard");
		request.Single(p => p.Path == "retriever").Parent.Should().Be("");
		search.Single(p => p.Location == "query" && p.Path == "allow_no_indices").Parent.Should().Be("");
		foreach (var doc in docs)
		{
			var properties = doc.Api!.Properties;
			var rows = properties.Select(p => (p.Location, ApiPropertyPath.ParentKey(p.Path))).ToHashSet();
			foreach (var property in properties.Where(p => p.Parent != ""))
				rows.Should().Contain(
					(property.Location, property.Parent),
					$"{doc.Api.OperationId} {property.Path} lists under an indexed row"
				);
		}
	}

	[Test]
	public void ConvertToDocuments_NumericTree_PointsEveryUrlIntoTheTree()
	{
		var doc = SearchOperation(new OpenApiDocumentExporter(StackVersions()).ConvertToDocuments(TestHelpers.EsTree("8", TreeSpec())));

		doc.Path.Should().Be("/docs/api/doc/elasticsearch/v8/operation/operation-search");
		doc
			.Parents
			.Select(p => $"{p.Title} {p.Path}")
			.Should()
			.Equal(
				"API /docs/api",
				"Elasticsearch API /docs/api/doc/elasticsearch/v8",
				"Search /docs/api/doc/elasticsearch/v8/group/endpoint-search"
			);
		doc.Api!.Key.Should().Be("elasticsearch");
		doc.Api.Trees.Should().Equal(new ApiTreeEntry { Tree = "8", Root = "/docs/api/doc/elasticsearch/v8" });
	}

	[Test]
	public void ConvertToDocuments_MainTree_KeepsUnversionedUrls()
	{
		var doc = SearchOperation(new OpenApiDocumentExporter(StackVersions()).ConvertToDocuments(TestHelpers.EsTree("main", TreeSpec())));

		doc.Path.Should().Be("/docs/api/doc/elasticsearch/operation/operation-search");
		doc.Api!.Trees.Should().Equal(new ApiTreeEntry { Tree = "main", Root = "/docs/api/doc/elasticsearch" });
	}

	[Test]
	public void Collapse_MainAndIdenticalNine_OneVisibleDocumentPerOperation()
	{
		var docs = ExportTrees(("main", TreeSpec()), ("9", TreeSpec()));

		docs.Select(d => d.Api!.OperationId).Should().Equal("search", "count");
		var search = SearchOperation(docs);
		search.Api!
			.Trees
			.Select(t => $"{t.Tree} {t.Root}")
			.Should()
			.Equal("main /docs/api/doc/elasticsearch", "9 /docs/api/doc/elasticsearch/v9");
		search.Hidden.Should().BeFalse();
		search.Path.Should().Be("/docs/api/doc/elasticsearch/operation/operation-search");
	}

	[Test]
	public void Collapse_EightDiffersOnSearch_AddsAHiddenEightDocument()
	{
		var docs = ExportTrees(("main", TreeSpec()), ("9", TreeSpec()), ("8", TreeSpec("Search the v8 way")));

		docs
			.Select(d => $"{d.Api!.OperationId} {string.Join(",", d.Api.Trees.Select(t => t.Tree))} {d.Hidden}")
			.Should()
			.Equal("search main,9 False", "count main,9,8 False", "search 8 True");
		var eight = docs.Single(d => d.Api!.Trees is [{ Tree: "8" }]);
		eight.Path.Should().Be("/docs/api/doc/elasticsearch/v8/operation/operation-search");
		eight.Parents[1].Path.Should().Be("/docs/api/doc/elasticsearch/v8");
		eight.Parents[1].Title.Should().Be("Elasticsearch API");
	}

	[Test]
	public void Collapse_OperationOnlyInEight_OneHiddenDocument()
	{
		var docs = ExportTrees(("main", TreeSpec()), ("8", TreeSpec(extra: ("knn-search", null))));

		var knn = docs.Single(d => d.Api!.OperationId == "knn-search");
		knn.Api!.Trees.Select(t => t.Tree).Should().Equal("8");
		knn.Hidden.Should().BeTrue();
		knn.Path.Should().Be("/docs/api/doc/elasticsearch/v8/operation/operation-knn-search");
	}

	[Test]
	public void Collapse_VersionlessApi_ListsOnlyMain()
	{
		var search = SearchOperation(ExportTrees(("main", TreeSpec())));

		search.Api!.Trees.Select(t => t.Tree).Should().Equal("main");
		search.Hidden.Should().BeFalse();
	}

	[Test]
	public void Collapse_OperationAddedAfterCurrentStack_IsLeftOutOfEveryTree()
	{
		var unreleased = TreeSpec(extra: ("new-thing", "Generally available; Added in 9.6.0"));

		var docs = ExportTrees(("main", unreleased), ("9", unreleased), ("8", unreleased));

		docs.Select(d => d.Api!.OperationId).Should().Equal("search", "count");
	}

	[Test]
	public void Collapse_DefaultTreeIsNine_SharedRenderingPointsIntoNine()
	{
		var renderings = new[] { ("main", TreeSpec()), ("9", TreeSpec()), ("8", TreeSpec("Search the v8 way")) }.SelectMany(
			t => TestHelpers.CurrentStackExporter().ConvertToDocuments(TestHelpers.EsTree(t.Item1, t.Item2))
		);

		var docs = ApiTreeCollapse.Collapse(renderings, defaultTree: "9").ToList();

		var shared = docs.Single(d => d.Api!.OperationId == "search" && !d.Hidden);
		shared.Api!.Trees.Select(t => t.Tree).Should().Equal("9", "main");
		shared.Path.Should().Be("/docs/api/doc/elasticsearch/v9/operation/operation-search");
		docs.Single(d => d.Api!.OperationId == "search" && d.Hidden).Api!.Trees.Select(t => t.Tree).Should().Equal("8");
	}

	[Test]
	public void ConvertToLandingDocument_Tree_IndexesTheApiRootAsALandingPage()
	{
		var spec = TreeSpec();
		spec.Info = new OpenApiInfo { Title = "Elastic Cloud API", Description = "Run searches, manage indices.\n\nMore detail." };

		var doc = TestHelpers.CurrentStackExporter().ConvertToLandingDocument(TestHelpers.EsTree("9", spec));

		JsonSerializer
			.Serialize(doc, SourceGenerationContext.Default.DocumentationDocument)
			.Should()
			.MatchInlineJsonSnapshot(
				"""
			{
			  "api": {
			    "key": "elasticsearch",
			    "trees": [
			      {
			        "tree": "9",
			        "root": "/docs/api/doc/elasticsearch/v9"
			      }
			    ],
			    "properties": [],
			    "routes": [],
			    "endpoints": [],
			    "endpoints_bare": [],
			    "method": "",
			    "path": "",
			    "operation_id": "",
			    "words": [],
			    "group": "",
			    "id_words": 0,
			    "landing": true,
			    "deprecated": false
			  },
			  "links": [],
			  "content_hash": "",
			  "content_last_updated": "0001-01-01T00:00:00+00:00",
			  "title": "Elastic Cloud API",
			  "search_title": "Elastic Cloud API",
			  "content_type": "api",
			  "path": "/docs/api/doc/elasticsearch/v9",
			  "url": "/docs/api/doc/elasticsearch/v9",
			  "hash": "",
			  "batch_index_date": "0001-01-01T00:00:00+00:00",
			  "last_updated": "0001-01-01T00:00:00+00:00",
			  "description": "Run searches, manage indices.",
			  "headings": [],
			  "body": "Run searches, manage indices.",
			  "parents": [
			    {
			      "title": "API",
			      "path": "/docs/api"
			    }
			  ],
			  "content_tier": "reference",
			  "navigation": {
			    "depth": 50,
			    "table_of_contents": 50
			  }
			}
			"""
			);
	}

	[Test]
	public void ConvertToLandingDocument_SpecWithoutTitle_FallsBackToTheProductLabel()
	{
		var doc = TestHelpers.CurrentStackExporter().ConvertToLandingDocument(TestHelpers.EsTree("main", TreeSpec()));

		doc.Title.Should().Be("Elasticsearch API");
		doc.SearchTitle.Should().Be("Elasticsearch API");
	}

	[Test]
	public void Collapse_LandingPageOfEveryTree_OneVisibleLandingDocumentListingTheTrees()
	{
		var exporter = TestHelpers.CurrentStackExporter();
		var landings = new[] { "main", "9", "8" }.Select(t => exporter.ConvertToLandingDocument(TestHelpers.EsTree(t, TreeSpec())));

		var docs = ApiTreeCollapse.Collapse(landings, defaultTree: "9").ToList();

		var landing = docs.Should().ContainSingle().Subject;
		landing.Api!
			.Trees
			.Select(t => $"{t.Tree} {t.Root}")
			.Should()
			.Equal("9 /docs/api/doc/elasticsearch/v9", "main /docs/api/doc/elasticsearch", "8 /docs/api/doc/elasticsearch/v8");
		landing.Path.Should().Be("/docs/api/doc/elasticsearch/v9");
		landing.Hidden.Should().BeFalse();
	}

	/// <summary>Collapses the trees under the API's real default-tree rule, with Stack current at 9.5.6 as in <c>config/versions.yml</c>.</summary>
	private static List<DocumentationDocument> ExportTrees(params (string Moniker, OpenApiDocument Spec)[] trees)
	{
		var exporter = TestHelpers.CurrentStackExporter();
		var defaultTree = ApiVersions.DefaultTree(trees.Select(t => t.Moniker))!;
		return [
			.. ApiTreeCollapse.Collapse(
				trees.SelectMany(t => exporter.ConvertToDocuments(TestHelpers.EsTree(t.Moniker, t.Spec))),
				defaultTree
			)
		];
	}

	private static DocumentationDocument SearchOperation(IEnumerable<DocumentationDocument> docs) =>
		docs.Single(d => d.Api!.OperationId == "search");

	/// <summary>A small spec in the shape of one Elasticsearch tree: <c>search</c> and <c>count</c> under the <c>search</c> tag, plus any extra operations.</summary>
	private static OpenApiDocument TreeSpec(string searchSummary = "Run a search", params (string Id, string? XState)[] extra)
	{
		var paths = new OpenApiPaths
		{
			["/_search"] = TaggedGet("search", searchSummary, null),
			["/_count"] = TaggedGet("count", "Count documents", null)
		};
		foreach (var (id, xState) in extra)
			paths[$"/_{id}"] = TaggedGet(id, $"Run {id}", xState);
		return new OpenApiDocument { Paths = paths, Tags = new HashSet<OpenApiTag> { new() { Name = "search" } } };
	}

	private static OpenApiPathItem TaggedGet(string operationId, string summary, string? xState) =>
		new()
		{
			Operations = new Dictionary<HttpMethod, OpenApiOperation>
			{
				[HttpMethod.Get] = new()
				{
					OperationId = operationId,
					Summary = summary,
					Tags = new HashSet<OpenApiTagReference> { new("search") },
					Extensions = xState is null
						? null
						: new Dictionary<string, IOpenApiExtension> { ["x-state"] = new JsonNodeExtension(JsonValue.Create(xState)) }
				}
			}
		};
}
