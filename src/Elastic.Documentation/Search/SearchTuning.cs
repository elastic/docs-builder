// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;

namespace Elastic.Documentation.Search;

/// <summary>
/// Search relevance tuning from <c>search.yml</c>: synonym groups, query rules and diminished terms. Loaded once per
/// process by <c>SearchTuningLoader</c>, only by commands that index or query search.
/// </summary>
public sealed record SearchTuning
{
	private readonly IReadOnlyList<string[]> _synonyms;

	public required IReadOnlyList<string[]> Synonyms
	{
		get => _synonyms;
		[MemberNotNull(nameof(_synonyms))]
		init
		{
			_synonyms = value;
			SynonymBiDirectional = value
				.SelectMany(a =>
				{
					var targets = new List<string[]>();
					foreach (var s in a)
					{
						if (s.Contains(' '))
							continue;

						List<string> newTarget = [s];
						if (s.Contains("=>"))
						{
							var tokens = s.Split("=>");
							if (tokens.Length > 1)
								newTarget = [tokens[0].Trim()];
							else
								continue;
						}
						newTarget.AddRange(a.Except([s]));
						targets.Add(newTarget.ToArray());
					}

					return targets;
				})
				.Where(a => a.Length > 1)
				.DistinctBy(a => a[0])
				.ToDictionary(a => a[0], a => a.Skip(1).ToArray(), StringComparer.OrdinalIgnoreCase);
		}
	}

	public IReadOnlyDictionary<string, string[]> SynonymBiDirectional { get; private set; } = new Dictionary<string, string[]>();

	public required IReadOnlyCollection<QueryRule> Rules { get; init; }
	public required IReadOnlyCollection<string> DiminishTerms { get; init; }
}

public sealed record QueryRule
{
	public required string RuleId { get; init; }
	public required QueryRuleType Type { get; init; }
	public required IReadOnlyCollection<QueryRuleCriteria> Criteria { get; init; }
	public required QueryRuleActions Actions { get; init; }
}

public enum QueryRuleType
{
	Pinned,
	Exclude
}

public sealed record QueryRuleCriteria
{
	public required QueryRuleCriteriaType Type { get; init; }
	public required string Metadata { get; init; }
	public required IReadOnlyCollection<string> Values { get; init; }
}

public enum QueryRuleCriteriaType
{
	Exact,
	Fuzzy,
	Prefix
}

public sealed record QueryRuleActions
{
	public required IReadOnlyCollection<string> Ids { get; init; }
}
