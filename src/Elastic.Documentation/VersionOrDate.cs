// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;

namespace Elastic.Documentation;

/// <summary>
/// A version representation that can be either a semantic version or a date.
/// This enables proper sorting of mixed version formats used in different release strategies.
/// </summary>
/// <remarks>
/// Comparison rules:
/// - Semver versions sort among themselves using semver rules
/// - Dates sort among themselves chronologically
/// - When mixing types: semver versions come before dates (typical for products transitioning versioning schemes)
/// - Raw strings (fallback) sort lexicographically and come after both semver and dates
/// </remarks>
/// <param name="SemVer">The semantic version, if parsed successfully.</param>
/// <param name="Date">The date, if parsed successfully from ISO 8601 format.</param>
/// <param name="Raw">The raw string for fallback comparison.</param>
public record VersionOrDate(SemVersion? SemVer, DateOnly? Date, string? Raw) : IComparable<VersionOrDate>
{
	/// <summary>
	/// Parses a version string that can be either semver (e.g., "9.3.0") or a date (e.g., "2025-08-05").
	/// Falls back to raw string comparison for non-standard formats.
	/// </summary>
	/// <param name="version">The version string to parse.</param>
	/// <returns>A <see cref="VersionOrDate"/> instance representing the parsed version.</returns>
	public static VersionOrDate Parse(string version)
	{
		// Try semver first
		if (SemVersion.TryParse(version, out var semVersion))
			return new VersionOrDate(semVersion, null, null);

		// Try date parsing (ISO 8601 YYYY-MM-DD format)
		if (DateOnly.TryParseExact(version, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
			return new VersionOrDate(null, date, null);

		// Fallback - treat as raw string for lexicographic sorting
		return new VersionOrDate(null, null, version);
	}

	/// <inheritdoc />
	public int CompareTo(VersionOrDate? other)
	{
		if (other is null)
			return 1;

		// Both are semver
		if (SemVer is not null && other.SemVer is not null)
			return SemVer.CompareTo(other.SemVer);

		// Both are dates
		if (Date.HasValue && other.Date.HasValue)
			return Date.Value.CompareTo(other.Date.Value);

		// Semver vs Date: semver comes first (returns positive when comparing semver to date for descending sort)
		if (SemVer is not null && other.Date.HasValue)
			return 1;
		if (Date.HasValue && other.SemVer is not null)
			return -1;

		// Semver vs Raw: semver comes first
		if (SemVer is not null && other.Raw is not null)
			return 1;
		if (Raw is not null && other.SemVer is not null)
			return -1;

		// Date vs Raw: date comes first
		if (Date.HasValue && other.Raw is not null)
			return 1;
		if (Raw is not null && other.Date.HasValue)
			return -1;

		// Both are raw strings
		return string.Compare(Raw, other.Raw, StringComparison.Ordinal);
	}

	/// <summary>Compares two <see cref="VersionOrDate"/> values.</summary>
	public static bool operator <(VersionOrDate left, VersionOrDate right) => left.CompareTo(right) < 0;

	/// <summary>Compares two <see cref="VersionOrDate"/> values.</summary>
	public static bool operator <=(VersionOrDate left, VersionOrDate right) => left.CompareTo(right) <= 0;

	/// <summary>Compares two <see cref="VersionOrDate"/> values.</summary>
	public static bool operator >(VersionOrDate left, VersionOrDate right) => left.CompareTo(right) > 0;

	/// <summary>Compares two <see cref="VersionOrDate"/> values.</summary>
	public static bool operator >=(VersionOrDate left, VersionOrDate right) => left.CompareTo(right) >= 0;
}
