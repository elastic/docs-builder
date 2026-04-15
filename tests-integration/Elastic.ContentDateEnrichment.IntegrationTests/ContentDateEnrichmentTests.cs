// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Elastic.Documentation.Search;
using Elastic.Documentation.Serialization;
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
/// actions that skip ingest pipelines.
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

	[Fact]
	public async Task FirstRun_AllDocumentsGetCurrentTimestamp()
	{
		var enrichment = CreateEnrichment("first-run");
		var index = "test-first-run";

		// Arrange: initialize enrichment infrastructure (empty lookup)
		await enrichment.InitializeAsync(CancellationToken.None);
		await CreateTestIndex(index, enrichment.PipelineName);
		await IndexDocumentsDirectly(index,
			("url1", "hash_a", "Doc 1"),
			("url2", "hash_b", "Doc 2")
		);
		await RefreshIndex(index);

		// Act: run the post-indexing resolution
		await enrichment.ResolveContentDatesAsync(index, CancellationToken.None);
		await RefreshIndex(index);

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
		var index = "test-unchanged";

		// === First run: establish baseline ===
		await enrichment.InitializeAsync(CancellationToken.None);
		await CreateTestIndex(index, enrichment.PipelineName);
		await IndexDocumentsDirectly(index,
			("url1", "hash_a", "Doc 1"),
			("url2", "hash_b", "Doc 2")
		);
		await RefreshIndex(index);
		await enrichment.ResolveContentDatesAsync(index, CancellationToken.None);
		await RefreshIndex(index);
		await enrichment.SyncLookupIndexAsync(index, CancellationToken.None);

		var firstRunDocs = await GetAllDocuments(index);
		var firstRunDates = firstRunDocs.ToDictionary(d => d.Url, d => d.ContentLastUpdated);

		// Wait to ensure timestamp separation
		await Task.Delay(TimeSpan.FromSeconds(1.5), TestContext.Current.CancellationToken);

		// Re-initialize for second run (re-executes enrich policy with updated lookup data)
		await enrichment.InitializeAsync(CancellationToken.None);

		// === Second run: same content_hash, simulating no content change ===
		await IndexDocumentsDirectly(index,
			("url1", "hash_a", "Doc 1 (re-indexed)"),
			("url2", "hash_b", "Doc 2 (re-indexed)")
		);
		await RefreshIndex(index);
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
		var index = "test-changed";

		// === First run: establish baseline ===
		await enrichment.InitializeAsync(CancellationToken.None);
		await CreateTestIndex(index, enrichment.PipelineName);
		await IndexDocumentsDirectly(index,
			("url1", "hash_a", "Doc 1"),
			("url2", "hash_b", "Doc 2")
		);
		await RefreshIndex(index);
		await enrichment.ResolveContentDatesAsync(index, CancellationToken.None);
		await RefreshIndex(index);
		await enrichment.SyncLookupIndexAsync(index, CancellationToken.None);

		var firstRunDocs = await GetAllDocuments(index);
		var firstRunDates = firstRunDocs.ToDictionary(d => d.Url, d => d.ContentLastUpdated);

		await Task.Delay(TimeSpan.FromSeconds(1.5), TestContext.Current.CancellationToken);

		await enrichment.InitializeAsync(CancellationToken.None);

		// === Second run: url1 content changed, url2 unchanged ===
		await IndexDocumentsDirectly(index,
			("url1", "hash_CHANGED", "Doc 1 (updated content)"),
			("url2", "hash_b", "Doc 2 (same content)")
		);
		await RefreshIndex(index);
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

	[Fact]
	public async Task BulkUpdateAction_SkipsPipeline_ResolveContentDatesFixes()
	{
		var enrichment = CreateEnrichment("bulk-update");
		var index = "test-bulk-update";

		await enrichment.InitializeAsync(CancellationToken.None);
		await CreateTestIndex(index, enrichment.PipelineName);

		// Step 1: Verify that _index action DOES trigger the pipeline
		await IndexViaIndexAction(index, "url1", "hash_a", "Doc 1");
		await RefreshIndex(index);
		var afterIndex = await GetDocument(index, "url1");
		afterIndex.ContentLastUpdated.Should().NotBeNull(
			"_index action should trigger the default_pipeline, setting content_last_updated");
		afterIndex.ContentLastUpdated.Value.Year.Should().BeGreaterThanOrEqualTo(2026);

		// Step 2: Verify that scripted upsert (HashedBulkUpdate) does NOT trigger the pipeline.
		// The full DocumentationDocument includes content_last_updated at default (0001-01-01).
		await IndexDocumentsDirectly(index, ("url2", "hash_b", "Doc 2"));
		await RefreshIndex(index);
		var afterUpdate = await GetDocument(index, "url2");
		afterUpdate.ContentLastUpdated.Should().NotBeNull(
			"scripted upsert writes content_last_updated from the serialized document");
		afterUpdate.ContentLastUpdated.Value.Should().BeBefore(DateTimeOffset.UnixEpoch,
			"scripted upsert skips the pipeline — content_last_updated is the unresolved default (0001-01-01)");

		// Step 3: Verify that ResolveContentDatesAsync fixes documents missed by the pipeline
		await enrichment.ResolveContentDatesAsync(index, CancellationToken.None);
		await RefreshIndex(index);
		var afterResolve = await GetDocument(index, "url2");
		afterResolve.ContentLastUpdated.Should().NotBeNull(
			"ResolveContentDatesAsync should apply the pipeline via _update_by_query");
		afterResolve.ContentLastUpdated.Value.Year.Should().BeGreaterThanOrEqualTo(2026);
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
			Type = "doc",
			Hash = "testhash123",
			ContentBodyHash = "contenthash123"
		};
		var serializedDoc = JsonSerializer.Serialize(doc, SourceGenerationContext.Default.DocumentationDocument);

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

		// The default DateTimeOffset (0001-01-01) should be stored — verify it's pre-epoch
		esDateValue.Should().NotBeNull("content_last_updated should be present in ES document");
		var esDate = DateTimeOffset.Parse(esDateValue, CultureInfo.InvariantCulture);
		esDate.Year.Should().Be(1, "default DateTimeOffset.MinValue should serialize as year 0001");
		esDate.Should().BeBefore(DateTimeOffset.UnixEpoch,
			"the default value should be well before 1970, making it safe to filter with 'must_not range gt 1970'");
	}

	/// <summary>
	/// Verifies the filter approach: after a scripted upsert writes a full DocumentationDocument
	/// with default content_last_updated, the must_not range filter correctly matches it.
	/// Also verifies a document with a real date is NOT matched.
	/// </summary>
	[Fact]
	public async Task FilteredResolve_SkipsDocumentsWithExistingDates()
	{
		var enrichment = CreateEnrichment("filtered");
		var index = "test-filtered";

		await enrichment.InitializeAsync(CancellationToken.None);
		await CreateTestIndex(index, enrichment.PipelineName);

		// doc1: content_last_updated at default (0001-01-01) via scripted upsert
		await IndexDocumentsDirectly(index, ("url1", "hash_a", "Doc 1"));

		// doc2: content_last_updated at default DateTimeOffset.MinValue (simulates production)
		var doc2 = new DocumentationDocument
		{
			Url = "url2",
			Title = "Doc 2",
			SearchTitle = "Doc 2",
			Type = "doc",
			ContentBodyHash = "hash_b"
		};
		var serialized2 = JsonSerializer.Serialize(doc2, SourceGenerationContext.Default.DocumentationDocument);
		await IndexFullDocumentViaScriptedUpsert(index, "url2", serialized2);

		// doc3: content_last_updated set to a real date (simulates unchanged doc from previous run)
		var doc3Json = new JsonObject
		{
			["url"] = "url3",
			["content_hash"] = "hash_c",
			["title"] = "Doc 3",
			["content_last_updated"] = "2026-01-15T12:00:00Z"
		};
		var putResponse = await _transport.PutAsync<StringResponse>(
			$"{index}/_doc/url3?pipeline=_none",
			PostData.String(doc3Json.ToJsonString()),
			CancellationToken.None
		);
		putResponse.ApiCallDetails.HasSuccessfulStatusCode.Should().BeTrue();

		await RefreshIndex(index);

		// Act: resolve content dates (with filter — after we apply the fix)
		await enrichment.ResolveContentDatesAsync(index, CancellationToken.None);
		await RefreshIndex(index);

		// Assert
		var docs = await GetAllDocuments(index);

		var result1 = docs.Single(d => d.Url == "url1");
		result1.ContentLastUpdated.Should().NotBeNull("doc1 had no date and should have been resolved");
		result1.ContentLastUpdated!.Value.Year.Should().BeGreaterThanOrEqualTo(2026);

		var result2 = docs.Single(d => d.Url == "url2");
		result2.ContentLastUpdated.Should().NotBeNull("doc2 had default date and should have been resolved");
		result2.ContentLastUpdated!.Value.Year.Should().BeGreaterThanOrEqualTo(2026);

		var result3 = docs.Single(d => d.Url == "url3");
		result3.ContentLastUpdated!.Value.Should().Be(
			DateTimeOffset.Parse("2026-01-15T12:00:00Z", CultureInfo.InvariantCulture),
			"doc3 already had a valid date and should NOT have been touched by the filter");
	}

	/// <summary>
	/// End-to-end test: first run resolves all docs. Second run only re-indexes
	/// the changed doc (unchanged docs are left alone, simulating HashedBulkUpdate noop).
	/// The filter should only process the changed doc.
	/// </summary>
	[Fact]
	public async Task SecondRun_WithFilter_OnlyChangedDocGetsNewDate()
	{
		var enrichment = CreateEnrichment("filter-e2e");
		var index = "test-filter-e2e";

		// === First run ===
		await enrichment.InitializeAsync(CancellationToken.None);
		await CreateTestIndex(index, enrichment.PipelineName);
		await IndexDocumentsDirectly(index,
			("url1", "hash_a", "Doc 1"),
			("url2", "hash_b", "Doc 2"),
			("url3", "hash_c", "Doc 3")
		);
		await RefreshIndex(index);
		await enrichment.ResolveContentDatesAsync(index, CancellationToken.None);
		await RefreshIndex(index);
		await enrichment.SyncLookupIndexAsync(index, CancellationToken.None);

		var firstRunDocs = await GetAllDocuments(index);
		var firstRunDates = firstRunDocs.ToDictionary(d => d.Url, d => d.ContentLastUpdated);

		await Task.Delay(TimeSpan.FromSeconds(1.5), TestContext.Current.CancellationToken);
		await enrichment.InitializeAsync(CancellationToken.None);

		// === Second run: only url1 changed, url2 and url3 are noops ===
		// Re-index ONLY url1 (simulating HashedBulkUpdate replacing a changed doc).
		// url2 and url3 are left untouched (simulating noop — content_last_updated preserved).
		await IndexDocumentsDirectly(index, ("url1", "hash_CHANGED", "Doc 1 updated"));
		await RefreshIndex(index);
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
				Type = "doc",
				Hash = contentHash,
				ContentBodyHash = contentHash
			};
			var serialized = JsonSerializer.Serialize(doc, SourceGenerationContext.Default.DocumentationDocument);
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

		// Matches HashedBulkUpdate's script: if hash matches → noop, else replace source
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
		var hasErrors = bulkResult?["errors"]?.GetValue<bool>() ?? false;
		hasErrors.Should().BeFalse($"Bulk response contained item errors: {response.Body}");
	}

	private sealed record TestDocument(string Url, string ContentHash, DateTimeOffset? ContentLastUpdated);
}
