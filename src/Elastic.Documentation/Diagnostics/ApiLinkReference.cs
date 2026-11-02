// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Diagnostics;

/// <summary>A link to an API reference page, recorded during the Markdown build and checked once the API pages exist.</summary>
public readonly record struct ApiLinkReference(string Url, string File, int Line, int Column, int Length);

public static class ApiLinkMatcher
{
	public const string ApiDocPrefix = "/docs/api/doc/";

	/// <summary>
	/// Returns the normalized page path (no query, fragment or trailing slash, lowercase) when
	/// <paramref name="url"/> targets an API reference page on elastic.co.
	/// </summary>
	public static bool TryGetApiPath(string? url, out string path)
	{
		path = string.Empty;
		if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
			return false;
		if (uri.Scheme is not ("http" or "https"))
			return false;
		if (
			!uri.Host.Equals("www.elastic.co", StringComparison.OrdinalIgnoreCase)
			&& !uri.Host.Equals("elastic.co", StringComparison.OrdinalIgnoreCase)
		)
			return false;

		var normalized = Normalize(uri.AbsolutePath);
		if (!normalized.StartsWith(ApiDocPrefix, StringComparison.Ordinal))
			return false;

		path = normalized;
		return true;
	}

	public static string Normalize(string path) => Uri.UnescapeDataString(path).TrimEnd('/').ToLowerInvariant();
}
