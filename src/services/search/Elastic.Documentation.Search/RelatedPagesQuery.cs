// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace Elastic.Documentation.Search;

public static partial class RelatedPagesQuery
{
	public const int MaximumPathLength = 2048;
	private const int MaximumQueryTerms = 12;

	private static readonly FrozenSet<string> IgnoredTerms = FrozenSet.ToFrozenSet<string>(
	[
		"docs", "doc", "guide", "guides", "current", "latest", "html", "htm",
		"en", "en-us", "de", "fr", "ja", "ko", "zh", "es", "pt"
	], StringComparer.OrdinalIgnoreCase);

	public static string FromPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
			return string.Empty;

		var value = path;
		if (Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
			value = uri.AbsolutePath;

		try
		{
			value = Uri.UnescapeDataString(value);
		}
		catch (UriFormatException)
		{
			// Keep the original path when malformed escape sequences cannot be decoded.
		}

		value = TrailingIndexRegex().Replace(value, string.Empty);

		var terms = value
			.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(segment => ExtensionRegex().Replace(segment, string.Empty))
			.Where(segment => !IgnoredTerms.Contains(segment) && !VersionRegex().IsMatch(segment))
			.SelectMany(segment => TokenRegex().Matches(segment).Select(m => m.Value.ToLowerInvariant()))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.TakeLast(MaximumQueryTerms);

		return string.Join(' ', terms);
	}

	[GeneratedRegex(@"[\p{L}\p{N}]+", RegexOptions.CultureInvariant)]
	private static partial Regex TokenRegex();

	[GeneratedRegex(@"(?:^|/)index(?:\.html?)?/?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex TrailingIndexRegex();

	[GeneratedRegex(@"\.html?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex ExtensionRegex();

	[GeneratedRegex(@"^v?\d+(?:\.\d+)*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex VersionRegex();
}
