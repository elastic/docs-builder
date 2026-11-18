// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Configuration.Search;

public sealed record SearchConfigDto
{
	public List<List<string>> Synonyms { get; set; } = [];
	public List<QueryRuleDto> Rules { get; set; } = [];
	public List<string> DiminishTerms { get; set; } = [];
}

public sealed record QueryRuleDto
{
	public string RuleId { get; set; } = string.Empty;
	public string Type { get; set; } = string.Empty;
	public List<QueryRuleCriteriaDto> Criteria { get; set; } = [];
	public QueryRuleActionsDto Actions { get; set; } = new();
}

public sealed record QueryRuleCriteriaDto
{
	public string Type { get; set; } = string.Empty;
	public string Metadata { get; set; } = string.Empty;
	public List<string> Values { get; set; } = [];
}

public sealed record QueryRuleActionsDto
{
	public List<string> Ids { get; set; } = [];
}
