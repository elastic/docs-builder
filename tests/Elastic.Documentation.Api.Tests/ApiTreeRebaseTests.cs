// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Contract;

namespace Elastic.Documentation.Api.Tests;

public class ApiTreeRebaseTests
{
	private static ApiOperationMetadata Elasticsearch(params (string Tree, string Root)[] trees) =>
		new()
		{
			Endpoints = ["_search"],
			Method = "POST",
			Path = "/_search",
			OperationId = "search",
			Key = "elasticsearch",
			Trees = trees.Select(t => new ApiTreeEntry { Tree = t.Tree, Root = t.Root }).ToArray()
		};

	private static readonly ApiOperationMetadata MainAndEight = Elasticsearch(
		("main", "/docs/api/doc/elasticsearch"),
		("8", "/docs/api/doc/elasticsearch/v8")
	);

	[Test]
	public void For_ScopeInASecondTree_SwapsTheFirstTreesRootForItsRoot() =>
		ApiTreeRebase
			.For(MainAndEight, new ApiTreeScope("elasticsearch", "8"))
			.Should()
			.Be(new ApiTreeRebase("/docs/api/doc/elasticsearch", "/docs/api/doc/elasticsearch/v8"));

	[Test]
	public void For_ScopeInTheFirstTree_NeedsNoSwap() =>
		ApiTreeRebase.For(MainAndEight, new ApiTreeScope("elasticsearch", "main")).Should().BeNull();

	[Test]
	public void For_TreeTheDocumentDoesNotServe_NeedsNoSwap() =>
		ApiTreeRebase.For(MainAndEight, new ApiTreeScope("elasticsearch", "7")).Should().BeNull();

	[Test]
	public void For_AnotherApi_NeedsNoSwap() => ApiTreeRebase.For(MainAndEight, new ApiTreeScope("cloud", "8")).Should().BeNull();

	[Test]
	public void For_NoScopeOrNoTrees_NeedsNoSwap()
	{
		ApiTreeRebase.For(MainAndEight, null).Should().BeNull();
		ApiTreeRebase.For(Elasticsearch(), new ApiTreeScope("elasticsearch", "8")).Should().BeNull();
		ApiTreeRebase.For(null, new ApiTreeScope("elasticsearch", "8")).Should().BeNull();
	}

	[Test]
	[Arguments("/docs/api/doc/elasticsearch/operation/operation-search#req-knn", "/docs/api/doc/elasticsearch/v8/operation/operation-search#req-knn")]
	[Arguments("/docs/api/doc/elasticsearch/operation/operation-search", "/docs/api/doc/elasticsearch/v8/operation/operation-search")]
	[Arguments("/docs/api/doc/elasticsearch", "/docs/api/doc/elasticsearch/v8")]
	[Arguments("/docs/api", "/docs/api")]
	[Arguments("/docs/api/doc/elasticsearch-serverless/operation/operation-search", "/docs/api/doc/elasticsearch-serverless/operation/operation-search")]
	public void Apply_Url_SwapsTheRootOnlyForUrlsUnderIt(string url, string expected) =>
		new ApiTreeRebase("/docs/api/doc/elasticsearch", "/docs/api/doc/elasticsearch/v8").Apply(url).Should().Be(expected);

	[Test]
	public void Apply_VariantDocumentForTheDefaultTree_SwapsBackToTheShorterRoot() =>
		ApiTreeRebase.For(
			Elasticsearch(("9", "/docs/api/doc/elasticsearch/v9"), ("main", "/docs/api/doc/elasticsearch")),
			new ApiTreeScope("elasticsearch", "main")
		)!
			.Apply("/docs/api/doc/elasticsearch/v9/operation/operation-search")
			.Should()
			.Be("/docs/api/doc/elasticsearch/operation/operation-search");
}
