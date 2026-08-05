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
	public void CrossRef_WithAngleBracketInText_IsEmittedCorrectly()
	{
		// <<target,a > b>> — text contains a literal >
		var md = Emit("= T\n\nSee <<setup,version > 7.0>>\n");
		md.Should().Contain("[version > 7.0]");
	}
}
