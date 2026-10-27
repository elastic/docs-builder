// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;

namespace Elastic.Authoring.Tests.Directives;

public class IncludeHoistsAnchorsAndTableOfContents : GeneratorTest
{
	protected override IReadOnlyCollection<TestFile> Files =>
		[
			Index(
				"""
			# A Document that lives at the root

			:::{include} _snippets/my-snippet.md
			:::
			"""
			),
			Snippet("_snippets/my-snippet.md", """
			## header from snippet [aa]
			"""),
			Page(
				"test-links.md",
				"""
			# parent.md

			## some header
			[link to root with included anchor](index.md#aa)
			"""
			),
		];

	[Test, DisplayName("validate index.md HTML includes snippet")]
	public async Task ValidateIndexHtml() =>
		await Docs.Converts("index.md").ToHtml(
			"""
			<h1>A Document that lives at the root</h1>
			<div class="heading-wrapper" id="aa">
			    <h2><a class="headerlink" href="#aa">header from snippet</a></h2>
			</div>
			"""
		);

	[Test, DisplayName("validate test-links.md HTML includes snippet")]
	public async Task ValidateTestLinksHtml() =>
		await Docs.Converts("test-links.md").ToHtml(
			"""
			<h1>parent.md</h1>
			<div class="heading-wrapper" id="some-header">
			    <h2><a class="headerlink" href="#some-header">some header</a></h2>
			</div>
			<p><a href="/#aa">link to root with included anchor</a></p>
			"""
		);

	[Test, DisplayName("validate index.md includes table of contents")]
	public async Task ValidateTableOfContents()
	{
		var page = await Docs.Converts("index.md").MarkdownFile();
		page.PageTableOfContent.Should().HaveCount(1);
		page.PageTableOfContent.Should().ContainKey("aa");
	}

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class IncludeCanContainLinksToParentPagesIncludes : GeneratorTest
{
	protected override IReadOnlyCollection<TestFile> Files =>
		[
			Index(
				"""
			# A Document that lives at the root

			:::{include} _snippets/my-snippet.md
			:::

			:::{include} _snippets/my-other-snippet.md
			:::

			Some more content after includes
			"""
			),
			Snippet("_snippets/my-snippet.md", """
			## header from snippet [aa]
			"""),
			Snippet("_snippets/my-other-snippet.md", """
			[link to root with included anchor](../index.md#aa)
			"""),
		];

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}
