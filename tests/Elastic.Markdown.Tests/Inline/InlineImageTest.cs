// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using FluentAssertions;
using Markdig.Syntax.Inlines;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.Inline;

public class InlineImageTest(ITestOutputHelper output) : InlineTest<LinkInline>(output,
"""
![Elasticsearch](/_static/img/observability.png){w=350px align=center}
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void GeneratesAttributesInHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p><img src="/_static/img/observability.png" w="350px" align="center" alt="Elasticsearch" /></p>"""
		);
}
