// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.Markdown.Myst;

namespace Elastic.ApiExplorer.Tests;

public class ApiMarkdownSplitTests
{
	private static string Html(string markdown) => Markdig.Markdown.ToHtml(markdown, MarkdownParser.ApiDescriptionPipeline);

	[Test]
	public void SplitAtFirstHeading_CutsBeforeTheFirstHeading()
	{
		var split = ApiMarkdown.SplitAtFirstHeading(Html("Intro.\n\n## Source\n\nBody.\n"));

		split.Lead.Should().Contain("Intro.").And.NotContain("Source");
		split.Remainder.Should().StartWith("<h2").And.Contain("Body.");
	}

	[Test]
	public void SplitAtFirstHeading_WithoutHeadings_KeepsEverythingInTheLead()
	{
		var split = ApiMarkdown.SplitAtFirstHeading(Html("Just an intro."));

		split.Lead.Should().Contain("Just an intro.");
		split.Remainder.Should().BeNull();
	}

	[Test]
	public void SplitAtFirstHeading_KeepsReferenceLinksDefinedBelowTheHeading()
	{
		var split = ApiMarkdown.SplitAtFirstHeading(Html("See the [guide][g].\n\n## Source\n\n[g]: https://example.com/guide\n"));

		split.Lead.Should().Contain("<a href=\"https://example.com/guide\"");
		split.Lead.Should().NotContain("[g]");
	}

	[Test]
	public void SplitAtFirstHeading_KeepsLevelOneSectionHeadingsInTheRemainder()
	{
		var split = ApiMarkdown.SplitAtFirstHeading(Html("Intro.\n\n# Authentication\n\nBody.\n"));

		split.Remainder.Should().StartWith("<h1").And.Contain("Authentication");
	}
}
