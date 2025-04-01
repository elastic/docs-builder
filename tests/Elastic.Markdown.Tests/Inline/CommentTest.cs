// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using FluentAssertions;

namespace Elastic.Markdown.Tests.Inline;

public class CommentTest(ITestOutputHelper output) : InlineTest(output,
"""
% comment
not a comment
"""
)
{

	[Fact]
	public void GeneratesAttributesInHtml() =>
		// language=html
		Html.Should().NotContain(
			"""<p>% comment"""
		)
		.And.Contain(
			"""<p>not a comment</p>"""
		).And.Be(
			"""
			<p>not a comment</p>
			"""
		);
}

public class MultipleLineCommentTest(ITestOutputHelper output) : InlineTest(output,
	"""
	not a comment, and multi line comment below
	<!---
	multi line comment
	Another line inside the commented area
	end of comments
	-->

	also not a comment
	"""
)
{

	[Fact]
	public void GeneratesAttributesInHtml() =>
		// language=html
		Html.ReplaceLineEndings().Should().NotContainAny(
				"<p><!---",
				"<p>Multi line comment, first line",
				"<p>Another line inside the commented area",
				"<p>end of comments",
				"<p>-->")
			.And.ContainAll(
				"<p>not a comment, and multi line comment below</p>",
				"<p>also not a comment</p>"
			).And.Be(
				"""
				<p>not a comment, and multi line comment below</p>
				<p>also not a comment</p>
				""".ReplaceLineEndings()
				);
}
