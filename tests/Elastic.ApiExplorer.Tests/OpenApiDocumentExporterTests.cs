// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using AwesomeAssertions;
using Elastic.ApiExplorer.Export;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Versions;
using Microsoft.OpenApi;

namespace Elastic.ApiExplorer.Tests;

public class OpenApiDocumentExporterTests
{
	private static readonly HttpClient HttpClient = new();
	private const string BaseUrl = "https://www.elastic.co";

	[Fact(Skip = "This spams elastic.co, run this manually")]
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
		await foreach (var doc in exporter.ExportDocuments(limitPerSource, TestContext.Current.CancellationToken))
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
			CancellationToken = TestContext.Current.CancellationToken
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

	[Fact]
	public void ConvertToDocuments_DescriptionWithBumpHtml_LeavesNoHtmlAndNoRebuiltList()
	{
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

		var spec = new OpenApiDocument
		{
			Paths = new OpenApiPaths
			{
				["/_search"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new OpenApiOperation
						{
							OperationId = "search",
							Summary = "Run a search",
							Description =
								"""
								**All methods and paths for this operation:**

								<div>
								  <span class="operation-verb get">GET</span>
								  <span class="operation-path">/_search</span>
								  </div>

								Returns hits that match the query defined in the request.
								"""
						}
					}
				}
			}
		};

		var exporter = new OpenApiDocumentExporter(versionsConfiguration);
		var documents = exporter.ConvertToDocuments(spec, "elasticsearch").ToArray();

		documents.Should().ContainSingle();
		var description = documents[0].Description;
		description.Should().NotBeNull();
		description.Should().NotContain("<div>");
		description.Should().NotContain("<span");
		description.Should().NotContain("- **GET**");
		description.Should().Contain("Returns hits that match the query");
	}
}
