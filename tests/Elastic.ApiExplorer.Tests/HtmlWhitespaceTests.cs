// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;

namespace Elastic.ApiExplorer.Tests;

public class HtmlWhitespaceTests
{
	[Test]
	public void Collapse_IndentationBetweenTags_BecomesOneLineBreak() =>
		HtmlWhitespace
			.Collapse("<dl>\n\t\t<dt>\n\t\t\t<code>a</code> <span>or</span>\n\t\t</dt>\n</dl>\n")
			.Should()
			.Be("<dl>\n<dt>\n<code>a</code> <span>or</span>\n</dt>\n</dl>\n");

	[Test]
	public void Collapse_PreformattedAndRawText_StayAsTheyAre()
	{
		const string kept =
			"<pre><code>{\n    \"a\": 1\n}</code></pre><textarea>x\n\n  y</textarea><script>\n\tlet a = `\n\t`\n</script><style>\n\ta {}\n</style><!--\n\tnote\n-->";
		HtmlWhitespace.Collapse(kept).Should().Be(kept);
	}

	[Test]
	public void Collapse_AttributeValues_StayAsTheyAre()
	{
		const string kept = "<span data-tippy-content=\"line one\n\n    line two > three\" title='a\n  b'>x</span>";
		HtmlWhitespace.Collapse(kept).Should().Be(kept);
	}
}
