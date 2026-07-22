// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// Bitmask that controls which clauses are included in the Elasticsearch query.
/// Used exclusively via <see cref="SearchRequest.Components"/> for load-test diagnosis
/// — pass <c>null</c> (the default) for the normal production query.
///
/// Build-up pattern: start with <c>probe=0</c> (match_all floor) and OR in individual
/// bits to isolate which clause drives latency under load.
///
/// Convenience composites: <see cref="Lexical"/>, <see cref="Semantic"/>,
/// <see cref="Aggregations"/>, <see cref="All"/>.
/// </summary>
[Flags]
public enum SearchQueryComponents : uint
{
	// --- Lexical sub-clauses (bits 0–9) ---

	/// <summary>Bool-prefix multimatch on <c>search_title.completion*</c> (boost 3).</summary>
	Completion = 1 << 0,   // 1

	/// <summary>Best-fields multimatch on <c>stripped_body</c> (boost 0.1).</summary>
	Body = 1 << 1,   // 2

	/// <summary>Term on <c>title.keyword</c> (lowercased), with synonym expansion.</summary>
	TitleKeyword = 1 << 2,   // 4

	/// <summary>ConstantScore TermQuery on <c>title.starts_with</c> (queries ≤ 10 chars).</summary>
	TitleStartsWith = 1 << 3, // 8

	/// <summary>ConstantScore MatchQuery on <c>url.match</c> (single-token queries only).</summary>
	UrlMatch = 1 << 4,   // 16

	/// <summary>Phrase multimatch on <c>stripped_body</c> (3+ token queries only).</summary>
	Phrase = 1 << 5,   // 32

	/// <summary>BoolQuery MustNot filter: exclude bare <c>/docs</c> roots and <c>hidden</c> docs.</summary>
	DocumentFilter = 1 << 6,  // 64

	/// <summary>2× RankFeatureQuery (nav depth/toc) + 2× TermQuery section boosts in <c>should</c>.</summary>
	Scoring = 1 << 7,   // 128

	/// <summary>BoostingQuery wrapping: negatively boosts documents matching diminish terms.</summary>
	Diminish = 1 << 8,   // 256

	/// <summary>RuleQuery wrap using the configured ruleset.</summary>
	Rules = 1 << 9,   // 512

	// --- Semantic sub-clauses (bits 10–13; each = one kNN inference call) ---

	/// <summary>SemanticQuery on <c>title.semantic_text</c> (boost 5).</summary>
	SemTitle = 1 << 10,  // 1024

	/// <summary>SemanticQuery on <c>abstract.semantic_text</c> (boost 3).</summary>
	SemAbstract = 1 << 11,  // 2048

	/// <summary>SemanticQuery on <c>ai_rag_optimized_summary.semantic_text</c> (boost 4).</summary>
	SemRag = 1 << 12,  // 4096

	/// <summary>SemanticQuery on <c>ai_questions.semantic_text</c> (boost 2).</summary>
	SemQuestions = 1 << 13,  // 8192

	// --- Request-level ES features (bits 14–18) ---

	/// <summary>Terms aggregation on <c>content_type</c>.</summary>
	AggType = 1 << 14,  // 16384

	/// <summary>Terms aggregation on <c>navigation_section</c>.</summary>
	AggSection = 1 << 15,  // 32768

	/// <summary>Terms aggregation on <c>related_products.id</c> (size 100).</summary>
	AggProduct = 1 << 16,  // 65536

	/// <summary>Highlight block on <c>title</c> + <c>stripped_body</c>.</summary>
	Highlight = 1 << 17,  // 131072

	/// <summary>Honor the <c>SortBy</c> field (Recent/Alpha); omitting reverts to score order.</summary>
	Sort = 1 << 18,  // 262144

	// --- Composites ---

	/// <summary>All 10 lexical sub-clauses combined.</summary>
	Lexical = Completion | Body | TitleKeyword | TitleStartsWith | UrlMatch | Phrase
					| DocumentFilter | Scoring | Diminish | Rules,  // 1023

	/// <summary>All 4 semantic sub-clauses combined (each triggers a separate kNN inference call).</summary>
	Semantic = SemTitle | SemAbstract | SemRag | SemQuestions,  // 15360

	/// <summary>All 3 aggregations.</summary>
	Aggregations = AggType | AggSection | AggProduct,  // 114688

	/// <summary>Every clause on — equivalent to the production query (subject to per-clause gating).</summary>
	All = (1u << 19) - 1  // 524287
}
