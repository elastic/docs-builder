// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.LegacyDocs.Migration.Asciidoc;

namespace Elastic.LegacyDocs.Migration.Tests;

public class EmitterTests
{
	private static string Emit(string asciidoc, Dictionary<string, string>? attrs = null)
	{
		var parser = new AsciidocParser(new AsciidocParserOptions { Attributes = attrs ?? [] });
		var doc = parser.Parse(asciidoc, "");
		return new MarkdownEmitter(new MarkdownEmitterOptions()).Emit(doc);
	}

	// ── Step 4: attribute emission ────────────────────────────────────────────

	[Fact]
	public void UndefinedAttribute_EmittedAsSingleBraces_NotDouble()
	{
		var md = Emit("= T\n\n{undefined-attr} text\n");
		// Should be {undefined-attr}, NOT {{undefined-attr}}
		md.Should().Contain("{undefined-attr}");
		md.Should().NotContain("{{undefined-attr}}");
	}

	[Fact]
	public void ProductNameAttribute_EmittedAsDoubleBraces()
	{
		// {es} is a ProductNames key — should remain {{es}} for docs-builder substitution
		var md = Emit("= T\n\n{es} cluster\n");
		md.Should().Contain("{{es}}");
	}

	[Fact]
	public void DefinedAttribute_IsSubstituted_NotEmittedAsRef()
	{
		var md = Emit("= T\n\n:myattr: hello world\n\n{myattr}\n");
		md.Should().Contain("hello world");
		md.Should().NotContain("{myattr}");
	}

	// ── Step 5: callout list in output ───────────────────────────────────────

	[Fact]
	public void CodeBlock_WithCallouts_EmitsOrderedList()
	{
		var md = Emit("= T\n\n[source,python]\n----\nfoo() # <1>\nbar() # <2>\n----\n<1> Call foo\n<2> Call bar\n");
		md.Should().Contain("1. Call foo");
		md.Should().Contain("2. Call bar");
	}

	// ── Step 7: xref with > in text ──────────────────────────────────────────

	[Fact]
	public void CrossRef_Simple_IsEmittedCorrectly()
	{
		var md = Emit("= T\n\nSee <<my-anchor>>.\n");
		md.Should().NotContain("<<");
		md.Should().Contain("[my-anchor](#my-anchor)");
	}

	[Fact]
	public void CrossRef_WithText_IsEmittedCorrectly()
	{
		var md = Emit("= T\n\nSee <<my-anchor,Click here>>.\n");
		md.Should().NotContain("<<");
		md.Should().Contain("[Click here](#my-anchor)");
	}

	[Fact]
	public void CrossRef_WithBacktickInText_IsEmittedCorrectly()
	{
		var md = Emit("= T\n\nfilters like <<analysis-lowercase-tokenfilter,`lowercase`>> to normalise.\n");
		md.Should().NotContain("<<");
		md.Should().Contain("[`lowercase`]");
	}

	[Fact]
	public void CrossRef_InMultiLineParagraph_WithBacktick_IsEmittedCorrectly()
	{
		// Multi-line paragraph where xref with backtick in text is on one line
		var asciidoc = "= T\n\nIt can be combined\nwith token filters like <<analysis-lowercase-tokenfilter,`lowercase`>> to\nnormalise the analysed terms.\n";
		var md = Emit(asciidoc);
		md.Should().NotContain("<<");
		md.Should().Contain("[`lowercase`]");
	}

	[Fact]
	public void CrossRef_AfterDLItemWithBlankLineSeparator_IsEmittedCorrectly()
	{
		// Description list where term is followed by blank line, then description paragraph containing xref
		var asciidoc = "= T\n\n<<term-anchor,Term>>::\n\nIt can be combined with token filters like <<analysis-lowercase-tokenfilter,`lowercase`>> to\nnormalise the analysed terms.\n";
		var md = Emit(asciidoc);
		md.Should().NotContain("<<");
		md.Should().Contain("[`lowercase`]");
	}

	[Fact]
	public void CrossRef_InMultiLineParagraph_WithCurlyQuotes_IsEmittedCorrectly()
	{
		// Paragraph using AsciiDoc curly-quotes ``...'' before the xref line
		var asciidoc = "= T\n\nIn order to use scrolling, the initial search request should specify the\n`scroll` parameter in the query string, which tells Elasticsearch how long it\nshould keep the ``search context'' alive (see <<scroll-search-context>>), eg `?scroll=1m`.\n";
		var md = Emit(asciidoc);
		md.Should().NotContain("<<");
		md.Should().Contain("[scroll-search-context]");
	}

	[Fact]
	public void CrossRef_InOrderedListItem_WithBacktickText_IsEmittedCorrectly()
	{
		// Ordered list item with xref that has backtick-wrapped text (space after comma)
		var md = Emit("= T\n\n. Define a runtime field using the <<query-dsl-term-query, `term`>> queries.\n");
		md.Should().NotContain("<<");
		md.Should().Contain("[`term`]");
	}

	[Fact]
	public void CrossRef_InOrderedListItem_WithPrecedingCode_IsEmittedCorrectly()
	{
		// Ordered list item where a `code` span precedes the xref
		var md = Emit("= T\n\n. A type of `lookup` that uses the <<query-dsl-term-query, `term`>> query.\n");
		md.Should().NotContain("<<");
		md.Should().Contain("[`term`]");
	}

	[Fact]
	public void CrossRef_WithAsteriskInLinkText_IsEmittedCorrectly()
	{
		// Bold marker * between text and xref text must not consume the <<
		var md = Emit("= T\n\nSemantic*text field can be target of <<copy-to,copy*to fields>>.\n");
		md.Should().NotContain("<<");
		md.Should().Contain("[copy*to fields]");
	}

	[Fact]
	public void CrossRef_WithAngleBracketInText_IsEmittedCorrectly()
	{
		// <<target,a > b>> — text contains a literal >
		var md = Emit("= T\n\nSee <<setup,version > 7.0>>\n");
		md.Should().Contain("[version > 7.0]");
	}

	// ── Passthrough inline (constrained +..+) ─────────────────────────────────

	[Fact]
	public void PassthroughInline_BasicCase_EmitsCodeSpan()
	{
		var md = Emit("= T\n\nDefaults to +max(1, 10)+.\n");
		md.Should().Contain("`max(1, 10)`");
	}

	[Fact]
	public void PassthroughInline_VersionSuffix_NotTreatedAsPassthrough()
	{
		// "8.0+" is a version indicator, NOT a passthrough delimiter — must not swallow following text
		var md = Emit("= T\n\nES 8.0+ does not (see <<removal-of-types>>). ES 8.0+ returns error.\n");
		md.Should().NotContain("<<removal-of-types>>");
		md.Should().Contain("[removal-of-types](#removal-of-types)");
		md.Should().Contain("8.0+");
	}

	// ── Verbatim include resolution (Step 6) ──────────────────────────────────

	[Fact]
	public void VerbatimBlock_StandardTaggedInclude_IsResolved()
	{
		// include::path[tag=name] inside a code block should be expanded
		var fileSystem = new Dictionary<string, string>
		{
			["/specs/string.csv-spec"] = "# tag::my-example[]\nrow1\nrow2\n# end::my-example[]\n"
		};
		var parser = new AsciidocParser(new AsciidocParserOptions
		{
			Attributes = new Dictionary<string, string> { ["specs"] = "/specs" },
			FileReader = p => fileSystem.TryGetValue(p, out var c) ? c : null
		});
		var doc = parser.Parse("= T\n\n[source,esql]\n----\ninclude::{specs}/string.csv-spec[tag=my-example]\n----\n", "");
		var md = new MarkdownEmitter(new MarkdownEmitterOptions()).Emit(doc);
		md.Should().Contain("row1");
		md.Should().Contain("row2");
		md.Should().NotContain("include::");
	}

	[Fact]
	public void VerbatimBlock_IfevalConditionMarkers_AreStripped()
	{
		// ifeval/endif markers inside verbatim blocks should be stripped; content is kept
		var adoc = "= T\n\n----\n{\nifeval::[\"{trust}\"==\"api-key\"]\n  \"credentials\": \"secret\",\nendif::[]\n}\n----\n";
		var md = Emit(adoc);
		md.Should().NotContain("ifeval::");
		md.Should().NotContain("endif::");
		md.Should().Contain("\"credentials\": \"secret\",");
	}

	// ── Conditional blocks (ifeval) ────────────────────────────────────────────

	[Fact]
	public void Ifeval_FalseCondition_ExcludesContent()
	{
		// ifeval::[expr] — condition false, block content must be excluded
		var adoc = "= T\n\nBefore.\n\nifeval::[\"{stack}\"==\"something-else\"]\nHidden text.\nendif::[]\n\nAfter.\n";
		var md = Emit(adoc, new Dictionary<string, string> { ["stack"] = "elastic" });
		md.Should().Contain("Before.");
		md.Should().Contain("After.");
		md.Should().NotContain("Hidden text.");
	}

	[Fact]
	public void Ifeval_TrueCondition_IncludesContent()
	{
		// ifeval::[expr] — condition true, block content must be included
		var adoc = "= T\n\nBefore.\n\nifeval::[\"{stack}\"==\"elastic\"]\nVisible text.\nendif::[]\n\nAfter.\n";
		var md = Emit(adoc, new Dictionary<string, string> { ["stack"] = "elastic" });
		md.Should().Contain("Before.");
		md.Should().Contain("After.");
		md.Should().Contain("Visible text.");
	}

	[Fact]
	public void CodeBlock_TrailingSpaceOnClosingDelimiter_StillClosesBlock()
	{
		// Source files sometimes have `---- ` (with trailing space) as the closing delimiter.
		// The lexer must treat this as a valid closing delimiter.
		var adoc = "= T\n\n[source,json]\n----\n{\"hello\":\"world\"}\n---- \n\n==== Next section\n";
		var md = Emit(adoc);
		md.Should().Contain("```json");
		md.Should().Contain(/*lang=json,strict*/ "{\"hello\":\"world\"}");
		// If the block closes, Next section becomes a real heading; if not, it's inside code block
		md.Should().NotContain("==== Next section");  // raw AsciiDoc must not appear
	}

	[Fact]
	public void Ifeval_ContentAfterBlock_NotLeaked_WhenConditionFalse()
	{
		// Regression: ConditionalProcessor was not pushing to the stack for ifeval,
		// causing all content after the ifeval block to leak through unconditionally.
		var adoc = "= T\n\nifeval::[\"{trust}\"==\"api-key\"]\nSecret\nendif::[]\n\nPublic paragraph.\n";
		var md = Emit(adoc, new Dictionary<string, string> { ["trust"] = "cert" });
		md.Should().NotContain("Secret");
		md.Should().Contain("Public paragraph.");
	}

	// ── Open block delimiter trailing whitespace ───────────────────────────────

	[Fact]
	public void OpenBlock_TrailingSpaceOnOpeningDelimiter_StillClosesBlock()
	{
		// Source files can have `--  ` (trailing spaces) as the opening `--` delimiter.
		// The parser must not leave the block unclosed because openingDelim = "--  " ≠ "--".
		var adoc = "= T\n\n.Requirements\n[sidebar]\n--  \n* req one\n--\n\nNormal paragraph.\n";
		var md = Emit(adoc);
		md.Should().Contain("req one");
		md.Should().Contain("Normal paragraph.");
		// The trailing `--` must NOT appear raw in the output
		md.Should().NotContain("\n--\n");
	}
}
