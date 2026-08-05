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
}
