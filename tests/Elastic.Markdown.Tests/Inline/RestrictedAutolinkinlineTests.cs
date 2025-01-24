// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using FluentAssertions;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.Inline;

public class AllowBrTagTest(ITestOutputHelper output)
	: InlineTest(output,
		"Hello,<br>World!")
{
	[Fact]
	public void GeneratesHtml() =>
		Html.Should().Contain(
			"<p>Hello,<br>World!</p>"
		);
}

public class BrTagNeedsToBeExact(ITestOutputHelper output)
	: InlineTest(output,
		"Hello,<br >World<br />!")
{
	[Fact]
	public void GeneratesHtml() =>
		Html.Should().Contain(
			"<p>Hello,&lt;br &gt;World&lt;br /&gt;!</p>"
		);
}

public class DisallowSpanTag(ITestOutputHelper output)
	: InlineTest(output,
		"Hello,<span>World!</span>")
{
	[Fact]
	// span tag is rendered as text
	public void GeneratesHtml() =>
		Html.Should().Contain(
			"<p>Hello,&lt;span&gt;World!&lt;/span&gt;</p>"
		);
}
