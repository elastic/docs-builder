// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using AngleSharp.Diffing;
using AngleSharp.Diffing.Core;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using JetBrains.Annotations;
using Xunit.Sdk;

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
///     Produces a humanized diff via <see cref="HtmlDiffString"/> that names the diff type
///     (NodeDiff / AttrDiff / MissingNodeDiff etc.) and shows expected vs. actual node text.
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
	// AngleSharp deep HTML diff
	// ────────────────────────────────────────────────────────────────────────────────

	internal static string HtmlDiffString(IEnumerable<IDiff> diffs)
	{
		var formatter = new PrettyMarkupFormatter();

		string NodeText(ComparisonSource source)
		{
			using var sw = new StringWriter();
			source.Node.ToHtml(sw, formatter);
			return sw.ToString();
		}

		string AttrText(AttributeComparisonSource source)
		{
			using var sw = new StringWriter();
			source.Attribute.ToHtml(sw, formatter);
			return sw.ToString();
		}

		// NodeDiffText and AttrDiffText accept non-nullable (callers pass .Control / .Test directly).
		// For Missing*/Unexpected* cases we pass a sentinel "missing" string instead of calling these helpers.
		string NodeDiffText(ComparisonSource control, ComparisonSource test) =>
			$"\nexpected: {NodeText(control)}\nactual: {NodeText(test)}\n";

		string MissingNodeText(ComparisonSource control) => $"\nexpected: {NodeText(control)}\nactual: missing\n";

		string UnexpectedNodeText(ComparisonSource test) => $"\nexpected: missing\nactual: {NodeText(test)}\n";

		string AttrDiffText(AttributeComparisonSource control, AttributeComparisonSource test) =>
			$"\nexpected: {AttrText(control)}\nactual: {AttrText(test)}\n";

		string MissingAttrText(AttributeComparisonSource control) => $"\nexpected: {AttrText(control)}\nactual: missing\n";

		string UnexpectedAttrText(AttributeComparisonSource test) => $"\nexpected: missing\nactual: {AttrText(test)}\n";

		static string NodeName(ComparisonSource source) => source.Node.NodeType.ToString().ToLowerInvariant();

		return string.Join('\n', diffs.Select(d =>
		{
			string label = d switch
			{
				NodeDiff nd when nd.Target == DiffTarget.Text && nd.Control.Path.Equals(nd.Test.Path, StringComparison.Ordinal) =>
					$"The text in {nd.Control.Path} is different.",
				NodeDiff nd when nd.Target == DiffTarget.Text =>
					$"The expected {NodeName(nd.Control)} at {nd.Control.Path} and the actual {NodeName(nd.Test)} at {nd.Test.Path} is different.",
				NodeDiff nd when nd.Control.Path.Equals(nd.Test.Path, StringComparison.Ordinal) =>
					$"The {NodeName(nd.Control)}s at {nd.Control.Path} are different.",
				NodeDiff nd =>
					$"The expected {NodeName(nd.Control)} at {nd.Control.Path} and the actual {NodeName(nd.Test)} at {nd.Test.Path} are different.",
				AttrDiff ad when ad.Control.Path.Equals(ad.Test.Path, StringComparison.Ordinal) =>
					$"The values of the attributes at {ad.Control.Path} are different.",
				AttrDiff ad => $"The value of the attribute {ad.Control.Path} and actual attribute {ad.Test.Path} are different.",
				MissingNodeDiff md => $"The {NodeName(md.Control)} at {md.Control.Path} is missing.",
				MissingAttrDiff ma => $"The attribute at {ma.Control.Path} is missing.",
				UnexpectedNodeDiff ud => $"The {NodeName(ud.Test)} at {ud.Test.Path} was not expected.",
				UnexpectedAttrDiff ua => $"The attribute at {ua.Test.Path} was not expected.",
				_ => throw new InvalidOperationException($"Unknown diff type detected: {d.GetType()}")
			};

			string detail = d switch
			{
				NodeDiff nd => NodeDiffText(nd.Control, nd.Test),
				AttrDiff ad => AttrDiffText(ad.Control, ad.Test),
				MissingNodeDiff md => MissingNodeText(md.Control),
				MissingAttrDiff ma => MissingAttrText(ma.Control),
				UnexpectedNodeDiff ud => UnexpectedNodeText(ud.Test),
				UnexpectedAttrDiff ua => UnexpectedAttrText(ua.Test),
				_ => throw new InvalidOperationException($"Unknown diff type detected: {d.GetType()}")
			};

			return label + detail;
		}));
	}

	// ────────────────────────────────────────────────────────────────────────────────
	// Pretty printer — matches the F# prettyHtml behaviour exactly
	// ────────────────────────────────────────────────────────────────────────────────

	private static string PrettyHtml(string html, string? querySelector)
	{
		var parser = new HtmlParser();
		var document = parser.ParseDocument(html);
		var element = querySelector is null ? document.Body! : document.QuerySelector(querySelector)!;

		// Strip HTMX attributes from links — they are navigation artifacts, not content.
		foreach (var link in element.QuerySelectorAll("a"))
		{
			link.RemoveAttribute("hx-get");
			link.RemoveAttribute("hx-select-oob");
			link.RemoveAttribute("hx-swap");
			link.RemoveAttribute("hx-indicator");
			link.RemoveAttribute("hx-push-url");
			link.RemoveAttribute("preload");
		}

		var formatter = new PrettyMarkupFormatter();
		using var sw = new StringWriter();

		// Skip the first child if it is an H1 — the fixture wraps Setup.Markdown content in one.
		var children = element.Children.ToList();
		for (var i = 0; i < children.Count; i++)
		{
			if (i == 0 && children[i].TagName == "H1")
				continue;
			children[i].ToHtml(sw, formatter);
		}

		return sw.ToString().TrimStart('\n');
	}

	private static void CreateDiff(string expected, string actual)
	{
		var diffs = DiffBuilder.Compare(expected).WithTest(actual).Build();

		var deepComparison = HtmlDiffString(diffs);
		if (string.IsNullOrEmpty(deepComparison))
			return;

		var textDiff = Diff(expected, actual);
		throw new XunitException(
			$"""
Html was not equal
-- DIFF --
{textDiff}

-- Comparison --
{deepComparison}
"""
		);
	}

	// ────────────────────────────────────────────────────────────────────────────────
	// Per-file assertions (called from MarkdownResultTask / Scenario)
	// ────────────────────────────────────────────────────────────────────────────────

	[DebuggerStepThrough]
	internal static void ToHtml([LanguageInjection("html")] string expected, MarkdownResult actual)
	{
		var expectedHtml = PrettyHtml(expected, null);
		var actualHtml = PrettyHtml(actual.Html, "section#elastic-docs-v3");
		CreateDiff(expectedHtml, actualHtml);
	}

	[DebuggerStepThrough]
	internal static void ContainsHtml([LanguageInjection("html")] string expected, MarkdownResult actual)
	{
		var prettyExpected = PrettyHtml(expected, null);
		var prettyActual = PrettyHtml(actual.Html, "section#elastic-docs-v3");

		if (!prettyActual.Contains(prettyExpected, StringComparison.Ordinal))
			throw new XunitException(
				$"""
Expected html to contain:
{prettyExpected}

But was not found in:

{prettyActual}
"""
			);
	}

	[DebuggerStepThrough]
	internal static void ContainsRawHtml(string expected, MarkdownResult actual)
	{
		if (!actual.Html.Contains(expected, StringComparison.Ordinal))
			throw new XunitException(
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
			throw new XunitException(
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

	private static MarkdownResult FindIndexMd(GeneratorResults results) =>
		results.MarkdownResults.FirstOrDefault(r => r.File.RelativePath == "index.md")
			?? throw new XunitException("Could not find 'index.md' in generator results");

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
