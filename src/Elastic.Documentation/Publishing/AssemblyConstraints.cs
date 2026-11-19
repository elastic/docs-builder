// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Publishing;

/// <summary>
/// The rules an <see cref="AssemblyConfiguration"/> must satisfy, in the order they are reported. They state what <c>AssemblyLoader</c>
/// guarantees about the repository sets it derives; none of them fires on a configuration the loader produced.
/// </summary>
[Constraints<AssemblyConfiguration>]
public static partial class AssemblyConstraints
{
	public static readonly Constraint<AssemblyConfiguration> RepositoryNameMatchesKey = Constraints<AssemblyConfiguration>
		.KeysMatch(a => a.ReferenceRepositories, r => r.Name, message: "a referenced repository is keyed by a name other than its own.")
		.WithSeverity(Severity.Warning)
		.Describe("A referenced repository's name is the key it is listed under in 'references'.");

	public static readonly Constraint<AssemblyConfiguration> PrivateKeysMatchNames = Constraints<AssemblyConfiguration>
		.KeysMatch(a => a.PrivateRepositories, r => r.Name, message: "a private repository is keyed by a name other than its own.")
		.WithSeverity(Severity.Warning)
		.Describe("A private repository's name is the key it is listed under.");

	public static readonly Constraint<AssemblyConfiguration> PrivateIsListed = Constraints<AssemblyConfiguration>
		.References<KeyValuePair<string, Repository>, string>(
			a => a.PrivateRepositories,
			kv => [kv.Key],
			a => a.AllPrivateRepositoryNames,
			StringComparer.OrdinalIgnoreCase,
			"a private repository is missing from AllPrivateRepositoryNames."
		)
		.WithSeverity(Severity.Warning)
		.Describe("Every private repository is in the set of all private repository names.");
}
