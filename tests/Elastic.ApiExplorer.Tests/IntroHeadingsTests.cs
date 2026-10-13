// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Landing;

namespace Elastic.ApiExplorer.Tests;

public class IntroHeadingsTests
{
	[Test]
	public void Parse_SkipsHeadingsInsideFences_IncludingListAndQuoteNested()
	{
		const string markdown =
			"""
			# Dropped first h1

			## Real heading

			- item with an example:

			    ```md
			    ## Not a heading, the fence is indented in a list item
			    ```

			> ```
			> ## Not a heading, the fence is quoted
			> ```

			### Another real heading
			""";

		var headings = IntroHeadings.Parse(markdown);

		headings.Select(h => h.Title).Should().Equal("Real heading", "Another real heading");
	}

	[Test]
	public void SplitAtFirstHeading_SplitsBeforeTheFirstSectionHeading()
	{
		var (lead, remainder) = IntroHeadings.SplitAtFirstHeading("# Title\n\nIntro.\n\n## Source\n\nBody.\n");

		lead.Should().Contain("Intro.").And.NotContain("## Source");
		remainder.Should().StartWith("## Source");
	}

	[Test]
	public void SplitAtFirstHeading_IgnoresHeadingsInsideFences()
	{
		var (lead, remainder) = IntroHeadings.SplitAtFirstHeading("Intro.\n\n```md\n## Not a heading\n```\n\n## Real\n");

		lead.Should().Contain("## Not a heading");
		remainder.Should().StartWith("## Real");
	}

	[Test]
	public void SplitAtFirstHeading_WithoutHeadings_KeepsEverythingInTheLead()
	{
		var (lead, remainder) = IntroHeadings.SplitAtFirstHeading("Just an intro.");

		lead.Should().Be("Just an intro.");
		remainder.Should().BeNull();
	}
}
