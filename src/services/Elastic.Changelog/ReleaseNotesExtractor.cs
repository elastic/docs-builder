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

	// A line that starts a section: a markdown heading ("## Impact") or a bare label ("Impact: ...").
	// Group 1 = heading hashes (empty for a label), group 2 = colon (if any), group 3 = inline text after the name.
	[GeneratedRegex(@"^[ \t]*(#{0,6})[ \t]*\**impact\**[ \t]*(:?)\**[ \t]*(.*)$", RegexOptions.IgnoreCase)]
	private static partial Regex ImpactStartRegex();

	[GeneratedRegex(@"^[ \t]*(#{0,6})[ \t]*\**actions?\**[ \t]*(:?)\**[ \t]*(.*)$", RegexOptions.IgnoreCase)]
	private static partial Regex ActionStartRegex();

	[GeneratedRegex(@"^[ \t]*(#{1,6})[ \t]+\S")]
	private static partial Regex HeadingRegex();

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
	/// Finds the "Impact" section of a PR description: a heading such as <c>## Impact</c> or a label such as <c>Impact: ...</c>.
	/// Heading sections run until the next heading of the same or a higher level; label sections end at the first blank line
	/// after their content or at the next heading. HTML comments are stripped first.
	/// </summary>
	public static string? FindImpact(string? markdown) => FindSection(markdown, ImpactStartRegex());

	/// <summary>Finds the "Action" section of a PR description. See <see cref="FindImpact"/> for the recognized forms.</summary>
	public static string? FindAction(string? markdown) => FindSection(markdown, ActionStartRegex());

	/// <summary>A match is a section start when it is a bare heading (<c>## Impact</c>) or a label with a colon (<c>Impact: ...</c>).</summary>
	private static bool IsSectionStart(Match start, out int hashes, out string inline)
	{
		hashes = 0;
		inline = string.Empty;
		if (!start.Success)
			return false;

		hashes = start.Groups[1].Value.Length;
		inline = start.Groups[3].Value.Trim();
		var hasColon = start.Groups[2].Value.Length > 0;
		return (hashes > 0 && inline.Length == 0) || hasColon;
	}

	private static string? FindSection(string? markdown, Regex startRegex)
	{
		if (string.IsNullOrWhiteSpace(markdown))
			return null;

		var lines = StripHtmlComments(markdown).Replace("\r\n", "\n").Split('\n');
		for (var i = 0; i < lines.Length; i++)
		{
			var start = startRegex.Match(lines[i]);
			if (!start.Success)
				continue;

			// Prose such as "Impact of this change..." or "Actions taken in CI" must not match.
			if (!IsSectionStart(start, out var hashes, out var inline))
				continue;

			var isHeading = hashes > 0 && inline.Length == 0;

			var content = new List<string>();
			if (inline.Length > 0)
				content.Add(inline);

			for (var j = i + 1; j < lines.Length; j++)
			{
				var line = lines[j];
				// One section never swallows the next: stop at the start of either Impact or Action.
				if (
					IsSectionStart(ImpactStartRegex().Match(line), out _, out _)
					|| IsSectionStart(ActionStartRegex().Match(line), out _, out _)
				)
					break;
				var heading = HeadingRegex().Match(line);
				if (heading.Success && (!isHeading || heading.Groups[1].Value.Length <= hashes))
					break;
				// Label sections stop at the first blank line once they have content.
				if (!isHeading && string.IsNullOrWhiteSpace(line) && content.Count > 0)
					break;
				content.Add(line);
			}

			var text = string.Join('\n', content).Trim();
			if (text.Length > 0)
				return text;
		}

		return null;
	}

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
