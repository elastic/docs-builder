// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.Uploading;

/// <summary>
/// Helpers for validating S3 object keys related to the per-product
/// <see cref="Registry"/> manifest.
/// </summary>
public static class RegistryKey
{
	private const string Suffix = "/registry.json";

	/// <summary>
	/// Returns true when <paramref name="key"/> is a top-level per-product manifest of the
	/// form <c>{product}/registry.json</c>, where <c>{product}</c> matches the same
	/// character class enforced by <c>ChangelogUploadService.ProductNameRegex</c>
	/// (<c>[a-zA-Z0-9_-]+</c>).
	/// </summary>
	/// <remarks>
	/// Used by the changelog scrubber Lambda to decide whether to pass an incoming
	/// <c>*.json</c> object through to the public bucket. Anything else (e.g. nested
	/// under a bundles/ prefix, or a multi-segment product) is rejected, which keeps
	/// arbitrary JSON out of the public surface.
	/// </remarks>
	public static bool IsRegistry(string key)
	{
		if (string.IsNullOrEmpty(key))
			return false;

		if (!key.EndsWith(Suffix, StringComparison.Ordinal))
			return false;

		var product = key.AsSpan(0, key.Length - Suffix.Length);
		if (product.IsEmpty || product.Contains('/'))
			return false;

		foreach (var c in product)
		{
			if (!(char.IsAsciiLetterOrDigit(c) || c == '_' || c == '-'))
				return false;
		}
		return true;
	}
}
