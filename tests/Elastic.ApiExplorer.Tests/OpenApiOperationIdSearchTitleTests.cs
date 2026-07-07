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

		var docs = exporter.ConvertToDocuments(CreateBulkSpec(), "elasticsearch").ToArray();

		docs.Should().HaveCount(1);
		var doc = docs[0];

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

		var docs = exporter.ConvertToDocuments(CreateSpecWithSummaryWhitespace("Bulk index or delete documents\n"), "elasticsearch").ToArray();

		docs.Should().HaveCount(1);
		var doc = docs[0];

		doc.Title.Should().Be("Bulk index or delete documents - Elasticsearch API");
		doc.SearchTitle.Should().Be("Bulk index or delete documents - Elasticsearch API - _bulk");
		doc.Title.Should().NotContain("\n");
		doc.SearchTitle.Should().NotContain("\n");
	}

	[Fact]
	public void Operation_BlankSummary_FallsBackToOperationId()
	{
		var exporter = new OpenApiDocumentExporter(VersionsConfiguration);

		var docs = exporter.ConvertToDocuments(CreateSpecWithSummaryWhitespace("   "), "elasticsearch").ToArray();

		docs.Should().HaveCount(1);
		var doc = docs[0];

		doc.Title.Should().Be("_bulk - Elasticsearch API");
	}
}
