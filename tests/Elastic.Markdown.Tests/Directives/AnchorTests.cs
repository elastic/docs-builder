// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.Directives.Anchor;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

public class AnchorTests(ITestOutputHelper output) : DirectiveTest<AnchorBlock>(output,
$$"""
::::{anchor} custom-id

:::{dropdown} Dropdown Title
Dropdown content
:::

::::
""")
{
	[Fact]
	public void ParsesAnchorBlock() => Block.Should().NotBeNull();

	[Fact]
	public void SetsCorrectAnchorId() => Block!.AnchorId.Should().Be("custom-id");

	[Fact]
	public void SetsCrossReferenceName() => Block!.CrossReferenceName.Should().Be("custom-id");

	[Fact]
	public void GeneratesCorrectHtml() =>
		Html.ShouldContainHtml("""<div id="custom-id">""");

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AnchorWithoutArgumentTests(ITestOutputHelper output) : DirectiveTest<AnchorBlock>(output,
$$"""
::::{anchor}

Some content

::::
""")
{
	[Fact]
	public void ParsesAnchorBlock() => Block.Should().NotBeNull();

	[Fact]
	public void HasError() => Collector.Diagnostics.Should().HaveCount(1);

	[Fact]
	public void ErrorMessageIsCorrect() =>
		Collector.Diagnostics.First().Message.Should().Be("Anchor directive requires an ID argument");
}

public class AnchorSlugifyTests(ITestOutputHelper output) : DirectiveTest<AnchorBlock>(output,
$$"""
::::{anchor} Custom ID With Spaces

Some content

::::
""")
{
	[Fact]
	public void ParsesAnchorBlock() => Block.Should().NotBeNull();

	[Fact]
	public void SlugifiesAnchorId() => Block!.AnchorId.Should().Be("custom-id-with-spaces");

	[Fact]
	public void SetsCrossReferenceName() => Block!.CrossReferenceName.Should().Be("custom-id-with-spaces");

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}

public class AnchorSimpleContentTests(ITestOutputHelper output) : DirectiveTest<AnchorBlock>(output,
$$"""
::::{anchor} simple-anchor

# A heading

Some paragraph content.

::::
""")
{
	[Fact]
	public void ParsesAnchorBlock() => Block.Should().NotBeNull();

	[Fact]
	public void SetsCorrectAnchorId() => Block!.AnchorId.Should().Be("simple-anchor");

	[Fact]
	public void GeneratesCorrectHtml() =>
		Html.ShouldContainHtml("""<div id="simple-anchor">""");

	[Fact]
	public void ContainsWrappedContent() =>
		Html.ShouldContainHtml("""<h1><a class="headerlink" href="#a-heading">A heading</a></h1>""");

	[Fact]
	public void HasNoErrors() => Collector.Diagnostics.Should().HaveCount(0);
}
