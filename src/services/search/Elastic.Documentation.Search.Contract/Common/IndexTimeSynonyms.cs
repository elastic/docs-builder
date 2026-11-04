// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Search.Contract.Mapping;

/// <summary>
/// Fixed index-time synonym expansions baked into each index at creation. These are
/// structural — not updateable at runtime — so they are hardcoded rather than loaded
/// from the shared synonym set. Keep in sync with the search.yml synonyms that have
/// fixed-expansion semantics (esql, data-stream, data-streams, machine-learning, agg).
/// </summary>
public static class IndexTimeSynonyms
{
	public static readonly string[] Docs =
	[
		"data-stream, data stream, datastream => data-streams",
		"data-streams, data streams, datastreams",
		"esql, es|ql => esql",
		"machine-learning, machine learning, ml => machine learning",
		"agg, aggs => aggregations",
	];
}
