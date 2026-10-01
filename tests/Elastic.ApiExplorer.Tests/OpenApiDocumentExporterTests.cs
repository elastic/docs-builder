// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using AwesomeAssertions;
using Elastic.ApiExplorer.Export;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Contract;
using Elastic.Documentation.Versions;
using Microsoft.OpenApi;
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
		const int limitPerSource = 300; // Get 50 from each source (Elasticsearch and Kibana)

		// Act - Collect all documents, tracking source
		var documents = new List<(string Url, string Source)>();
		await foreach (var doc in exporter.ExportDocuments(limitPerSource, TestContext.Current!.Execution.CancellationToken))
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

		var documents = new List<DocumentationDocument>();
		await foreach (var doc in exporter.ExportDocuments(limitPerSource: 100, TestContext.Current!.Execution.CancellationToken))
			documents.Add(doc);

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

		return new OpenApiDocumentExporter(StackVersions()).ConvertToDocuments(spec, "elasticsearch").Single();
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
}
