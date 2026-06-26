// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Elastic.Documentation.Search;
using Elastic.Documentation.Serialization;
using System.Text.Json;
using Elastic.Ingest.Elasticsearch;
using Elastic.Internal.Search.Mapping;
using Elastic.Markdown.Exporters.Elasticsearch;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;
using Microsoft.Extensions.Logging;
using Xunit;
using ElasticsearchTransportConfig = Elastic.Transport.Products.Elasticsearch.ElasticsearchConfiguration;

namespace Elastic.ContentDateEnrichment.IntegrationTests;

public class ElasticsearchFixture : IAsyncLifetime
{
	private IContainer _container = null!;
	public DistributedTransport Transport { get; private set; } = null!;

	public async ValueTask InitializeAsync()
	{
		_container = new ContainerBuilder()
			.WithImage("docker.elastic.co/elasticsearch/elasticsearch:8.18.0")
			.WithEnvironment("discovery.type", "single-node")
			.WithEnvironment("xpack.security.enabled", "false")
			.WithEnvironment("xpack.security.http.ssl.enabled", "false")
			.WithPortBinding(9200, true)
			.WithWaitStrategy(Wait.ForUnixContainer()
				.UntilHttpRequestIsSucceeded(r => r.ForPort(9200).ForPath("/_cluster/health")))
			.Build();

		await _container.StartAsync();

		var port = _container.GetMappedPublicPort(9200);
		var settings = new ElasticsearchTransportConfig(new Uri($"http://localhost:{port}"));
		Transport = new DistributedTransport(settings);
	}

	public async ValueTask DisposeAsync()
	{
		await _container.DisposeAsync();
		GC.SuppressFinalize(this);
	}
}

[CollectionDefinition("Elasticsearch")]
public class ElasticsearchTestCluster : ICollectionFixture<ElasticsearchFixture>;

/// <summary>
/// Integration tests verifying that content_last_updated is correctly resolved
/// via the enrichment pipeline, even when documents are written via bulk update
/// actions that skip ingest pipelines. Uses the real IngestChannel (HashedBulkUpdate)
/// from Elastic.Ingest.Elasticsearch — the same code path as production.
/// </summary>
[Collection("Elasticsearch")]
public class ContentDateEnrichmentTests(ElasticsearchFixture fixture, ITestOutputHelper output)
{
	private readonly DistributedTransport _transport = fixture.Transport;

	private Elastic.Markdown.Exporters.Elasticsearch.ContentDateEnrichment CreateEnrichment(string testName)
	{
		var loggerFactory = LoggerFactory.Create(b => b.AddXUnit(output));
		var logger = loggerFactory.CreateLogger<ContentDateEnrichmentTests>();
		var operations = new ElasticsearchOperations(_transport, logger);
		// Each test uses a unique buildType to isolate its pipeline/lookup infrastructure
		return new Elastic.Markdown.Exporters.Elasticsearch.ContentDateEnrichment(
			_transport, operations, logger, testName, "test"
		);
	}

	/// <summary>
	/// Creates an IngestChannel using the same type context and serializer as production.
	/// On first bootstrap (no existing index), the channel uses index bulk actions.
	/// When <paramref name="indexNameOverride"/> is provided (pointing to an existing index),
	/// the channel reuses that index and switches to update bulk actions (HashedBulkUpdate) —
	/// the same scripted-upsert path that production uses on subsequent deploys.
	/// </summary>
	private async Task<IngestChannel<DocumentationDocument>> CreateChannelAsync(
		Elastic.Markdown.Exporters.Elasticsearch.ContentDateEnrichment enrichment,
		string testName,
		string? indexNameOverride = null)
	{
		var synonymSetName = $"docs-{testName}-test";

		// Register an empty synonym set — the analyzer references it by name
		var synonymBody = new JsonObject { ["synonyms_set"] = new JsonArray() };
		await _transport.PutAsync<StringResponse>(
			$"_synonyms/{synonymSetName}",
			PostData.String(synonymBody.ToJsonString()),
			CancellationToken.None
		);

		var typeContext = DocumentationMappingContext.DocumentationDocument
			.CreateContext(type: testName, env: "test") with
		{
			ConfigureAnalysis = a => SharedAnalysisFactory.BuildAnalysis(a, synonymSetName, ["test, testing"]),
			IndexSettings = new Dictionary<string, string>
			{
				["index.default_pipeline"] = enrichment.PipelineName
			}
		};

		var options = new IngestChannelOptions<DocumentationDocument>(_transport, typeContext, indexNameOverride: indexNameOverride)
		{
			SerializerContext = Elastic.Documentation.Serialization.SourceGenerationContext.Default
		};
		return new IngestChannel<DocumentationDocument>(options);
	}

	/// <summary>
	/// Writes documents through the real IngestChannel (HashedBulkUpdate) and waits for drain.
	/// </summary>
	private static async Task WriteDocuments(
		IngestChannel<DocumentationDocument> channel,
		params DocumentationDocument[] docs)
	{
		foreach (var doc in docs)
			await channel.WaitToWriteAsync(doc, CancellationToken.None);

		await channel.WaitForDrainAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
	}

	private static DocumentationDocument CreateDoc(string url, string contentHash, string title) => new()
	{
		Url = url,
		Title = title,
		SearchTitle = title,
		Hash = contentHash,
		ContentBodyHash = contentHash
	};

	[Fact]
	public async Task FirstRun_AllDocumentsGetCurrentTimestamp()
	{
		var enrichment = CreateEnrichment("first-run");

		// Arrange: initialize enrichment infrastructure (empty lookup)
		await enrichment.InitializeAsync(CancellationToken.None);
		using var channel = await CreateChannelAsync(enrichment, "first-run");
		await channel.BootstrapElasticsearchAsync(BootstrapMethod.Failure, CancellationToken.None);
		var index = channel.IndexName;

		await WriteDocuments(channel,
			CreateDoc("url1", "hash_a", "Doc 1"),
			CreateDoc("url2", "hash_b", "Doc 2")
		);
		await channel.RefreshAsync(CancellationToken.None);
		index = await ResolveIndexName(index);

		// Diagnostic: read back what the channel actually stored
		var beforeResolve = await GetDocument(index, "url1");
		output.WriteLine($"Before resolve — url1 content_last_updated: {beforeResolve.ContentLastUpdated?.ToString("O") ?? "NULL"}");

		// Act: run the post-indexing resolution
		await enrichment.ResolveContentDatesAsync(index, CancellationToken.None);
		await channel.RefreshAsync(CancellationToken.None);

		// Assert: all docs should have content_last_updated near "now" (not 0001-01-01)
		var docs = await GetAllDocuments(index);
		docs.Should().HaveCount(2);
		foreach (var doc in docs)
		{
			doc.ContentLastUpdated.Should().NotBeNull($"document {doc.Url} should have content_last_updated");
			doc.ContentLastUpdated.Value.Year.Should().BeGreaterThanOrEqualTo(2026,
				$"document {doc.Url} should have a recent content_last_updated");
		}
	}

	[Fact]
	public async Task SecondRun_UnchangedContentPreservesOldDate()
	{
		var enrichment = CreateEnrichment("unchanged");

		// === First run: new index, channel uses index actions (pipeline fires) ===
		await enrichment.InitializeAsync(CancellationToken.None);
		string index;
		using (var channel1 = await CreateChannelAsync(enrichment, "unchanged"))
		{
			await channel1.BootstrapElasticsearchAsync(BootstrapMethod.Failure, CancellationToken.None);
			index = channel1.IndexName; // wildcard — resolved to concrete name below

			await WriteDocuments(channel1,
				CreateDoc("url1", "hash_a", "Doc 1"),
				CreateDoc("url2", "hash_b", "Doc 2")
			);
			await channel1.RefreshAsync(CancellationToken.None);
		}
		index = await ResolveIndexName(index);
		await enrichment.ResolveContentDatesAsync(index, CancellationToken.None);
		await RefreshIndex(index);
		await enrichment.SyncLookupIndexAsync(index, CancellationToken.None);

		var firstRunDocs = await GetAllDocuments(index);
		var firstRunDates = firstRunDocs.ToDictionary(d => d.Url, d => d.ContentLastUpdated);

		// Wait to ensure timestamp separation
		await Task.Delay(TimeSpan.FromSeconds(1.5), TestContext.Current.CancellationToken);

		// Re-initialize for second run (re-executes enrich policy with updated lookup data)
		await enrichment.InitializeAsync(CancellationToken.None);

		// === Second run: reuse existing index → channel uses update actions (HashedBulkUpdate) ===
		using (var channel2 = await CreateChannelAsync(enrichment, "unchanged", indexNameOverride: index))
		{
			await channel2.BootstrapElasticsearchAsync(BootstrapMethod.Failure, CancellationToken.None);

			await WriteDocuments(channel2,
				CreateDoc("url1", "hash_a", "Doc 1 (re-indexed)"),
				CreateDoc("url2", "hash_b", "Doc 2 (re-indexed)")
			);
			await channel2.RefreshAsync(CancellationToken.None);
		}

		// Diagnostic: check what HashedBulkUpdate stored before resolve
		var url1Before = await GetDocument(index, "url1");
		output.WriteLine($"Before resolve — url1 content_last_updated: {url1Before.ContentLastUpdated?.ToString("O") ?? "NULL"}");

		await enrichment.ResolveContentDatesAsync(index, CancellationToken.None);
		await RefreshIndex(index);

		// Assert: dates should be preserved from the first run
		var secondRunDocs = await GetAllDocuments(index);
		foreach (var doc in secondRunDocs)
		{
			var originalDate = firstRunDates[doc.Url];
			doc.ContentLastUpdated.Should().Be(originalDate!.Value,
				$"document {doc.Url} content didn't change, so content_last_updated should be preserved");
		}
	}

	[Fact]
	public async Task SecondRun_ChangedContentGetsNewDate()
	{
		var enrichment = CreateEnrichment("changed");

		// === First run: new index, channel uses index actions (pipeline fires) ===
		await enrichment.InitializeAsync(CancellationToken.None);
		string index;
		using (var channel1 = await CreateChannelAsync(enrichment, "changed"))
		{
			await channel1.BootstrapElasticsearchAsync(BootstrapMethod.Failure, CancellationToken.None);
			index = channel1.IndexName; // wildcard — resolved to concrete name below

			await WriteDocuments(channel1,
				CreateDoc("url1", "hash_a", "Doc 1"),
				CreateDoc("url2", "hash_b", "Doc 2")
			);
			await channel1.RefreshAsync(CancellationToken.None);
		}
		index = await ResolveIndexName(index);
		await enrichment.ResolveContentDatesAsync(index, CancellationToken.None);
		await RefreshIndex(index);
		await enrichment.SyncLookupIndexAsync(index, CancellationToken.None);

		var firstRunDocs = await GetAllDocuments(index);
		var firstRunDates = firstRunDocs.ToDictionary(d => d.Url, d => d.ContentLastUpdated);

		await Task.Delay(TimeSpan.FromSeconds(1.5), TestContext.Current.CancellationToken);

		await enrichment.InitializeAsync(CancellationToken.None);

		// === Second run: reuse existing index → channel uses update actions (HashedBulkUpdate) ===
		using (var channel2 = await CreateChannelAsync(enrichment, "changed", indexNameOverride: index))
		{
			await channel2.BootstrapElasticsearchAsync(BootstrapMethod.Failure, CancellationToken.None);

			// url1 content changed, url2 unchanged
			await WriteDocuments(channel2,
				CreateDoc("url1", "hash_CHANGED", "Doc 1 (updated content)"),
				CreateDoc("url2", "hash_b", "Doc 2 (same content)")
			);
			await channel2.RefreshAsync(CancellationToken.None);
		}

		// Diagnostic: check url1 before resolve
		var url1Before = await GetDocument(index, "url1");
		output.WriteLine($"Before resolve — url1 content_last_updated: {url1Before.ContentLastUpdated?.ToString("O") ?? "NULL"}");

		await enrichment.ResolveContentDatesAsync(index, CancellationToken.None);
		await RefreshIndex(index);

		// Assert
		var secondRunDocs = await GetAllDocuments(index);
		var changed = secondRunDocs.Single(d => d.Url == "url1");
		var unchanged = secondRunDocs.Single(d => d.Url == "url2");

		changed.ContentLastUpdated.Should().BeAfter(firstRunDates["url1"]!.Value,
			"url1 content changed, so content_last_updated should advance");

		unchanged.ContentLastUpdated.Should().Be(firstRunDates["url2"]!.Value,
			"url2 content didn't change, so content_last_updated should be preserved");
	}

	/// <summary>
	/// Discovery test: indexes a full DocumentationDocument via a scripted upsert
	/// (the same mechanism HashedBulkUpdate uses) and reads back what ES actually
	/// stores for content_last_updated when the field is never explicitly set.
	/// This tells us the exact value to filter on in ResolveContentDatesAsync.
	/// </summary>
	[Fact]
	public async Task ScriptedUpsert_WithFullDocument_ContentLastUpdatedValue()
	{
		var index = "test-discovery";

		// Create index with the content_last_updated mapping
		await CreateTestIndex(index, "_none");

		// Serialize a DocumentationDocument with default ContentLastUpdated (never set)
		// using the same serializer context that HashedBulkUpdate uses in production
		var doc = new DocumentationDocument
		{
			Url = "test-discovery-url",
			Title = "Discovery Test",
			SearchTitle = "Discovery Test",
			ContentType = "doc",
			Hash = "testhash123",
			ContentBodyHash = "contenthash123"
		};
		var serializedDoc = JsonSerializer.Serialize(doc, Elastic.Documentation.Serialization.SourceGenerationContext.Default.DocumentationDocument);

		// Index via scripted upsert (same as HashedBulkUpdate)
		await IndexFullDocumentViaScriptedUpsert(index, doc.Url, serializedDoc);
		await RefreshIndex(index);

		// Read back from ES
		var response = await _transport.GetAsync<StringResponse>(
			$"{index}/_doc/{doc.Url}", CancellationToken.None
		);
		response.ApiCallDetails.HasSuccessfulStatusCode.Should().BeTrue(
			$"Failed to get document: {response.ApiCallDetails.DebugInformation}");

		var source = JsonNode.Parse(response.Body)?["_source"];
		source.Should().NotBeNull();

		var esDateValue = source["content_last_updated"]?.ToString();

		// The default DateTimeOffset (0001-01-01) should be stored -- verify it's pre-epoch
		esDateValue.Should().NotBeNull("content_last_updated should be present in ES document");
		var esDate = DateTimeOffset.Parse(esDateValue, CultureInfo.InvariantCulture);
		esDate.Year.Should().Be(1, "default DateTimeOffset.MinValue should serialize as year 0001");
		esDate.Should().BeBefore(DateTimeOffset.UnixEpoch,
			"the default value should be well before 1970, making it safe to filter with 'must_not range gt 1970'");
	}

	/// <summary>
	/// Verifies the filter approach: on the second run (reuse path with HashedBulkUpdate),
	/// the filter only processes documents that were changed (unresolved dates),
	/// while unchanged documents (noop) retain their existing resolved dates.
	/// </summary>
	[Fact]
	public async Task FilteredResolve_SkipsDocumentsWithExistingDates()
	{
		var enrichment = CreateEnrichment("filtered");

		// === First run: all docs get resolved dates via pipeline ===
		await enrichment.InitializeAsync(CancellationToken.None);
		string index;
		using (var channel1 = await CreateChannelAsync(enrichment, "filtered"))
		{
			await channel1.BootstrapElasticsearchAsync(BootstrapMethod.Failure, CancellationToken.None);
			index = channel1.IndexName; // wildcard -- resolved to concrete name below

			await WriteDocuments(channel1,
				CreateDoc("url1", "hash_a", "Doc 1"),
				CreateDoc("url2", "hash_b", "Doc 2"),
				CreateDoc("url3", "hash_c", "Doc 3")
			);
			await channel1.RefreshAsync(CancellationToken.None);
		}
		index = await ResolveIndexName(index);
		await enrichment.ResolveContentDatesAsync(index, CancellationToken.None);
		await RefreshIndex(index);
		await enrichment.SyncLookupIndexAsync(index, CancellationToken.None);

		var firstRunDocs = await GetAllDocuments(index);
		var firstRunDates = firstRunDocs.ToDictionary(d => d.Url, d => d.ContentLastUpdated);

		await Task.Delay(TimeSpan.FromSeconds(1.5), TestContext.Current.CancellationToken);
		await enrichment.InitializeAsync(CancellationToken.None);

		// === Second run: reuse index → HashedBulkUpdate. Only url1 changed. ===
		using (var channel2 = await CreateChannelAsync(enrichment, "filtered", indexNameOverride: index))
		{
			await channel2.BootstrapElasticsearchAsync(BootstrapMethod.Failure, CancellationToken.None);

			await WriteDocuments(channel2,
				CreateDoc("url1", "hash_CHANGED", "Doc 1 updated"),
				CreateDoc("url2", "hash_b", "Doc 2"),
				CreateDoc("url3", "hash_c", "Doc 3")
			);
			await channel2.RefreshAsync(CancellationToken.None);
		}

		// Diagnostic: check what HashedBulkUpdate stored
		var url1Before = await GetDocument(index, "url1");
		output.WriteLine($"Before resolve — url1 (changed) content_last_updated: {url1Before.ContentLastUpdated?.ToString("O") ?? "NULL"}");
		var url2Before = await GetDocument(index, "url2");
		output.WriteLine($"Before resolve — url2 (noop) content_last_updated: {url2Before.ContentLastUpdated?.ToString("O") ?? "NULL"}");

		// Act: resolve content dates
		await enrichment.ResolveContentDatesAsync(index, CancellationToken.None);
		await RefreshIndex(index);

		// Assert
		var docs = await GetAllDocuments(index);

		var result1 = docs.Single(d => d.Url == "url1");
		result1.ContentLastUpdated.Should().NotBeNull("url1 changed — should have been resolved");
		result1.ContentLastUpdated!.Value.Should().BeAfter(firstRunDates["url1"]!.Value,
			"url1 content changed — date should advance");

		var result2 = docs.Single(d => d.Url == "url2");
		result2.ContentLastUpdated!.Value.Should().Be(firstRunDates["url2"]!.Value,
			"url2 was a noop — filter should have skipped it");

		var result3 = docs.Single(d => d.Url == "url3");
		result3.ContentLastUpdated!.Value.Should().Be(firstRunDates["url3"]!.Value,
			"url3 was a noop — filter should have skipped it");
	}

	/// <summary>
	/// End-to-end test: first run resolves all docs. Second run reuses the index
	/// (HashedBulkUpdate path): unchanged docs are noop (date preserved),
	/// changed doc gets replaced (needs resolve).
	/// </summary>
	[Fact]
	public async Task SecondRun_WithFilter_OnlyChangedDocGetsNewDate()
	{
		var enrichment = CreateEnrichment("filter-e2e");

		// === First run: new index, pipeline fires on all docs ===
		await enrichment.InitializeAsync(CancellationToken.None);
		string index;
		using (var channel1 = await CreateChannelAsync(enrichment, "filter-e2e"))
		{
			await channel1.BootstrapElasticsearchAsync(BootstrapMethod.Failure, CancellationToken.None);
			index = channel1.IndexName; // wildcard — resolved to concrete name below

			await WriteDocuments(channel1,
				CreateDoc("url1", "hash_a", "Doc 1"),
				CreateDoc("url2", "hash_b", "Doc 2"),
				CreateDoc("url3", "hash_c", "Doc 3")
			);
			await channel1.RefreshAsync(CancellationToken.None);
		}
		index = await ResolveIndexName(index);
		await enrichment.ResolveContentDatesAsync(index, CancellationToken.None);
		await RefreshIndex(index);
		await enrichment.SyncLookupIndexAsync(index, CancellationToken.None);

		var firstRunDocs = await GetAllDocuments(index);
		var firstRunDates = firstRunDocs.ToDictionary(d => d.Url, d => d.ContentLastUpdated);

		await Task.Delay(TimeSpan.FromSeconds(1.5), TestContext.Current.CancellationToken);
		await enrichment.InitializeAsync(CancellationToken.None);

		// === Second run: reuse index → HashedBulkUpdate. Only url1 changed. ===
		using (var channel2 = await CreateChannelAsync(enrichment, "filter-e2e", indexNameOverride: index))
		{
			await channel2.BootstrapElasticsearchAsync(BootstrapMethod.Failure, CancellationToken.None);

			await WriteDocuments(channel2,
				CreateDoc("url1", "hash_CHANGED", "Doc 1 updated"),
				CreateDoc("url2", "hash_b", "Doc 2"),
				CreateDoc("url3", "hash_c", "Doc 3")
			);
			await channel2.RefreshAsync(CancellationToken.None);
		}

		// Diagnostic: check url1 after HashedBulkUpdate, before resolve
		var url1Before = await GetDocument(index, "url1");
		output.WriteLine($"Before resolve — url1 content_last_updated: {url1Before.ContentLastUpdated?.ToString("O") ?? "NULL"}");
		var url2Before = await GetDocument(index, "url2");
		output.WriteLine($"Before resolve — url2 content_last_updated: {url2Before.ContentLastUpdated?.ToString("O") ?? "NULL"}");

		await enrichment.ResolveContentDatesAsync(index, CancellationToken.None);
		await RefreshIndex(index);

		// Assert
		var secondRunDocs = await GetAllDocuments(index);

		var changed = secondRunDocs.Single(d => d.Url == "url1");
		changed.ContentLastUpdated.Should().BeAfter(firstRunDates["url1"]!.Value,
			"url1 content changed — date should advance");

		var unchanged2 = secondRunDocs.Single(d => d.Url == "url2");
		unchanged2.ContentLastUpdated.Should().Be(firstRunDates["url2"]!.Value,
			"url2 was a noop — filter should have skipped it, preserving its date");

		var unchanged3 = secondRunDocs.Single(d => d.Url == "url3");
		unchanged3.ContentLastUpdated.Should().Be(firstRunDates["url3"]!.Value,
			"url3 was a noop — filter should have skipped it, preserving its date");
	}

	// --- Helpers ---

	private async Task CreateTestIndex(string index, string pipelineName)
	{
		_ = await _transport.DeleteAsync<StringResponse>(
			index, new DefaultRequestParameters(), PostData.Empty, CancellationToken.None
		);

		var body = new JsonObject
		{
			["settings"] = new JsonObject
			{
				["index.default_pipeline"] = pipelineName,
				["number_of_replicas"] = 0
			},
			["mappings"] = new JsonObject
			{
				["properties"] = new JsonObject
				{
					["url"] = new JsonObject { ["type"] = "keyword" },
					["content_hash"] = new JsonObject { ["type"] = "keyword" },
					["content_last_updated"] = new JsonObject { ["type"] = "date" },
					["title"] = new JsonObject { ["type"] = "text" }
				}
			}
		};

		var response = await _transport.PutAsync<StringResponse>(
			index, PostData.String(body.ToJsonString()), CancellationToken.None
		);
		response.ApiCallDetails.HasSuccessfulStatusCode.Should().BeTrue(
			$"Failed to create test index: {response.ApiCallDetails.DebugInformation}");
	}

	/// <summary>
	/// Indexes documents via scripted upsert with full DocumentationDocument serialization,
	/// matching how HashedBulkUpdate works in production. If the hash matches an existing
	/// document, the script noops (preserving existing fields like content_last_updated).
	/// If the hash differs (or is new), the script replaces the entire document.
	/// </summary>
	private async Task IndexDocumentsDirectly(string index, params (string url, string contentHash, string title)[] docs)
	{
		foreach (var (url, contentHash, title) in docs)
		{
			var doc = new DocumentationDocument
			{
				Url = url,
				Title = title,
				SearchTitle = title,
				ContentType = "doc",
				Hash = contentHash,
				ContentBodyHash = contentHash
			};
			var serialized = JsonSerializer.Serialize(doc, Elastic.Documentation.Serialization.SourceGenerationContext.Default.DocumentationDocument);
			await IndexFullDocumentViaScriptedUpsert(index, url, serialized);
		}
	}

	/// <summary>Uses the _index API which DOES trigger the default_pipeline.</summary>
	private async Task IndexViaIndexAction(string index, string url, string contentHash, string title)
	{
		var doc = new JsonObject
		{
			["url"] = url,
			["content_hash"] = contentHash,
			["title"] = title
		};

		var response = await _transport.PutAsync<StringResponse>(
			$"{index}/_doc/{url}",
			PostData.String(doc.ToJsonString()),
			CancellationToken.None
		);
		response.ApiCallDetails.HasSuccessfulStatusCode.Should().BeTrue(
			$"Failed to index document {url}: {response.ApiCallDetails.DebugInformation}");
	}


	private async Task<TestDocument> GetDocument(string index, string id)
	{
		var response = await _transport.GetAsync<StringResponse>($"{index}/_doc/{id}", CancellationToken.None);
		response.ApiCallDetails.HasSuccessfulStatusCode.Should().BeTrue(
			$"Failed to get document {id}: {response.ApiCallDetails.DebugInformation}");

		var source = JsonNode.Parse(response.Body)?["_source"];
		source.Should().NotBeNull();

		var dateStr = source["content_last_updated"]?.GetValue<string>();
		DateTimeOffset? date = dateStr is not null ? DateTimeOffset.Parse(dateStr, CultureInfo.InvariantCulture) : null;

		return new TestDocument(
			source["url"]?.GetValue<string>() ?? "",
			source["content_hash"]?.GetValue<string>() ?? "",
			date
		);
	}

	/// <summary>Resolves the concrete index name from a wildcard pattern or alias.</summary>
	private async Task<string> ResolveIndexName(string indexPattern)
	{
		var response = await _transport.GetAsync<StringResponse>(
			$"/_resolve/index/{indexPattern}", CancellationToken.None
		);
		response.ApiCallDetails.HasSuccessfulStatusCode.Should().BeTrue(
			$"Failed to resolve index pattern {indexPattern}: {response.ApiCallDetails.DebugInformation}");

		var json = JsonNode.Parse(response.Body);
		var indices = json?["indices"]?.AsArray();
		indices.Should().NotBeNull();
		indices!.Count.Should().Be(1, $"expected exactly one index for pattern {indexPattern}");
		return indices[0]!["name"]!.GetValue<string>();
	}

	private async Task RefreshIndex(string index) =>
		await _transport.PostAsync<StringResponse>($"/{index}/_refresh", PostData.Empty, CancellationToken.None);

	private async Task<List<TestDocument>> GetAllDocuments(string index)
	{
		var body = new JsonObject
		{
			["size"] = 100,
			["_source"] = new JsonArray("url", "content_hash", "content_last_updated", "title")
		};

		var response = await _transport.PostAsync<StringResponse>(
			$"{index}/_search",
			PostData.String(body.ToJsonString()),
			CancellationToken.None
		);
		response.ApiCallDetails.HasSuccessfulStatusCode.Should().BeTrue(
			$"Search failed: {response.ApiCallDetails.DebugInformation}");

		var json = JsonNode.Parse(response.Body);
		var hits = json?["hits"]?["hits"]?.AsArray() ?? [];

		return hits
			.Where(h => h?["_source"] is not null)
			.Select(h =>
			{
				var source = h!["_source"]!;
				var dateStr = source["content_last_updated"]?.GetValue<string>();
				return new TestDocument(
					source["url"]?.GetValue<string>() ?? "",
					source["content_hash"]?.GetValue<string>() ?? "",
					dateStr is not null ? DateTimeOffset.Parse(dateStr, CultureInfo.InvariantCulture) : null
				);
			})
			.ToList();
	}

	/// <summary>
	/// Indexes a pre-serialized DocumentationDocument via scripted upsert,
	/// matching the exact pattern HashedBulkUpdate uses in production.
	/// </summary>
	private async Task IndexFullDocumentViaScriptedUpsert(string index, string id, string serializedDoc)
	{
		const string hashField = "hash";
		var docNode = JsonNode.Parse(serializedDoc)!;
		var hashValue = docNode[hashField]?.ToString() ?? "";

		var actionLine = new JsonObject
		{
			["update"] = new JsonObject
			{
				["_index"] = index,
				["_id"] = id
			}
		};

		// Matches HashedBulkUpdate's script: if hash matches -> noop, else replace source
		var bodyLine = new JsonObject
		{
			["scripted_upsert"] = true,
			["upsert"] = new JsonObject(),
			["script"] = new JsonObject
			{
				["source"] = $"if (ctx._source.{hashField} == params.hash) {{ ctx.op = 'noop' }} else {{ ctx._source = params.doc; ctx._source.{hashField} = params.hash }}",
				["params"] = new JsonObject
				{
					["hash"] = hashValue,
					["doc"] = docNode
				}
			}
		};

		var bulkBody = $"{actionLine.ToJsonString()}\n{bodyLine.ToJsonString()}\n";
		var response = await _transport.PostAsync<StringResponse>(
			"/_bulk",
			PostData.String(bulkBody),
			CancellationToken.None
		);
		response.ApiCallDetails.HasSuccessfulStatusCode.Should().BeTrue(
			$"Bulk update failed: {response.ApiCallDetails.DebugInformation}");

		var bulkResult = JsonNode.Parse(response.Body);
		bulkResult.Should().NotBeNull("bulk response body should be valid JSON");
		bulkResult!["errors"].Should().NotBeNull("bulk response should contain an 'errors' field");
		bulkResult["errors"]!.GetValue<bool>().Should().BeFalse(
			$"Bulk response contained item errors: {response.Body}");
	}

	private sealed record TestDocument(string Url, string ContentHash, DateTimeOffset? ContentLastUpdated);
}
