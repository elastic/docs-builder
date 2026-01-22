// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text.RegularExpressions;

namespace Elastic.Changelog.GitHub;

/// <summary>
/// Format of release notes detected during parsing
/// </summary>
public enum ReleaseNoteFormat
{
	/// <summary>Unknown or unrecognized format</summary>
	Unknown,

	/// <summary>Release Drafter format with emoji-prefixed category headers</summary>
	ReleaseDrafter,

	/// <summary>GitHub's default "Generate release notes" format with flat PR list</summary>
	GitHubDefault
}

/// <summary>
/// Represents a PR reference extracted from release notes
/// </summary>
public record ExtractedPrReference
{
	/// <summary>The PR number</summary>
	public int PrNumber { get; init; }

	/// <summary>The PR title as shown in release notes</summary>
	public string? Title { get; init; }

	/// <summary>The author username</summary>
	public string? Author { get; init; }

	/// <summary>The section title this PR appeared under (for Release Drafter format)</summary>
	public string? SectionTitle { get; init; }

	/// <summary>The changelog type inferred from the section header (for Release Drafter format)</summary>
	public string? InferredType { get; init; }
}

/// <summary>
/// Result of parsing release notes
/// </summary>
public record ParsedReleaseNotes
{
	/// <summary>The detected format of the release notes</summary>
	public ReleaseNoteFormat Format { get; init; }

	/// <summary>All PR references extracted from the release notes</summary>
	public IReadOnlyList<ExtractedPrReference> PrReferences { get; init; } = [];

	/// <summary>The full changelog URL if present (GitHub default format)</summary>
	public string? FullChangelogUrl { get; init; }
}

/// <summary>
/// Parses GitHub release notes to extract PR references and infer types
/// </summary>
public static partial class ReleaseNoteParser
{
	// Regex for PR line: "* Title by @author in #123" or "* Title by @author in https://..."
	[GeneratedRegex(@"^\*\s+(.+?)\s+by\s+@([\w-]+)\s+in\s+(?:#(\d+)|https://github\.com/[^/]+/[^/]+/pull/(\d+))", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
	private static partial Regex PrLineRegex();

	// Regex for section headers: "### 💥 Breaking Changes" or "### ✨ Features"
	[GeneratedRegex(@"^###\s+(.+)$", RegexOptions.Multiline)]
	private static partial Regex SectionHeaderRegex();

	// Regex for full changelog URL
	[GeneratedRegex(@"\*\*Full Changelog\*\*:\s*(https://[^\s]+)", RegexOptions.IgnoreCase)]
	private static partial Regex FullChangelogRegex();

	// Common emojis used in Release Drafter
	private static readonly string[] ReleaseDrafterEmojis = ["💥", "✨", "🐛", "📝", "🧰", "⚙️", "🎨", "🔒", "⚠️", "🚀"];

	// Mapping from section header keywords to changelog types
	private static readonly Dictionary<string, string> SectionToType = new(StringComparer.OrdinalIgnoreCase)
	{
		{ "Breaking Changes", "breaking-change" },
		{ "Breaking", "breaking-change" },
		{ "Features", "feature" },
		{ "Feature", "feature" },
		{ "Enhancements", "enhancement" },
		{ "Enhancement", "enhancement" },
		{ "Bug Fixes", "bug-fix" },
		{ "Bug Fix", "bug-fix" },
		{ "Bugfixes", "bug-fix" },
		{ "Fixes", "bug-fix" },
		{ "Documentation", "docs" },
		{ "Docs", "docs" },
		{ "Maintenance", "other" },
		{ "Chore", "other" },
		{ "Automation", "other" },
		{ "CI", "other" },
		{ "Deprecations", "deprecation" },
		{ "Deprecation", "deprecation" },
		{ "Deprecated", "deprecation" },
		{ "Security", "security" },
		{ "Redesign", "other" },
		{ "Other", "other" }
	};

	/// <summary>
	/// Parses release notes and extracts PR references
	/// </summary>
	/// <param name="body">The release notes body (markdown)</param>
	/// <returns>Parsed release notes with PR references and detected format</returns>
	public static ParsedReleaseNotes Parse(string body)
	{
		if (string.IsNullOrWhiteSpace(body))
		{
			return new ParsedReleaseNotes
			{
				Format = ReleaseNoteFormat.Unknown,
				PrReferences = []
			};
		}

		var format = DetectFormat(body);
		var fullChangelogUrl = ExtractFullChangelogUrl(body);

		var prReferences = format switch
		{
			ReleaseNoteFormat.ReleaseDrafter => ParseReleaseDrafterFormat(body),
			ReleaseNoteFormat.GitHubDefault => ParseGitHubDefaultFormat(body),
			_ => ParseUnknownFormat(body)
		};

		return new ParsedReleaseNotes
		{
			Format = format,
			PrReferences = prReferences,
			FullChangelogUrl = fullChangelogUrl
		};
	}

	/// <summary>
	/// Detects the format of release notes
	/// </summary>
	public static ReleaseNoteFormat DetectFormat(string body)
	{
		// Release Drafter format has emoji prefixed section headers like:
		// "### 💥 Breaking Changes", "### ✨ Features", "### 🐛 Bug Fixes"
		if (HasEmojiSectionHeaders(body))
			return ReleaseNoteFormat.ReleaseDrafter;

		// GitHub default just has "## What's Changed" with a flat list
		if (body.Contains("## What's Changed", StringComparison.OrdinalIgnoreCase) && !HasEmojiSectionHeaders(body))
			return ReleaseNoteFormat.GitHubDefault;

		return ReleaseNoteFormat.Unknown;
	}

	private static bool HasEmojiSectionHeaders(string body) =>
		body.Contains("###") && ReleaseDrafterEmojis.Any(body.Contains);

	private static string? ExtractFullChangelogUrl(string body)
	{
		var match = FullChangelogRegex().Match(body);
		return match.Success ? match.Groups[1].Value : null;
	}

	private static List<ExtractedPrReference> ParseReleaseDrafterFormat(string body)
	{
		var prReferences = new List<ExtractedPrReference>();
		var lines = body.Split('\n');

		string? currentSection = null;
		string? currentInferredType = null;

		foreach (var line in lines)
		{
			// Check for section header
			var sectionMatch = SectionHeaderRegex().Match(line);
			if (sectionMatch.Success)
			{
				currentSection = sectionMatch.Groups[1].Value.Trim();
				currentInferredType = MapSectionToType(currentSection);
				continue;
			}

			// Check for PR line
			var prMatch = PrLineRegex().Match(line);
			if (prMatch.Success)
			{
				var title = prMatch.Groups[1].Value.Trim();
				var author = prMatch.Groups[2].Value;
				var prNumber = prMatch.Groups[3].Success
					? int.Parse(prMatch.Groups[3].Value, CultureInfo.InvariantCulture)
					: int.Parse(prMatch.Groups[4].Value, CultureInfo.InvariantCulture);

				prReferences.Add(new ExtractedPrReference
				{
					PrNumber = prNumber,
					Title = title,
					Author = author,
					SectionTitle = currentSection,
					InferredType = currentInferredType
				});
			}
		}

		return prReferences;
	}

	private static List<ExtractedPrReference> ParseGitHubDefaultFormat(string body)
	{
		var prReferences = new List<ExtractedPrReference>();

		// Find all PR lines - in GitHub default format, there are no category sections
		var matches = PrLineRegex().Matches(body);

		foreach (Match match in matches)
		{
			var title = match.Groups[1].Value.Trim();
			var author = match.Groups[2].Value;
			var prNumber = match.Groups[3].Success
				? int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture)
				: int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);

			prReferences.Add(new ExtractedPrReference
			{
				PrNumber = prNumber,
				Title = title,
				Author = author,
				SectionTitle = null,
				InferredType = null // No type inference in GitHub default format
			});
		}

		return prReferences;
	}

	private static List<ExtractedPrReference> ParseUnknownFormat(string body) =>
		// Try to extract any PR references we can find
		ParseGitHubDefaultFormat(body);

	private static string? MapSectionToType(string sectionTitle)
	{
		// Strip emoji prefix and any extra whitespace
		var normalized = StripEmojiPrefix(sectionTitle).Trim();

		// Try direct match
		if (SectionToType.TryGetValue(normalized, out var type))
			return type;

		// Try partial match (section title contains the keyword)
		foreach (var kvp in SectionToType)
		{
			if (normalized.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
				return kvp.Value;
		}

		return null;
	}

	private static string StripEmojiPrefix(string text)
	{
		// Remove common emoji characters from the beginning
		var result = text.TrimStart();

		foreach (var emoji in ReleaseDrafterEmojis)
		{
			if (!result.StartsWith(emoji, StringComparison.Ordinal))
				continue;
			result = result[emoji.Length..].TrimStart();
			break;
		}

		return result;
	}
}
