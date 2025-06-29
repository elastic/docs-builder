// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using FluentAssertions;

namespace Elastic.Markdown.Tests.Inline;

public class IconParserTests(ITestOutputHelper output) : InlineTest(output,
	"""
	A check mark {icon}`check`. A cross {icon}`cross`. A warning {icon}`warning`.

	An unknown icon {icon}`not_a_real_icon` should not be replaced.
	This should not be an icon either/this:apm_trace:is:not:an:icon.
	Nor should this be an icon :invalid-icon:.
	An empty one is not an icon either ::.
	"""
)
{
	[Fact]
	public void Render() =>
		Html.Should().Contain("<span aria-label=\"Icon for check\" class=\"icon icon-check\">")
			.And.Contain("<span aria-label=\"Icon for cross\" class=\"icon icon-cross\">")
			.And.Contain("<span aria-label=\"Icon for warning\" class=\"icon icon-warning\">")
			.And.NotContain("{icon}`check`")
			.And.NotContain("{icon}`cross`")
			.And.NotContain("{icon}`warning`")
			.And.Contain("/this:apm_trace:is:not:an:icon")
			.And.Contain(":invalid-icon:")
			.And.Contain("::");
}

public class IconInListItemTest(ITestOutputHelper output) : InlineTest(output,
	"""
	- {icon}`check` A check mark.
	"""
)
{
	[Fact]
	public void Render() =>
		Html.Should()
			.Contain("<span aria-label=\"Icon for check\" class=\"icon icon-check\">")
			.And.NotContain("{icon}`check`")
			.And.NotContain("<li></li>");
}

public class IconInHeadingShouldBeRemovedFromAnchor(ITestOutputHelper output) : InlineTest(output,
	"""
	## Users {icon}`check`
	"""
)
{
	[Fact]
	public void Render() =>
		Html.Should()
			.Contain("<div class=\"heading-wrapper\" id=\"users\"><h2><a class=\"headerlink\" href=\"#users\">Users <span aria-label=\"Icon for check\" class=\"icon icon-check\"><svg xmlns=\"http://www.w3.org/2000/svg\" width=\"16\" height=\"16\" viewBox=\"0 0 16 16\">\n  <path fill-rule=\"evenodd\" d=\"M15.354 4.354 6.5 13.207 1.646 8.354l.708-.708L6.5 11.793l8.146-8.147.708.708Z\" clip-rule=\"evenodd\"/>\n</svg>\n</span></a></h2>\n</div>");
}
