// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Constraints;
using Elastic.Documentation.Publishing;

namespace Elastic.Documentation.Entities.Tests;

/// <summary>
/// This repository's own laws harness, unchanged, run over the Nullean.Entities rules through the adapter: the valid model (the shipped configuration) breaks nothing, every rule has a
/// counter-example, and each counter-example breaks that rule and no other.
/// </summary>
[InheritsTests]
public class AssemblyEntityRulesLawsTests : ConstraintSetTests<AssemblyConfiguration>
{
	protected override IReadOnlyList<Constraint<AssemblyConfiguration>> Constraints => EntityRules.Assembly.ToDomain();

	protected override AssemblyConfiguration Valid() => Support.ShippedAssembly;

	private static FrozenDictionary<string, Repository> With(
		IReadOnlyDictionary<string, Repository> dictionary,
		string key,
		Repository value
	) => dictionary.Where(kv => kv.Key != key).Append(new KeyValuePair<string, Repository>(key, value)).ToFrozenDictionary();

	private static Repository First(AssemblyConfiguration a) => a.AvailableRepositories.Values.First();

	protected override IReadOnlyDictionary<string, Func<AssemblyConfiguration, AssemblyConfiguration>> CounterExamples
	{
		get;
	} = new Dictionary<string, Func<AssemblyConfiguration, AssemblyConfiguration>>
	{
		// the key stops being the repository's name, in each of the three dictionaries that repeat it
		["reference-keys-match-names"] =
			a => a with { ReferenceRepositories = With(a.ReferenceRepositories, First(a).Name, First(a) with { Name = "renamed" }) },
		["available-keys-match-names"] =
			a => a with { AvailableRepositories = With(a.AvailableRepositories, First(a).Name, First(a) with { Name = "renamed" }) },
		["private-keys-match-names"] =
			a =>
				a with
				{
					PrivateRepositories = With(a.PrivateRepositories, First(a).Name, First(a) with { Name = "renamed" }),
					AllPrivateRepositoryNames = new HashSet<string>(a.AllPrivateRepositoryNames) { First(a).Name },
				},
		// a private repository the assembly does not publish
		["private-is-available"] =
			a =>
				a with
				{
					PrivateRepositories = With(a.PrivateRepositories, "ghost", new Repository { Name = "ghost", Private = true }),
					AllPrivateRepositoryNames = new HashSet<string>(a.AllPrivateRepositoryNames) { "ghost" },
				},
		// a private repository missing from the list that hides its links
		["private-is-listed"] =
			a =>
				a with
				{
					PrivateRepositories = With(a.PrivateRepositories, First(a).Name, First(a) with { Private = true }),
					AllPrivateRepositoryNames = a.AllPrivateRepositoryNames.Where(n => n != First(a).Name).ToHashSet(),
				},
		// an available repository that says it does not publish
		["available-repositories-publish"] =
			a => a with { AvailableRepositories = With(a.AvailableRepositories, First(a).Name, First(a) with { Skip = true }) },
	};
}
