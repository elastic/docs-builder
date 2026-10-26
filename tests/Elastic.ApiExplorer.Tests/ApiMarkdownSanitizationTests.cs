// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;

namespace Elastic.ApiExplorer.Tests;

public class ApiMarkdownSanitizationTests
{
	// ── ApiHtmlSanitizer.Sanitize ─────────────────────────────────────────────

	[Test]
	public void SanitizeHtml_ScriptTag_IsRemovedWithContent()
	{
		var result = ApiHtmlSanitizer.Sanitize("<p>Hello</p><script>alert(1)</script><p>World</p>");
		result.Should().NotContain("<script");
		result.Should().NotContain("alert(1)");
		result.Should().Contain("Hello");
		result.Should().Contain("World");
	}

	[Test]
	public void SanitizeHtml_OnErrorAttribute_IsStripped()
	{
		var result = ApiHtmlSanitizer.Sanitize("<img src=\"x\" onerror=\"alert(1)\">");
		result.Should().NotContain("onerror");
		result.Should().NotContain("alert(1)");
		result.Should().Contain("<img");
	}

	[Test]
	public void SanitizeHtml_JavascriptHref_IsRemoved()
	{
		var result = ApiHtmlSanitizer.Sanitize("<a href=\"javascript:alert(1)\">click</a>");
		result.Should().NotContain("javascript:");
		result.Should().Contain("click");
	}

	[Test]
	public void SanitizeHtml_DataUriInSrc_IsRemoved()
	{
		var result = ApiHtmlSanitizer.Sanitize("<img src=\"data:text/html,payload\">");
		result.Should().NotContain("data:");
	}

	[Test]
	public void SanitizeHtml_BumpShFormatting_IsKeptIntact()
	{
		const string input = "<span class=\"operation-verb get\">GET</span> <span class=\"operation-path\">/index</span>";
		var result = ApiHtmlSanitizer.Sanitize(input);
		result.Should().Be(input);
	}

	[Test]
	public void SanitizeHtml_ListedHeadingWrapperId_IsKept()
	{
		const string input = "<div class=\"heading-wrapper\" id=\"when-to-use-this-api\"><h2>When to use this API</h2></div>";
		var result = ApiHtmlSanitizer.Sanitize(input, Anchors("when-to-use-this-api"));
		result.Should().Contain("id=\"when-to-use-this-api\"");
	}

	[Test]
	[Arguments(true)]
	[Arguments(false)]
	public void SanitizeHtml_HeadingLink_KeepsHrefOnlyWhenTheHeadingKeepsItsId(bool listed)
	{
		const string input =
			"<div class=\"heading-wrapper\" id=\"responses\"><h2><a class=\"headerlink\" href=\"#responses\">Responses</a></h2></div>";
		var result = ApiHtmlSanitizer.Sanitize(input, listed ? Anchors("responses") : null);
		if (listed)
			result.Should().Contain("href=\"#responses\"");
		else
			result.Should().NotContain("href=").And.Contain("Responses");
	}

	[Test]
	public void SanitizeHtml_HeadingWrapperIdByDefault_IsStripped()
	{
		// Operation and schema pages own ids such as "responses" that a description heading could duplicate.
		const string input = "<div class=\"heading-wrapper\" id=\"responses\"><h2>Responses</h2></div>";
		var result = ApiHtmlSanitizer.Sanitize(input);
		result.Should().NotContain("id=");
	}

	[Test]
	[Arguments("<p id=\"markdown-content\">x</p>")]
	[Arguments("<span class=\"heading-wrapper\" id=\"pages-nav\">x</span>")]
	[Arguments("<div class=\"heading-wrapper\" id=\"some-section\">x</div>")]
	public void SanitizeHtml_IdNotListedOnAHeadingWrapper_IsStripped(string input)
	{
		var result = ApiHtmlSanitizer.Sanitize(input, Anchors("get-started"));
		result.Should().NotContain("id=");
	}

	[Test]
	public void SanitizeHtml_IframeTag_IsRemovedWithContent()
	{
		var result = ApiHtmlSanitizer.Sanitize("<p>Before</p><iframe src=\"evil.com\"></iframe><p>After</p>");
		result.Should().NotContain("<iframe");
		result.Should().Contain("Before");
		result.Should().Contain("After");
	}

	// ── StripHtml ─────────────────────────────────────────────────────────────

	[Test]
	public void StripHtml_BreakTag_InsertsSpaceSoWordsStaySeparated()
	{
		var result = ApiMarkdown.StripHtml("foo<br>bar");
		result.Should().Be("foo bar");
	}

	[Test]
	public void StripHtml_ParagraphTag_InsertsSpaceBetweenParagraphs()
	{
		var result = ApiMarkdown.StripHtml("<p>First</p><p>Second</p>");
		result.Should().Contain("First");
		result.Should().Contain("Second");
		result.Should().NotContain("<p>");
		result.Should().NotContain("FirstSecond", "words must not merge");
	}

	[Test]
	public void StripHtml_SpanAndDivTags_AreRemoved()
	{
		var result = ApiMarkdown.StripHtml("<div><span class=\"operation-verb get\">GET</span></div>");
		result.Should().NotContain("<div>");
		result.Should().NotContain("<span");
		result.Should().Contain("GET");
	}

	[Test]
	public void StripHtml_UnknownElement_SurroundingTextIsPreserved()
	{
		// AngleSharp parses <index> as an unknown HTML element. The tag name itself is not a
		// text node (same as any HTML parser), but the text nodes around it are preserved.
		var result = ApiMarkdown.StripHtml("Use <index> pattern to query.");
		result.Should().NotContain("<index>", "angle brackets should be removed");
		result.Should().Contain("pattern to query", "text after the unknown element is kept");
	}

	[Test]
	public void StripHtml_NullAndEmpty_ReturnEmpty()
	{
		ApiMarkdown.StripHtml(null).Should().BeEmpty();
		ApiMarkdown.StripHtml("").Should().BeEmpty();
	}

	[Test]
	public void StripHtml_PlainText_IsReturnedUnchanged()
	{
		var result = ApiMarkdown.StripHtml("No HTML here.");
		result.Should().Be("No HTML here.");
	}

	private static HashSet<string> Anchors(params string[] slugs) => [.. slugs];
}
