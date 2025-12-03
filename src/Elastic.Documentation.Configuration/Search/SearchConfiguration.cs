// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Elastic.Documentation.Configuration.Search;

public record SearchConfiguration
{
	private readonly IReadOnlyDictionary<string, string[]> _synonyms;

	public required IReadOnlyDictionary<string, string[]> Synonyms
	{
		get => _synonyms;
		[MemberNotNull(nameof(_synonyms))]
		init
		{
			_synonyms = value;
			SynonymBiDirectional = value
				.Select(kv => kv.Value.Concat([kv.Key]).ToArray())
				.SelectMany(a =>
				{
					var targets = new List<string[]>();
					foreach (var s in a)
					{
						if (s.Contains(' ') || s.Contains("=>"))
							continue;

						List<string> newTarget = [s];
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

public record QueryRule
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

public record QueryRuleCriteria
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

public record QueryRuleActions
{
	public required IReadOnlyCollection<string> Ids { get; init; }
}

internal sealed record SearchConfigDto
{
	public List<List<string>> Synonyms { get; set; } = [];
	public List<QueryRuleDto> Rules { get; set; } = [];
	public List<string> DiminishTerms { get; set; } = [];
}

internal sealed record QueryRuleDto
{
	public string RuleId { get; set; } = string.Empty;
	public string Type { get; set; } = string.Empty;
	public List<QueryRuleCriteriaDto> Criteria { get; set; } = [];
	public QueryRuleActionsDto Actions { get; set; } = new();
}

internal sealed record QueryRuleCriteriaDto
{
	public string Type { get; set; } = string.Empty;
	public string Metadata { get; set; } = string.Empty;
	public List<string> Values { get; set; } = [];
}

internal sealed record QueryRuleActionsDto
{
	public List<string> Ids { get; set; } = [];
}

public static class SearchConfigurationExtensions
{
	public static SearchConfiguration CreateSearchConfiguration(this ConfigurationFileProvider provider)
	{
		var searchFile = provider.SearchFile;
		var synonyms = new Dictionary<string, string[]>();

		if (!searchFile.Exists)
			return new SearchConfiguration { Synonyms = synonyms, Rules = [], DiminishTerms = [] };

		var searchDto = ConfigurationFileProvider.Deserializer.Deserialize<SearchConfigDto>(searchFile.OpenText());
		synonyms = searchDto.Synonyms
			.Where(s => s.Count > 1)
			.ToDictionary(k => k[0], sl => sl.Skip(1).ToArray(), StringComparer.OrdinalIgnoreCase);
		var rules = searchDto.Rules.Select(ParseRule).ToImmutableArray();
		var diminishTerms = searchDto.DiminishTerms.ToImmutableArray();
		return new SearchConfiguration { Synonyms = synonyms, Rules = rules, DiminishTerms = diminishTerms };
	}

	private static QueryRule ParseRule(QueryRuleDto dto) =>
		new()
		{
			RuleId = dto.RuleId,
			Type = Enum.TryParse<QueryRuleType>(dto.Type, ignoreCase: true, out var ruleType)
				? ruleType
				: QueryRuleType.Pinned,
			Criteria = dto.Criteria.Select(ParseCriteria).ToImmutableArray(),
			Actions = new QueryRuleActions { Ids = dto.Actions.Ids.ToImmutableArray() }
		};

	private static QueryRuleCriteria ParseCriteria(QueryRuleCriteriaDto dto) =>
		new()
		{
			Type = Enum.TryParse<QueryRuleCriteriaType>(dto.Type, ignoreCase: true, out var criteriaType)
				? criteriaType
				: QueryRuleCriteriaType.Exact,
			Metadata = dto.Metadata,
			Values = dto.Values.ToImmutableArray()
		};
}
