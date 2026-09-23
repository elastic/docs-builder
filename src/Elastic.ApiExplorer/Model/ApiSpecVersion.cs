// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Elastic.ApiExplorer.Model;

/// <summary>
/// One rendered API spec tree: <see cref="Latest"/> at the unversioned path, or a released
/// <see cref="Major"/> at <c>/vN/</c>. Parsed once from <c>index.json</c> by
/// <see cref="VersionIndexClient"/>. Not serialized.
/// </summary>
public readonly record struct ApiSpecVersion : IComparable<ApiSpecVersion>
{
	internal const string LatestIndexKey = "main";

	private readonly int? _major;

	private ApiSpecVersion(int? major) => _major = major;

	public static ApiSpecVersion Latest => default;

	public static ApiSpecVersion Major(int major)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(major);
		return new(major);
	}

	public bool IsLatest => _major is null;

	public bool TryGetMajor(out int major)
	{
		major = _major ?? 0;
		return _major is not null;
	}

	/// <summary>The <c>index.json</c> key: <c>main</c> or invariant digits.</summary>
	public string IndexKey => _major?.ToString(CultureInfo.InvariantCulture) ?? LatestIndexKey;

	public static bool TryParse([NotNullWhen(true)] string? indexKey, out ApiSpecVersion version)
	{
		version = default;
		if (indexKey == LatestIndexKey)
			return true;

		if (!int.TryParse(indexKey, NumberStyles.None, CultureInfo.InvariantCulture, out var major))
			return false;

		if (major.ToString(CultureInfo.InvariantCulture) != indexKey)
			return false;

		version = new(major);
		return true;
	}

	public int CompareTo(ApiSpecVersion other) => (IsLatest, other.IsLatest) switch
	{
		(true, true) => 0,
		(true, false) => 1,
		(false, true) => -1,
		_ => _major!.Value.CompareTo(other._major!.Value)
	};

	public static bool operator <(ApiSpecVersion left, ApiSpecVersion right) => left.CompareTo(right) < 0;
	public static bool operator >(ApiSpecVersion left, ApiSpecVersion right) => left.CompareTo(right) > 0;
	public static bool operator <=(ApiSpecVersion left, ApiSpecVersion right) => left.CompareTo(right) <= 0;
	public static bool operator >=(ApiSpecVersion left, ApiSpecVersion right) => left.CompareTo(right) >= 0;

	public override string ToString() => IndexKey;
}
