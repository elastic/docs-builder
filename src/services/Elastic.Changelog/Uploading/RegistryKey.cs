// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.Uploading;

/// <summary>
/// Helpers for validating S3 object keys related to the per-product
/// <see cref="Registry"/> manifests (the bundle index and the changelog-entry index).
/// </summary>
public static class RegistryKey
{
	private const string RegistrySuffix = "/registry.json";
	private const string BundlePrefix = "bundle/";
	private const string ChangelogPrefix = "changelog/";

	/// <summary>
	/// Returns true when <paramref name="key"/> is a manifest of either artifact-root form
	/// <c>bundle/{product}/registry.json</c> (the bundle index) or
	/// <c>changelog/{repo}/registry.json</c> (the changelog-entry index). The bundle product
	/// segment matches the producer's product class (<c>[a-zA-Z0-9_-]+</c>); the changelog repo
	/// segment additionally allows <c>.</c> (GitHub repo names can contain dots).
	/// </summary>
	/// <remarks>
	/// Used by the changelog scrubber Lambda to decide whether to pass an incoming
	/// <c>*.json</c> object through to the public bucket. Anything else (a bare
	/// <c>{x}/registry.json</c>, a deeper nesting, or an unknown top-level prefix) is
	/// rejected, which keeps arbitrary JSON out of the public surface.
	/// </remarks>
	public static bool IsRegistry(string key)
	{
		if (string.IsNullOrEmpty(key))
			return false;

		return IsScopedRegistry(key, BundlePrefix) || IsScopedRegistry(key, ChangelogPrefix);
	}

	private static bool IsScopedRegistry(string key, string prefix)
	{
		if (!key.StartsWith(prefix, StringComparison.Ordinal) || !key.EndsWith(RegistrySuffix, StringComparison.Ordinal))
			return false;

		var segmentLength = key.Length - prefix.Length - RegistrySuffix.Length;
		if (segmentLength <= 0)
			return false;

		// Bundle products never contain dots (producer class is [a-zA-Z0-9_-]+); only changelog repo
		// segments may, so dots are accepted under changelog/ but not bundle/.
		var allowDots = string.Equals(prefix, ChangelogPrefix, StringComparison.Ordinal);
		return IsValidSegment(key.AsSpan(prefix.Length, segmentLength), allowDots);
	}

	private static bool IsValidSegment(ReadOnlySpan<char> segment, bool allowDots)
	{
		if (segment.IsEmpty || segment.Contains('/') || segment is "." or "..")
			return false;

		foreach (var c in segment)
		{
			if (!(char.IsAsciiLetterOrDigit(c) || c == '_' || c == '-' || (allowDots && c == '.')))
				return false;
		}
		return true;
	}
}
