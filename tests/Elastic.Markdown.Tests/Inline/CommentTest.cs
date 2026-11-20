// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using AwesomeAssertions;
using Elastic.Documentation.Testing;
using Nullean.Snapshots;
using Nullean.Snapshots.Html;

namespace Elastic.Markdown.Tests.Inline;

public class CommentTest() : InlineTest("""
% comment
not a comment
""")
{
	[Test]
	public void GeneratesAttributesInHtml() =>
		Html.Should().MatchInlineHtmlSnapshot("""
			<p>not a comment</p>
			""", DocsHtml.Body());
}

public class MultipleLineCommentTest() : InlineTest(
	"""
	not a comment, and multi line comment below
	<!--
	multi line comment
	Another line inside the commented area
	end of comments
	-->

	also not a comment
	"""
)
{
	[Test]
	public void GeneratesAttributesInHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
			<p>not a comment, and multi line comment below</p>
			<p>also not a comment</p>
			""",
			DocsHtml.Body()
		);
}

public class MultipleLineCommentWithLinkTest() : InlineTest(
	"""
	not a comment, and multi line comment below
	<!--
	multi line comment
	[regular link](http://elastic.co/non-existing-link)
	[global search field]({{this-variable-does-not-exist}}/introduction.html)
	end of comments
	-->

	also not a comment
	"""
)
{
	[Test]
	public void HasNoErrors() => Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");

	[Test]
	public void GeneratesAttributesInHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
			<p>not a comment, and multi line comment below</p>
			<p>also not a comment</p>
			""",
			DocsHtml.Body()
		);
}

/// <summary>
/// Tests for GitHub issue #2456: Silent build errors on malformed multiline comments.
/// When closing --> is on the same line as other content, the comment should still close properly.
/// </summary>
public class CommentWithClosingTagAtEndOfLineTest() : InlineTest(
	"""
	content before comment

	<!-- :::{note}
	TODO: Uncomment once page is live.
	The chat UI is available in both standalone and flyout modes.
	::: -->

	content after comment
	"""
)
{
	[Test]
	// This test verifies GitHub issue #2456 is fixed.
	// The "content after comment" should be rendered, not silently dropped.
	public void GeneratesHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>content before comment</p>
		<p>content after comment</p>
		""",
			DocsHtml.Body()
		);
}

/// <summary>
/// Tests single-line HTML comments like <!-- comment -->
/// </summary>
public class SingleLineCommentTest() : InlineTest("""
	content before

	<!-- This is a single line comment -->

	content after
	""")
{
	[Test]
	public void GeneratesHtml() =>
		Html.Should().MatchInlineHtmlSnapshot("""
		<p>content before</p>
		<p>content after</p>
		""", DocsHtml.Body());
}

/// <summary>
/// Tests comment with opening and content on same line, closing on different line
/// </summary>
public class CommentWithOpeningContentOnSameLineTest() : InlineTest(
	"""
	content before

	<!-- start of comment
	middle of comment
	end of comment -->

	content after
	"""
)
{
	[Test]
	public void GeneratesHtml() =>
		Html.Should().MatchInlineHtmlSnapshot("""
		<p>content before</p>
		<p>content after</p>
		""", DocsHtml.Body());
}
