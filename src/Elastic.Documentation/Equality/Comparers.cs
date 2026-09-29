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
