// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Search.Contract.Mapping;

namespace Elastic.SiteSearch.Tests;

/// <summary>
/// Verifies the fixed index-time synonym rules baked into each index — see
/// <see cref="IndexTimeSynonyms"/>. Keep in sync with config/search.yml.
/// </summary>
public class IndexTimeSynonymsTests
{
	[Fact]
	public void Docs_ContainsAggAliasRule() =>
		IndexTimeSynonyms.Docs.Should().Contain("agg, aggs => aggregations");

	[Fact]
	public void Docs_ContainsEsqlAliasRule() =>
		IndexTimeSynonyms.Docs.Should().Contain("esql, es|ql => esql");

	[Fact]
	public void Docs_ContainsDataStreamsAliasRules() =>
		IndexTimeSynonyms.Docs.Should().Contain("data-streams, data streams, datastreams");
}
