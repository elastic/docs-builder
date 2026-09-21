// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using AwesomeAssertions;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Tests.Inline;

public class InlineImageTest() : InlineTest<LinkInline>("""
![Elasticsearch](/_static/img/observability.png)
""")
{
	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void GeneratesAttributesInHtml() =>
		// language=html
		Html.ShouldContainHtml("""<p><img src="/docs/_static/img/observability.png" alt="Elasticsearch" title="Elasticsearch" /></p>""");
}

public class RelativeInlineImageTest() : InlineTest<LinkInline>("""
![Elasticsearch](_static/img/observability.png)
""")
{
	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void GeneratesAttributesInHtml() =>
		// language=html
		Html.ShouldContainHtml("""<p><img src="/docs/_static/img/observability.png" alt="Elasticsearch" title="Elasticsearch" /></p>""");
}

// Test image sizing with space before =
public class InlineImageWithSizingSpaceBeforeTest() : InlineTest<LinkInline>(
	"""
![Elasticsearch](/_static/img/observability.png " =50%")
"""
)
{
	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void GeneratesAttributesInHtml() =>
		// language=html
		Html.ShouldContainHtml(
			"""<p><img src="/docs/_static/img/observability.png" alt="Elasticsearch" width="50%" height="50%" title="Elasticsearch" /></p>"""
		);
}

// Test image sizing without space before =
public class InlineImageWithSizingNoSpaceBeforeTest() : InlineTest<LinkInline>(
	"""
![Elasticsearch](/_static/img/observability.png "=50%")
"""
)
{
	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void GeneratesAttributesInHtml() =>
		// language=html
		Html.ShouldContainHtml(
			"""<p><img src="/docs/_static/img/observability.png" alt="Elasticsearch" width="50%" height="50%" title="Elasticsearch" /></p>"""
		);
}

// Test image sizing with pixels
public class InlineImageWithPixelSizingTest() : InlineTest<LinkInline>("""
![Elasticsearch](/_static/img/observability.png "=250x330")
""")
{
	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void GeneratesAttributesInHtml() =>
		// language=html
		Html.ShouldContainHtml(
			"""<p><img src="/docs/_static/img/observability.png" alt="Elasticsearch" width="250px" height="330px" title="Elasticsearch" /></p>"""
		);
}

// Test image sizing with title and sizing — explicit title in markdown is ignored; alt text is always used as title
public class InlineImageWithTitleAndSizingTest() : InlineTest<LinkInline>(
	"""
![Elasticsearch](/_static/img/observability.png "My Title =50%")
"""
)
{
	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void GeneratesAttributesInHtml() =>
		// language=html
		Html.ShouldContainHtml(
			"""<p><img src="/docs/_static/img/observability.png" alt="Elasticsearch" width="50%" height="50%" title="Elasticsearch" /></p>"""
		);
}

// Test image sizing with width only
public class InlineImageWithWidthOnlyTest() : InlineTest<LinkInline>("""
![Elasticsearch](/_static/img/observability.png "=250")
""")
{
	[Test]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Test]
	public void GeneratesAttributesInHtml() =>
		// language=html
		Html.ShouldContainHtml(
			"""<p><img src="/docs/_static/img/observability.png" alt="Elasticsearch" width="250px" height="250px" title="Elasticsearch" /></p>"""
		);
}
