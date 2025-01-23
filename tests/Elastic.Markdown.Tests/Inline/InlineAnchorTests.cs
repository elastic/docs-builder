// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.InlineParsers;
using FluentAssertions;
using Markdig.Syntax;
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
