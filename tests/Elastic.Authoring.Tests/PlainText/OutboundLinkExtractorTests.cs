// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Exporters;
using Markdig;

namespace Elastic.Authoring.Tests.PlainText;

public class OutboundLinkExtractorTests
{
	private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().Build();

	[Test]
	public void Extract_PageWithLinks_ReturnsDistinctTargetsWithoutAnchorsOrImages()
	{
		const string markdown =
			"""
			See [Elastic](https://www.elastic.co/docs) and <https://example.com/auto>.
			Again [Elastic](https://www.elastic.co/docs), a [section](#below), and ![logo](logo.png).

			## Below
			""";

		var links = OutboundLinkExtractor.Extract(Markdig.Markdown.Parse(markdown, Pipeline));

		links.Should().Equal("https://www.elastic.co/docs", "https://example.com/auto");
	}

	[Test]
	public void Extract_PageWithoutLinks_ReturnsNull() =>
		OutboundLinkExtractor.Extract(Markdig.Markdown.Parse("Plain text with no links.", Pipeline)).Should().BeNull();
}
