// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Generator.Equals;

namespace Elastic.Documentation.Search;

/// <summary>
/// Search relevance tuning from <c>search.yml</c>: synonym groups, query rules and diminished terms. Loaded once per
/// process by <c>SearchTuningLoader</c>, only by commands that index or query search.
/// </summary>
/// <remarks>
/// Constructed with <c>SearchTuningLoader.Load</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
[Equatable]
public sealed partial record SearchTuning
{
	[OrderedEquality]
	public required IReadOnlyList<SynonymGroup> Synonyms { get; init; }

	/// <summary>Each single-word synonym mapped to the rest of its group; derived from <see cref="Synonyms"/>.</summary>
	[IgnoreEquality]
	public IReadOnlyDictionary<string, string[]> SynonymBiDirectional => field ??= BuildBiDirectional(Synonyms);

	private static Dictionary<string, string[]> BuildBiDirectional(IReadOnlyList<SynonymGroup> synonyms) =>
		synonyms
			.Select(g => g.Terms)
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

	[OrderedEquality]
	public required IReadOnlyCollection<QueryRule> Rules { get; init; }
	[OrderedEquality]
	public required IReadOnlyCollection<string> DiminishTerms { get; init; }

	/// <summary>
	/// The name of the file the tuning was read from, or null when it was not read from one. The loader reads <c>synonyms.yml</c> when there is no
	/// <c>search.yml</c>, and nothing else in the model tells the two apart.
	/// </summary>
	[IgnoreEquality]
	public string? SourceFileName { get; internal init; }

	/// <summary>No synonyms, rules or diminished terms: search as untuned.</summary>
	public static SearchTuning None { get; } = new() { Synonyms = [], Rules = [], DiminishTerms = [] };
}

/// <summary>One query rule from <c>search.yml</c>: an id, a type, the criteria that trigger it and the actions it takes.</summary>
/// <remarks>
/// Constructed through <c>SearchTuning</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
[Equatable]
public sealed partial record QueryRule
{
	public required string RuleId { get; init; }
	public required QueryRuleType Type { get; init; }

	/// <summary><c>type</c> as written; empty when absent, null when the rule was not loaded from a file. An unknown or empty text is read as <see cref="QueryRuleType.Pinned"/>.</summary>
	[IgnoreEquality]
	public string? TypeRaw { get; internal init; }

	[OrderedEquality]
	public required IReadOnlyCollection<QueryRuleCriteria> Criteria { get; init; }
	public required QueryRuleActions Actions { get; init; }
}

public enum QueryRuleType
{
	Pinned,
	Exclude
}

/// <summary>A criterion of a <see cref="QueryRule"/>: a metadata field, how it is compared and the values.</summary>
/// <remarks>
/// Constructed through <c>QueryRule</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
[Equatable]
public sealed partial record QueryRuleCriteria
{
	public required QueryRuleCriteriaType Type { get; init; }

	/// <summary><c>type</c> as written; empty when absent, null when the criteria was not loaded from a file. An unknown or empty text is read as <see cref="QueryRuleCriteriaType.Exact"/>.</summary>
	[IgnoreEquality]
	public string? TypeRaw { get; internal init; }

	public required string Metadata { get; init; }
	[OrderedEquality]
	public required IReadOnlyCollection<string> Values { get; init; }
}

public enum QueryRuleCriteriaType
{
	Exact,
	Fuzzy,
	Prefix
}

/// <summary>The actions of a <see cref="QueryRule"/>: the ids it pins.</summary>
/// <remarks>
/// Constructed through <c>QueryRule</c>.
/// </remarks>
[Equatable]
public sealed partial record QueryRuleActions
{
	[OrderedEquality]
	public required IReadOnlyCollection<string> Ids { get; init; }
}

/// <summary>
/// One synonym group from <c>search.yml</c>: equivalent terms, identified by the first. A term written <c>a => b</c>
/// maps <c>a</c> to the rest of the group in one direction only.
/// </summary>
/// <remarks>
/// Constructed through <c>SearchTuning</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
[Equatable]
public sealed partial record SynonymGroup
{
	internal SynonymGroup(IReadOnlyList<string> terms) => Terms = terms;

	[OrderedEquality]
	public IReadOnlyList<string> Terms { get; internal init; }

	public void Deconstruct(out IReadOnlyList<string> terms) => terms = Terms;
}
