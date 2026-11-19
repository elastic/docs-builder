// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Search;

namespace Elastic.Documentation.Configuration.Loading;

/// <summary>Loads the <see cref="SearchTuning"/> aggregate from <c>search.yml</c> (or the legacy <c>synonyms.yml</c>).</summary>
public static class SearchTuningLoader
{
	internal static readonly Loader<SearchConfigDto, SearchTuning> Yaml = Loader.For(parse: ReadDto, map: (dto, _) => ToRecord(dto));

	/// <summary>
	/// The tuning of the provider's search file; <see cref="SearchTuning.None"/> when there is none. A file that cannot be read fails with the
	/// parser's or mapper's own exception, as it always has: the warnings the model's rules find in a file are not reported here.
	/// </summary>
	public static SearchTuning Load(ConfigurationFileProvider provider)
	{
		var searchFile = provider.SearchFile;
		if (!searchFile.Exists)
			return SearchTuning.None;

		string text;
		using (var reader = searchFile.OpenText())
			text = reader.ReadToEnd();

		if (Yaml.Load(text, searchFile.FullName) is Loaded<SearchTuning> loaded)
			return loaded.Value with { SourceFileName = provider.SearchFileSourceName };

		// the loader reports a failure of the parse or the map step as a finding; running both again raises the exception itself
		_ = ToRecord(ReadDto(text, searchFile.FullName));
		throw new InvalidOperationException($"Could not load {searchFile.Name}");
	}

	private static SearchConfigDto ReadDto(string text, string file) => ConfigurationFileProvider.Deserialize<SearchConfigDto>(text, file);

	private static SearchTuning ToRecord(SearchConfigDto dto)
	{
		var synonyms = dto
			.Synonyms
			.Where(s => s.Count > 1)
			.Select(s => s.ToArray())
			.GroupBy(s => s[0], StringComparer.OrdinalIgnoreCase)
			.Select(g => new SynonymGroup(g.First()))
			.ToArray();
		var rules = dto.Rules.Select(ParseRule).ToImmutableArray();
		var diminishTerms = dto.DiminishTerms.ToImmutableArray();
		return new SearchTuning { Synonyms = synonyms, Rules = rules, DiminishTerms = diminishTerms };
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
