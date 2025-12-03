// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;

namespace Elastic.Documentation.Configuration.Search;

public record SearchConfiguration
{
	public required IReadOnlyCollection<string> Synonyms { get; init; }
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

		if (!searchFile.Exists)
			return new SearchConfiguration { Synonyms = [], Rules = [], DiminishTerms = [] };

		var searchDto = ConfigurationFileProvider.Deserializer.Deserialize<SearchConfigDto>(searchFile.OpenText());
		var flattenedSynonyms = searchDto.Synonyms.Select(sl => string.Join(',', sl)).ToImmutableArray();
		var rules = searchDto.Rules.Select(ParseRule).ToImmutableArray();
		var diminishTerms = searchDto.DiminishTerms.ToImmutableArray();
		return new SearchConfiguration { Synonyms = flattenedSynonyms, Rules = rules, DiminishTerms = diminishTerms };
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
