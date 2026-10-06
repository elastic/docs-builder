// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Search;

/// <summary>Every rule a <see cref="SearchTuning"/> must satisfy, in the order they are reported.</summary>
[Constraints]
public static partial class SearchTuningConstraints
{
	public static Constraint<SearchTuning> SynonymTermsSingleWord { get; } = Constraints<SearchTuning>
		.Each(
			s => s.Synonyms.SelectMany(g => g.Terms.Select(t => (Group: g, Term: t))),
			d => !d.Term.Contains(' '),
			d =>
				$"Synonym term '{d.Term}' of the group starting with '{d.Group.Terms[0]}' contains a space, so it is left out of the bidirectional synonym map."
		)
		.WithSeverity(Severity.Warning)
		.Describe("G97: Every synonym term is a single word; a term with spaces is left out of the bidirectional map.");

	public static Constraint<SearchTuning> QueryRuleIdUnique { get; } = Constraints<SearchTuning>
		.Unique(s => s.Rules, r => r.RuleId, StringComparer.Ordinal)
		.WithSeverity(Severity.Warning)
		.Describe("G99: A query rule id is used by one rule only; the loader does not check it.");

	public static Constraint<SearchTuning> QueryRuleComplete { get; } = Constraints<SearchTuning>.EachCheck(
		s => s.Rules,
		(_, rule) => IncompleteParts(rule)
	).Describe(
		"G99: A query rule has a rule_id, criteria with metadata and values, and action ids; none of them is required by the loader."
	);

	public static Constraint<SearchTuning> SearchFileNotLegacy { get; } = Constraints<SearchTuning>
		.Always(
			s => s.SourceFileName is not { } name || !string.Equals(name, "synonyms.yml", StringComparison.OrdinalIgnoreCase),
			"Search tuning was read from the legacy file synonyms.yml because there is no search.yml. Rename it to search.yml."
		)
		.WithSeverity(Severity.Warning)
		.Describe("G4: Search tuning lives in search.yml; the loader falls back to the legacy synonyms.yml without notice.");

	public static Constraint<SearchTuning> QueryRuleTypeKnown { get; } = Constraints<SearchTuning>
		.Each(
			s => s.Rules,
			r => r.TypeRaw is not { } raw || Enum.GetNames<QueryRuleType>().Contains(raw, StringComparer.OrdinalIgnoreCase),
			r => $"Query rule '{r.RuleId}' has type '{r.TypeRaw}', which is not pinned or exclude, so it is read as pinned."
		)
		.WithSeverity(Severity.Warning)
		.Describe("G98: A query rule's type is pinned or exclude; an empty or unknown type is read as pinned without notice.");

	public static Constraint<SearchTuning> QueryCriteriaTypeKnown { get; } = Constraints<SearchTuning>
		.Each(
			s => s.Rules.SelectMany(r => r.Criteria.Select(c => (Rule: r, Criteria: c))),
			d => d.Criteria.TypeRaw is not { } raw || Enum.GetNames<QueryRuleCriteriaType>().Contains(
				raw,
				StringComparer.OrdinalIgnoreCase
			),
			d =>
				$"A criteria of query rule '{d.Rule.RuleId}' has type '{d.Criteria.TypeRaw}', which is not exact, fuzzy or prefix, so it is read as exact."
		)
		.WithSeverity(Severity.Warning)
		.Describe("G98: A query rule criteria's type is exact, fuzzy or prefix; an empty or unknown type is read as exact without notice.");

	private static IEnumerable<Violation> IncompleteParts(QueryRule rule)
	{
		if (string.IsNullOrWhiteSpace(rule.RuleId))
			yield return Violation.Warning("A query rule has no rule_id.");
		if (rule.Actions.Ids.Count == 0)
			yield return Violation.Warning($"Query rule '{rule.RuleId}' has no actions.ids.");
		foreach (var criteria in rule.Criteria)
		{
			if (string.IsNullOrWhiteSpace(criteria.Metadata))
				yield return Violation.Warning($"A criteria of query rule '{rule.RuleId}' has no metadata.");
			if (criteria.Values.Count == 0)
				yield return Violation.Warning($"A criteria of query rule '{rule.RuleId}' has no values.");
		}
	}
}
