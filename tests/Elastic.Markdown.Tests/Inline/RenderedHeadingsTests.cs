// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Markdown.IO;
using Elastic.Markdown.Myst;
using Elastic.Markdown.Myst.Renderers;

namespace Elastic.Markdown.Tests.Inline;

public class RenderedHeadingsTests() : InlineTest("")
{
	[Test]
	public void Read_FirstH1AndFencedHeadings_AreSkipped()
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

		var headings = ReadHeadings(markdown);

		headings.Select(h => (h.Heading, h.Level)).Should().Equal(("Real heading", 2), ("Another real heading", 3));
	}

	[Test]
	public void Read_ExplicitAnchor_UsesAnchorAsSlugAndDropsItFromTitle() =>
		ReadHeadings("## Get started [quickstart]").Should().Equal(new TocItem("Get started", "quickstart", 2));

	[Test]
	[Arguments("## When to use this API")]
	[Arguments("## ES|QL visualizations")]
	[Arguments("## Panel type `availability`")]
	[Arguments("## Café & résumé: v1.2_beta")]
	[Arguments("## Use **bold** and [links](https://example.com)")]
	[Arguments("## Get started [quickstart]")]
	[Arguments("~~~\n# Tilde fence\n~~~\n\n## After the fence")]
	public void Read_EveryHeading_MatchesARenderedHeadingId(string markdown)
	{
		var html = MarkdownFile.CreateHtml(Parse(markdown), stripFirstHeadingLevel1: true, MarkdownParser.ApiDescriptionPipeline);

		var headings = ReadHeadings(markdown);

		headings.Should().NotBeEmpty();
		foreach (var heading in headings)
			html.Should().Contain($"<div class=\"heading-wrapper\" id=\"{heading.Slug}\">");
		html.Split("class=\"heading-wrapper\"").Length.Should().Be(headings.Count + 1, "every rendered heading is listed");
	}

	private IReadOnlyList<TocItem> ReadHeadings(string markdown) => RenderedHeadings.Read(Parse(markdown), stripFirstHeadingLevel1: true);

	private Markdig.Syntax.MarkdownDocument Parse(string markdown) =>
		Set.MarkdownParser.ParseApiDescriptionString(markdown, FileSystem.FileInfo.New("docs/index.md"));
}
