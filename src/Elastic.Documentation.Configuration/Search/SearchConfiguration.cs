// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;

using Elastic.Documentation.Search;

namespace Elastic.Documentation.Configuration.Search;

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

internal static class SearchConfigurationExtensions
{
	public static SearchTuning CreateSearchConfiguration(this ConfigurationFileProvider provider)
	{
		var searchFile = provider.SearchFile;

		if (!searchFile.Exists)
			return SearchTuning.None;

		var searchDto = ConfigurationFileProvider.Deserializer.Deserialize<SearchConfigDto>(searchFile.OpenText());
		var synonyms = searchDto
			.Synonyms
			.Where(s => s.Count > 1)
			.Select(s => s.ToArray())
			.GroupBy(s => s[0], StringComparer.OrdinalIgnoreCase)
			.Select(g => new SynonymGroup(g.First()))
			.ToArray();
		var rules = searchDto.Rules.Select(ParseRule).ToImmutableArray();
		var diminishTerms = searchDto.DiminishTerms.ToImmutableArray();
		return new SearchTuning { Synonyms = synonyms, Rules = rules, DiminishTerms = diminishTerms, SourceFileName = searchFile.Name };
	}

	private static QueryRule ParseRule(QueryRuleDto dto) =>
		new()
		{
			RuleId = dto.RuleId,
			Type = Enum.TryParse<QueryRuleType>(dto.Type, ignoreCase: true, out var ruleType) ? ruleType : QueryRuleType.Pinned,
			TypeRaw = dto.Type,
			Criteria = dto.Criteria.Select(ParseCriteria).ToImmutableArray(),
			Actions = new QueryRuleActions { Ids = dto.Actions.Ids.ToImmutableArray() }
		};

	private static QueryRuleCriteria ParseCriteria(QueryRuleCriteriaDto dto) =>
		new()
		{
			Type = Enum.TryParse<QueryRuleCriteriaType>(dto.Type, ignoreCase: true, out var criteriaType)
				? criteriaType
				: QueryRuleCriteriaType.Exact,
			TypeRaw = dto.Type,
			Metadata = dto.Metadata,
			Values = dto.Values.ToImmutableArray()
		};
}
