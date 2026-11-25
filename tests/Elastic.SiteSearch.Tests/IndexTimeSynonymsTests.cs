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
	[Test]
	public void Docs_ContainsAggAliasRule() => IndexTimeSynonyms.Docs.Should().Contain("agg, aggs => aggregations");

	[Test]
	public void Docs_ContainsEsqlAliasRule() => IndexTimeSynonyms.Docs.Should().Contain("esql, es|ql => esql");

	[Test]
	public void Docs_ContainsDataStreamsAliasRules() => IndexTimeSynonyms.Docs.Should().Contain("data-streams, datastreams");

	// A multi-word spelling in an equivalence stacks a token graph at one position, and the search_as_you_type
	// shingle subfields reject the document: "Data streams" failed with "first position increment must be > 0".
	[Test]
	public void Docs_Equivalences_ListSingleTokenSpellingsOnly() =>
		IndexTimeSynonyms
			.Docs
			.Where(rule => !rule.Contains("=>"))
			.SelectMany(rule => rule.Split(','))
			.Should()
			.OnlyContain(spelling => !spelling.Trim().Contains(' '));
}
