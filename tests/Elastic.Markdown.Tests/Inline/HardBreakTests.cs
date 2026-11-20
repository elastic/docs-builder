// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Testing;
using Nullean.Snapshots.Html;

namespace Elastic.Markdown.Tests.Inline;

public class AllowBrTagTest() : InlineTest("Hello,<br>World!")
{
	[Test]
	public void GeneratesHtml() => Html.Should().MatchInlineHtmlSnapshot("<p>Hello,<br>World!</p>", DocsHtml.Body());
}

public class BrTagNeedsToBeExact() : InlineTest("Hello,<br >World<br />!")
{
	[Test]
	public void GeneratesHtml() => Html.Should().MatchInlineHtmlSnapshot("<p>Hello,&lt;br &gt;World&lt;br /&gt;!</p>", DocsHtml.Body());
}

public class DisallowSpanTag() : InlineTest("Hello,<span>World!</span>")
{
	[Test]
	// span tag is rendered as text
	public void GeneratesHtml() => Html.Should().MatchInlineHtmlSnapshot("<p>Hello,&lt;span&gt;World!&lt;/span&gt;</p>", DocsHtml.Body());
}
