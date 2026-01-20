// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;

namespace Elastic.Changelog;

/// <summary>
/// Utility class for extracting release notes from PR descriptions
/// </summary>
public static partial class ReleaseNotesExtractor
{
	[GeneratedRegex(@"<!--[\s\S]*?-->", RegexOptions.None)]
	private static partial Regex HtmlCommentRegex();

	[GeneratedRegex(@"(\r?\n){3,}", RegexOptions.None)]
	private static partial Regex MultipleNewlinesRegex();

	[GeneratedRegex(@"(?:\n|^)\s*#*\s*release[\s-]?notes?[:\s-]*(.*?)(?:(\r?\n|\r){2}|$|((\r?\n|\r)\s*#+))", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
	private static partial Regex ReleaseNoteRegex();

	private const int MaxReleaseNoteTitleLength = 120;

	/// <summary>
	/// Strips HTML comments from markdown text.
	/// This handles both single-line and multi-line comments.
	/// Also collapses excessive blank lines that may result from comment removal,
	/// to prevent creating artificial section breaks.
	/// </summary>
	private static string StripHtmlComments(string markdown)
	{
		if (string.IsNullOrWhiteSpace(markdown))
		{
			return markdown;
		}

		// Remove HTML comments
		var withoutComments = HtmlCommentRegex().Replace(markdown, string.Empty);

		// Collapse 3+ consecutive newlines into 2 (preserving paragraph breaks but not creating extra ones)
		var normalized = MultipleNewlinesRegex().Replace(withoutComments, "\n\n");

		return normalized;
	}

	/// <summary>
	/// Finds and retrieves the actual "release note" details from a PR description (in markdown format).
	/// It will look for:
	/// - paragraphs beginning with "release note" (or slight variations of that) and the sentence till the end of line.
	/// - markdown headers like "## Release Note"
	///
	/// HTML comments are stripped before extraction to avoid picking up template instructions.
	/// </summary>
	/// <param name="markdown">The PR description body</param>
	/// <returns>The extracted release note content, or null if not found</returns>
	public static string? FindReleaseNote(string? markdown)
	{
		if (string.IsNullOrWhiteSpace(markdown))
		{
			return null;
		}

		// Strip HTML comments first to avoid extracting template instructions
		var cleanedMarkdown = StripHtmlComments(markdown);

		// Regex breakdown:
		// - (?:\n|^)\s*#*\s* - start of line, optional whitespace and markdown headers
		// - release[\s-]?notes? - matches "release note", "release notes", "release-note", "release-notes", etc.
		// - [:\s-]* - matches separator after "release note" (colon, dash, whitespace) but NOT other non-word chars like {
		// - (.*?) - lazily capture the release note content
		// - Terminator: double newline, end of string, or new markdown header
		var match = ReleaseNoteRegex().Match(cleanedMarkdown);

		if (match.Success && match.Groups.Count > 1)
		{
			var releaseNote = match.Groups[1].Value.Trim();
			return string.IsNullOrWhiteSpace(releaseNote) ? null : releaseNote;
		}

		return null;
	}

	/// <summary>
	/// Extracts release notes from PR body and determines how to use them.
	/// </summary>
	/// <param name="prBody">The PR description body</param>
	/// <returns>
	/// A tuple where:
	/// - Item1: The title to use (either original title or extracted release note if short)
	/// - Item2: The description to use (extracted release note if long, otherwise null)
	/// </returns>
	public static (string? title, string? description) ExtractReleaseNotes(string? prBody)
	{
		var releaseNote = FindReleaseNote(prBody);

		// No release note found: return nulls (use defaults)
		if (string.IsNullOrWhiteSpace(releaseNote))
		{
			return (null, null);
		}

		// Long release note (>120 characters or multi-line): use in description
		if (releaseNote.Length > MaxReleaseNoteTitleLength || releaseNote.Contains('\n'))
		{
			return (null, releaseNote);
		}

		// Short release note (≤120 characters, single line): use in title
		return (releaseNote, null);
	}
}
