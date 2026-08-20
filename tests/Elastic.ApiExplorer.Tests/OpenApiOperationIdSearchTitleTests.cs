// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Export;
using Elastic.ApiExplorer.Model;
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
	private static readonly VersionsConfiguration VersionsConfiguration =
		TestHelpers.CreateStackVersionsConfiguration(currentMajor: 9, currentMinor: 2);

	private static OpenApiConvertContext ElasticsearchMain() =>
		new("elasticsearch", "main", new SemVersion(9, 2, 0), "Elasticsearch", "elasticsearch");

	private static OpenApiDocument CreateBulkSpec() => new()
	{
		Paths = new OpenApiPaths
		{
			["/_bulk"] = new OpenApiPathItem
			{
				Operations = new Dictionary<HttpMethod, OpenApiOperation>
				{
					[HttpMethod.Put] = new OpenApiOperation
					{
						OperationId = "_bulk",
						Summary = "Bulk index or delete documents"
					}
				}
			}
		}
	};

	[Fact]
	public void BulkOperation_SearchTitleContainsTheRawOperationIdWithUnderscore()
	{
		var exporter = new OpenApiDocumentExporter(VersionsConfiguration);

		var docs = exporter.ConvertToDocuments(CreateBulkSpec(), ElasticsearchMain()).ToArray();
		var operations = docs.Where(d => d.Path.Contains("/operation/", StringComparison.Ordinal)).ToArray();

		operations.Should().HaveCount(1);
		var doc = operations[0];

		doc.Title.Should().Be("Bulk index or delete documents - Elasticsearch API");
		doc.SearchTitle.Should().Be("Bulk index or delete documents - Elasticsearch API - _bulk");
		doc.SearchTitle.Should().Contain("_bulk");
	}

	private static OpenApiDocument CreateSpecWithSummaryWhitespace(string summary) => new()
	{
		Paths = new OpenApiPaths
		{
			["/_bulk"] = new OpenApiPathItem
			{
				Operations = new Dictionary<HttpMethod, OpenApiOperation>
				{
					[HttpMethod.Put] = new OpenApiOperation
					{
						OperationId = "_bulk",
						Summary = summary
					}
				}
			}
		}
	};

	[Fact]
	public void Operation_SummaryWithTrailingNewline_DoesNotLeakIntoTitleOrSearchTitle()
	{
		var exporter = new OpenApiDocumentExporter(VersionsConfiguration);

		var docs = exporter.ConvertToDocuments(CreateSpecWithSummaryWhitespace("Bulk index or delete documents\n"), ElasticsearchMain()).ToArray();
		var operations = docs.Where(d => d.Path.Contains("/operation/", StringComparison.Ordinal)).ToArray();

		operations.Should().HaveCount(1);
		var doc = operations[0];

		doc.Title.Should().Be("Bulk index or delete documents - Elasticsearch API");
		doc.SearchTitle.Should().Be("Bulk index or delete documents - Elasticsearch API - _bulk");
		doc.Title.Should().NotContain("\n");
		doc.SearchTitle.Should().NotContain("\n");
	}

	[Fact]
	public void Operation_BlankSummary_FallsBackToOperationId()
	{
		var exporter = new OpenApiDocumentExporter(VersionsConfiguration);

		var docs = exporter.ConvertToDocuments(CreateSpecWithSummaryWhitespace("   "), ElasticsearchMain()).ToArray();
		var operations = docs.Where(d => d.Path.Contains("/operation/", StringComparison.Ordinal)).ToArray();

		operations.Should().HaveCount(1);
		var doc = operations[0];

		doc.Title.Should().Be("_bulk - Elasticsearch API");
	}
}
