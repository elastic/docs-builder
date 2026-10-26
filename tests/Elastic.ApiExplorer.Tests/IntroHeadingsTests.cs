// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.ApiExplorer.Landing;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst;

namespace Elastic.ApiExplorer.Tests;

public partial class IntroHeadingsTests
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
	[Arguments("When to use this API")]
	[Arguments("ES|QL visualizations")]
	[Arguments("Panel type `availability`")]
	[Arguments("Café & résumé: v1.2_beta")]
	public void Parse_Slug_MatchesSanitizedHeadingId(string title)
	{
		var markdown = $"# Page title\n\nIntro.\n\n## {title}\n\nBody.";

		// The same parser, h1 drop and sanitizer as a group intro page, so a lost or renamed id fails here.
		var parsed = Markdig.Markdown.Parse(markdown, MarkdownParser.ApiDescriptionPipeline);
		var slugs = IntroHeadings.Parse(markdown).Select(h => h.Slug).ToArray();
		var html = ApiMarkdown.SanitizeHtml(
			MarkdownFile.CreateHtml(parsed, stripFirstHeadingLevel1: true, MarkdownParser.ApiDescriptionPipeline),
			slugs.ToHashSet()
		);
		var renderedIds = HeadingWrapperId().Matches(html).Select(m => m.Groups[1].Value);

		// The sanitizer drops an id the sidebar does not list, so a slug mismatch leaves the link without a target.
		renderedIds.Should().Equal(slugs);
	}

	[GeneratedRegex("<div class=\"heading-wrapper\" id=\"([^\"]+)\">")]
	private static partial Regex HeadingWrapperId();
}
