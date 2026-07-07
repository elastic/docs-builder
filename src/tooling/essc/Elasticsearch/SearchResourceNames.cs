// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.SiteSearch.Cli.Elasticsearch;

/// <summary>
/// Canonical names for the docs-builder-published Elasticsearch resources that essc
/// depends on. Centralised here so <see cref="SearchResourceValidator"/> and
/// <see cref="SearchResourceSynchronizer"/> never drift out of sync.
/// </summary>
internal static class SearchResourceNames
{
	/// <summary>Returns the synonym-set name for the given environment, e.g. <c>docs-assembler-prod</c>.</summary>
	public static string SynonymSet(string environment) => $"docs-assembler-{environment}";

	/// <summary>Returns the query-ruleset name for the given environment, e.g. <c>docs-ruleset-assembler-prod</c>.</summary>
	public static string QueryRuleset(string environment) => $"docs-ruleset-assembler-{environment}";
}
