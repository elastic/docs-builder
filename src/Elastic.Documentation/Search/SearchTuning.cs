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
/// <para><b>Built by:</b> <c>SearchTuningLoader.Load</c> in <c>Elastic.Documentation.Configuration</c>, reading <c>search.yml</c> (<c>SearchTuning.None</c> is the empty tuning).</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>Members that must always be set</b> (they were <c>required</c> before the type was sealed; C# forbids <c>required</c> on a member whose setter is less visible than the type, CS9032): <c>Synonyms</c>, <c>Rules</c>, <c>DiminishTerms</c>. A loader or test that leaves one out gets the default; the aggregate's constraint set (<see cref="SearchTuningConstraints"/>) is where a missing member is reported, and the loader's mapping is what normally sets them.</para>
/// <para><b>In a test:</b> build it with an object initializer (<c>new SearchTuning { Synonyms = [], Rules = [], DiminishTerms = [] }</c>) or <c>DomainGen.Of&lt;SearchTuning&gt;()</c> (tests), and <c>with</c> for variations.</para>
/// </remarks>
[Equatable]
public sealed partial record SearchTuning
{
	[OrderedEquality]
	public IReadOnlyList<SynonymGroup> Synonyms { get; internal init; } = default!;

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
	public IReadOnlyCollection<QueryRule> Rules { get; internal init; } = default!;
	[OrderedEquality]
	public IReadOnlyCollection<string> DiminishTerms { get; internal init; } = default!;

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
/// <para><b>Built by:</b> <c>SearchTuningLoader.Load</c> in <c>Elastic.Documentation.Configuration</c>, reading <c>search.yml</c>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>Members that must always be set</b> (they were <c>required</c> before the type was sealed; C# forbids <c>required</c> on a member whose setter is less visible than the type, CS9032): <c>RuleId</c>, <c>Type</c>, <c>Criteria</c>, <c>Actions</c>. A loader or test that leaves one out gets the default; the aggregate's constraint set (<see cref="SearchTuningConstraints"/>) reports it: <see cref="SearchTuningConstraints.QueryRuleComplete"/> reports a rule without an id, criteria or actions.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new QueryRule { ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
[Equatable]
public sealed partial record QueryRule
{
	public string RuleId { get; internal init; } = default!;
	public QueryRuleType Type { get; internal init; } = default!;

	/// <summary><c>type</c> as written; empty when absent, null when the rule was not loaded from a file. An unknown or empty text is read as <see cref="QueryRuleType.Pinned"/>.</summary>
	[IgnoreEquality]
	public string? TypeRaw { get; internal init; }

	[OrderedEquality]
	public IReadOnlyCollection<QueryRuleCriteria> Criteria { get; internal init; } = default!;
	public QueryRuleActions Actions { get; internal init; } = default!;
}

public enum QueryRuleType
{
	Pinned,
	Exclude
}

/// <summary>A criterion of a <see cref="QueryRule"/>: a metadata field, how it is compared and the values.</summary>
/// <remarks>
/// <para><b>Built by:</b> <c>SearchTuningLoader.Load</c> in <c>Elastic.Documentation.Configuration</c>, reading <c>search.yml</c>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>Members that must always be set</b> (they were <c>required</c> before the type was sealed; C# forbids <c>required</c> on a member whose setter is less visible than the type, CS9032): <c>Type</c>, <c>Metadata</c>, <c>Values</c>. A loader or test that leaves one out gets the default; the aggregate's constraint set (<see cref="SearchTuningConstraints"/>) reports it: <see cref="SearchTuningConstraints.QueryRuleComplete"/> reports criteria without metadata or values.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new QueryRuleCriteria { ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
[Equatable]
public sealed partial record QueryRuleCriteria
{
	public QueryRuleCriteriaType Type { get; internal init; } = default!;

	/// <summary><c>type</c> as written; empty when absent, null when the criteria was not loaded from a file. An unknown or empty text is read as <see cref="QueryRuleCriteriaType.Exact"/>.</summary>
	[IgnoreEquality]
	public string? TypeRaw { get; internal init; }

	public string Metadata { get; internal init; } = default!;
	[OrderedEquality]
	public IReadOnlyCollection<string> Values { get; internal init; } = default!;
}

public enum QueryRuleCriteriaType
{
	Exact,
	Fuzzy,
	Prefix
}

/// <summary>The actions of a <see cref="QueryRule"/>: the ids it pins.</summary>
/// <remarks>
/// <para><b>Built by:</b> <c>SearchTuningLoader.Load</c> in <c>Elastic.Documentation.Configuration</c>, reading <c>search.yml</c>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>Members that must always be set</b> (they were <c>required</c> before the type was sealed; C# forbids <c>required</c> on a member whose setter is less visible than the type, CS9032): <c>Ids</c>. A loader or test that leaves one out gets the default; the aggregate's constraint set (<see cref="SearchTuningConstraints"/>) reports it: <see cref="SearchTuningConstraints.QueryRuleComplete"/> reports a rule without action ids.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new QueryRuleActions { ... }</c>) and <c>with</c> for variations.</para>
/// </remarks>
[Equatable]
public sealed partial record QueryRuleActions
{
	[OrderedEquality]
	public IReadOnlyCollection<string> Ids { get; internal init; } = default!;
}

/// <summary>
/// One synonym group from <c>search.yml</c>: equivalent terms, identified by the first. A term written <c>a => b</c>
/// maps <c>a</c> to the rest of the group in one direction only.
/// </summary>
/// <remarks>
/// <para><b>Built by:</b> <c>SearchTuningLoader.Load</c> in <c>Elastic.Documentation.Configuration</c>, reading <c>search.yml</c> (through <c>SearchConfiguration</c>, which builds one group per synonym line).</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>In a test:</b> build it with its constructor (<c>new SynonymGroup(["a", "b"])</c>), which is internal and visible to the tests.</para>
/// </remarks>
[Equatable]
public sealed partial record SynonymGroup
{
	internal SynonymGroup(IReadOnlyList<string> terms) => Terms = terms;

	[OrderedEquality]
	public IReadOnlyList<string> Terms { get; internal init; }

	public void Deconstruct(out IReadOnlyList<string> terms) => terms = Terms;
}
