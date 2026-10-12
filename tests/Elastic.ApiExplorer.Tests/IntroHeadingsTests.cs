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
}
