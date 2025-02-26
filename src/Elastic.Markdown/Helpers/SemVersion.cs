// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Elastic.Markdown.Helpers;

/// <summary>
/// A semver2 compatible version.
/// </summary>
public partial class SemVersion :
	IEquatable<SemVersion>,
	IComparable<SemVersion>,
	IComparable
{
	// https://semver.org/#is-there-a-suggested-regular-expression-regex-to-check-a-semver-string
	private static readonly Regex Regex = MyRegex();

	/// <summary>
	/// The major version part.
	/// </summary>
	public int Major { get; }

	/// <summary>
	/// The minor version part.
	/// </summary>
	public int Minor { get; }

	/// <summary>
	/// The patch version part.
	/// </summary>
	public int Patch { get; }

	/// <summary>
	/// The prerelease version part.
	/// </summary>
	public string Prerelease { get; }

	/// <summary>
	/// The metadata version part.
	/// </summary>
	public string Metadata { get; }

	/// <summary>
	/// Initializes a new <see cref="SemVersion"/> instance.
	/// </summary>
	/// <param name="major">The major version part.</param>
	/// <param name="minor">The minor version part.</param>
	/// <param name="patch">The patch version part.</param>
	public SemVersion(int major, int minor, int patch)
	{
		Major = major;
		Minor = minor;
		Patch = patch;
		Prerelease = string.Empty;
		Metadata = string.Empty;
	}

	/// <summary>
	/// Initializes a new <see cref="SemVersion"/> instance.
	/// </summary>
	/// <param name="major">The major version part.</param>
	/// <param name="minor">The minor version part.</param>
	/// <param name="patch">The patch version part.</param>
	/// <param name="prerelease">The prerelease version part.</param>
	public SemVersion(int major, int minor, int patch, string? prerelease)
	{
		Major = major;
		Minor = minor;
		Patch = patch;
		Prerelease = prerelease ?? string.Empty;
		Metadata = string.Empty;
	}

	/// <summary>
	/// Initializes a new <see cref="SemVersion"/> instance.
	/// </summary>
	/// <param name="major">The major version part.</param>
	/// <param name="minor">The minor version part.</param>
	/// <param name="patch">The patch version part.</param>
	/// <param name="prerelease">The prerelease version part.</param>
	/// <param name="metadata">The metadata version part.</param>
	public SemVersion(int major, int minor, int patch, string? prerelease, string? metadata)
	{
		Major = major;
		Minor = minor;
		Patch = patch;
		Prerelease = prerelease ?? string.Empty;
		Metadata = metadata ?? string.Empty;
	}

	public static explicit operator SemVersion(string b)
	{
		var semVersion = TryParse(b, out var version) ? version : TryParse(b + ".0", out version) ? version : null;
		return semVersion ?? throw new ArgumentException($"'{b}' is not a valid semver2 version string.");
	}

	public static implicit operator string(SemVersion d) => d.ToString();

	/// <summary>
	///
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator ==(SemVersion left, SemVersion right) => Equals(left, right);

	/// <summary>
	///
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator !=(SemVersion left, SemVersion right) => !Equals(left, right);

	/// <summary>
	///
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator >(SemVersion left, SemVersion right) => left.CompareTo(right) > 0;

	/// <summary>
	///
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator >=(SemVersion left, SemVersion right) => left == right || left > right;

	/// <summary>
	///
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator <(SemVersion left, SemVersion right) => left.CompareTo(right) < 0;

	/// <summary>
	///
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator <=(SemVersion left, SemVersion right) => left == right || left < right;

	/// <summary>
	/// Tries to initialize a new <see cref="SemVersion"/> instance from the given string.
	/// </summary>
	/// <param name="input">The semver2 compatible version string.</param>
	/// <param name="version">The parsed <see cref="SemVersion"/> instance.</param>
	/// <returns><c>True</c> if the passed string is a valid semver2 version string or <c>false</c>, if not.</returns>
	public static bool TryParse(string input, [NotNullWhen(true)] out SemVersion? version)
	{
		version = null;

		var match = Regex.Match(input);
		if (!match.Success)
			return false;

		if (!int.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var major))
			return false;
		if (!int.TryParse(match.Groups[2].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var minor))
			return false;
		if (!int.TryParse(match.Groups[3].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var patch))
			return false;

		version = new SemVersion(major, minor, patch, match.Groups[4].Value, match.Groups[5].Value);

		return true;
	}

	/// <summary>
	/// Returns a new <see cref="SemVersion"/> instance with updated components. Unchanged parts should be set to <c>null</c>.
	/// </summary>
	/// <param name="major">The major version part, or <c>null</c> to keep the current value.</param>
	/// <param name="minor">The minor version part, or <c>null</c> to keep the current value.</param>
	/// <param name="patch">The patch version part, or <c>null</c> to keep the current value.</param>
	/// <param name="prerelease">The prerelease version part, or <c>null</c> to keep the current value.</param>
	/// <param name="metadata">The metadata version part, or <c>null</c> to keep the current value.</param>
	/// <returns></returns>
	public SemVersion Update(int? major = null, int? minor = null, int? patch = null, string? prerelease = null, string? metadata = null) =>
		new(major ?? Major,
			minor ?? Minor,
			patch ?? Patch,
			prerelease ?? Prerelease,
			metadata ?? Metadata);

	/// <summary>
	/// Compares the current version to another version in a natural way (by component/part precedence).
	/// </summary>
	/// <param name="other">The <see cref="SemVersion"/> to compare to.</param>
	/// <returns><c>0</c> if both versions are equal, a positive number, if the other version is lower or a negative number if the other version is higher.</returns>
	private int CompareByPrecedence(SemVersion? other)
	{
		if (other is null)
			return 1;

		var result = Major.CompareTo(other.Major);
		if (result != 0)
			return result;

		result = Minor.CompareTo(other.Minor);
		if (result != 0)
			return result;

		result = Patch.CompareTo(other.Patch);
		if (result != 0)
			return result;

		result = CompareComponent(Prerelease, other.Prerelease, true);
		return result != 0 ? result : CompareComponent(Prerelease, other.Metadata, true);
	}

	/// <inheritdoc cref="IComparable{T}.CompareTo"/>
	public int CompareTo(SemVersion? other) => other is null ? 1 : CompareByPrecedence(other);

	/// <inheritdoc cref="IComparable.CompareTo"/>
	public int CompareTo(object? obj) => CompareTo(obj as SemVersion);

	/// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
	public bool Equals(SemVersion? other) =>
		other is not null && (
			ReferenceEquals(this, other)
			|| (Major == other.Major && Minor == other.Minor && Patch == other.Patch
				&& Prerelease == other.Prerelease && Metadata == other.Metadata)
		);

	/// <inheritdoc cref="object.Equals(object)"/>
	public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is SemVersion other && Equals(other));

	/// <inheritdoc cref="object.GetHashCode"/>
	public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch, Prerelease, Metadata);

	/// <inheritdoc cref="object.ToString"/>
	public override string ToString()
	{
		var version = $"{Major}.{Minor}.{Patch}";

		if (!string.IsNullOrEmpty(Prerelease))
			version += "-" + Prerelease;
		if (!string.IsNullOrEmpty(Metadata))
			version += "+" + Metadata;

		return version;
	}

	private static int CompareComponent(string a, string b, bool lower = false)
	{
		var aEmpty = string.IsNullOrEmpty(a);
		var bEmpty = string.IsNullOrEmpty(b);
		if (aEmpty && bEmpty)
			return 0;

		if (aEmpty)
			return lower ? 1 : -1;
		if (bEmpty)
			return lower ? -1 : 1;

		var aComps = a.Split('.');
		var bComps = b.Split('.');

		var minLen = Math.Min(aComps.Length, bComps.Length);
		for (var i = 0; i < minLen; i++)
		{
			var ac = aComps[i];
			var bc = bComps[i];
			var isanum = int.TryParse(ac, out var anum);
			var isbnum = int.TryParse(bc, out var bnum);
			int r;
			if (isanum && isbnum)
			{
				r = anum.CompareTo(bnum);
				if (r != 0)
					return anum.CompareTo(bnum);
			}
			else
			{
				if (isanum)
					return -1;
				if (isbnum)
					return 1;

				r = string.CompareOrdinal(ac, bc);
				if (r != 0)
					return r;
			}
		}

		return aComps.Length.CompareTo(bComps.Length);
	}

	[GeneratedRegex(@"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$")]
	private static partial Regex MyRegex();
}

