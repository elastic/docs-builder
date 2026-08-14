// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.LegacyDocs.Migration.Asciidoc;
using Elastic.LegacyDocs.Migration.Asciidoc.Ast;

namespace Elastic.LegacyDocs.Migration.Tests;

public class ParserTests
{
	/// <summary>
	/// Parse without a section title — children go directly into doc.Children.
	/// Note: in this codebase, `== Title` (Level 1) sets doc.Title and makes subsequent
	/// content direct doc.Children; `= Title` (Level 0) creates a Level-0 SectionNode wrapper.
	/// </summary>
	private static AsciidocDocument Parse(string content, Dictionary<string, string>? attrs = null) =>
		new AsciidocParser(new AsciidocParserOptions { Attributes = attrs ?? [] })
			.Parse(content, "");

	/// <summary>Recursively finds the first node of type T in the document tree.</summary>
	private static T? FindFirst<T>(IEnumerable<IAsciidocNode> nodes) where T : class
	{
		foreach (var node in nodes)
		{
			if (node is T found)
				return found;
			if (node is SectionNode s)
			{ var r = FindFirst<T>(s.Children); if (r is not null) return r; }
			if (node is OpenBlockNode o)
			{ var r = FindFirst<T>(o.Children); if (r is not null) return r; }
			if (node is AdmonitionNode a)
			{ var r = FindFirst<T>(a.Children); if (r is not null) return r; }
		}
		return null;
	}

	private static T? FindFirst<T>(AsciidocDocument doc) where T : class =>
		FindFirst<T>(doc.Children);

	// ── Step 1: -- open blocks ────────────────────────────────────────────────

	[Fact]
	public void OpenBlock_DashDash_ProducesOpenBlockNode()
	{
		// No section title — direct doc.Children
		var doc = Parse("--\nSome content\n--\n");
		var block = FindFirst<OpenBlockNode>(doc);
		block.Should().NotBeNull();
		block.Children.Should().HaveCountGreaterThan(0);
	}

	[Fact]
	public void OpenBlock_NoteStyle_ProducesAdmonitionNode()
	{
		var doc = Parse("[NOTE]\n--\nNote content here.\n--\n");
		var admonition = FindFirst<AdmonitionNode>(doc);
		admonition.Should().NotBeNull();
		admonition.Type.Should().Be(AdmonitionType.Note);
	}

	[Fact]
	public void VerbatimBlock_FourDashes_ProducesCodeBlockNode()
	{
		var doc = Parse("[source,yaml]\n----\nfoo: bar\n----\n");
		var code = FindFirst<CodeBlockNode>(doc);
		code.Should().NotBeNull();
		code.Source.Should().Be("foo: bar");
		code.Language.Should().Be("yaml");
	}

	[Fact]
	public void OpenBlock_And_VerbatimBlock_AreDifferentNodeTypes()
	{
		// -- open block should NOT produce a CodeBlockNode
		var openDoc = Parse("--\ncontent\n--\n");
		FindFirst<CodeBlockNode>(openDoc).Should().BeNull();
		FindFirst<OpenBlockNode>(openDoc).Should().NotBeNull();

		// ---- verbatim block should NOT produce an OpenBlockNode
		var codeDoc = Parse("----\ncontent\n----\n");
		FindFirst<OpenBlockNode>(codeDoc).Should().BeNull();
		FindFirst<CodeBlockNode>(codeDoc).Should().NotBeNull();
	}

	// ── Step 3: Attribute resolution ─────────────────────────────────────────

	[Fact]
	public void SetAttribute_EagerlyExpandsValues()
	{
		// :branch: 8.19
		// :ref: https://example.com/{branch}
		// After eager expansion, {ref} should be the fully resolved URL
		var content = ":branch: 8.19\n:ref: https://example.com/{branch}\n\nSee {ref}/setup.html\n";
		var doc = Parse(content);
		doc.Attributes.TryGetValue("ref", out var refValue).Should().BeTrue();
		refValue.Should().Be("https://example.com/8.19");
	}

	[Fact]
	public void SetAttribute_ProductNameKeys_AreNotStored()
	{
		// ProductNames keys should stay unresolved so the emitter emits {{es}} not "Elasticsearch"
		var content = ":es: Elasticsearch Override\n\n{es}\n";
		var doc = Parse(content);
		// The parser skips storing ProductNames keys, so {es} remains unresolved → AttributeRefInline
		var para = doc.Children.OfType<ParagraphNode>().FirstOrDefault();
		para.Should().NotBeNull();
		var attrRef = para.Inlines.OfType<AttributeRefInline>().FirstOrDefault();
		attrRef.Should().NotBeNull();
		attrRef.Name.Should().Be("es");
	}

	[Fact]
	public void AttributeResolution_SeedAttributes_AreAvailable()
	{
		var attrs = new Dictionary<string, string>
		{
			["branch"] = "8.19",
			["docs-root"] = "/work/docs-repo"
		};
		var content = "Branch is {branch}\n";
		var parser = new AsciidocParser(new AsciidocParserOptions { Attributes = attrs });
		var doc = parser.Parse(content, "");
		var para = doc.Children.OfType<ParagraphNode>().FirstOrDefault();
		var text = para?.Inlines.OfType<TextInline>().FirstOrDefault();
		text?.Text.Should().Contain("8.19");
	}

	// ── Step 5: Callouts ─────────────────────────────────────────────────────

	[Fact]
	public void CodeBlock_Callouts_AreCollected()
	{
		var content = "[source,java]\n----\nfoo(); // <1>\nbar(); // <2>\n----\n<1> First callout\n<2> Second callout\n";
		var doc = Parse(content);
		var code = FindFirst<CodeBlockNode>(doc);
		code.Should().NotBeNull();
		code.Callouts.Should().HaveCount(2);
		code.Callouts[0].Should().Be("First callout");
		code.Callouts[1].Should().Be("Second callout");
	}

	[Fact]
	public void CodeBlock_CalloutsOutOfOrder_AreNormalized()
	{
		// Callout markers appear in document order by number
		var content = "----\nfoo <2>\nbar <1>\n----\n<1> Bar annotation\n<2> Foo annotation\n";
		var doc = Parse(content);
		var code = FindFirst<CodeBlockNode>(doc);
		code.Should().NotBeNull();
		code.Callouts.Should().HaveCount(2);
		code.Callouts[0].Should().Be("Bar annotation");
		code.Callouts[1].Should().Be("Foo annotation");
	}

	// ── Step 7: Multi-line admonitions ───────────────────────────────────────

	[Fact]
	public void AdmonitionParagraph_MultiLine_CollectsAllContent()
	{
		var content = "NOTE: First line\nSecond line\nThird line\n\nNext paragraph\n";
		var doc = Parse(content);
		var admonition = doc.Children.OfType<AdmonitionNode>().FirstOrDefault();
		admonition.Should().NotBeNull();
		// All three lines should be inside the admonition's paragraph
		var para = admonition.Children.OfType<ParagraphNode>().FirstOrDefault();
		para.Should().NotBeNull();
		var allText = string.Join("", para.Inlines.Select(i => i switch
		{
			TextInline t => t.Text,
			_ => ""
		}));
		allText.Should().Contain("First line");
		allText.Should().Contain("Second line");
		allText.Should().Contain("Third line");
	}

	// ── Lexer: trailing whitespace on delimiter ───────────────────────────────

	[Fact]
	public void Lexer_VerbatimBlock_TrailingSpaceOnClosingDelimiter_ClosesBlock()
	{
		var input = "[source,json]\n----\n{\"k\":\"v\"}\n---- \n\nsome text\n";
		var tokens = AsciidocLexer.Tokenize(input);
		// The `---- ` line must be a closing BlockDelimiter, not Text
		var textTokens = tokens.Where(t => t.Type == TokenType.Text).Select(t => t.Raw).ToList();
		textTokens.Should().NotContain("---- "); // closing delim must NOT appear as Text
												 // The JSON content should appear as Text
		textTokens.Should().Contain(/*lang=json,strict*/ "{\"k\":\"v\"}");
	}

	// ── Step 2: include:: dispatch in ParseBlock ──────────────────────────────

	[Fact]
	public void IncludeDirective_InsideDelimitedBlock_IsResolvedWhenFileExists()
	{
		var files = new Dictionary<string, string>
		{
			["/base/inner.adoc"] = "included content"
		};
		var content = "[NOTE]\n====\ninclude::inner.adoc[]\n====\n";
		var parser = new AsciidocParser(new AsciidocParserOptions
		{
			FileReader = path => files.TryGetValue(path, out var c) ? c : null
		});
		var doc = parser.Parse(content, "/base");
		var admonition = doc.Children.OfType<AdmonitionNode>().FirstOrDefault();
		admonition.Should().NotBeNull();
		admonition.Children.Should().HaveCountGreaterThan(0);
	}

	[Fact]
	public void Parse_file_starting_with_level1_section_promotes_it_to_doc_title()
	{
		// When a file starts with a == section (Level 1 in AST), Parse() treats it as doc.Title.
		// The === subsections become top-level doc.Children (not nested under a SectionNode).
		// ProcessInclude uses a different loop that does NOT promote == to doc.Title — it creates
		// a SectionNode — so included files behave correctly during chunking.
		const string source = """
			== The search API

			Some intro text.

			[discrete]
			[[run-an-es-search]]
			=== Run a search

			Run search content.

			[discrete]
			[[common-search-options]]
			=== Common search options

			Common options content.
			""";

		var parser = new AsciidocParser(new AsciidocParserOptions());
		var doc = parser.Parse(source, ".");

		// The == section becomes the document title, not a SectionNode child
		doc.Title.Should().Be("The search API");

		// The === subsections appear at the top level (they're children of the document, not the == section)
		var sectionChildren = doc.Children.OfType<SectionNode>().ToList();
		sectionChildren.Should().HaveCount(2);
		sectionChildren[0].Level.Should().Be(2);
		sectionChildren[0].Title.Should().Be("Run a search");
		sectionChildren[1].Level.Should().Be(2);
		sectionChildren[1].Title.Should().Be("Common search options");
	}

	[Fact]
	public void ChunkLevel2_keeps_level3_within_level2_page()
	{
		const string source = """
            = Book Title

            [[search-your-data]]
            == The search API

            Some intro text.

            [discrete]
            [[run-an-es-search]]
            === Run a search

            Run search content.

            [discrete]
            [[common-search-options]]
            === Common search options

            Common options content.
            """;

		var parser = new AsciidocParser(new AsciidocParserOptions());
		var doc = parser.Parse(source, ".");

		var emitter = new MarkdownEmitter(new MarkdownEmitterOptions { BookPrefix = "test", Version = "1.0" });
		// chunkLevel=1 matches conf.yaml chunk:1 — extracts == (Level 1) sections, keeps === within them
		var pages = PageChunker.Chunk(doc, chunkLevel: 1, emitter);

		// Should produce: index + 1 page for "The search API"
		pages.Should().HaveCount(2);
		var searchApiPage = pages.First(p => p.Slug == "search-your-data");
		searchApiPage.MarkdownContent.Should().Contain("Run a search");
		searchApiPage.MarkdownContent.Should().Contain("Common search options");
	}

	[Fact]
	public void IncludeChain_EachIncludedFile_BecomesASeparatePage()
	{
		// Mirrors the elastic.co search-your-data structure:
		// - index.adoc includes search-your-data.adoc (= level, chunk boundary)
		// - search-your-data.adoc has inline discrete === section (stays in its page)
		// - search-your-data.adoc includes search-api.adoc (== level, chunk boundary)
		// - search-api.adoc includes sort-results.adoc (=== level, still chunk boundary)
		var files = new Dictionary<string, string>
		{
			["/base/index.adoc"] = """
				= Elasticsearch Guide

				include::search-your-data.adoc[]
				""",
			["/base/search-your-data.adoc"] = """
				[[search-with-elasticsearch]]
				= Search your data

				Intro paragraph.

				[discrete]
				=== Run a search

				Inline section content.

				include::search-api.adoc[]
				""",
			["/base/search-api.adoc"] = """
				[[search-your-data-api]]
				== The search API

				API intro.

				[discrete]
				=== API Run a search

				Inline API section.

				include::sort-results.adoc[]
				""",
			["/base/sort-results.adoc"] = """
				[[sort-results]]
				=== Sort search results

				Sort content.
				""",
		};

		var parser = new AsciidocParser(new AsciidocParserOptions
		{
			FileReader = path => files.TryGetValue(path, out var c) ? c : null
		});
		var doc = parser.Parse(files["/base/index.adoc"], "/base");
		var emitter = new MarkdownEmitter(new MarkdownEmitterOptions { BookPrefix = "test", Version = "1.0" });
		var pages = PageChunker.Chunk(doc, chunkLevel: 1, emitter);

		// Each included file becomes its own page; included-inside-body files are child pages.
		// Flatten the tree to verify all pages exist regardless of depth.
		var allPages = Flatten(pages);
		var slugs = allPages.Select(p => p.Slug).ToList();

		// index (from = Elasticsearch Guide root)
		slugs.Should().Contain("index");

		// = Search your data → its own top-level page
		slugs.Should().Contain("search-with-elasticsearch");
		var searchYourData = allPages.First(p => p.Slug == "search-with-elasticsearch");
		searchYourData.MarkdownContent.Should().Contain("Intro paragraph");
		searchYourData.MarkdownContent.Should().Contain("Run a search");        // inline section stays
		searchYourData.MarkdownContent.Should().NotContain("The search API");   // NOT merged into this page

		// == The search API → child page of search-with-elasticsearch (nested include)
		slugs.Should().Contain("search-your-data-api");
		var theSearchApi = allPages.First(p => p.Slug == "search-your-data-api");
		theSearchApi.MarkdownContent.Should().Contain("API intro");
		theSearchApi.MarkdownContent.Should().Contain("API Run a search");      // inline stays
		theSearchApi.MarkdownContent.Should().NotContain("Sort search results"); // NOT merged

		// === Sort search results → child page of search-your-data-api (nested include)
		slugs.Should().Contain("sort-results");
		var sortResults = allPages.First(p => p.Slug == "sort-results");
		sortResults.MarkdownContent.Should().Contain("Sort content");
	}

	private static List<PageOutput> Flatten(IReadOnlyList<PageOutput> pages)
	{
		var result = new List<PageOutput>();
		foreach (var p in pages)
		{
			result.Add(p);
			result.AddRange(Flatten(p.Children));
		}
		return result;
	}
}
