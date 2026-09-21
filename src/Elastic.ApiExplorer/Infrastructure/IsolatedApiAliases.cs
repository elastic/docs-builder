// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>
/// Isolated fixtures use <c>docs-builder-{product}</c> keys so they do not collide with
/// assembler / docs-content. Map the short product path people type or paste.
/// </summary>
public static class IsolatedApiAliases
{
	public const string FixturePrefix = "docs-builder-";

	public static bool TryPrefixedDocSlug(string slug, [NotNullWhen(true)] out string? prefixed)
	{
		prefixed = null;
		var trimmed = slug.Trim('/');
		const string doc = "doc/";
		if (!trimmed.StartsWith(doc, StringComparison.Ordinal))
			return false;

		var remainder = trimmed[doc.Length..];
		if (remainder.Length == 0)
			return false;

		var slash = remainder.IndexOf('/', StringComparison.Ordinal);
		var key = slash < 0 ? remainder : remainder[..slash];
		if (key.Length == 0 || key.StartsWith(FixturePrefix, StringComparison.Ordinal))
			return false;

		prefixed = slash < 0 ? $"{doc}{FixturePrefix}{key}" : $"{doc}{FixturePrefix}{key}{remainder[slash..]}";
		return true;
	}
}
