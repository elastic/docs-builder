// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Xunit.Sdk;

namespace Elastic.Markdown.Tests;

public class PrettyHtmlExtensionsTests
{
	[Fact]
	public void ShouldContainHtml_WhenExpectedHtmlIsMissing_Throws()
	{
		var actual = """
		             <p>Rendered output</p>
		             """;

		var expected = """
		               <strong>Missing output</strong>
		               """;

		var act = () => actual.ShouldContainHtml(expected);

		act.Should().Throw<XunitException>();
	}
}
