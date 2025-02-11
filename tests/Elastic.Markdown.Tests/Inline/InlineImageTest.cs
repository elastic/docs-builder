// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using FluentAssertions;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Tests.Inline;

public class InlineImageTest(ITestOutputHelper output) : InlineTest<LinkInline>(output,
"""
![Elasticsearch](/_static/img/observability.png)
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void GeneratesAttributesInHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p><img src="/docs/_static/img/observability.png" alt="Elasticsearch" /></p>"""
		);
}

public class RelativeInlineImageTest(ITestOutputHelper output) : InlineTest<LinkInline>(output,
"""
![Elasticsearch](_static/img/observability.png)
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void GeneratesAttributesInHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p><img src="/docs/_static/img/observability.png" alt="Elasticsearch" /></p>"""
		);
}
