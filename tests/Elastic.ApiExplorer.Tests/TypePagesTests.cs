// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Model;

namespace Elastic.ApiExplorer.Tests;

/// <summary>The types documented on a page of their own, matched by schema id.</summary>
public class TypePagesTests
{
	[Test]
	public void All_ListsEachPageOnceWithItsCategory() =>
		TypePages
			.All
			.Select(p => (p.DisplayName, p.Category.Title))
			.Should()
			.Equal(
				("QueryContainer", "Query DSL"),
				("AggregationContainer", "Aggregations"),
				("Aggregate", "Aggregations"),
				("ProcessorContainer", "Ingest")
			);

	[Test]
	[Arguments("ingest._types.ProcessorContainer", null, true)]
	[Arguments("ingest._types.ProcessorContainer", "ingest._types.ProcessorContainer", false)]
	[Arguments("ingest._types.ProcessorContainer", "_types.query_dsl.QueryContainer", true)]
	[Arguments("my_plugin.ProcessorContainer", null, false)]
	[Arguments(null, null, false)]
	public void Links_MatchesTheSchemaIdAndNeverTheCurrentPage(string? schemaId, string? currentPage, bool links) =>
		TypePages.Links(schemaId, currentPage).Should().Be(links);

	[Test]
	public void Url_UsesTheSchemaMoniker() =>
		TypePages
			.Url("/api/doc/elasticsearch/", "ingest._types.ProcessorContainer")
			.Should()
			.Be("/api/doc/elasticsearch/types/ingest-_types-processorcontainer");
}
