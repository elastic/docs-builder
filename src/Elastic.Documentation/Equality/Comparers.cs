// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Links;

namespace Elastic.Documentation.Equality;

// Comparers for the few member shapes Generator.Equals can't compare structurally on its own.
// Referenced from [CustomEquality(typeof(...))]; each exposes the static Default the generator looks up.

/// <summary>A file or directory is its location: equal when the full paths are equal.</summary>
public sealed class FileSystemInfoLocationComparer : IEqualityComparer<IFileSystemInfo>
{
	public static FileSystemInfoLocationComparer Default { get; } = new();

	public bool Equals(IFileSystemInfo? x, IFileSystemInfo? y) =>
		ReferenceEquals(x, y) || (x is not null && y is not null && string.Equals(x.FullName, y.FullName, StringComparison.Ordinal));

	public int GetHashCode(IFileSystemInfo obj) => StringComparer.Ordinal.GetHashCode(obj.FullName);
}

/// <summary>
/// Entries of a dictionary whose values are string lists: equal when the keys match and the lists hold the same strings
/// in the same order. Generator.Equals applies a dictionary's element comparer to whole entries.
/// </summary>
public sealed class StringListEntryComparer : IEqualityComparer<KeyValuePair<string, IReadOnlyList<string>>>
{
	public static StringListEntryComparer Default { get; } = new();

	public bool Equals(KeyValuePair<string, IReadOnlyList<string>> x, KeyValuePair<string, IReadOnlyList<string>> y) =>
		string.Equals(x.Key, y.Key, StringComparison.Ordinal)
			&& (ReferenceEquals(x.Value, y.Value)
				|| (x.Value is not null && y.Value is not null && x.Value.SequenceEqual(y.Value, StringComparer.Ordinal)));

	public int GetHashCode(KeyValuePair<string, IReadOnlyList<string>> obj)
	{
		var hash = new HashCode();
		hash.Add(obj.Key, StringComparer.Ordinal);
		foreach (var item in obj.Value ?? [])
			hash.Add(item, StringComparer.Ordinal);
		return hash.ToHashCode();
	}
}

/// <summary>
/// Entries of <see cref="LinkRegistry.Repositories"/> (repository to branch to entry): equal when the keys match and the inner dictionaries hold equal values
/// under the same keys, in any order.
/// </summary>
public sealed class LinkRegistryEntriesComparer : IEqualityComparer<Dictionary<string, Dictionary<string, LinkRegistryEntry>>>
{
	public static LinkRegistryEntriesComparer Default { get; } = new();

	public bool Equals(
		Dictionary<string, Dictionary<string, LinkRegistryEntry>>? x,
		Dictionary<string, Dictionary<string, LinkRegistryEntry>>? y
	) =>
		ReferenceEquals(x, y)
			|| (x is not null
				&& y is not null
				&& x.Count == y.Count
				&& x.All(
					repository => y.TryGetValue(repository.Key, out var other) &&
						(ReferenceEquals(repository.Value, other) ||
							(repository.Value is not null &&
								other is not null && repository.Value.Count == other.Count && repository.Value.All(
								branch => other.TryGetValue(
									branch.Key,
									out var entry
								) && EqualityComparer<LinkRegistryEntry>.Default.Equals(branch.Value, entry)
							)))
				));

	// Order-independent: the repositories and their branch counts identify the registry; Equals settles the rest.
	public int GetHashCode(Dictionary<string, Dictionary<string, LinkRegistryEntry>> obj)
	{
		var hash = 0;
		foreach (var (key, branches) in obj)
			hash ^= HashCode.Combine(StringComparer.Ordinal.GetHashCode(key), branches?.Count ?? 0);
		return hash;
	}
}

/// <summary>
/// <see cref="FloatingLinkSection.Aliases"/> (version to canonical page to aliases): equal when the versions and the canonical pages match and each
/// holds the same aliases in the same order, with the versions and pages in any order.
/// </summary>
public sealed class FloatingAliasesComparer : IEqualityComparer<Dictionary<string, Dictionary<string, string[]>>>
{
	public static FloatingAliasesComparer Default { get; } = new();

	public bool Equals(Dictionary<string, Dictionary<string, string[]>>? x, Dictionary<string, Dictionary<string, string[]>>? y) =>
		ReferenceEquals(x, y)
			|| (x is not null
				&& y is not null
				&& x.Count == y.Count
				&& x.All(version => y.TryGetValue(version.Key, out var other) && PagesEqual(version.Value, other)));

	private static bool PagesEqual(Dictionary<string, string[]>? x, Dictionary<string, string[]>? y) =>
		ReferenceEquals(x, y)
			|| (x is not null
				&& y is not null
				&& x.Count == y.Count
				&& x.All(page => y.TryGetValue(page.Key, out var aliases) && page.Value.AsSpan().SequenceEqual(aliases)));

	public int GetHashCode(Dictionary<string, Dictionary<string, string[]>> obj)
	{
		var hash = 0;
		foreach (var (version, pages) in obj)
			hash ^= HashCode.Combine(StringComparer.Ordinal.GetHashCode(version), pages?.Count ?? 0);
		return hash;
	}
}
