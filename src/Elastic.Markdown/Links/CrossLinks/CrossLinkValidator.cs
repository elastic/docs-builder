// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;

namespace Elastic.Markdown.Links.CrossLinks;

/// <summary>
/// Utility class for validating and identifying cross-repository links
/// </summary>
public static class CrossLinkValidator
{
	/// <summary>
	/// URI schemes that are excluded from being treated as cross-repository links.
	/// These are standard web/protocol schemes that should not be processed as crosslinks.
	/// </summary>
	private static readonly ImmutableHashSet<string> ExcludedSchemes =
		ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase,
			"http", "https", "ftp", "file", "tel", "jdbc", "mailto");

	/// <summary>
	/// Validates that a URI string is a valid cross-repository link.
	/// </summary>
	/// <param name="uriString">The URI string to validate</param>
	/// <param name="errorMessage">Error message if validation fails</param>
	/// <returns>True if valid crosslink, false otherwise</returns>
	public static bool IsValidCrossLink(string? uriString, out string? errorMessage)
	{
		errorMessage = null;

		if (string.IsNullOrWhiteSpace(uriString))
		{
			errorMessage = "Cross-link entries must specify a non-empty URI";
			return false;
		}

		if (!Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
		{
			errorMessage = $"Cross-link URI '{uriString}' is not a valid absolute URI format";
			return false;
		}

		if (ExcludedSchemes.Contains(uri.Scheme))
		{
			errorMessage = $"Cross-link URI '{uriString}' cannot use standard web/protocol schemes ({string.Join(", ", ExcludedSchemes)}). Use cross-repository schemes like 'docs-content://', 'kibana://', etc.";
			return false;
		}

		return true;
	}

	/// <summary>
	/// Determines if a URI is a cross-repository link (for identification purposes).
	/// This is more permissive than validation and is used by the Markdown parser.
	/// </summary>
	/// <param name="uri">The URI to check</param>
	/// <returns>True if this should be treated as a crosslink</returns>
	public static bool IsCrossLink(Uri? uri) =>
		uri != null
		&& !ExcludedSchemes.Contains(uri.Scheme)
		&& !uri.IsFile
		&& !string.IsNullOrEmpty(uri.Scheme);

	/// <summary>
	/// Gets the list of excluded URI schemes for reference
	/// </summary>
	public static IReadOnlySet<string> GetExcludedSchemes() => ExcludedSchemes;
}
