// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using FluentAssertions;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Tests.Inline;

public class InlineImageTest(ITestOutputHelper output) : InlineTest<LinkInline>(output,
"""
![Elasticsearch](/_static/img/observability.png)
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void GeneratesAttributesInHtml() =>
		// language=html
		Html.ShouldContainHtml(
			"""<p><img src="/docs/_static/img/observability.png" alt="Elasticsearch" /></p>"""
		);
}

public class RelativeInlineImageTest(ITestOutputHelper output) : InlineTest<LinkInline>(output,
"""
![Elasticsearch](_static/img/observability.png)
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void GeneratesAttributesInHtml() =>
		// language=html
		Html.ShouldContainHtml(
			"""<p><img src="/docs/_static/img/observability.png" alt="Elasticsearch" /></p>"""
		);
}

// Test image sizing with space before =
public class InlineImageWithSizingSpaceBeforeTest(ITestOutputHelper output) : InlineTest<LinkInline>(output,
"""
![Elasticsearch](/_static/img/observability.png " =50%")
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void GeneratesAttributesInHtml() =>
		// language=html
		Html.ShouldContainHtml(
			"""<p><img src="/docs/_static/img/observability.png" alt="Elasticsearch" width="50%" height="50%" /></p>"""
		);
}

// Test image sizing without space before =
public class InlineImageWithSizingNoSpaceBeforeTest(ITestOutputHelper output) : InlineTest<LinkInline>(output,
"""
![Elasticsearch](/_static/img/observability.png "=50%")
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void GeneratesAttributesInHtml() =>
		// language=html
		Html.ShouldContainHtml(
			"""<p><img src="/docs/_static/img/observability.png" alt="Elasticsearch" width="50%" height="50%" /></p>"""
		);
}

// Test image sizing with pixels
public class InlineImageWithPixelSizingTest(ITestOutputHelper output) : InlineTest<LinkInline>(output,
"""
![Elasticsearch](/_static/img/observability.png "=250x330")
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void GeneratesAttributesInHtml() =>
		// language=html
		Html.ShouldContainHtml(
			"""<p><img src="/docs/_static/img/observability.png" alt="Elasticsearch" width="250px" height="330px" /></p>"""
		);
}

// Test image sizing with title and sizing
public class InlineImageWithTitleAndSizingTest(ITestOutputHelper output) : InlineTest<LinkInline>(output,
"""
![Elasticsearch](/_static/img/observability.png "My Title =50%")
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void GeneratesAttributesInHtml() =>
		// language=html
		Html.ShouldContainHtml(
			"""<p><img src="/docs/_static/img/observability.png" alt="Elasticsearch" title="My Title" width="50%" height="50%" /></p>"""
		);
}

// Test image sizing with width only
public class InlineImageWithWidthOnlyTest(ITestOutputHelper output) : InlineTest<LinkInline>(output,
"""
![Elasticsearch](/_static/img/observability.png "=250")
"""
)
{
	[Fact]
	public void ParsesBlock() => Block.Should().NotBeNull();

	[Fact]
	public void GeneratesAttributesInHtml() =>
		// language=html
		Html.ShouldContainHtml(
			"""<p><img src="/docs/_static/img/observability.png" alt="Elasticsearch" width="250px" height="250px" /></p>"""
		);
}
