// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Export;
using Elastic.ApiExplorer.Model;
using Elastic.ApiExplorer.Operations;
using Elastic.Documentation.Configuration.Versions;
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
	private static readonly VersionsConfiguration VersionsConfiguration = TestHelpers.CreateStackVersionsConfiguration(
		currentMajor: 9,
		currentMinor: 2
	);

	[Fact]
	public void BulkOperation_SearchTitleContainsTheRawOperationIdWithUnderscore()
	{
		var exporter = new OpenApiDocumentExporter(VersionsConfiguration);

		var docs = exporter.ConvertToDocuments(TestHelpers.CreateBulkSpec(), "elasticsearch").ToArray();

		docs.Should().HaveCount(1);
		var doc = docs[0];

		doc.Title.Should().Be("Bulk index or delete documents");
		doc.ApiVersion.Should().Be("latest");
		doc.Path.Should().Be("/docs/api/doc/elasticsearch/operation/operation-_bulk");
		doc.Parents.Should().HaveCount(3);
		doc.Parents[1].Title.Should().Be("Elasticsearch API");
		doc.Parents[2].Title.Should().Be("latest");
		doc.Parents[2].Path.Should().Be("/docs/api/doc/elasticsearch");
		doc.SearchTitle.Should().Be("Bulk index or delete documents - Elasticsearch API - _bulk - PUT /_bulk");
		doc.SearchTitle.Should().Contain("_bulk");
		doc.SearchTitle.Should().Contain("PUT /_bulk");
	}

	[Fact]
	public void Operation_SummaryWithTrailingNewline_DoesNotLeakIntoTitleOrSearchTitle()
	{
		var exporter = new OpenApiDocumentExporter(VersionsConfiguration);

		var docs = exporter.ConvertToDocuments(TestHelpers.CreateBulkSpec("Bulk index or delete documents\n"), "elasticsearch").ToArray();

		docs.Should().HaveCount(1);
		var doc = docs[0];

		doc.Title.Should().Be("Bulk index or delete documents");
		doc.SearchTitle.Should().Be("Bulk index or delete documents - Elasticsearch API - _bulk - PUT /_bulk");
		doc.Title.Should().NotContain("\n");
		doc.SearchTitle.Should().NotContain("\n");
	}

	[Fact]
	public void Operation_BlankSummary_FallsBackToOperationId()
	{
		var exporter = new OpenApiDocumentExporter(VersionsConfiguration);

		var docs = exporter.ConvertToDocuments(TestHelpers.CreateBulkSpec("   "), "elasticsearch").ToArray();

		docs.Should().HaveCount(1);
		var doc = docs[0];

		doc.Title.Should().Be("_bulk");
		doc.Title.Should().NotContain("API");
	}

	[Fact]
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

		var docs = new OpenApiDocumentExporter(VersionsConfiguration).ConvertToDocuments(spec, "elasticsearch").ToArray();

		docs.Should().HaveCount(1);
		var doc = docs[0];
		doc.Title.Should().Be("Get index information");
		doc.SearchTitle.Should().Contain("indices.get");
		doc.SearchTitle.Should().Contain("indices get");
		doc.SearchTitle.Should().Contain("GET /{index}");
	}

	[Fact]
	public void NumericMoniker_WritesVersionedPathAndV8Parent()
	{
		var docs = new OpenApiDocumentExporter(VersionsConfiguration)
			.ConvertToDocuments(TestHelpers.CreateBulkSpec(), "elasticsearch", "8")
			.ToArray();

		docs.Should().HaveCount(1);
		var doc = docs[0];
		doc.ApiVersion.Should().Be("v8");
		doc.Path.Should().Be("/docs/api/doc/elasticsearch/v8/operation/operation-_bulk");
		doc.Parents.Select(p => p.Title).Should().Equal("API", "Elasticsearch API", "v8");
		doc.Parents[2].Path.Should().Be("/docs/api/doc/elasticsearch/v8");
	}
}
