// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using AwesomeAssertions;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Elastic.Documentation.Testing;
using JetBrains.Annotations;
using Nullean.Snapshots.Html;

namespace Elastic.Authoring.Tests.Framework;

/// <summary>
/// Port of <c>HtmlAssertions.fs</c> and the <c>diff</c> helper from <c>MarkdownResultsAssertions.fs</c>.
///
/// Key differences from <c>Elastic.Markdown.Tests.PrettyHtmlExtensions</c>:
/// <list type="bullet">
///   <item><description>
///     Selects <c>section#elastic-docs-v3</c> from the rendered page (not <c>document.Body</c>).
///   </description></item>
///   <item><description>
///     Strips the first child element when it is an H1, matching what the F# harness does —
///     the fixture wraps every <c>Setup.Markdown</c> call in <c># Test Document</c> so the H1
///     would appear verbatim in every assertion snapshot otherwise.
///   </description></item>
///   <item><description>
///     Compares through Nullean.Snapshots.Html, which reports a structural diff and then a line diff.
///   </description></item>
/// </list>
/// All assertion methods are synchronous — the async wrapper lives on <see cref="Scenario"/>.
/// </summary>
internal static class HtmlAssertions
{
	// ────────────────────────────────────────────────────────────────────────────────
	// DiffPlex plain-text diff (used by PlainText, LlmMarkdown assertions too)
	// ────────────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Returns a readable unified-diff string (prefixed <c>- </c>/<c>+ </c>/<c>  </c>) between
	/// <paramref name="expected"/> and <paramref name="actual"/>, or an empty string when they
	/// are identical.
	/// Port of <c>ResultsAssertions.diff</c>.
	/// </summary>
	internal static string Diff(string expected, string actual)
	{
		var result = InlineDiffBuilder.Diff(expected, actual);
		var lines = result.Lines;
		var mutatedCount = lines.Count(l => l.Type is ChangeType.Modified or ChangeType.Inserted or ChangeType.Deleted);

		if (mutatedCount == 0)
			return string.Empty;

		var actualLineLength = actual.Split('\n').Length;
		if (mutatedCount >= actualLineLength)
			return $"Mutations {mutatedCount} on all {actualLineLength} showing actual: \n\n{actual}";

		return string.Join(
			'\n',
			lines.Select(
				l =>
					l.Type switch
					{
						ChangeType.Deleted => "- " + l.Text,
						ChangeType.Modified or ChangeType.Inserted => "+ " + l.Text,
						_ => "  " + l.Text
					}
			)
		);
	}

	// ────────────────────────────────────────────────────────────────────────────────
	// HTML comparison on Nullean.Snapshots.Html
	// ────────────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// The rendered page's content section, minus the fixture's leading H1 and the htmx attributes on links.
	/// The expected fragment is wrapped in the same section so both sides go through identical normalisation.
	/// </summary>
	private static HtmlSnapshotOptions Options => DocsHtml.Section();

	private static string InSection(string fragment) => $"<section id=\"elastic-docs-v3\">{fragment}</section>";

	// ────────────────────────────────────────────────────────────────────────────────
	// Per-file assertions (called from MarkdownResultTask / Scenario)
	// ────────────────────────────────────────────────────────────────────────────────

	[DebuggerStepThrough]
	internal static void ToHtml([LanguageInjection("html")] string expected, MarkdownResult actual) =>
		actual.Html.Should().BeHtml(InSection(expected), Options);

	[DebuggerStepThrough]
	internal static void ContainsHtml([LanguageInjection("html")] string expected, MarkdownResult actual) =>
		actual.Html.Should().ContainHtml(InSection(expected), Options);

	[DebuggerStepThrough]
	internal static void ContainsRawHtml(string expected, MarkdownResult actual)
	{
		if (!actual.Html.Contains(expected, StringComparison.Ordinal))
			throw new AwesomeAssertions.Execution.AssertionFailedException(
				$"""
Expected html to contain:
{expected}

But it was not found in:

{actual.Html}
"""
			);
	}

	[DebuggerStepThrough]
	internal static void DoesNotContainHtml(string expected, MarkdownResult actual)
	{
		if (actual.Html.Contains(expected, StringComparison.Ordinal))
			throw new AwesomeAssertions.Execution.AssertionFailedException(
				$"""
Expected html NOT to contain:
{expected}

But it was found in:

{actual.Html}
"""
			);
	}

	// ────────────────────────────────────────────────────────────────────────────────
	// Generator-level assertions (operate on the index.md result)
	// ────────────────────────────────────────────────────────────────────────────────

	internal static string IndexHtml(GeneratorResults results) => FindIndexMd(results).Html;

	private static MarkdownResult FindIndexMd(GeneratorResults results) =>
		results.MarkdownResults.FirstOrDefault(r => r.File.RelativePath == "index.md")
			?? throw new AwesomeAssertions.Execution.AssertionFailedException("Could not find 'index.md' in generator results");

	[DebuggerStepThrough]
	internal static void ConvertsToHtml([LanguageInjection("html")] string expected, GeneratorResults results) =>
		ToHtml(expected, FindIndexMd(results));

	[DebuggerStepThrough]
	internal static void ConvertsToContainingHtml([LanguageInjection("html")] string expected, GeneratorResults results) =>
		ContainsHtml(expected, FindIndexMd(results));

	[DebuggerStepThrough]
	internal static void ConvertsToContainingRawHtml(string expected, GeneratorResults results) =>
		ContainsRawHtml(expected, FindIndexMd(results));

	[DebuggerStepThrough]
	internal static void DoesNotConvertToContainingHtml(string expected, GeneratorResults results) =>
		DoesNotContainHtml(expected, FindIndexMd(results));
}
