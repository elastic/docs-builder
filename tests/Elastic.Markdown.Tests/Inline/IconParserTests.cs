// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using FluentAssertions;

namespace Elastic.Markdown.Tests.Inline;

public class IconParserTests(ITestOutputHelper output) : InlineTest(output,
	"""
	A check mark i:check:. A cross i:cross:. A warning i:warning:.

	An unknown icon i:not_a_real_icon: should not be replaced.
	This should not be an icon either/this:i:apm_trace:is:not:an:icon.
	Nor should this be an icon i:invalid-icon:.
	An empty one is not an icon either i::.
	"""
)
{
	[Fact]
	public void ReplacesKnownIconsAndIgnoresInvalid() =>
		Html.Should().Contain("<span class=\"icon icon-check\">")
			.And.Contain("<span class=\"icon icon-cross\">")
			.And.Contain("<span class=\"icon icon-warning\">")
			.And.NotContain("i:check:")
			.And.NotContain("i:cross:")
			.And.NotContain("i:warning:")
			.And.Contain("i:apm_trace:")
			.And.Contain("i:not_a_real_icon:")
			.And.Contain("/this:i:apm_trace:is:not:an:icon")
			.And.Contain("i:invalid-icon:")
			.And.Contain("i::");
}
