// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Myst.InlineParsers;

namespace Elastic.Authoring.Tests.Inline.InlineAnchors;

public class InlineAnchorInTheMiddle : MarkdownTest
{
	protected override string Markdown =>
		"""
		this is *regular* text and this $$$is-an-inline-anchor$$$ and this continues to be regular text
		""";

	[Fact(DisplayName = "validate HTML")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<p>this is <em>regular</em> text and this
		    <a id="is-an-inline-anchor"></a> and this continues to be regular text
		</p>
		"""
		);

	[Fact(DisplayName = "has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class InlineAnchorsEmbeddedInDefinitionLists : GeneratorTest
{
	protected override IReadOnlyCollection<TestFile> Files =>
		[
			Index(
				"""
			# Testing nested inline anchors

			$$$search-type$$$

			`search_type`
			:   (Optional, string) How distributed term frequencies are calculated for relevance scoring.

			    ::::{dropdown} Valid values for `search_type`
			    `query_then_fetch`
			    :   (Default) Distributed term frequencies are calculated locally for each shard running the search. We recommend this option for faster searches with potentially less accurate scoring.

			    $$$dfs-query-then-fetch$$$

			    `dfs_query_then_fetch`
			    :   Distributed term frequencies are calculated globally, using information gathered from all shards running the search.

			    ::::
			"""
			),
			Page(
				"file.md",
				"""
			 [Link to first](index.md#search-type)
			 [Link to second](index.md#dfs-query-then-fetch)
			"""
			)
		];

	[Fact(DisplayName = "emits nested inline anchor")]
	public async Task EmitsNestedInlineAnchor() => await Docs.ConvertsToContainingHtml("""<a id="dfs-query-then-fetch"></a>""");

	[Fact(DisplayName = "emits definition list block anchor")]
	public async Task EmitsDefinitionListBlockAnchor() => await Docs.ConvertsToContainingHtml("""<a id="search-type"></a>""");

	[Fact(DisplayName = "has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();

	[Fact(DisplayName = "minimal parse sees two inline anchors")]
	public async Task MinimalParseSeesInlineAnchors()
	{
		var anchors = await Docs.Converts("index.md").ParsesMinimal<InlineAnchor>();
		anchors.Should().HaveCount(2);
	}
}

public class InlineAnchorsEmbeddedInIndentedCode : GeneratorTest
{
	protected override IReadOnlyCollection<TestFile> Files =>
		[
			Index(
				"""
			# Testing nested inline anchors

			$$$search-type$$$

			    indented codeblock

			    $$$dfs-query-then-fetch$$$

			    block
			"""
			),
			Page(
				"file.md",
				"""
			 [Link to first](index.md#search-type)
			 [Link to second](index.md#dfs-query-then-fetch)
			"""
			)
		];

	[Fact(DisplayName = "emits nested inline anchor")]
	public async Task EmitsNestedInlineAnchor() => await Docs.ConvertsToContainingHtml("""<a id="dfs-query-then-fetch"></a>""");

	[Fact(DisplayName = "emits definition list block anchor")]
	public async Task EmitsDefinitionListBlockAnchor() => await Docs.ConvertsToContainingHtml("""<a id="search-type"></a>""");

	[Fact(DisplayName = "has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();

	[Fact(DisplayName = "minimal parse sees two inline anchors")]
	public async Task MinimalParseSeesInlineAnchors()
	{
		var anchors = await Docs.Converts("index.md").ParsesMinimal<InlineAnchor>();
		anchors.Should().HaveCount(2);
	}
}
