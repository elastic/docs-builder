// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.Myst.InlineParsers;
using FluentAssertions;
using JetBrains.Annotations;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Xunit.Abstractions;

namespace Elastic.Markdown.Tests.Inline;

public class InlineAnchorTests(ITestOutputHelper output) : LeafTest<InlineAnchor>(output,
	"""
	this is regular text and this $$$is-an-inline-anchor$$$ and this continues to be regular text
	"""
)
{
	[Fact]
	public void ParsesBlock()
	{
		Block.Should().NotBeNull();
		Block!.Anchor.Should().Be("is-an-inline-anchor");
	}

	[Fact]
	public void GeneratesAttributesInHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p>this is regular text and this <a id="is-an-inline-anchor"></a> and this continues to be regular text</p>"""
		);
}

public class InlineAnchorAtStartTests(ITestOutputHelper output) : LeafTest<InlineAnchor>(output,
	"""
	$$$is-an-inline-anchor$$$ and this continues to be regular text
	"""
)
{
	[Fact]
	public void ParsesBlock()
	{
		Block.Should().NotBeNull();
		Block!.Anchor.Should().Be("is-an-inline-anchor");
	}

	[Fact]
	public void GeneratesAttributesInHtml() =>
		// language=html
		Html.Should().Be(
			"""<p><a id="is-an-inline-anchor"></a> and this continues to be regular text</p>"""
		);
}

public class InlineAnchorAtEndTests(ITestOutputHelper output) : LeafTest<InlineAnchor>(output,
	"""
	this is regular text and this $$$is-an-inline-anchor$$$
	"""
)
{
	[Fact]
	public void ParsesBlock()
	{
		Block.Should().NotBeNull();
		Block!.Anchor.Should().Be("is-an-inline-anchor");
	}

	[Fact]
	public void GeneratesAttributesInHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p>this is regular text and this <a id="is-an-inline-anchor"></a></p>"""
		);
}

public class BadStartInlineAnchorTests(ITestOutputHelper output) : BlockTest<ParagraphBlock>(output,
	"""
	this is regular text and this $$is-an-inline-anchor$$$
	"""
)
{
	[Fact]
	public void GeneratesAttributesInHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p>this is regular text and this $$is-an-inline-anchor$$$</p>"""
		);
}

public class BadEndInlineAnchorTests(ITestOutputHelper output) : BlockTest<ParagraphBlock>(output,
	"""
	this is regular text and this $$$is-an-inline-anchor$$
	"""
)
{
	[Fact]
	public void GeneratesAttributesInHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p>this is regular text and this $$$is-an-inline-anchor$$</p>"""
		);
}

public class InlineAnchorInHeading(ITestOutputHelper output) : BlockTest<HeadingBlock>(output,
	"""
	## Hello world $$$my-anchor$$$
	"""
)
{
	[Fact]
	public void GeneratesAttributesInHtml() =>
		// language=html
		Html.Should().Be(
			"""
			<section id="hello-world"><h2>Hello world <a id="my-anchor"></a><a class="headerlink" href="#hello-world" title="Link to this heading">¶</a>
			</h2>
			</section>
			""".TrimEnd()
		);
}

public class ExplicitSlugInHeader(ITestOutputHelper output) : BlockTest<HeadingBlock>(output,
	"""
	## Hello world [#my-anchor]
	"""
)
{
	[Fact]
	public void GeneratesAttributesInHtml() =>
		// language=html
		Html.Should().Be(
			"""
			<section id="my-anchor"><h2>Hello world <a class="headerlink" href="#my-anchor" title="Link to this heading">¶</a>
			</h2>
			</section>
			""".TrimEnd()
		);
}


public abstract class InlineAnchorLinkTestBase(ITestOutputHelper output, [LanguageInjection("markdown")] string content)
	: InlineTest<LinkInline>(output,
$"""
## Hello world

A paragraph

{content}

$$$same-page-anchor$$$

""")
{
	protected override void AddToFileSystem(MockFileSystem fileSystem)
	{
		// language=markdown
		var inclusion =
"""
# Special Requirements

## Sub Requirements

To follow this tutorial you will need to install the following components:

## New Requirements [#new-reqs]

These are new requirements

With a custom anchor that exists temporarily. $$$custom-anchor$$$
""";
		fileSystem.AddFile(@"docs/testing/req.md", inclusion);
		fileSystem.AddFile(@"docs/_static/img/observability.png", new MockFileData(""));
	}

}

public class InlineAnchorCanBeLinkedToo(ITestOutputHelper output) : InlineAnchorLinkTestBase(output,
"""
[Hello](#same-page-anchor)
"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p><a href="#same-page-anchor">Hello</a></p>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class ExternalPageInlineAnchorCanBeLinkedToo(ITestOutputHelper output) : InlineAnchorLinkTestBase(output,
"""
[Sub Requirements](testing/req.md#custom-anchor)
"""
)
{
	[Fact]
	public void GeneratesHtml() =>
		// language=html
		Html.Should().Contain(
			"""<p><a href="testing/req.html#custom-anchor">Sub Requirements</a></p>"""
		);

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}
