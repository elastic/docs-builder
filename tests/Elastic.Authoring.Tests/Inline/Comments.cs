// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Authoring.Tests.Inline.Comments;

public class CommentedLine : MarkdownTest
{
	protected override string Markdown => """
		% comment
		not a comment
		""";

	[Test, DisplayName("validate HTML: commented line should not be emitted")]
	public async Task ValidateHtml() => await Docs.ConvertsToHtml("""<p>not a comment</p>""");
}
