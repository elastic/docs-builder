// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Links;

/// <summary>Every rule the link registry (the link index of published <c>links.json</c> files) must satisfy, in the order they are reported.</summary>
[Constraints]
public static partial class LinkRegistryConstraints
{
	public static Constraint<LinkRegistry> RegistryEntryUpdatedAtMonotonic { get; } = Constraints<LinkRegistry>
		.MonotonicPerKey(
			Entries,
			e => (e.Repository, e.Branch),
			e => e.Entry.UpdatedAt,
			MonotonicDirection.NonDecreasing,
			describe: (key, before, after) =>
				$"Entry {key.Repository}/{key.Branch} moved back from updated_at {before:O} to {after:O}; an entry is replaced only by a strictly newer one."
		)
		.WithSeverity(Severity.Warning)
		.Describe("N38: An entry's updated_at never moves back; adding an entry replaces the existing one only when it is strictly newer.");

	public static Constraint<LinkRegistry> RegistryEntryHasRefAndTimestamp { get; } = Constraints<LinkRegistry>.EachCheck(
		Entries,
		(_, e) => LegacyParts(e)
	).Describe(
		"N39: An entry has an updated_at; legacy entries default to updated_at MinValue (the git ref part of this row is registry-entry-git-ref-known)."
	);

	// TODO(severity): promote to Error with WithSeverityUntil(Warning, <date>, Error) once the date is picked with the user
	public static Constraint<LinkRegistry> RegistryEntryGitRefKnown { get; } = Constraints<LinkRegistry>
		.Each(
			Entries,
			e => e.Entry.GitReference != LinkRegistryEntry.UnknownGitReference,
			e => $"Entry {e.Repository}/{e.Branch} is a legacy entry: its ref is '{LinkRegistryEntry.UnknownGitReference}'."
		)
		.WithSeverity(Severity.Warning)
		.Describe(
			"N39: A registry entry's git ref is never 'unknown' (a legacy entry that has not republished); cloning 'unknown' fails later."
		);

	public static Constraint<LinkRegistry> RepositoryHasMainOrMaster { get; } = Constraints<LinkRegistry>.Each(
		r => r.Repositories,
		r => r.Value.ContainsKey("main") || r.Value.ContainsKey("master"),
		r =>
			$"Repository '{r.Key}' has neither a 'main' nor a 'master' entry in the link registry (it has: {string.Join(", ", r.Value.Keys)})."
	).Describe(
		"N42: A repository has a 'main' or a 'master' entry, because branch selection falls back from main to master and then fails."
	);

	public static Constraint<LinkRegistry> RegistryEntryPathMatchesKey { get; } = Constraints<LinkRegistry>
		.Each(
			Entries,
			e => e.Entry.Path == $"elastic/{e.Repository}/{e.Branch}/links.json",
			e => $"Entry {e.Repository}/{e.Branch} has path '{e.Entry.Path}', expected 'elastic/{e.Repository}/{e.Branch}/links.json'."
		)
		.WithSeverity(Severity.Warning)
		.Describe("N46: An entry's path is 'elastic/{repository}/{branch}/links.json' for the repository and branch it is filed under.");

	public static Constraint<LinkRegistry> RegistryRepositoryNameSingleSegment { get; } = Constraints<LinkRegistry>.Each(
		r => r.Repositories.Keys,
		k => !IsRootedOrParent(k),
		k => $"Repository name '{k}' is rooted or contains a '..' segment."
	).Describe(
		"P49: A repository name is neither rooted nor contains a '..' segment, because it is used as a directory name when cloning."
	);

	private static IEnumerable<(string Repository, string Branch, LinkRegistryEntry Entry)> Entries(LinkRegistry registry) =>
		registry.Repositories.SelectMany(r => r.Value.Select(b => (r.Key, b.Key, b.Value)));

	private static IEnumerable<Violation> LegacyParts((string Repository, string Branch, LinkRegistryEntry Entry) e)
	{
		if (e.Entry.UpdatedAt == DateTime.MinValue)
			yield return Violation.Warning($"Entry {e.Repository}/{e.Branch} is a legacy entry: its updated_at is unset.");
	}

	private static bool IsRootedOrParent(string name) =>
		Path.IsPathRooted(name) || name.StartsWith('/') || name.StartsWith('\\') || name.Split('/', '\\').Contains("..");
}
