// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Indexing.Exporters.Elasticsearch;
using Elastic.Documentation.Search.Contract;

namespace Elastic.Documentation.Indexing.Tests;

public class DocumentHashTests
{
	private static DocumentationDocument Operation(params ApiPropertyEntry[] properties) =>
		new()
		{
			Title = "Search",
			SearchTitle = "Search",
			Path = "/docs/api/doc/elasticsearch/operation/operation-search",
			Body = "Run a search.",
			Api = new ApiOperationMetadata
			{
				Endpoints = ["/_search"],
				Method = "POST",
				Path = "/_search",
				OperationId = "search",
				Properties = properties
			}
		};

	private static ApiPropertyEntry Property(string path) =>
		new()
		{
			Name = path.Split('.')[^1],
			Path = path,
			Type = "boolean",
			Location = "request",
			Anchor = $"req-{path.Replace('.', '-')}",
			Description = "Count hits"
		};

	private static string Hash(DocumentationDocument doc) =>
		ElasticsearchMarkdownExporter.DocumentHash(doc, "semantic", "lexical", "synonyms");

	[Test]
	public void DocumentHash_SameProperties_IsStable() => Hash(Operation(Property("knn"))).Should().Be(Hash(Operation(Property("knn"))));

	[Test]
	public void DocumentHash_PropertyPathChanges_ChangesTheHash() =>
		Hash(Operation(Property("track_total_hits"))).Should().NotBe(Hash(Operation(Property("knn.field"))));

	[Test]
	public void DocumentHash_PropertyAdded_ChangesTheHash() =>
		Hash(Operation(Property("knn"))).Should().NotBe(Hash(Operation(Property("knn"), Property("size"))));

	[Test]
	public void DocumentHash_HiddenFlips_ChangesTheHash() =>
		Hash(Operation(Property("knn")) with { Hidden = true }).Should().NotBe(Hash(Operation(Property("knn"))));

	[Test]
	public void DocumentHash_ParentChanges_ChangesTheHash() =>
		Hash(Operation(Property("knn.field") with { Parent = "knn" })).Should().NotBe(Hash(Operation(Property("knn.field"))));
}
