// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Nullean.Snapshots;

namespace Elastic.Markdown.Tests.Inline;

// The API Authentication page renders its sections as API description Markdown and links to them by Bump.sh anchors.
public class ApiDescriptionHeadingTests() : InlineTest("""
Placeholder.
""")
{
	private const string Sections = """
		## Api key [doc-authentication-api-key-auth]

		Send the encoded key.
		""";

	[Test]
	public void RenderApiDescription_HeadingWithAnchor_RendersALinkableDocsHeading()
	{
		var writer = new HtmlWriter(Set, FileSystem, new DescriptionGenerator());

		writer
			.RenderApiDescription(Sections, null)
			.Should()
			.MatchInlineSnapshot(
				"""
			<div class="heading-wrapper" id="doc-authentication-api-key-auth"><h2><a class="headerlink" href="#doc-authentication-api-key-auth">Api key</a></h2>
			</div>
			<p>Send the encoded key.</p>
			"""
			);
	}

	[Test]
	public void ReadApiDescriptionHeadings_HeadingWithAnchor_UsesTheAnchorAsSlug()
	{
		var writer = new HtmlWriter(Set, FileSystem, new DescriptionGenerator());

		var heading = writer.ReadApiDescriptionHeadings(Sections, null).Single();

		$"{heading.Level} {heading.Heading} #{heading.Slug}".Should().Be("2 Api key #doc-authentication-api-key-auth");
	}

	[Test]
	public void ReadApiDescriptionHeadings_ReferenceLinkAtTheEnd_ReadsItsLabelAsTheAnchor()
	{
		var writer = new HtmlWriter(Set, FileSystem, new DescriptionGenerator());

		var heading = writer.ReadApiDescriptionHeadings("## Configure [API keys][keys]\n", null).Single();

		// The API .md output (ApiCommonMark.StripHeadingAnchors) drops the same text, so it matches this heading.
		$"{heading.Heading} #{heading.Slug}".Should().Be("Configure [API keys] #keys");
	}
}
