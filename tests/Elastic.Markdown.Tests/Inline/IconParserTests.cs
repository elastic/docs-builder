// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;

namespace Elastic.Markdown.Tests.Inline;

public class IconParserTests() : InlineTest(
	"""
	A check mark {icon}`check`. A cross {icon}`cross`. A warning {icon}`warning`.

	An unknown icon {icon}`not_a_real_icon` should not be replaced.
	This should not be an icon either/this:apm_trace:is:not:an:icon.
	Nor should this be an icon :invalid-icon:.
	An empty one is not an icon either ::.
	"""
)
{
	[Test]
	public void Render() =>
		Html
			.Should()
			.Contain("<span aria-label=\"Icon for check\" class=\"icon icon-check\">")
			.And
			.Contain("<span aria-label=\"Icon for cross\" class=\"icon icon-cross\">")
			.And
			.Contain("<span aria-label=\"Icon for warning\" class=\"icon icon-warning\">")
			.And
			.NotContain("{icon}`check`")
			.And
			.NotContain("{icon}`cross`")
			.And
			.NotContain("{icon}`warning`")
			.And
			.Contain("/this:apm_trace:is:not:an:icon")
			.And
			.Contain(":invalid-icon:")
			.And
			.Contain("::");
}

public class IconInListItemTest() : InlineTest("""
	- {icon}`check` A check mark.
	""")
{
	[Test]
	public void Render() =>
		Html
			.Should()
			.Contain("<span aria-label=\"Icon for check\" class=\"icon icon-check\">")
			.And
			.NotContain("{icon}`check`")
			.And
			.NotContain("<li></li>");
}

public class IconInHeadingShouldBeRemovedFromAnchor() : InlineTest("""
	## Users {icon}`check`
	""")
{
	[Test]
	public void Render() =>
		Html
			.Should()
			.Contain("<a class=\"headerlink\" href=\"#users\">")
			.And
			.Contain("Users <span aria-label=\"Icon for check\"")
			.And
			.Contain("<svg ");
}
