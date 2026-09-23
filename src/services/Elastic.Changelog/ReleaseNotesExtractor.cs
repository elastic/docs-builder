// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog;

/// <summary>The extracted release note text and information about omitted trailing content.</summary>
public sealed record ReleaseNoteExtractionResult(string? Content, bool WasTruncated);

internal static class ReleaseNoteExtractionDiagnostics
{
	public static void EmitHint(IDiagnosticsCollector collector, string source, int prNumber, ReleaseNoteExtractionResult extraction)
	{
		if (extraction.Content == null)
		{
			collector.EmitHint(
				source,
				$"No release note description was found for PR #{prNumber}. Add a 'Release note' section to the PR body if the entry needs more detail than its title."
			);
		}
		else if (extraction.WasTruncated)
		{
			collector.EmitHint(
				source,
				$"The release note description for PR #{prNumber} contains content after the first paragraph or heading. Only the first paragraph was used."
			);
		}
	}
}

/// <summary>
/// Utility class for extracting release notes from PR descriptions
/// </summary>
public static partial class ReleaseNotesExtractor
{
	[GeneratedRegex(@"<!--[\s\S]*?-->", RegexOptions.None)]
	private static partial Regex HtmlCommentRegex();

	[GeneratedRegex(@"(\r?\n){3,}", RegexOptions.None)]
	private static partial Regex MultipleNewlinesRegex();

	[GeneratedRegex(@"(?:\n|^)\s*#*\s*release[\s-]?notes?[:\s-]*(.*?)(?:(\r?\n|\r){2}|$|((\r?\n|\r)\s*#+))", RegexOptions.IgnoreCase
		| RegexOptions.Singleline)]
	private static partial Regex ReleaseNoteRegex();

	/// <summary>
	/// Strips HTML comments from markdown text.
	/// This handles both single-line and multi-line comments.
	/// Also collapses excessive blank lines that may result from comment removal,
	/// to prevent creating artificial section breaks.
	/// </summary>
	private static string StripHtmlComments(string markdown)
	{
		if (string.IsNullOrWhiteSpace(markdown))
			return markdown;

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
	public static string? FindReleaseNote(string? markdown) => ExtractReleaseNote(markdown).Content;

	/// <summary>
	/// Extracts release note content and reports when later PR body content was not included.
	/// </summary>
	/// <param name="markdown">The PR description body.</param>
	/// <returns>The extraction result.</returns>
	public static ReleaseNoteExtractionResult ExtractReleaseNote(string? markdown)
	{
		if (string.IsNullOrWhiteSpace(markdown))
			return new ReleaseNoteExtractionResult(null, false);

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
			var content = string.IsNullOrWhiteSpace(releaseNote) ? null : releaseNote;
			var remainingContent = cleanedMarkdown[(match.Index + match.Length)..];
			return new ReleaseNoteExtractionResult(content, content != null && !string.IsNullOrWhiteSpace(remainingContent));
		}

		return new ReleaseNoteExtractionResult(null, false);
	}
}
