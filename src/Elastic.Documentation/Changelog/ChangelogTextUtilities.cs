// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;

namespace Elastic.Documentation.Changelog;

/// <summary>
/// Static utility methods for text processing in changelog generation
/// </summary>
public static partial class ChangelogTextUtilities
{
	[GeneratedRegex(@"\d+$", RegexOptions.None)]
	private static partial Regex TrailingNumberRegex();

	/// <summary>
	/// Capitalizes first letter and ensures text ends with period
	/// </summary>
	public static string Beautify(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return string.Empty;

		// Capitalize first letter and ensure ends with period
		var result = text.Length < 2
			? char.ToUpperInvariant(text[0]).ToString()
			: char.ToUpperInvariant(text[0]) + text[1..];
		if (!result.EndsWith('.'))
			result += ".";
		return result;
	}

	/// <summary>
	/// Indents each line with two spaces
	/// </summary>
	public static string Indent(string text)
	{
		var lines = text.Split('\n');
		return string.Join("\n", lines.Select(line => "  " + line));
	}

	/// <summary>
	/// Converts title to slug format for folder names and anchors (lowercase, dashes instead of spaces)
	/// </summary>
	public static string TitleToSlug(string title)
	{
		if (string.IsNullOrWhiteSpace(title))
			return string.Empty;

		return title.ToLowerInvariant().Replace(' ', '-');
	}

	/// <summary>
	/// Formats area header - capitalizes first letter and replaces hyphens with spaces
	/// </summary>
	public static string FormatAreaHeader(string area)
	{
		if (string.IsNullOrWhiteSpace(area))
			return string.Empty;

		var result = area.Length < 2
			? char.ToUpperInvariant(area[0]).ToString()
			: char.ToUpperInvariant(area[0]) + area[1..];
		return result.Replace("-", " ");
	}

	/// <summary>
	/// Formats subtype header - capitalizes first letter and replaces hyphens with spaces
	/// </summary>
	public static string FormatSubtypeHeader(string subtype)
	{
		if (string.IsNullOrWhiteSpace(subtype))
			return string.Empty;

		var result = subtype.Length < 2
			? char.ToUpperInvariant(subtype[0]).ToString()
			: char.ToUpperInvariant(subtype[0]) + subtype[1..];
		return result.Replace("-", " ");
	}

	/// <summary>
	/// Sanitizes filename by converting to lowercase, replacing special characters with dashes, and limiting length
	/// </summary>
	public static string SanitizeFilename(string input)
	{
		var sanitized = input.ToLowerInvariant()
			.Replace(" ", "-")
			.Replace("/", "-")
			.Replace("\\", "-")
			.Replace(":", "")
			.Replace("'", "")
			.Replace("\"", "");

		// Limit length
		if (sanitized.Length > 50)
			sanitized = sanitized[..50];

		return sanitized;
	}

	/// <summary>
	/// Strips square bracket prefix and optional colon from title (e.g., "[Inference API] Title" -> "Title")
	/// </summary>
	public static string StripSquareBracketPrefix(string title)
	{
		if (string.IsNullOrWhiteSpace(title))
			return title;

		// Check if title starts with '['
		if (!title.StartsWith('['))
			return title;

		// Find the matching ']'
		var closingBracketIndex = title.IndexOf(']', 1);
		if (closingBracketIndex < 0)
			return title; // No matching ']', return as-is

		// Extract everything after the closing bracket
		var remaining = title[(closingBracketIndex + 1)..];

		// Remove colon if it exists right after the closing bracket
		if (remaining.StartsWith(':'))
			remaining = remaining[1..];

		// Trim whitespace
		return remaining.TrimStart();
	}

	/// <summary>
	/// Extracts PR number from PR URL or reference
	/// </summary>
	public static int? ExtractPrNumber(string prUrl, string? defaultOwner = null, string? defaultRepo = null)
	{
		// Handle full URL: https://github.com/owner/repo/pull/123
		if (prUrl.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase) ||
			prUrl.StartsWith("http://github.com/", StringComparison.OrdinalIgnoreCase))
		{
			var uri = new Uri(prUrl);
			var segments = uri.Segments;
			// segments[0] is "/", segments[1] is "owner/", segments[2] is "repo/", segments[3] is "pull/", segments[4] is "123"
			if (segments.Length >= 5 &&
				segments[3].Equals("pull/", StringComparison.OrdinalIgnoreCase) &&
				int.TryParse(segments[4].TrimEnd('/'), out var prNum))
				return prNum;
		}

		// Handle short format: owner/repo#123
		var hashIndex = prUrl.LastIndexOf('#');
		if (hashIndex > 0 && hashIndex < prUrl.Length - 1)
		{
			var prPart = prUrl[(hashIndex + 1)..];
			if (int.TryParse(prPart, out var prNum))
				return prNum;
		}

		// Handle just a PR number when owner/repo are provided
		if (int.TryParse(prUrl, out var prNumber) &&
			!string.IsNullOrWhiteSpace(defaultOwner) && !string.IsNullOrWhiteSpace(defaultRepo))
			return prNumber;

		return null;
	}

	/// <summary>
	/// Formats PR link as markdown
	/// </summary>
	public static string FormatPrLink(string pr, string repo, bool hidePrivateLinks)
	{
		// Extract PR number
		var match = TrailingNumberRegex().Match(pr);
		var prNumber = match.Success ? match.Value : pr;

		// Format as markdown link
		string link;
		if (pr.StartsWith("http", StringComparison.OrdinalIgnoreCase))
			link = $"[#{prNumber}]({pr})";
		else
		{
			var url = $"https://github.com/elastic/{repo}/pull/{prNumber}";
			link = $"[#{prNumber}]({url})";
		}

		// Comment out link if hiding private links
		if (hidePrivateLinks)
			return $"% {link}";

		return link;
	}

	/// <summary>
	/// Formats issue link as markdown
	/// </summary>
	public static string FormatIssueLink(string issue, string repo, bool hidePrivateLinks)
	{
		// Extract issue number
		var match = TrailingNumberRegex().Match(issue);
		var issueNumber = match.Success ? match.Value : issue;

		// Format as markdown link
		string link;
		if (issue.StartsWith("http", StringComparison.OrdinalIgnoreCase))
			link = $"[#{issueNumber}]({issue})";
		else
		{
			var url = $"https://github.com/elastic/{repo}/issues/{issueNumber}";
			link = $"[#{issueNumber}]({url})";
		}

		// Comment out link if hiding private links
		if (hidePrivateLinks)
			return $"% {link}";

		return link;
	}

	/// <summary>
	/// Formats PR link as asciidoc
	/// </summary>
	public static string FormatPrLinkAsciidoc(string pr, string repo, bool hidePrivateLinks)
	{
		// Extract PR number
		var match = TrailingNumberRegex().Match(pr);
		var prNumber = match.Success ? match.Value : pr;

		// Format as asciidoc link attribute reference
		// Format: {repo-pull}PRNUMBER[#PRNUMBER]
		var attributeName = ConvertRepoToAttributeName(repo, "pull");
		var link = $"{{{attributeName}}}{prNumber}[#{prNumber}]";

		// Comment out link if hiding private links
		if (hidePrivateLinks)
			return $"// {link}";

		return link;
	}

	/// <summary>
	/// Formats issue link as asciidoc
	/// </summary>
	public static string FormatIssueLinkAsciidoc(string issue, string repo, bool hidePrivateLinks)
	{
		// Extract issue number
		var match = TrailingNumberRegex().Match(issue);
		var issueNumber = match.Success ? match.Value : issue;

		// Format as asciidoc link attribute reference
		// Format: {repo-issue}ISSUENUMBER[#ISSUENUMBER]
		var attributeName = ConvertRepoToAttributeName(repo, "issue");
		var link = $"{{{attributeName}}}{issueNumber}[#{issueNumber}]";

		// Comment out link if hiding private links
		if (hidePrivateLinks)
			return $"// {link}";

		return link;
	}

	/// <summary>
	/// Converts repo name to attribute format for asciidoc links
	/// </summary>
	private static string ConvertRepoToAttributeName(string repo, string suffix)
	{
		if (string.IsNullOrWhiteSpace(repo))
			return $"repo-{suffix}";

		// Handle common repo name patterns
		if (repo.Equals("elasticsearch", StringComparison.OrdinalIgnoreCase))
			return $"es-{suffix}";

		// Remove "elastic-" prefix if present
		var normalized = repo;
		if (normalized.StartsWith("elastic-", StringComparison.OrdinalIgnoreCase))
			normalized = normalized.Substring("elastic-".Length);

		// Return normalized name with suffix
		return $"{normalized}-{suffix}";
	}

	/// <summary>
	/// Infers lifecycle from a version tag name.
	/// Examples:
	///   v1.0.0 → ga
	///   v1.0.0-beta1 → beta
	///   v1.0.0-preview.1 → preview
	///   1.0.0-alpha1 → preview
	///   1.0.0-rc1 → beta
	/// </summary>
	public static string InferLifecycleFromVersion(string tagName)
	{
		if (string.IsNullOrWhiteSpace(tagName))
			return "ga";

		var normalizedTag = tagName.TrimStart('v', 'V').ToLowerInvariant();

		// Check for prerelease suffixes
		if (normalizedTag.Contains("-preview") || normalizedTag.Contains("-alpha"))
			return "preview";

		if (normalizedTag.Contains("-beta") || normalizedTag.Contains("-rc"))
			return "beta";

		// No prerelease suffix = GA
		return "ga";
	}

	/// <summary>
	/// Extracts the base version number without prerelease suffix.
	/// Examples:
	///   v1.0.0 → 1.0.0
	///   v1.0.0-beta1 → 1.0.0
	///   1.2.3-preview.1 → 1.2.3
	/// </summary>
	public static string ExtractBaseVersion(string tagName)
	{
		if (string.IsNullOrWhiteSpace(tagName))
			return tagName;

		var normalizedTag = tagName.TrimStart('v', 'V');

		// Remove prerelease suffix (everything after first hyphen)
		var hyphenIndex = normalizedTag.IndexOf('-');
		return hyphenIndex > 0 ? normalizedTag[..hyphenIndex] : normalizedTag;
	}

	/// <summary>
	/// Parses repository string to extract owner and repo name.
	/// Handles formats: "owner/repo", "repo"
	/// </summary>
	public static (string? Owner, string Repo) ParseRepository(string repository)
	{
		if (string.IsNullOrWhiteSpace(repository))
			return (null, string.Empty);

		var parts = repository.Split('/');
		return parts.Length >= 2
			? (parts[0], parts[1])
			: (null, parts[0]);
	}

	[GeneratedRegex(@"[^a-z0-9]+", RegexOptions.None)]
	private static partial Regex NonAlphanumericRegex();

	/// <summary>
	/// Generates a URL-safe slug from a title.
	/// Takes first 6 words, lowercased, only A-Z0-9, joined with dashes.
	/// </summary>
	public static string GenerateSlug(string title, int maxWords = 6)
	{
		if (string.IsNullOrWhiteSpace(title))
			return "untitled";

		// Split on whitespace and take first N words
		var words = title
			.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
			.Take(maxWords)
			.Select(word => NonAlphanumericRegex().Replace(word.ToLowerInvariant(), string.Empty))
			.Where(word => !string.IsNullOrEmpty(word))
			.ToArray();

		if (words.Length == 0)
			return "untitled";

		return string.Join("-", words);
	}
}

/// <summary>
/// Constants for changelog entry types
/// </summary>
public static class ChangelogEntryTypes
{
	public const string Feature = "feature";
	public const string Enhancement = "enhancement";
	public const string Security = "security";
	public const string BugFix = "bug-fix";
	public const string BreakingChange = "breaking-change";
	public const string Deprecation = "deprecation";
	public const string KnownIssue = "known-issue";
	public const string Docs = "docs";
	public const string Regression = "regression";
	public const string Other = "other";
}
