// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;

namespace Elastic.Markdown.Tests.Inline;

public class MathRoleTests(ITestOutputHelper output) : InlineTest(output,
	"""
	Einstein's famous equation {math}`E = mc^2` relates energy and mass.
	"""
)
{
	[Fact]
	public void Render() =>
		Html.Should().Contain("<span class=\"math\">E = mc^2</span>")
			.And.NotContain("{math}`E = mc^2`");
}

public class MathRoleEscapesContentTests(ITestOutputHelper output) : InlineTest(output,
	"""
	Compare {math}`a < b & b > c` for ordering.
	"""
)
{
	[Fact]
	public void Render() =>
		Html.Should().Contain("<span class=\"math\">a &lt; b &amp; b &gt; c</span>");
}
