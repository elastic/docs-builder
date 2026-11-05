// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.LegacyDocs.Migration.Asciidoc;
using Nullean.Snapshots;

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

	[Test]
	public void UndefinedAttribute_EmittedAsSingleBraces_NotDouble()
	{
		var md = Emit("= T\n\n{undefined-attr} text\n");
		// Should be {undefined-attr}, NOT {{undefined-attr}}
		md.Should().MatchInlineSnapshot("""
			# T

			{undefined-attr} text
			""");
	}

	[Test]
	public void ProductNameAttribute_EmittedAsDoubleBraces()
	{
		// {es} is a ProductNames key — should remain {{es}} for docs-builder substitution
		var md = Emit("= T\n\n{es} cluster\n");
		md.Should().MatchInlineSnapshot("""
			# T

			{{es}} cluster
			""");
	}

	[Test]
	public void DefinedAttribute_IsSubstituted_NotEmittedAsRef()
	{
		var md = Emit("= T\n\n:myattr: hello world\n\n{myattr}\n");
		md.Should().MatchInlineSnapshot("""
			# T

			hello world
			""");
	}

	// ── Step 5: callout list in output ───────────────────────────────────────

	[Test]
	public void CodeBlock_WithCallouts_EmitsOrderedList()
	{
		var md = Emit("= T\n\n[source,python]\n----\nfoo() # <1>\nbar() # <2>\n----\n<1> Call foo\n<2> Call bar\n");
		md.Should().MatchInlineSnapshot(
			"""
			# T

			```python
			foo() # <1>
			bar() # <2>
			```

			1. Call foo
			2. Call bar
			"""
		);
	}

	// ── Step 7: xref with > in text ──────────────────────────────────────────

	[Test]
	public void CrossRef_Simple_IsEmittedCorrectly()
	{
		var md = Emit("= T\n\nSee <<my-anchor>>.\n");
		md.Should().MatchInlineSnapshot("""
			# T

			See [my-anchor](#my-anchor).
			""");
	}

	[Test]
	public void CrossRef_WithText_IsEmittedCorrectly()
	{
		var md = Emit("= T\n\nSee <<my-anchor,Click here>>.\n");
		md.Should().MatchInlineSnapshot("""
			# T

			See [Click here](#my-anchor).
			""");
	}

	[Test]
	public void CrossRef_WithBacktickInText_IsEmittedCorrectly()
	{
		var md = Emit("= T\n\nfilters like <<analysis-lowercase-tokenfilter,`lowercase`>> to normalise.\n");
		md.Should().MatchInlineSnapshot(
			"""
			# T

			filters like [`lowercase`](#analysis-lowercase-tokenfilter) to normalise.
			"""
		);
	}

	[Test]
	public void CrossRef_InMultiLineParagraph_WithBacktick_IsEmittedCorrectly()
	{
		// Multi-line paragraph where xref with backtick in text is on one line
		var asciidoc =
			"= T\n\nIt can be combined\nwith token filters like <<analysis-lowercase-tokenfilter,`lowercase`>> to\nnormalise the analysed terms.\n";
		var md = Emit(asciidoc);
		md.Should().MatchInlineSnapshot(
			"""
			# T

			It can be combined
			with token filters like [`lowercase`](#analysis-lowercase-tokenfilter) to
			normalise the analysed terms.
			"""
		);
	}

	[Test]
	public void CrossRef_AfterDLItemWithBlankLineSeparator_IsEmittedCorrectly()
	{
		// Description list where term is followed by blank line, then description paragraph containing xref
		var asciidoc =
			"= T\n\n<<term-anchor,Term>>::\n\nIt can be combined with token filters like <<analysis-lowercase-tokenfilter,`lowercase`>> to\nnormalise the analysed terms.\n";
		var md = Emit(asciidoc);
		md.Should().MatchInlineSnapshot(
			"""
			# T

			[Term](#term-anchor)


			It can be combined with token filters like [`lowercase`](#analysis-lowercase-tokenfilter) to
			normalise the analysed terms.
			"""
		);
	}

	[Test]
	public void CrossRef_InMultiLineParagraph_WithCurlyQuotes_IsEmittedCorrectly()
	{
		// Paragraph using AsciiDoc curly-quotes ``...'' before the xref line
		var asciidoc =
			"= T\n\nIn order to use scrolling, the initial search request should specify the\n`scroll` parameter in the query string, which tells Elasticsearch how long it\nshould keep the ``search context'' alive (see <<scroll-search-context>>), eg `?scroll=1m`.\n";
		var md = Emit(asciidoc);
		md.Should().MatchInlineSnapshot(
			"""
			# T

			In order to use scrolling, the initial search request should specify the
			`scroll` parameter in the query string, which tells Elasticsearch how long it
			should keep the "search context" alive (see [scroll-search-context](#scroll-search-context)), eg `?scroll=1m`.
			"""
		);
	}

	[Test]
	public void CrossRef_InOrderedListItem_WithBacktickText_IsEmittedCorrectly()
	{
		// Ordered list item with xref that has backtick-wrapped text (space after comma)
		var md = Emit("= T\n\n. Define a runtime field using the <<query-dsl-term-query, `term`>> queries.\n");
		md.Should().MatchInlineSnapshot(
			"""
			# T

			1. Define a runtime field using the [`term`](#query-dsl-term-query) queries.
			"""
		);
	}

	[Test]
	public void CrossRef_InOrderedListItem_WithPrecedingCode_IsEmittedCorrectly()
	{
		// Ordered list item where a `code` span precedes the xref
		var md = Emit("= T\n\n. A type of `lookup` that uses the <<query-dsl-term-query, `term`>> query.\n");
		md.Should().MatchInlineSnapshot(
			"""
			# T

			1. A type of `lookup` that uses the [`term`](#query-dsl-term-query) query.
			"""
		);
	}

	[Test]
	public void CrossRef_WithAsteriskInLinkText_IsEmittedCorrectly()
	{
		// Bold marker * between text and xref text must not consume the <<
		var md = Emit("= T\n\nSemantic*text field can be target of <<copy-to,copy*to fields>>.\n");
		md.Should().MatchInlineSnapshot(
			"""
			# T

			Semantic*text field can be target of [copy*to fields](#copy-to).
			"""
		);
	}

	[Test]
	public void CrossRef_WithAngleBracketInText_IsEmittedCorrectly()
	{
		// <<target,a > b>> — text contains a literal >
		var md = Emit("= T\n\nSee <<setup,version > 7.0>>\n");
		md.Should().MatchInlineSnapshot("""
			# T

			See [version > 7.0](#setup)
			""");
	}

	// ── Passthrough inline (constrained +..+) ─────────────────────────────────

	[Test]
	public void PassthroughInline_BasicCase_EmitsCodeSpan()
	{
		var md = Emit("= T\n\nDefaults to +max(1, 10)+.\n");
		md.Should().MatchInlineSnapshot("""
			# T

			Defaults to `max(1, 10)`.
			""");
	}

	[Test]
	public void PassthroughInline_VersionSuffix_NotTreatedAsPassthrough()
	{
		// "8.0+" is a version indicator, NOT a passthrough delimiter — must not swallow following text
		var md = Emit("= T\n\nES 8.0+ does not (see <<removal-of-types>>). ES 8.0+ returns error.\n");
		md.Should().MatchInlineSnapshot(
			"""
			# T

			ES 8.0+ does not (see [removal-of-types](#removal-of-types)). ES 8.0+ returns error.
			"""
		);
	}

	// ── Verbatim include resolution (Step 6) ──────────────────────────────────

	[Test]
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
		md.Should().MatchInlineSnapshot(
			"""
			# T

			```esql
			row1
			row2
			```
			"""
		);
	}

	[Test]
	public void VerbatimBlock_IfevalConditionMarkers_AreStripped()
	{
		// ifeval/endif markers inside verbatim blocks should be stripped; content is kept
		var adoc = "= T\n\n----\n{\nifeval::[\"{trust}\"==\"api-key\"]\n  \"credentials\": \"secret\",\nendif::[]\n}\n----\n";
		var md = Emit(adoc);
		md.Should().MatchInlineSnapshot(
			"""
			# T

			```
			{
			  "credentials": "secret",
			}
			```
			"""
		);
	}

	// ── Conditional blocks (ifeval) ────────────────────────────────────────────

	[Test]
	public void Ifeval_FalseCondition_ExcludesContent()
	{
		// ifeval::[expr] — condition false, block content must be excluded
		var adoc = "= T\n\nBefore.\n\nifeval::[\"{stack}\"==\"something-else\"]\nHidden text.\nendif::[]\n\nAfter.\n";
		var md = Emit(adoc, new Dictionary<string, string> { ["stack"] = "elastic" });
		md.Should().MatchInlineSnapshot("""
			# T

			Before.

			After.
			""");
	}

	[Test]
	public void Ifeval_TrueCondition_IncludesContent()
	{
		// ifeval::[expr] — condition true, block content must be included
		var adoc = "= T\n\nBefore.\n\nifeval::[\"{stack}\"==\"elastic\"]\nVisible text.\nendif::[]\n\nAfter.\n";
		var md = Emit(adoc, new Dictionary<string, string> { ["stack"] = "elastic" });
		md.Should().MatchInlineSnapshot("""
			# T

			Before.

			Visible text.

			After.
			""");
	}

	[Test]
	public void CodeBlock_TrailingSpaceOnClosingDelimiter_StillClosesBlock()
	{
		// Source files sometimes have `---- ` (with trailing space) as the closing delimiter.
		// The lexer must treat this as a valid closing delimiter.
		var adoc = "= T\n\n[source,json]\n----\n{\"hello\":\"world\"}\n---- \n\n==== Next section\n";
		var md = Emit(adoc);
		// If the block closes, Next section becomes a real heading; if not, it's inside code block
		md.Should().MatchInlineSnapshot(
			"""
			# T

			```json
			{"hello":"world"}
			```

			#### Next section
			"""
		);
	}

	[Test]
	public void Ifeval_ContentAfterBlock_NotLeaked_WhenConditionFalse()
	{
		// Regression: ConditionalProcessor was not pushing to the stack for ifeval,
		// causing all content after the ifeval block to leak through unconditionally.
		var adoc = "= T\n\nifeval::[\"{trust}\"==\"api-key\"]\nSecret\nendif::[]\n\nPublic paragraph.\n";
		var md = Emit(adoc, new Dictionary<string, string> { ["trust"] = "cert" });
		md.Should().MatchInlineSnapshot("""
			# T

			Public paragraph.
			""");
	}

	// ── Long dash delimiter (50 dashes) ──────────────────────────────────────

	[Test]
	public void CodeBlock_LongDashDelimiter_IsTreatedAsCodeFence()
	{
		// Watcher 2.4 docs use 50-dash lines as code block delimiters (valid AsciiDoc: 4+ dashes).
		var adoc =
			"= T\n\n[source,js]\n--------------------------------------------------\n\"input\": {}\n--------------------------------------------------\n\nNormal paragraph.\n";
		var md = Emit(adoc);
		md.Should().MatchInlineSnapshot(
			"""
			# T

			```js
			"input": {}
			```

			Normal paragraph.
			"""
		);
	}

	[Test]
	public void CodeBlock_LongDashDelimiter_InNestedSection_IsTreatedAsCodeFence()
	{
		// Watcher 2.4 docs: code blocks with 50-dash delimiters inside nested sections (== > === > ====).
		var adoc = string.Join(
			"\n",
			"= Doc",
			"",
			"[[top]]",
			"== Level1",
			"",
			"[[sec2]]",
			"=== Level2",
			"",
			"[[sec3]]",
			"==== Level3",
			"",
			"Some text:",
			"",
			"[source,js]",
			"--------------------------------------------------",
			"\"key\": \"value\"",
			"--------------------------------------------------",
			"",
			"Normal paragraph.",
			""
		);
		var md = Emit(adoc);
		md.Should().MatchInlineSnapshot(
			"""
			# Doc

			## Level1 [#top]

			### Level2 [#sec2]

			#### Level3 [#sec3]

			Some text:

			```js
			"key": "value"
			```

			Normal paragraph.
			"""
		);
	}

	// ── Open block delimiter trailing whitespace ───────────────────────────────

	[Test]
	public void OpenBlock_TrailingSpaceOnOpeningDelimiter_StillClosesBlock()
	{
		// Source files can have `--  ` (trailing spaces) as the opening `--` delimiter.
		// The parser must not leave the block unclosed because openingDelim = "--  " ≠ "--".
		var adoc = "= T\n\n.Requirements\n[sidebar]\n--  \n* req one\n--\n\nNormal paragraph.\n";
		var md = Emit(adoc);
		// The trailing `--` must NOT appear raw in the output
		md.Should().MatchInlineSnapshot(
			"""
			# T

			:::{admonition} Sidebar
			- req one

			:::

			Normal paragraph.
			"""
		);
	}
}
