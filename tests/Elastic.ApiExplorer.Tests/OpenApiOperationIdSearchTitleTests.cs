// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Export;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Search.Contract;
using Elastic.Documentation.Versions;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

/// <summary>
/// The Bulk API's operation id ("_bulk") must survive into search_title verbatim, underscore
/// intact — it's a high-value search token users type literally. Exercises
/// OpenApiDocumentExporter.ConvertToDocuments directly against an in-memory spec, no network.
/// </summary>
public class OpenApiOperationIdSearchTitleTests
{
	private static readonly VersionsConfiguration VersionsConfiguration = new()
	{
		VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>
		{
			{
				VersioningSystemId.Stack,
				new VersioningSystem { Id = VersioningSystemId.Stack, Base = new SemVersion(8, 0, 0), Current = new SemVersion(9, 2, 0) }
			}
		}
	};

	private static OpenApiDocument CreateBulkSpec() =>
		new()
		{
			Paths = new OpenApiPaths
			{
				["/_bulk"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Put] = new OpenApiOperation { OperationId = "_bulk", Summary = "Bulk index or delete documents" }
					}
				}
			}
		};

	[Test]
	public void BulkOperation_SearchTitleContainsTheRawOperationIdWithUnderscore()
	{
		var exporter = new OpenApiDocumentExporter(VersionsConfiguration);

		var docs = exporter.ConvertToDocuments(new IndexedTree("elasticsearch", "elasticsearch", "main", CreateBulkSpec())).ToArray();

		docs.Should().HaveCount(1);
		var doc = docs[0];

		doc.Title.Should().Be("Bulk index or delete documents");
		doc.Parents.Should().HaveCount(2);
		doc.Parents[1].Title.Should().Be("Elasticsearch API");
		doc.SearchTitle.Should().Be("Bulk index or delete documents - Elasticsearch API - _bulk - PUT /_bulk - bulk");
		doc.SearchTitle.Should().Contain("_bulk");
		doc.SearchTitle.Should().Contain("PUT /_bulk");
	}

	private static OpenApiDocument CreateSpecWithSummaryWhitespace(string summary) =>
		new()
		{
			Paths = new OpenApiPaths
			{
				["/_bulk"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Put] = new OpenApiOperation { OperationId = "_bulk", Summary = summary }
					}
				}
			}
		};

	[Test]
	public void Operation_SummaryWithTrailingNewline_DoesNotLeakIntoTitleOrSearchTitle()
	{
		var exporter = new OpenApiDocumentExporter(VersionsConfiguration);

		var docs = exporter.ConvertToDocuments(
			new IndexedTree("elasticsearch", "elasticsearch", "main", CreateSpecWithSummaryWhitespace("Bulk index or delete documents\n"))
		).ToArray();

		docs.Should().HaveCount(1);
		var doc = docs[0];

		doc.Title.Should().Be("Bulk index or delete documents");
		doc.SearchTitle.Should().Be("Bulk index or delete documents - Elasticsearch API - _bulk - PUT /_bulk - bulk");
		doc.Title.Should().NotContain("\n");
		doc.SearchTitle.Should().NotContain("\n");
	}

	[Test]
	public void Operation_BlankSummary_FallsBackToOperationId()
	{
		var exporter = new OpenApiDocumentExporter(VersionsConfiguration);

		var docs = exporter.ConvertToDocuments(
			new IndexedTree("elasticsearch", "elasticsearch", "main", CreateSpecWithSummaryWhitespace("   "))
		).ToArray();

		docs.Should().HaveCount(1);
		var doc = docs[0];

		doc.Title.Should().Be("_bulk");
		doc.Title.Should().NotContain("API");
	}

	[Test]
	public void DottedOperationId_IsSearchableAsWrittenAndAsSpaceSeparatedTokens()
	{
		var spec = new OpenApiDocument
		{
			Paths = new OpenApiPaths
			{
				["/{index}"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new OpenApiOperation { OperationId = "indices.get", Summary = "Get index information" }
					}
				}
			}
		};

		var docs = new OpenApiDocumentExporter(VersionsConfiguration)
			.ConvertToDocuments(new IndexedTree("elasticsearch", "elasticsearch", "main", spec))
			.ToArray();

		docs.Should().HaveCount(1);
		var doc = docs[0];
		doc.Title.Should().Be("Get index information");
		doc.SearchTitle.Should().Contain("indices.get");
		doc.SearchTitle.Should().Contain("indices get");
		doc.SearchTitle.Should().Contain("GET /{index}");
	}

	[Test]
	public async Task ConvertToDocuments_TaggedOperations_IndexGroupAndWordsInSearchTitle()
	{
		var spec = await TestSpecs.LoadSpecAsync(
			"""
			{"openapi":"3.0.3","info":{"title":"t","version":"1"},
			 "tags":[{"name":"ml anomaly","x-displayName":"Machine learning anomaly detection"}],
			 "paths":{
			   "/_ml/anomaly_detectors/{job_id}/_flush":{"post":{"operationId":"ml-flush-job","summary":"Force buffered data to be processed","tags":["ml anomaly"],"parameters":[{"name":"job_id","in":"path","required":true,"schema":{"type":"string"}}],"responses":{"200":{"description":"ok"}}}},
			   "/_ml/anomaly_detectors/{job_id}/_close":{"post":{"operationId":"ml-close-job","summary":"Close anomaly detection jobs","deprecated":true,"tags":["ml anomaly"],"parameters":[{"name":"job_id","in":"path","required":true,"schema":{"type":"string"}}],"responses":{"200":{"description":"ok"}}}},
			   "/_bulk":{"put":{"operationId":"_bulk","summary":"Bulk index or delete documents","responses":{"200":{"description":"ok"}}}}
			 }}
			"""
		);

		var docs = new OpenApiDocumentExporter(VersionsConfiguration)
			.ConvertToDocuments(new IndexedTree("elasticsearch", "elasticsearch", "main", spec))
			.ToDictionary(d => d.Api!.OperationId);

		var flush = docs["ml-flush-job"];
		flush
			.SearchTitle
			.Should()
			.Be(
				"Force buffered data to be processed - Elasticsearch API - ml-flush-job - POST /_ml/anomaly_detectors/{job_id}/_flush - Machine learning anomaly detection - ml flush job anomaly detectors"
			);
		flush.Headings.Should().Equal("ml anomaly");
		flush
			.Parents
			.Should()
			.BeEquivalentTo(
				[
					new ParentDocument { Title = "API", Path = "/docs/api" },
					new ParentDocument { Title = "Elasticsearch API", Path = "/docs/api/doc/elasticsearch" },
					new ParentDocument
					{
						Title = "Machine learning anomaly detection",
						Path = "/docs/api/doc/elasticsearch/group/endpoint-ml-anomaly"
					}
				],
				o => o.WithStrictOrdering()
			);
		flush.Api!.Deprecated.Should().BeFalse();
		flush.Api.Words.Should().Equal("ml", "flush", "job", "anomaly", "detectors");
		flush.Api.Group.Should().Be("Machine learning anomaly detection");
		flush.Api.IdWords.Should().Be(3);
		docs["ml-close-job"].Api!.Deprecated.Should().BeTrue();
		var bulk = docs["_bulk"];
		bulk.SearchTitle.Should().Be("Bulk index or delete documents - Elasticsearch API - _bulk - PUT /_bulk - bulk");
		bulk.Api!.Words.Should().Equal("bulk");
		bulk.Api.Group.Should().Be("");
		bulk.Api.IdWords.Should().Be(1);
		bulk.Api.EndpointsBare.Should().Equal("bulk");
		bulk
			.Parents
			.Should()
			.BeEquivalentTo(
				[
					new ParentDocument { Title = "API", Path = "/docs/api" },
					new ParentDocument { Title = "Elasticsearch API", Path = "/docs/api/doc/elasticsearch" }
				],
				o => o.WithStrictOrdering()
			);
	}
}
