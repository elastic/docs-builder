// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using AwesomeAssertions;
using Elastic.ApiExplorer.Model;
using Elastic.Documentation.Configuration;

namespace Elastic.ApiExplorer.Tests;

/// <summary>The types documented on a page of their own, matched by schema id.</summary>
public class TypePagesTests
{
	[Test]
	public void All_ListsEachPageOnceWithItsTitle() =>
		TypePages
			.All
			.Select(p => (p.SchemaId, p.Title))
			.Should()
			.Equal(
				("_types.query_dsl.QueryContainer", "Query"),
				("_types.aggregations.AggregationContainer", "Aggregation"),
				("_types.aggregations.Aggregate", "Aggregation result"),
				("ingest._types.ProcessorContainer", "Processor"),
				("_types.mapping.Property", "Field mapping")
			);

	[Test]
	public async Task All_EverySchemaIdExistsInTheElasticsearchSpec()
	{
		var specPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs", "elasticsearch.json");
		var document = await OpenApiReader.Instance.ReadAsync(new FileSystem().FileInfo.New(specPath));

		var schemas = document!.Components!.Schemas!;
		TypePages
			.All
			.Select(p => p.SchemaId)
			.Should()
			.OnlyContain(id => schemas.ContainsKey(id), "a renamed schema would silently lose its page");
	}

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
