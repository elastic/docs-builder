// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;

namespace Elastic.Documentation;

public sealed class AllVersionsSpec : VersionSpec
{
	private static readonly SemVersion AllVersionsSemVersion = new(99999, 0, 0);

	private AllVersionsSpec() : base(AllVersionsSemVersion, null, VersionSpecKind.GreaterThanOrEqual)
	{
	}

	public static AllVersionsSpec Instance { get; } = new();

	public override string ToString() => "all";
}

public enum VersionSpecKind
{
	GreaterThanOrEqual, // x.x, x.x+, x.x.x, x.x.x+
	Range,              // x.x-y.y, x.x.x-y.y.y
	Exact               // =x.x, =x.x.x
}

/// <summary>
/// Represents a version specification that can be a single version with greater-than-or-equal semantics,
/// a range of versions, or an exact version match.
/// </summary>
public class VersionSpec : IComparable<VersionSpec>, IEquatable<VersionSpec>
{
	/// <summary>
	/// The minimum version (or the exact version for Exact kind).
	/// </summary>
	public SemVersion Min { get; }

	/// <summary>
	/// The maximum version for ranges. Null for GreaterThanOrEqual and Exact kinds.
	/// </summary>
	public SemVersion? Max { get; }

	/// <summary>
	/// The kind of version specification.
	/// </summary>
	public VersionSpecKind Kind { get; }

	// Internal constructor to prevent direct instantiation outside of TryParse
	// except for AllVersionsSpec which needs to inherit from this class
	protected VersionSpec(SemVersion min, SemVersion? max, VersionSpecKind kind)
	{
		Min = min;
		Max = max;
		Kind = kind;
	}

	/// <summary>
	/// Creates an Exact version spec from a SemVersion.
	/// </summary>
	public static VersionSpec Exact(SemVersion version) => new(version, null, VersionSpecKind.Exact);

	/// <summary>
	/// Creates a Range version spec from two SemVersions.
	/// </summary>
	public static VersionSpec Range(SemVersion min, SemVersion max) => new(min, max, VersionSpecKind.Range);

	/// <summary>
	/// Creates a GreaterThanOrEqual version spec from a SemVersion.
	/// </summary>
	public static VersionSpec GreaterThanOrEqual(SemVersion min) => new(min, null, VersionSpecKind.GreaterThanOrEqual);

	/// <summary>
	/// Tries to parse a version specification string.
	/// Supports: x.x, x.x+, x.x.x, x.x.x+ (gte), x.x-y.y (range), =x.x (exact)
	/// </summary>
	public static bool TryParse(string? input, [NotNullWhen(true)] out VersionSpec? spec)
	{
		spec = null;

		if (string.IsNullOrWhiteSpace(input))
			return false;

		var trimmed = input.Trim();

		// Check for exact syntax: =x.x or =x.x.x
		if (trimmed.StartsWith('='))
		{
			var versionPart = trimmed[1..];
			if (!TryParseVersion(versionPart, out var version))
				return false;

			spec = new(version, null, VersionSpecKind.Exact);
			return true;
		}

		// Check for range syntax: x.x-y.y or x.x.x-y.y.y
		var dashIndex = FindRangeSeparator(trimmed);
		if (dashIndex > 0)
		{
			var minPart = trimmed[..dashIndex];
			var maxPart = trimmed[(dashIndex + 1)..];

			if (!TryParseVersion(minPart, out var minVersion) ||
				!TryParseVersion(maxPart, out var maxVersion))
				return false;

			spec = new(minVersion, maxVersion, VersionSpecKind.Range);
			return true;
		}

		// Otherwise, it's greater-than-or-equal syntax
		// Strip trailing + if present
		var versionString = trimmed.EndsWith('+') ? trimmed[..^1] : trimmed;

		if (!TryParseVersion(versionString, out var gteVersion))
			return false;

		spec = new(gteVersion, null, VersionSpecKind.GreaterThanOrEqual);
		return true;
	}

	/// <summary>
	/// Finds the position of the dash separator in a range specification.
	/// Returns -1 if no valid range separator is found.
	/// </summary>
	private static int FindRangeSeparator(string input)
	{
		// Look for a dash that's not part of a prerelease version
		// We need to distinguish between "9.0-9.1" (range) and "9.0-alpha" (prerelease)
		// Strategy: Find dashes and check if what follows looks like a version number

		for (var i = 0; i < input.Length; i++)
		{
			if (input[i] == '-')
			{
				// Check if there's content before and after the dash
				if (i == 0 || i == input.Length - 1)
					continue;

				// Check if the character after dash is a digit (indicating a version)
				if (i + 1 < input.Length && char.IsDigit(input[i + 1]))
				{
					// Also verify that what comes before looks like a version
					var beforeDash = input[..i];
					if (TryParseVersion(beforeDash, out _))
						return i;
				}
			}
		}

		return -1;
	}

	/// <summary>
	/// Tries to parse a version string, normalizing minor versions to include patch 0.
	/// </summary>
	private static bool TryParseVersion(string input, [NotNullWhen(true)] out SemVersion? version)
	{
		version = null;

		if (string.IsNullOrWhiteSpace(input))
			return false;

		var trimmed = input.Trim();

		// Try to parse as-is first
		if (SemVersion.TryParse(trimmed, out version))
			return true;

		// If that fails, try appending .0 to support minor version format (e.g., "9.2" -> "9.2.0")
		if (SemVersion.TryParse(trimmed + ".0", out version))
			return true;

		return false;
	}

	/// <summary>
	/// Returns the canonical string representation of this version spec.
	/// Format: "9.2+" for GreaterThanOrEqual, "9.0-9.1" for Range, "=9.2" for Exact
	/// </summary>
	public override string ToString() => Kind switch
	{
		VersionSpecKind.Exact => $"={Min.Major}.{Min.Minor}",
		VersionSpecKind.Range => $"{Min.Major}.{Min.Minor}-{Max!.Major}.{Max.Minor}",
		VersionSpecKind.GreaterThanOrEqual => $"{Min.Major}.{Min.Minor}+",
		_ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, null)
	};

	/// <summary>
	/// Compares this VersionSpec to another for sorting.
	/// Uses Max for ranges, otherwise uses Min.
	/// </summary>
	public int CompareTo(VersionSpec? other)
	{
		if (other is null)
			return 1;

		// For sorting, we want to compare the "highest" version in each spec
		var thisCompareVersion = Kind == VersionSpecKind.Range && Max is not null ? Max : Min;
		var otherCompareVersion = other.Kind == VersionSpecKind.Range && other.Max is not null ? other.Max : other.Min;

		return thisCompareVersion.CompareTo(otherCompareVersion);
	}

	/// <summary>
	/// Checks if this VersionSpec is equal to another.
	/// </summary>
	public bool Equals(VersionSpec? other)
	{
		if (other is null)
			return false;

		if (ReferenceEquals(this, other))
			return true;

		return Kind == other.Kind && Min.Equals(other.Min) &&
			   (Max?.Equals(other.Max) ?? (other.Max is null));
	}

	public override bool Equals(object? obj) => obj is VersionSpec other && Equals(other);

	public override int GetHashCode() => HashCode.Combine(Kind, Min, Max);

	public static bool operator ==(VersionSpec? left, VersionSpec? right)
	{
		if (left is null)
			return right is null;
		return left.Equals(right);
	}

	public static bool operator !=(VersionSpec? left, VersionSpec? right) => !(left == right);

	public static bool operator <(VersionSpec? left, VersionSpec? right) =>
		left is null ? right is not null : left.CompareTo(right) < 0;

	public static bool operator <=(VersionSpec? left, VersionSpec? right) =>
		left is null || left.CompareTo(right) <= 0;

	public static bool operator >(VersionSpec? left, VersionSpec? right) =>
		left is not null && left.CompareTo(right) > 0;

	public static bool operator >=(VersionSpec? left, VersionSpec? right) =>
		left is null ? right is null : left.CompareTo(right) >= 0;

	/// <summary>
	/// Explicit conversion from string to VersionSpec
	/// </summary>
	public static explicit operator VersionSpec(string s)
	{
		if (TryParse(s, out var spec))
			return spec!;
		throw new ArgumentException($"'{s}' is not a valid version specification string.");
	}
}
