// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Documentation;
using Elastic.Documentation.Links;

namespace Elastic.Documentation.Diagnostics;

public static partial class ApiLinkMatcher
{
	/// <summary>Where the API Explorer serves its pages on elastic.co: <c>/docs/api/{key}/...</c>.</summary>
	public const string ApiPrefix = "/docs/api/";

	private const string ApiSegment = "api/";

	/// <summary>
	/// Returns the normalized page path (no query, fragment or trailing slash, lowercase) when
	/// <paramref name="url"/> targets an API reference page on elastic.co, in either the current or the legacy form.
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
		// The catalog and the bare legacy root are not pages of an API.
		if (!normalized.StartsWith(ApiPrefix, StringComparison.Ordinal) || ApiLegacyRedirects.IsLegacyRoot(normalized))
			return false;

		path = normalized;
		return true;
	}

	/// <summary>
	/// Returns <paramref name="url"/> as an absolute elastic.co API URL when it targets an API page, either as such or as a
	/// site path under <paramref name="urlPathPrefix"/>, which is how the API Explorer writes links between its own pages.
	/// </summary>
	public static bool TryCanonicalize(string? url, string? urlPathPrefix, out string canonical)
	{
		canonical = string.Empty;
		if (string.IsNullOrWhiteSpace(url))
			return false;

		if (TryGetApiPath(url, out _))
		{
			canonical = url;
			return true;
		}

		var prefix = urlPathPrefix?.TrimEnd('/') ?? string.Empty;
		if (
			!url.StartsWith($"{prefix}/{ApiSegment}", StringComparison.OrdinalIgnoreCase)
			|| url.Length <= prefix.Length + ApiSegment.Length + 1
		)
			return false;

		canonical = $"https://www.elastic.co/docs{url[prefix.Length..]}";
		return true;
	}

	public static string Normalize(string path) =>
		DuplicateSlashes().Replace(Uri.UnescapeDataString(path), "/").TrimEnd('/').ToLowerInvariant();

	[GeneratedRegex("/{2,}", RegexOptions.None, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex DuplicateSlashes();
}
