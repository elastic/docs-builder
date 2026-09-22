// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;

namespace Elastic.ApiExplorer.Tests;

public class ApiMarkdownSanitizationTests
{
	// ── SanitizeHtml ──────────────────────────────────────────────────────────

	[Fact]
	public void SanitizeHtml_ScriptTag_IsRemovedWithContent()
	{
		var result = ApiMarkdown.SanitizeHtml("<p>Hello</p><script>alert(1)</script><p>World</p>");
		result.Should().NotContain("<script");
		result.Should().NotContain("alert(1)");
		result.Should().Contain("Hello");
		result.Should().Contain("World");
	}

	[Fact]
	public void SanitizeHtml_OnErrorAttribute_IsStripped()
	{
		var result = ApiMarkdown.SanitizeHtml("<img src=\"x\" onerror=\"alert(1)\">");
		result.Should().NotContain("onerror");
		result.Should().NotContain("alert(1)");
		result.Should().Contain("<img");
	}

	[Fact]
	public void SanitizeHtml_JavascriptHref_IsRemoved()
	{
		var result = ApiMarkdown.SanitizeHtml("<a href=\"javascript:alert(1)\">click</a>");
		result.Should().NotContain("javascript:");
		result.Should().Contain("click");
	}

	[Fact]
	public void SanitizeHtml_DataUriInSrc_IsRemoved()
	{
		var result = ApiMarkdown.SanitizeHtml("<img src=\"data:text/html,payload\">");
		result.Should().NotContain("data:");
	}

	[Fact]
	public void SanitizeHtml_BumpShFormatting_IsKeptIntact()
	{
		const string input = "<span class=\"operation-verb get\">GET</span> <span class=\"operation-path\">/index</span>";
		var result = ApiMarkdown.SanitizeHtml(input);
		result.Should().Be(input);
	}

	[Fact]
	public void SanitizeHtml_IframeTag_IsRemovedWithContent()
	{
		var result = ApiMarkdown.SanitizeHtml("<p>Before</p><iframe src=\"evil.com\"></iframe><p>After</p>");
		result.Should().NotContain("<iframe");
		result.Should().Contain("Before");
		result.Should().Contain("After");
	}

	// ── StripHtml ─────────────────────────────────────────────────────────────

	[Fact]
	public void StripHtml_BreakTag_InsertsSpaceSoWordsStaySeparated()
	{
		var result = ApiMarkdown.StripHtml("foo<br>bar");
		result.Should().Be("foo bar");
	}

	[Fact]
	public void StripHtml_ParagraphTag_InsertsSpaceBetweenParagraphs()
	{
		var result = ApiMarkdown.StripHtml("<p>First</p><p>Second</p>");
		result.Should().Contain("First");
		result.Should().Contain("Second");
		result.Should().NotContain("<p>");
		result.Should().NotContain("FirstSecond", "words must not merge");
	}

	[Fact]
	public void StripHtml_SpanAndDivTags_AreRemoved()
	{
		var result = ApiMarkdown.StripHtml("<div><span class=\"operation-verb get\">GET</span></div>");
		result.Should().NotContain("<div>");
		result.Should().NotContain("<span");
		result.Should().Contain("GET");
	}

	[Fact]
	public void StripHtml_UnknownElement_SurroundingTextIsPreserved()
	{
		// AngleSharp parses <index> as an unknown HTML element. The tag name itself is not a
		// text node (same as any HTML parser), but the text nodes around it are preserved.
		var result = ApiMarkdown.StripHtml("Use <index> pattern to query.");
		result.Should().NotContain("<index>", "angle brackets should be removed");
		result.Should().Contain("pattern to query", "text after the unknown element is kept");
	}

	[Fact]
	public void StripHtml_NullAndEmpty_ReturnEmpty()
	{
		ApiMarkdown.StripHtml(null).Should().BeEmpty();
		ApiMarkdown.StripHtml("").Should().BeEmpty();
	}

	[Fact]
	public void StripHtml_PlainText_IsReturnedUnchanged()
	{
		var result = ApiMarkdown.StripHtml("No HTML here.");
		result.Should().Be("No HTML here.");
	}
}
