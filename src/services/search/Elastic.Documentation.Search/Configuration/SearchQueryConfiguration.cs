// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

#pragma warning disable IDE0130 // Namespace kept at root for consumer convenience
namespace Elastic.Documentation.Search;

/// <summary>
/// Query-time search configuration, derived from the docs-builder search config and the
/// Elasticsearch environment. Carries only the four values the query path actually reads.
/// </summary>
public sealed record SearchQueryConfiguration
{
	public IReadOnlyDictionary<string, string[]> SynonymBiDirectional { get; init; } =
		new Dictionary<string, string[]>();

	public IReadOnlyCollection<string> DiminishTerms { get; init; } = [];

	public string? RulesetName { get; init; }

	public bool SemanticEnabled { get; init; } = true;
}
