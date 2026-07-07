// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.Uploading;

/// <summary>
/// Helpers for validating S3 object keys related to the per-group
/// <see cref="Registry"/> manifests (the bundle index and the changelog-entry index).
/// </summary>
public static class RegistryKey
{
	private const string RegistrySuffix = "/registry.json";
	private const string BundlePrefix = "bundle/";
	private const string ChangelogPrefix = "changelog/";

	/// <summary>
	/// Returns true when <paramref name="key"/> is a manifest of either artifact-root form
	/// <c>bundle/{product}/registry.json</c> (the bundle index, exactly one product segment) or
	/// <c>changelog/{org}/{repo}/{branch}/registry.json</c> (the changelog-entry index, at least three
	/// segments because the branch is stored verbatim and may itself contain <c>/</c>). The bundle product
	/// segment matches the producer's product class (<c>[a-zA-Z0-9_-]+</c>); the changelog segments
	/// additionally allow <c>.</c> (GitHub repo names and branches such as <c>8.x</c> can contain dots).
	/// </summary>
	/// <remarks>
	/// Used by the changelog scrubber Lambda to decide whether to pass an incoming
	/// <c>*.json</c> object through to the public bucket. Anything else (a bare
	/// <c>{x}/registry.json</c>, a changelog manifest shallower than <c>org/repo/branch</c>, or an unknown
	/// top-level prefix) is rejected, which keeps arbitrary JSON out of the public surface.
	/// </remarks>
	public static bool IsRegistry(string key)
	{
		if (string.IsNullOrEmpty(key))
			return false;

		// Bundle index: exactly one product segment, no dots. Changelog-entry index: the
		// {org}/{repo}/{branch} prefix — at least three segments, dots allowed (repo/branch may contain them).
		return IsScopedRegistry(key, BundlePrefix, minSegments: 1, maxSegments: 1, allowDots: false)
			|| IsScopedRegistry(key, ChangelogPrefix, minSegments: 3, maxSegments: int.MaxValue, allowDots: true);
	}

	private static bool IsScopedRegistry(string key, string prefix, int minSegments, int maxSegments, bool allowDots)
	{
		if (!key.StartsWith(prefix, StringComparison.Ordinal) || !key.EndsWith(RegistrySuffix, StringComparison.Ordinal))
			return false;

		var middleLength = key.Length - prefix.Length - RegistrySuffix.Length;
		if (middleLength <= 0)
			return false;

		var middle = key.AsSpan(prefix.Length, middleLength);
		var segments = 0;
		var start = 0;
		for (var i = 0; i <= middle.Length; i++)
		{
			if (i != middle.Length && middle[i] != '/')
				continue;

			segments++;
			if (segments > maxSegments || !IsValidSegment(middle[start..i], allowDots))
				return false;
			start = i + 1;
		}

		return segments >= minSegments;
	}

	private static bool IsValidSegment(ReadOnlySpan<char> segment, bool allowDots)
	{
		if (segment.IsEmpty || segment is "." or "..")
			return false;

		foreach (var c in segment)
		{
			if (!(char.IsAsciiLetterOrDigit(c) || c == '_' || c == '-' || (allowDots && c == '.')))
				return false;
		}
		return true;
	}
}
