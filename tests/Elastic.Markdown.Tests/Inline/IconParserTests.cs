// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using FluentAssertions;

namespace Elastic.Markdown.Tests.Inline;

public class IconParserTests(ITestOutputHelper output) : InlineTest(output,
	"""
	A check mark :check:. A cross :cross:. A warning :warning:.

	An unknown icon :not_a_real_icon: should not be replaced.
	This should not be an icon either/this::apm_trace:is:not:an:icon.
	Nor should this be an icon :invalid-icon:.
	An empty one is not an icon either ::.
	"""
)
{
	[Fact]
	public void ReplacesKnownIconsAndIgnoresInvalid() =>
		Html.Should().Contain("<span aria-label=\"Icon for check\" class=\"icon icon-check\">")
			.And.Contain("<span aria-label=\"Icon for cross\" class=\"icon icon-cross\">")
			.And.Contain("<span aria-label=\"Icon for warning\" class=\"icon icon-warning\">")
			.And.NotContain(":check:")
			.And.NotContain(":cross:")
			.And.NotContain(":warning:")
			.And.Contain(":apm_trace:")
			.And.Contain(":not_a_real_icon:")
			.And.Contain("/this::apm_trace:is:not:an:icon")
			.And.Contain(":invalid-icon:")
			.And.Contain("::");
}

public class IconInListItemTest(ITestOutputHelper output) : InlineTest(output,
	"""
	- :check: A check mark.
	"""
)
{
	[Fact]
	public void ReplacesKnownIconsAndIgnoresInvalid() =>
		Html.Should()
			.Contain("<span aria-label=\"Icon for check\" class=\"icon icon-check\">")
			.And.NotContain(":check:")
			.And.NotContain("<li></li>");
}
