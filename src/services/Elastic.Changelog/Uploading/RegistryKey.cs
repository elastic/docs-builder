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
	private const string BundleSuffix = "/registry.json";
	private const string ChangelogSuffix = "/changelog/registry.json";

	/// <summary>
	/// Returns true when <paramref name="key"/> is a per-product manifest of either form
	/// <c>{product}/registry.json</c> (the bundle index) or
	/// <c>{product}/changelog/registry.json</c> (the changelog-entry index), where
	/// <c>{product}</c> matches the same character class enforced by
	/// <c>ChangelogUploadService.ProductNameRegex</c> (<c>[a-zA-Z0-9_-]+</c>).
	/// </summary>
	/// <remarks>
	/// Used by the changelog scrubber Lambda to decide whether to pass an incoming
	/// <c>*.json</c> object through to the public bucket. Anything else (e.g. nested
	/// under a bundle/ prefix, or a multi-segment product) is rejected, which keeps
	/// arbitrary JSON out of the public surface.
	/// </remarks>
	public static bool IsRegistry(string key)
	{
		if (string.IsNullOrEmpty(key))
			return false;

		if (key.EndsWith(ChangelogSuffix, StringComparison.Ordinal))
			return IsValidProduct(key.AsSpan(0, key.Length - ChangelogSuffix.Length));

		if (key.EndsWith(BundleSuffix, StringComparison.Ordinal))
			return IsValidProduct(key.AsSpan(0, key.Length - BundleSuffix.Length));

		return false;
	}

	private static bool IsValidProduct(ReadOnlySpan<char> product)
	{
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
