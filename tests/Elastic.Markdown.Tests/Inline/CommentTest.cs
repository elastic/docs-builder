// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using AwesomeAssertions;

namespace Elastic.Markdown.Tests.Inline;

public class CommentTest() : InlineTest("""
% comment
not a comment
""")
{
	[Test]
	public void GeneratesAttributesInHtml()
	{
		// language=html
		Html.Should().NotContain("""<p>% comment""").And.Contain("""<p>not a comment</p>""");
		Html.ShouldBeHtml("""
			<p>not a comment</p>
			""");
	}
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
	public void GeneratesAttributesInHtml()
	{
		// language=html
		Html
			.Should()
			.NotContainAny(
				"<p><!--",
				"<p>Multi line comment, first line",
				"<p>Another line inside the commented area",
				"<p>end of comments",
				"<p>-->"
			)
			.And
			.ContainAll("<p>not a comment, and multi line comment below</p>", "<p>also not a comment</p>");
		Html.ShouldBeHtml(
			"""
			<p>not a comment, and multi line comment below</p>
			<p>also not a comment</p>
			"""
		);
	}
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
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);

	[Test]
	public void GeneratesAttributesInHtml()
	{
		// language=html
		Html
			.ReplaceLineEndings()
			.Should()
			.NotContainAny(
				"<p><!--",
				"<p>Multi line comment, first line",
				"regular link",
				"global search field",
				"<p>end of comments",
				"<p>-->"
			)
			.And
			.ContainAll("<p>not a comment, and multi line comment below</p>", "<p>also not a comment</p>");
		Html.ShouldBeHtml(
			"""
			<p>not a comment, and multi line comment below</p>
			<p>also not a comment</p>
			"""
		);
	}
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
	public void ContentAfterCommentShouldBeRendered()
	{
		// This test verifies GitHub issue #2456 is fixed.
		// The "content after comment" should be rendered, not silently dropped.
		Html.Should().Contain("<p>content after comment</p>");
	}

	[Test]
	public void ContentBeforeCommentShouldBeRendered() => Html.Should().Contain("<p>content before comment</p>");

	[Test]
	public void CommentContentShouldNotBeRendered() => Html.Should().NotContain("TODO: Uncomment once page is live.");
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
	public void ContentBeforeAndAfterShouldBeRendered() =>
		Html.Should().Contain("<p>content before</p>").And.Contain("<p>content after</p>");

	[Test]
	public void CommentContentShouldNotBeRendered() => Html.Should().NotContain("single line comment");
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
	public void ContentBeforeAndAfterShouldBeRendered() =>
		Html.Should().Contain("<p>content before</p>").And.Contain("<p>content after</p>");

	[Test]
	public void CommentContentShouldNotBeRendered() =>
		Html.Should().NotContain("start of comment").And.NotContain("middle of comment").And.NotContain("end of comment");
}
