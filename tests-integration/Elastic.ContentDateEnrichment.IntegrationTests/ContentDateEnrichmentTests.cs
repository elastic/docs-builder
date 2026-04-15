// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
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

		// Step 2: Verify that bulk update (scripted upsert) does NOT trigger the pipeline
		await IndexViaScriptedUpsert(index, "url2", "hash_b", "Doc 2");
		await RefreshIndex(index);
		var afterUpdate = await GetDocument(index, "url2");
		afterUpdate.ContentLastUpdated.Should().BeNull(
			"bulk update action should skip the default_pipeline — this is the bug");

		// Step 3: Verify that ResolveContentDatesAsync fixes documents missed by the pipeline
		await enrichment.ResolveContentDatesAsync(index, CancellationToken.None);
		await RefreshIndex(index);
		var afterResolve = await GetDocument(index, "url2");
		afterResolve.ContentLastUpdated.Should().NotBeNull(
			"ResolveContentDatesAsync should apply the pipeline via _update_by_query");
		afterResolve.ContentLastUpdated.Value.Year.Should().BeGreaterThanOrEqualTo(2026);
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
	/// Indexes documents with pipeline=_none to simulate HashedBulkUpdate's bulk update actions
	/// which bypass ingest pipelines.
	/// </summary>
	private async Task IndexDocumentsDirectly(string index, params (string url, string contentHash, string title)[] docs)
	{
		foreach (var (url, contentHash, title) in docs)
		{
			var doc = new JsonObject
			{
				["url"] = url,
				["content_hash"] = contentHash,
				["title"] = title
			};

			var response = await _transport.PutAsync<StringResponse>(
				$"{index}/_doc/{url}?pipeline=_none",
				PostData.String(doc.ToJsonString()),
				CancellationToken.None
			);
			response.ApiCallDetails.HasSuccessfulStatusCode.Should().BeTrue(
				$"Failed to index document {url}: {response.ApiCallDetails.DebugInformation}");
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

	/// <summary>
	/// Uses the bulk API with a scripted upsert update action — exactly what HashedBulkUpdate does.
	/// This does NOT trigger the default_pipeline or final_pipeline.
	/// </summary>
	private async Task IndexViaScriptedUpsert(string index, string url, string contentHash, string title)
	{
		var doc = new JsonObject
		{
			["url"] = url,
			["content_hash"] = contentHash,
			["title"] = title
		};

		// This is the exact bulk format that HashedBulkUpdate uses
		var actionLine = new JsonObject
		{
			["update"] = new JsonObject
			{
				["_index"] = index,
				["_id"] = url
			}
		};
		var bodyLine = new JsonObject
		{
			["scripted_upsert"] = true,
			["upsert"] = new JsonObject(),
			["script"] = new JsonObject
			{
				["source"] = "ctx._source = params.doc",
				["params"] = new JsonObject
				{
					["doc"] = doc
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

	private sealed record TestDocument(string Url, string ContentHash, DateTimeOffset? ContentLastUpdated);
}
