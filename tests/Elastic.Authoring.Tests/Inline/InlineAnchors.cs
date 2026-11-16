// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Myst.InlineParsers;

namespace Elastic.Authoring.Tests.Inline;

public class InlineAnchorInTheMiddle : MarkdownTest
{
	protected override string Markdown =>
		"""
		this is *regular* text and this $$$is-an-inline-anchor$$$ and this continues to be regular text
		""";

	[Test, DisplayName("validate HTML")]
	public async Task ValidateHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<p>this is <em>regular</em> text and this
		    <a id="is-an-inline-anchor"></a> and this continues to be regular text
		</p>
		"""
		);

	[Test, DisplayName("has no errors")]
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

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<p>
			<a id="search-type"></a>
		</p>
		<dl>
			<dt>
				<code>search_type</code>
			</dt>
			<dd>
				<p>(Optional, string) How distributed term frequencies are calculated for relevance scoring.</p>
				<details class="dropdown">
					<summary class="dropdown-title">
						<div class="dropdown-title__container">
							<span class="dropdown-title__summary-text">Valid values for search_type</span>
						</div>
						<div class="dropdown-title__icon">
							<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-4 mr-1 shrink -rotate-90 group-has-checked/label:rotate-0 cursor-pointer text-ink">
								<path stroke-linecap="round" stroke-linejoin="round" d="m19.5 8.25-7.5 7.5-7.5-7.5"></path>
							</svg>
						</div>
					</summary>
					<div class="dropdown-content">
						<dl>
							<dt>
								<code>query_then_fetch</code>
							</dt>
							<dd>(Default) Distributed term frequencies are calculated locally for each shard running the search. We recommend this option for faster searches with potentially less accurate scoring.</dd>
						</dl>
					</div>
				</details>
				<p>
					<a id="dfs-query-then-fetch"></a>
				</p>
				<dl>
					<dt>
						<code>dfs_query_then_fetch</code>
					</dt>
					<dd>Distributed term frequencies are calculated globally, using information gathered from all shards running the search.</dd>
				</dl>
			</dd>
		</dl>
		"""
		);

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();

	[Test, DisplayName("minimal parse sees two inline anchors")]
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

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() =>
		await Docs.ConvertsToHtml(
			"""
		<p>
			<a id="search-type"></a>
		</p>
		<p>indented codeblock</p>
		<p>
			<a id="dfs-query-then-fetch"></a>
		</p>
		<p>block</p>
		"""
		);

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();

	[Test, DisplayName("minimal parse sees two inline anchors")]
	public async Task MinimalParseSeesInlineAnchors()
	{
		var anchors = await Docs.Converts("index.md").ParsesMinimal<InlineAnchor>();
		anchors.Should().HaveCount(2);
	}
}
