// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Tests.Domain;

public class SearchTuningConstraintsTests
{
	[Test]
	[MethodDataSource(nameof(Axes))]
	public Task EveryDeclaredAxisHolds(AxisCase axis) => axis.RunAsync();

	public static IEnumerable<Func<AxisCase>> Axes() =>
		AutoTest
			.For(
				SearchTuningConstraints.All,
				GenAuto.Of<SearchTuning>(
					AutoMode.Plausible,
					o => o
						.From(SearchTuningConstraints.All)
						.Normalize<SynonymGroup>(g => new SynonymGroup([.. g.Terms.Select(t => t.Replace(' ', '-'))]))
						.Normalize<QueryRuleCriteria>(
							c =>
								c with
								{
									TypeRaw = c.Type.ToString(),
									Metadata = string.IsNullOrWhiteSpace(c.Metadata) ? "query" : c.Metadata,
									Values = c.Values.Count > 0 ? c.Values : ["search"]
								}
						)
						.Normalize<QueryRule>(
							r =>
								r with
								{
									TypeRaw = r.Type.ToString(),
									Actions = r.Actions.Ids.Count > 0 ? r.Actions : new QueryRuleActions { Ids = ["doc-1"] }
								}
						)
						.Normalize<SearchTuning>(s => s with { SourceFileName = "search.yml" })
				)
			)
			.Seed(RunSeed.Value)
			.CounterExamples(CounterExamples)
			.AsDataSource();

	private static QueryRule Rule(string id = "rule-1") =>
		new()
		{
			RuleId = id,
			Type = QueryRuleType.Pinned,
			Criteria = [new QueryRuleCriteria { Type = QueryRuleCriteriaType.Exact, Metadata = "query", Values = ["search"] }],
			Actions = new QueryRuleActions { Ids = ["doc-1"] }
		};

	private static SearchTuning WithRules(SearchTuning s, params QueryRule[] rules) => s with { Rules = [.. s.Rules, .. rules] };

	private static IReadOnlyDictionary<string, Func<SearchTuning, SearchTuning>> CounterExamples =>
		new Dictionary<string, Func<SearchTuning, SearchTuning>>
		{
			["synonym-terms-single-word"] = s => s with { Synonyms = [.. s.Synonyms, new SynonymGroup(["ice cream", "gelato"])] },
			["search-file-not-legacy"] = s => s with { SourceFileName = "synonyms.yml" },
			["query-criteria-type-known"] =
				s => WithRules(s, Rule("criteria") with { Criteria = [Rule().Criteria.First() with { TypeRaw = "" }] })
		};
}
