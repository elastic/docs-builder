// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Tests.Inline;

public class AllowBrTagTest() : InlineTest("Hello,<br>World!")
{
	[Test]
	public void GeneratesHtml() => Html.ShouldContainHtml("<p>Hello,<br>World!</p>");
}

public class BrTagNeedsToBeExact() : InlineTest("Hello,<br >World<br />!")
{
	[Test]
	public void GeneratesHtml() => Html.ShouldContainHtml("<p>Hello,&lt;br &gt;World&lt;br /&gt;!</p>");
}

public class DisallowSpanTag() : InlineTest("Hello,<span>World!</span>")
{
	[Test]
	// span tag is rendered as text
	public void GeneratesHtml() => Html.ShouldContainHtml("<p>Hello,&lt;span&gt;World!&lt;/span&gt;</p>");
}
