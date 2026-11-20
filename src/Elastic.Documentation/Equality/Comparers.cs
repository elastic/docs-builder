// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;

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
