// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Exporters;

namespace Elastic.Authoring.Tests.PlainText;

public class OutboundLinksOnAPage : GeneratorTest
{
	protected override IReadOnlyCollection<TestFile> Files =>
		[
			Index(
				"""
				See [Elastic](https://www.elastic.co/docs) and <https://example.com/auto>.
				Again [Elastic](https://www.elastic.co/docs), a [section](#below), and ![logo](logo.png).

				% [internal note](https://example.com/note)

				## Below
				"""
			),
			Static("logo.png"),
		];

	[Test]
	public async Task Extract_PageWithLinks_ReturnsDistinctTargetsWithoutAnchorsImagesOrComments()
	{
		var result = await Docs.Converts("index.md").Inner;

		OutboundLinkExtractor.Extract(result.Document).Should().Equal("https://www.elastic.co/docs", "https://example.com/auto");
	}
}

public class OutboundLinksFromAnIncludedSnippet : GeneratorTest
{
	protected override IReadOnlyCollection<TestFile> Files =>
		[
			Index("""
				:::{include} _snippets/links.md
				:::
				"""),
			Snippet("_snippets/links.md", "Read the [guide](https://example.com/guide)."),
		];

	[Test]
	public async Task Extract_LinksOnlyInSnippet_ReturnsSnippetLinks()
	{
		var result = await Docs.Converts("index.md").Inner;

		OutboundLinkExtractor.Extract(result.Document).Should().Equal("https://example.com/guide");
	}
}

public class OutboundLinksFromCardDirectives : GeneratorTest
{
	protected override IReadOnlyCollection<TestFile> Files =>
		[
			Index(
				"""
				:::{page-card} [Other](other.md)
				Another page.
				:::

				:::{link-card}
				title: Writing content
				link: /index.md
				links:
				  - label: Other
				    url: /other.md
				:::
				"""
			),
			Page("other.md", "# Other"),
		];

	[Test]
	public async Task Extract_LinksOnlyInCards_ReturnsCardTargets()
	{
		var result = await Docs.Converts("index.md").Inner;

		OutboundLinkExtractor.Extract(result.Document).Should().Equal("/other", "/index.md", "/other.md");
	}
}

public class NoLinksOnAPage : GeneratorTest
{
	protected override IReadOnlyCollection<TestFile> Files => [Index("Plain text with no links.")];

	[Test]
	public async Task Extract_PageWithoutLinks_ReturnsNull()
	{
		var result = await Docs.Converts("index.md").Inner;

		OutboundLinkExtractor.Extract(result.Document).Should().BeNull();
	}
}
